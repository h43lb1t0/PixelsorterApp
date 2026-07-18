namespace PixelsorterApp.Models.Presets;

/// <summary>
/// Represents the parsed state of a preset to be applied to the ViewModel.
/// </summary>
public sealed class PresetState
{
    public bool? UseCanny { get; set; }
    public bool? UseSubjectMask { get; set; }
    public int? CannyThresholdPercent { get; set; }
    public int? SubjectMaskPadding { get; set; }
    public bool? UseInvertedSubjectMask { get; set; }
    public string? SortByName { get; set; }
    public bool? UseSubtractMasks { get; set; }
    public string? DirectionName { get; set; }
}
