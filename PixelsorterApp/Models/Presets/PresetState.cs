namespace PixelsorterApp.Models.Presets;

/// <summary>
/// Represents the parsed state of a preset to be applied to the ViewModel.
/// </summary>
public sealed class PresetState
{
    public bool? UseCanny { get; init; }
    public bool? UseSubjectMask { get; init; }
    public int? CannyThresholdPercent { get; init; }
    public int? SubjectMaskPadding { get; init; }
    public bool? UseInvertedSubjectMask { get; init; }
    public string? SortByName { get; init; }
    public bool? UseSubtractMasks { get; init; }
    public string? DirectionName { get; init; }
    public bool? UseLumMask { get; init; }
    public int? LumThresholdPercent { get; init; }
    public bool? UseInvertedLumMask { get; init; }
}
