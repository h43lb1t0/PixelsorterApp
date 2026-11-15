using NumSharp;
using PixelsorterClassLib.Core;
using PixelsorterClassLib.Masks;
using Hsl = SixLabors.ImageSharp.ColorProfiles.Hsl;
using Image = PixelsorterClassLib.Core.Image;

namespace PixelsorterApp.Services;

/// <summary>
/// Provides image sorting, mask generation, and gallery save operations.
/// </summary>
/// <param name="serviceProvider">Service provider used to resolve platform-specific dependencies.</param>
public sealed class ImageProcessingService(IServiceProvider serviceProvider) : IImageProcessingService, IDisposable
{
    private readonly BackgroundMask backgroundMasker = new();
    private readonly CannyMask cannyMasker = new();
    private readonly LuminanceMask luminanceMasker = new();

    private string? cachedImagePath;
    private int cachedSubjectPadding = -1;
    private float cachedSubjectDetectionSensitivity = -1f;
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
    private bool disposed;

    private readonly record struct MaskBuildCacheKey(
        bool UseSubjectMask,
        bool UseCanny,
        bool UseSubtractMasks,
        bool UseInvertedSubjectMask,
        int SubjectDetectionSensitivityBucket,
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
        ObjectDisposedException.ThrowIf(disposed, this);
        return backgroundMasker.DownloadModel();
    }

    /// <inheritdoc/>
    public Task<(NDArray SubjectMask, NDArray InvertedSubjectMask)> CreateSubjectMaskAsync(string imagePath, float detectionSensitivity, int padding)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return backgroundMasker.GetMaskAsync(imagePath, new BackgroundMaskOptions(padding, detectionSensitivity));
    }

    /// <inheritdoc/>
    public Task<(NDArray CannyMask, NDArray InvertedCannyMask)> CreateCannyMaskAsync(string imagePath, float threshold)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return cannyMasker.GetMaskAsync(imagePath, new CannyMaskOptions(threshold));
    }

    /// <inheritdoc/>
    public Task<(NDArray LumMask, NDArray InvertedLumMask)> CreateLumMaskAsync(string imagePath, float threshold)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return luminanceMasker.GetMaskAsync(imagePath, new LuminanceMaskOptions(threshold));
    }

    /// <inheritdoc/>
    public async Task<NDArray?> BuildMaskAsync(
        string imagePath,
        bool useSubjectMask,
        bool useCanny,
        bool useSubtractMasks,
        bool useInvertedSubjectMask,
        float subjectDetectionSensitivity,
        int subjectMaskPadding,
        bool useLumMask,
        float lumThreshold,
        bool useInvertedLumMask,
        float cannyThreshold)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        EnsureCacheScope(imagePath);

        var cacheKey = new MaskBuildCacheKey(
            useSubjectMask,
            useCanny,
            useSubtractMasks,
            useInvertedSubjectMask,
            GetThresholdBucket(subjectDetectionSensitivity),
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
            await EnsureSubjectMaskAsync(imagePath, subjectDetectionSensitivity, subjectMaskPadding);
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
        try
        {
            for (var i = 1; i < masks.Count; i++)
            {
                var next = useSubtractMasks
                    ? MaskCombiner.SubtractMasks(result, masks[i].inverted)
                    : MaskCombiner.AddMasks(result, masks[i].normal);

                if (!ReferenceEquals(result, masks[0].normal))
                {
                    result.Dispose();
                }

                result = next;
            }
        }
        catch
        {
            if (!ReferenceEquals(result, masks[0].normal))
            {
                result.Dispose();
            }

            throw;
        }

        if (disposed)
        {
            if (!ReferenceEquals(result, masks[0].normal))
            {
                result.Dispose();
            }

            throw new ObjectDisposedException(nameof(ImageProcessingService));
        }

        return CacheAndReturn(cacheKey, result);
    }

    /// <summary>
    /// Stores the built mask in the cache and returns it.
    /// </summary>
    private NDArray? CacheAndReturn(MaskBuildCacheKey key, NDArray? mask)
    {
        if (cachedBuiltMask is not null && !ReferenceEquals(cachedBuiltMask, mask) && !IsSourceMask(cachedBuiltMask))
        {
            cachedBuiltMask.Dispose();
        }

        cachedMaskBuildKey = key;
        cachedBuiltMask = mask;
        return mask;
    }

    /// <inheritdoc/>
    public async Task<string> SortImageAsync(string imagePath, Func<Hsl, float> sortingCriterion, SortDirections sortingDirection, NDArray? maskToUse, float angle = -1f)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        var sortedImagePath = Path.Combine(FileSystem.CacheDirectory, $"sorted_temp_{Guid.NewGuid()}.png");

        await Task.Run(() =>
        {
            var imgData = Sorter.SortImage(
                Image.LoadImage(imagePath),
                sortingCriterion,
                sortingDirection,
                maskToUse,
                angle);

            Image.SaveImage(imgData, sortedImagePath);
        });

        return sortedImagePath;
    }

    /// <inheritdoc/>
    public async Task<bool> SaveImageToGalleryAsync(string imagePath, string fileName)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
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

    /// <inheritdoc/>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        ResetCacheState();
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

        ResetCacheState();
        cachedImagePath = imagePath;
    }

    /// <summary>
    /// Resets all cache metadata and disposes any owned cached masks.
    /// </summary>
    private void ResetCacheState()
    {
        cachedImagePath = null;
        cachedSubjectPadding = -1;
        cachedSubjectDetectionSensitivity = -1f;
        cachedCannyThreshold = -1;
        cachedLumThreshold = -1;
        ClearCachedMasks();
    }

    /// <summary>
    /// Disposes all cached mask arrays without double-disposing aliased references.
    /// </summary>
    private void ClearCachedMasks()
    {
        InvalidateBuiltMaskCache();

        subjectMask?.Dispose();
        subjectMask = null;

        invertedSubjectMask?.Dispose();
        invertedSubjectMask = null;

        cannyMask?.Dispose();
        cannyMask = null;

        invertedCannyMask?.Dispose();
        invertedCannyMask = null;

        lumMask?.Dispose();
        lumMask = null;

        invertedLumMask?.Dispose();
        invertedLumMask = null;
    }

    /// <summary>
    /// Clears the cached composed mask and disposes it if it is not aliasing a cached source mask.
    /// </summary>
    private void InvalidateBuiltMaskCache()
    {
        if (cachedBuiltMask is not null && !IsSourceMask(cachedBuiltMask))
        {
            cachedBuiltMask.Dispose();
        }

        cachedBuiltMask = null;
        cachedMaskBuildKey = null;
    }

    /// <summary>
    /// Returns whether the given mask instance is one of the cached source masks.
    /// </summary>
    private bool IsSourceMask(NDArray mask) =>
        ReferenceEquals(mask, subjectMask) ||
        ReferenceEquals(mask, invertedSubjectMask) ||
        ReferenceEquals(mask, cannyMask) ||
        ReferenceEquals(mask, invertedCannyMask) ||
        ReferenceEquals(mask, lumMask) ||
        ReferenceEquals(mask, invertedLumMask);

    /// <summary>
    /// Invalidates the built mask cache and disposes a source mask pair before replacement.
    /// </summary>
    private void PrepareMaskPairReplacement(ref NDArray? mask, ref NDArray? invertedMask)
    {
        InvalidateBuiltMaskCache();
        mask?.Dispose();
        mask = null;
        invertedMask?.Dispose();
        invertedMask = null;
    }

    /// <summary>
    /// Disposes freshly created masks and throws if the service was disposed while awaiting generation.
    /// </summary>
    private (NDArray Mask, NDArray InvertedMask) GuardNotDisposed((NDArray Mask, NDArray InvertedMask) created)
    {
        if (disposed)
        {
            created.Mask.Dispose();
            created.InvertedMask.Dispose();
            throw new ObjectDisposedException(nameof(ImageProcessingService));
        }

        return created;
    }

    /// <summary>
    /// Converts a normalized threshold value (0-1) to an integer bucket for caching purposes.
    /// </summary>
    /// <param name="threshold"></param>
    /// <returns></returns>
    private static int GetThresholdBucket(float threshold)
    {
        return (int)MathF.Round(threshold * 10000f);
    }

    /// <summary>
    /// Ensures that subject masks are available for the current image and padding settings.
    /// </summary>
    /// <param name="imagePath">Path of the image being processed.</param>
    /// <param name="detectionSensitivity">Subject mask detection sensitivity.</param>
    /// <param name="padding">Subject mask padding in pixels.</param>
    private async Task EnsureSubjectMaskAsync(string imagePath, float detectionSensitivity, int padding)
    {
        if (subjectMask is not null && invertedSubjectMask is not null && cachedSubjectPadding == padding && Math.Abs(cachedSubjectDetectionSensitivity - detectionSensitivity) < 0.0001f)
        {
            return;
        }

        PrepareMaskPairReplacement(ref subjectMask, ref invertedSubjectMask);
        cachedSubjectPadding = -1;
        cachedSubjectDetectionSensitivity = -1f;

        if (!IsBackgroundMaskReady)
        {
            return;
        }

        (subjectMask, invertedSubjectMask) = GuardNotDisposed(await CreateSubjectMaskAsync(imagePath, detectionSensitivity, padding));
        cachedSubjectPadding = padding;
        cachedSubjectDetectionSensitivity = detectionSensitivity;
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

        PrepareMaskPairReplacement(ref cannyMask, ref invertedCannyMask);
        cachedCannyThreshold = -1f;

        (cannyMask, invertedCannyMask) = GuardNotDisposed(await CreateCannyMaskAsync(imagePath, threshold));
        cachedCannyThreshold = threshold;
    }

    /// <summary>
    /// Ensures that luminance masks are available for the current image and threshold settings.
    /// </summary>
    /// <param name="imagePath">Path of the image being processed.</param>
    /// <param name="threshold">Luminance threshold in normalized 0-1 range.</param>
    /// <returns></returns>
    private async Task EnsureLumMaskAsync(string imagePath, float threshold)
    {
        if (lumMask is not null && invertedLumMask is not null && Math.Abs(cachedLumThreshold - threshold) < 0.0001f)
        {
            return;
        }

        PrepareMaskPairReplacement(ref lumMask, ref invertedLumMask);
        cachedLumThreshold = -1f;

        (lumMask, invertedLumMask) = GuardNotDisposed(await CreateLumMaskAsync(imagePath, threshold));
        cachedLumThreshold = threshold;
    }
}
