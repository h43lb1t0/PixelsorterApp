using System.Text.Json.Serialization;
using Tomlyn.Serialization;

namespace PixelsorterApp.Models.Presets;

/// <summary>
/// Represents a collection of TOML mapping configurations for sorting and masking operations.
/// </summary>
public sealed class TomlMap
{
    [JsonPropertyName("sortBy")]
    public Dictionary<string, string>? SortBy { get; init; }

    [JsonPropertyName("direction")]
    public Dictionary<string, string>? Direction { get; init; }

    [JsonPropertyName("maskCombination")]
    public Dictionary<string, string>? MaskCombination { get; init; }

    [JsonPropertyName("whatToSort")]
    public Dictionary<string, string>? WhatToSort { get; init; }
}

/// <summary>
/// Represents a deserialized TOML preset containing configuration options for sorting, masking, edge detection,
/// subject settings, and mask combination.
/// </summary>
public sealed class PresetToml
{
    [TomlPropertyName("sort_settings")]
    public SortSettings? SortSettings { get; init; }

    [TomlPropertyName("masking_options")]
    public MaskingOptions? MaskingOptions { get; init; }

    [TomlPropertyName("canny_options")]
    public CannyOptions? CannyOptions { get; init; }

    [TomlPropertyName("subject_settings")]
    public SubjectSettings? SubjectSettings { get; init; }

    [TomlPropertyName("mask_combination")]
    public MaskCombination? MaskCombination { get; init; }
}

/// <summary>
/// Represents the configuration settings for sorting, including the property to sort by and the sort direction.
/// </summary>
public sealed class SortSettings
{
    [TomlPropertyName("sort_by")]
    public string? SortBy { get; init; }

    [TomlPropertyName("direction")]
    public string? Direction { get; init; }
}

/// <summary>
/// Represents configuration options for controlling masking behavior in image processing operations.
/// </summary>
public sealed class MaskingOptions
{
    [TomlPropertyName("use_canny")]
    public bool UseCanny { get; init; }

    [TomlPropertyName("use_subject")]
    public bool UseSubject { get; init; }
}

/// <summary>
/// Represents configuration options for the Canny edge detection algorithm.
/// </summary>
public sealed class CannyOptions
{
    [TomlPropertyName("threshold")]
    public int? Threshold { get; init; }
}

/// <summary>
/// Represents configuration options for subject-based masking.
/// </summary>
public sealed class SubjectSettings
{
    [TomlPropertyName("padding")]
    public int? Padding { get; init; }

    [TomlPropertyName("what_to_sort")]
    public string? WhatToSort { get; init; }
}

/// <summary>
/// Represents a combination of masks used for configuration settings.
/// </summary>
public sealed class MaskCombination
{
    [TomlPropertyName("mode")]
    public string? Mode { get; init; }
}
