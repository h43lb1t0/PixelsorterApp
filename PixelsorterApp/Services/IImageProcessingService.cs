using NumSharp;
using PixelsorterClassLib.Core;
using SixLabors.ImageSharp.ColorSpaces;

namespace PixelsorterApp.Services;

public interface IImageProcessingService
{
    bool IsBackgroundMaskReady { get; }

    Task DownloadBackgroundModelAsync();

    Task<(NDArray SubjectMask, NDArray InvertedSubjectMask)> CreateSubjectMaskAsync(string imagePath, int padding);

    Task<(NDArray CannyMask, NDArray InvertedCannyMask)> CreateCannyMaskAsync(string imagePath, float threshold);

    Task<NDArray?> BuildMaskAsync(
        string imagePath,
        bool useSubjectMask,
        bool useCanny,
        bool useSubtractMasks,
        bool useInvertedSubjectMask,
        int subjectMaskPadding,
        float cannyThreshold);

    Task<string> SortImageAsync(string imagePath, Func<Hsl, float> sortingCriterion, SortDirections sortingDirection, NDArray? maskToUse);

    Task<bool> SaveImageToGalleryAsync(string imagePath, string fileName);
}
