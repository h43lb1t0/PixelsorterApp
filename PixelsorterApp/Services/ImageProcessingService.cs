using Microsoft.Extensions.DependencyInjection;
using NumSharp;
using PixelsorterClassLib.Core;
using PixelsorterClassLib.Masks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.ColorSpaces;
using Image = PixelsorterClassLib.Core.Image;

namespace PixelsorterApp.Services;

public sealed class ImageProcessingService(IServiceProvider serviceProvider) : IImageProcessingService
{
    private readonly BackgroundMask backgroundMasker = new();
    private readonly CannyMask cannyMasker = new();

    private string? cachedImagePath;
    private int cachedSubjectPadding = -1;
    private float cachedCannyThreshold = -1;
    private NDArray? subjectMask;
    private NDArray? invertedSubjectMask;
    private NDArray? cannyMask;
    private NDArray? invertedCannyMask;

    public bool IsBackgroundMaskReady => backgroundMasker.IsReadyToUse;

    public Task DownloadBackgroundModelAsync()
    {
        return backgroundMasker.DownloadModel();
    }

    public Task<(NDArray SubjectMask, NDArray InvertedSubjectMask)> CreateSubjectMaskAsync(string imagePath, int padding)
    {
        return backgroundMasker.GetMaskAsync(imagePath, new BackgroundMaskOptions(padding));
    }

    public Task<(NDArray CannyMask, NDArray InvertedCannyMask)> CreateCannyMaskAsync(string imagePath, float threshold)
    {
        return cannyMasker.GetMaskAsync(imagePath, new CannyMaskOptions(threshold));
    }

    public async Task<NDArray?> BuildMaskAsync(
        string imagePath,
        bool useSubjectMask,
        bool useCanny,
        bool useSubtractMasks,
        bool useInvertedSubjectMask,
        int subjectMaskPadding,
        float cannyThreshold)
    {
        EnsureCacheScope(imagePath);

        if (!useSubjectMask && !useCanny)
        {
            return null;
        }

        if (useSubjectMask)
        {
            await EnsureSubjectMaskAsync(imagePath, subjectMaskPadding);
        }

        if (useCanny)
        {
            await EnsureCannyMaskAsync(imagePath, cannyThreshold);
        }

        if (useSubjectMask && useCanny)
        {
            if (subjectMask is null || invertedCannyMask is null || cannyMask is null)
            {
                return null;
            }

            return useSubtractMasks
                ? MaskCombiner.SubtractMasks(subjectMask, invertedCannyMask)
                : MaskCombiner.AddMasks(subjectMask, cannyMask);
        }

        if (useCanny)
        {
            return cannyMask;
        }

        if (useSubjectMask)
        {
            if (subjectMask is null || invertedSubjectMask is null)
            {
                return null;
            }

            return useInvertedSubjectMask ? invertedSubjectMask : subjectMask;
        }

        return null;
    }

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

            using var imageData = Image.NdarrayToImgData(imgData);
            imageData.SaveAsPng(sortedImagePath);
        });

        return sortedImagePath;
    }

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

    private void EnsureCacheScope(string imagePath)
    {
        if (string.Equals(cachedImagePath, imagePath, StringComparison.Ordinal))
        {
            return;
        }

        cachedImagePath = imagePath;
        cachedSubjectPadding = -1;
        cachedCannyThreshold = -1;
        subjectMask = null;
        invertedSubjectMask = null;
        cannyMask = null;
        invertedCannyMask = null;
    }

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

    private async Task EnsureCannyMaskAsync(string imagePath, float threshold)
    {
        if (cannyMask is not null && invertedCannyMask is not null && Math.Abs(cachedCannyThreshold - threshold) < 0.0001f)
        {
            return;
        }

        (cannyMask, invertedCannyMask) = await CreateCannyMaskAsync(imagePath, threshold);
        cachedCannyThreshold = threshold;
    }
}
