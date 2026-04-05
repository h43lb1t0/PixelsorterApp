using NumSharp;
using PixelsorterClassLib.Core;
using SixLabors.ImageSharp.ColorSpaces;

namespace PixelsorterApp.Services;

/// <summary>
/// Defines image processing operations used by the main page workflow.
/// </summary>
public interface IImageProcessingService
{
    /// <summary>
    /// Gets a value indicating whether the background masking model is ready to use.
    /// </summary>
    bool IsBackgroundMaskReady { get; }

    /// <summary>
    /// Downloads the background masking model.
    /// </summary>
    /// <returns>A task that completes when the model download finishes.</returns>
    Task DownloadBackgroundModelAsync();

    /// <summary>
    /// Creates subject and inverted subject masks for an image.
    /// </summary>
    /// <param name="imagePath">Path to the source image.</param>
    /// <param name="padding">Padding for the subject mask in pixels.</param>
    /// <returns>The subject and inverted subject masks.</returns>
    Task<(NDArray SubjectMask, NDArray InvertedSubjectMask)> CreateSubjectMaskAsync(string imagePath, int padding);

    /// <summary>
    /// Creates Canny and inverted Canny masks for an image.
    /// </summary>
    /// <param name="imagePath">Path to the source image.</param>
    /// <param name="threshold">Canny threshold in normalized 0-1 range.</param>
    /// <returns>The Canny and inverted Canny masks.</returns>
    Task<(NDArray CannyMask, NDArray InvertedCannyMask)> CreateCannyMaskAsync(string imagePath, float threshold);

    /// <summary>
    /// Builds the effective mask according to current mask settings.
    /// </summary>
    /// <param name="imagePath">Path to the source image.</param>
    /// <param name="useSubjectMask">Whether subject masking is enabled.</param>
    /// <param name="useCanny">Whether Canny masking is enabled.</param>
    /// <param name="useSubtractMasks">Whether masks should be combined by subtraction when both are enabled.</param>
    /// <param name="useInvertedSubjectMask">Whether to use the inverted subject mask when only subject masking is enabled.</param>
    /// <param name="subjectMaskPadding">Subject mask padding in pixels.</param>
    /// <param name="cannyThreshold">Canny threshold in normalized 0-1 range.</param>
    /// <returns>The composed mask or <see langword="null"/> when no mask is applicable.</returns>
    Task<NDArray?> BuildMaskAsync(
        string imagePath,
        bool useSubjectMask,
        bool useCanny,
        bool useSubtractMasks,
        bool useInvertedSubjectMask,
        int subjectMaskPadding,
        float cannyThreshold);

    /// <summary>
    /// Sorts an image and writes the result to a temporary file.
    /// </summary>
    /// <param name="imagePath">Path to the source image.</param>
    /// <param name="sortingCriterion">Pixel sorting criterion delegate.</param>
    /// <param name="sortingDirection">Sorting direction.</param>
    /// <param name="maskToUse">Optional mask used while sorting.</param>
    /// <returns>The path of the generated sorted image file.</returns>
    Task<string> SortImageAsync(string imagePath, Func<Hsl, float> sortingCriterion, SortDirections sortingDirection, NDArray? maskToUse);

    /// <summary>
    /// Saves an image file to the device gallery.
    /// </summary>
    /// <param name="imagePath">Path to the image file to save.</param>
    /// <param name="fileName">Target file name in gallery.</param>
    /// <returns><see langword="true"/> when saving succeeds; otherwise, <see langword="false"/>.</returns>
    Task<bool> SaveImageToGalleryAsync(string imagePath, string fileName);
}
