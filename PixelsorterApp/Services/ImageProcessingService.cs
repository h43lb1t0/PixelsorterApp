using NumSharp;
using PixelsorterClassLib.Core;
using PixelsorterClassLib.Masks;
using SixLabors.ImageSharp.ColorSpaces;
using Image = PixelsorterClassLib.Core.Image;

namespace PixelsorterApp.Services;

/// <summary>
/// Provides image sorting, mask generation, and gallery save operations.
/// </summary>
/// <param name="serviceProvider">Service provider used to resolve platform-specific dependencies.</param>
public sealed class ImageProcessingService(IServiceProvider serviceProvider) : IImageProcessingService
{
    private readonly BackgroundMask backgroundMasker = new();
    private readonly CannyMask cannyMasker = new();
    private readonly LuminanceMask luminanceMasker = new();

    private string? cachedImagePath;
    private int cachedSubjectPadding = -1;
    private float cachedCannyThreshold = -1;
    private NDArray? subjectMask;
    private NDArray? invertedSubjectMask;
    private NDArray? cannyMask;
    private NDArray? invertedCannyMask;
    private NDArray? lumMask;
    private NDArray? invertedLumMask;
    private float cachedLumThreshold = -1;
    private MaskBuildCacheKey? cachedMaskBuildKey;
    private NDArray? cachedBuiltMask;

    private readonly record struct MaskBuildCacheKey(
        bool UseSubjectMask,
        bool UseCanny,
        bool UseSubtractMasks,
        bool UseInvertedSubjectMask,
        int SubjectMaskPadding,
        int CannyThresholdBucket,
        bool UseLumMask,
        bool UseInvertedLumMask,
        int LumThresholdBucket);

    /// <inheritdoc/>
    public bool IsBackgroundMaskReady => backgroundMasker.IsReadyToUse;

    /// <inheritdoc/>
    public Task DownloadBackgroundModelAsync()
    {
        return backgroundMasker.DownloadModel();
    }

    /// <inheritdoc/>
    public Task<(NDArray SubjectMask, NDArray InvertedSubjectMask)> CreateSubjectMaskAsync(string imagePath, int padding)
    {
        return backgroundMasker.GetMaskAsync(imagePath, new BackgroundMaskOptions(padding));
    }

    /// <inheritdoc/>
    public Task<(NDArray CannyMask, NDArray InvertedCannyMask)> CreateCannyMaskAsync(string imagePath, float threshold)
    {
        return cannyMasker.GetMaskAsync(imagePath, new CannyMaskOptions(threshold));
    }

    public Task<(NDArray LumMask, NDArray InvertedLumMask)> CreateLumMaskAsync(string imagePath, float threshold)
    {
        return luminanceMasker.GetMaskAsync(imagePath, new LuminanceMaskOptions(threshold));
    }

    /// <inheritdoc/>
    public async Task<NDArray?> BuildMaskAsync(
        string imagePath,
        bool useSubjectMask,
        bool useCanny,
        bool useSubtractMasks,
        bool useInvertedSubjectMask,
        int subjectMaskPadding,
        bool useLumMask,
        float lumThreshold,
        bool useInvertedLumMask,
        float cannyThreshold)
    {
        EnsureCacheScope(imagePath);

        var cacheKey = new MaskBuildCacheKey(
            useSubjectMask,
            useCanny,
            useSubtractMasks,
            useInvertedSubjectMask,
            subjectMaskPadding,
            GetThresholdBucket(cannyThreshold),
            useLumMask,
            useInvertedLumMask,
            GetThresholdBucket(lumThreshold));

        if (cachedMaskBuildKey is MaskBuildCacheKey existingCacheKey && existingCacheKey == cacheKey)
        {
            return cachedBuiltMask;
        }

        if (!useSubjectMask && !useCanny && !useLumMask)
        {
            return CacheAndReturn(cacheKey, null);
        }

        // Gather all enabled masks (normal + inverted)
        var masks = new List<(NDArray normal, NDArray inverted)>();

        if (useSubjectMask)
        {
            await EnsureSubjectMaskAsync(imagePath, subjectMaskPadding);
            if (subjectMask is null || invertedSubjectMask is null)
                return CacheAndReturn(cacheKey, null);

            // Apply inversion preference for subject mask
            var effective = useInvertedSubjectMask
                ? (invertedSubjectMask, subjectMask)
                : (subjectMask, invertedSubjectMask);
            masks.Add(effective);
        }

        if (useCanny)
        {
            await EnsureCannyMaskAsync(imagePath, cannyThreshold);
            if (cannyMask is null || invertedCannyMask is null)
                return CacheAndReturn(cacheKey, null);
            masks.Add((cannyMask, invertedCannyMask));
        }

        if (useLumMask)
        {
            await EnsureLumMaskAsync(imagePath, lumThreshold);
            if (lumMask is null || invertedLumMask is null)
                return CacheAndReturn(cacheKey, null);
            var effectiveLum = useInvertedLumMask
                ? (invertedLumMask, lumMask)
                : (lumMask, invertedLumMask);
            masks.Add(effectiveLum);
        }

        if (masks.Count == 0)
        {
            return CacheAndReturn(cacheKey, null);
        }

        // Fold masks together: start with the first, combine the rest
        var result = masks[0].normal;
        for (var i = 1; i < masks.Count; i++)
        {
            result = useSubtractMasks
                ? MaskCombiner.SubtractMasks(result, masks[i].inverted)
                : MaskCombiner.AddMasks(result, masks[i].normal);
        }

        return CacheAndReturn(cacheKey, result);
    }

    /// <summary>
    /// Stores the built mask in the cache and returns it.
    /// </summary>
    private NDArray? CacheAndReturn(MaskBuildCacheKey key, NDArray? mask)
    {
        cachedMaskBuildKey = key;
        cachedBuiltMask = mask;
        return mask;
    }

    /// <inheritdoc/>
    public async Task<string> SortImageAsync(string imagePath, Func<Hsl, float> sortingCriterion, SortDirections sortingDirection, NDArray? maskToUse)
    {
        var sortedImagePath = Path.Combine(FileSystem.CacheDirectory, $"sorted_temp_{Guid.NewGuid()}.png");

        await Task.Run(() =>
        {
            var imgData = Sorter.SortImage(
                Image.LoadImage(imagePath),
                sortingCriterion,
                sortingDirection,
                maskToUse);

            Image.SaveImage(imgData, sortedImagePath);
        });

        return sortedImagePath;
    }

    /// <inheritdoc/>
    public async Task<bool> SaveImageToGalleryAsync(string imagePath, string fileName)
    {
        if (!File.Exists(imagePath))
        {
            return false;
        }

        var galleryService = serviceProvider.GetService<IGalleryService>();
        if (galleryService is null)
        {
            return false;
        }

        var imageBytes = await File.ReadAllBytesAsync(imagePath);
        return await galleryService.SaveImageAsync(imageBytes, fileName);
    }

    /// <summary>
    /// Resets cached masks when the requested image path changes.
    /// </summary>
    /// <param name="imagePath">Path of the image being processed.</param>
    private void EnsureCacheScope(string imagePath)
    {
        if (string.Equals(cachedImagePath, imagePath, StringComparison.Ordinal))
        {
            return;
        }

        cachedImagePath = imagePath;
        cachedSubjectPadding = -1;
        cachedCannyThreshold = -1;
        cachedLumThreshold = -1;
        subjectMask = null;
        invertedSubjectMask = null;
        cannyMask = null;
        invertedCannyMask = null;
        lumMask = null;
        invertedLumMask = null;
        cachedMaskBuildKey = null;
        cachedBuiltMask = null;
    }

    private static int GetThresholdBucket(float threshold)
    {
        return (int)MathF.Round(threshold * 10000f);
    }

    /// <summary>
    /// Ensures that subject masks are available for the current image and padding settings.
    /// </summary>
    /// <param name="imagePath">Path of the image being processed.</param>
    /// <param name="padding">Subject mask padding in pixels.</param>
    private async Task EnsureSubjectMaskAsync(string imagePath, int padding)
    {
        if (subjectMask is not null && invertedSubjectMask is not null && cachedSubjectPadding == padding)
        {
            return;
        }

        if (!IsBackgroundMaskReady)
        {
            subjectMask = null;
            invertedSubjectMask = null;
            return;
        }

        (subjectMask, invertedSubjectMask) = await CreateSubjectMaskAsync(imagePath, padding);
        cachedSubjectPadding = padding;
    }

    /// <summary>
    /// Ensures that Canny masks are available for the current image and threshold settings.
    /// </summary>
    /// <param name="imagePath">Path of the image being processed.</param>
    /// <param name="threshold">Canny threshold in normalized 0-1 range.</param>
    private async Task EnsureCannyMaskAsync(string imagePath, float threshold)
    {
        if (cannyMask is not null && invertedCannyMask is not null && Math.Abs(cachedCannyThreshold - threshold) < 0.0001f)
        {
            return;
        }

        (cannyMask, invertedCannyMask) = await CreateCannyMaskAsync(imagePath, threshold);
        cachedCannyThreshold = threshold;
    }

    private async Task EnsureLumMaskAsync(string imagePath, float threshold)
    {
       if (lumMask is not null && invertedLumMask is not null && Math.Abs(cachedLumThreshold - threshold) < 0.0001f)
        {
            return;
        }

        (lumMask, invertedLumMask) = await CreateLumMaskAsync(imagePath, threshold);
        cachedLumThreshold = threshold;
    }
}
