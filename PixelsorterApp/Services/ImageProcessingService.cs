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
}
