using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PixelsorterApp.Services;
using PixelsorterClassLib.Core;
using SixLabors.ImageSharp.ColorSpaces;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Tomlyn;
using Tomlyn.Serialization;

namespace PixelsorterApp.ViewModels;

/// <summary>
/// Represents UI state and commands for the main pixel sorting screen.
/// </summary>
public sealed partial class MainPageViewModel : BaseViewModel
{
    private const string BasePresetPath = "presets/base.toml";
    private const string TomlMapPath = "presets/tomlMap.json";

    private readonly IHelpNavigationService helpNavigationService;
    private readonly Dictionary<string, Func<Hsl, float>> sortByOptions = SortBy.GetAllSortingCriteria();
    private readonly Dictionary<string, SortDirections> sortDirectionOptions = [];

    /// <summary>
    /// Gets or sets a value indicating whether the page is busy.
    /// </summary>
    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether subject masking is enabled.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowSubjectPadding))]
    [NotifyPropertyChangedFor(nameof(ShowWhatToSort))]
    [NotifyPropertyChangedFor(nameof(ShowHowToCombine))]
    public partial bool UseSubjectMask { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether Canny masking is enabled.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowCannyThreshold))]
    [NotifyPropertyChangedFor(nameof(ShowWhatToSort))]
    [NotifyPropertyChangedFor(nameof(ShowHowToCombine))]
    public partial bool UseCanny { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether sorting is currently enabled.
    /// </summary>
    [ObservableProperty]
    public partial bool IsSortEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the save button should be visible.
    /// </summary>
    [ObservableProperty]
    public partial bool IsSaveVisible { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether saving is currently enabled.
    /// </summary>
    [ObservableProperty]
    public partial bool IsSaveEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether interactive controls should be enabled.
    /// </summary>
    [ObservableProperty]
    public partial bool IsInteractionEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the selected sort criterion index.
    /// </summary>
    [ObservableProperty]
    public partial int SelectedSortByIndex { get; set; }

    /// <summary>
    /// Gets or sets the selected sort direction index.
    /// </summary>
    [ObservableProperty]
    public partial int SelectedSortDirectionIndex { get; set; }

    /// <summary>
    /// Gets or sets the Canny threshold value in percent (1-99).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CannyThreshold))]
    [NotifyPropertyChangedFor(nameof(CannyThresholdText))]
    public partial int CannyThresholdPercent { get; set; } = 30;

    /// <summary>
    /// Gets or sets the subject mask padding in pixels (1-100).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SubjectMaskPaddingText))]
    public partial int SubjectMaskPadding { get; set; } = 15;

    /// <summary>
    /// Gets or sets a value indicating whether the subject mask should be inverted.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SortBackgroundSelected))]
    [NotifyPropertyChangedFor(nameof(SortForegroundSelected))]
    public partial bool UseInvertedSubjectMask { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether masks should be combined by subtraction.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UseSubtractMasksSelected))]
    [NotifyPropertyChangedFor(nameof(UseAddMasksSelected))]
    public partial bool UseSubtractMasks { get; set; } = true;

    /// <summary>
    /// Gets or sets the caption shown for the currently displayed image.
    /// </summary>
    [ObservableProperty]
    public partial string CurrentCaption { get; set; } = "Tap to load an image";

    private readonly IRelayCommand sortCommand;
    private readonly IRelayCommand saveCommand;
    private readonly IRelayCommand loadImageCommand;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainPageViewModel"/> class.
    /// </summary>
    public MainPageViewModel(IHelpNavigationService helpNavigationService)
    {
        this.helpNavigationService = helpNavigationService;

        sortCommand = new RelayCommand(() => SortRequested?.Invoke(), () => IsSortEnabled);
        saveCommand = new RelayCommand(() => SaveRequested?.Invoke(), () => IsSaveEnabled);
        loadImageCommand = new RelayCommand(() => LoadImageRequested?.Invoke(), () => IsInteractionEnabled);

        foreach (SortDirections direction in Enum.GetValues<SortDirections>())
        {
            string name = Regex.Replace(direction.ToString(), "([A-Z])", " $1").Trim();
            sortDirectionOptions[name] = direction;
        }

        SortByOptions = [.. sortByOptions.Keys];
        SelectedSortByIndex = SortByOptions.Count > 0 ? 0 : -1;

        RefreshSortDirectionOptions();
        SelectedSortDirectionIndex = SortDirectionOptions.Count > 0 ? 0 : -1;

        _ = LoadPresetDefaultsAsync();
    }

    /// <summary>
    /// Gets the Canny threshold value as a 0-1 floating point number.
    /// </summary>
    public float CannyThreshold => CannyThresholdPercent / 100f;

    /// <summary>
    /// Gets the formatted Canny threshold label.
    /// </summary>
    public string CannyThresholdText => $"{CannyThresholdPercent}%";

    /// <summary>
    /// Gets the formatted subject mask padding label.
    /// </summary>
    public string SubjectMaskPaddingText => $"{SubjectMaskPadding} px";

    /// <summary>
    /// Gets or sets a value indicating whether background sorting is selected.
    /// </summary>
    public bool SortBackgroundSelected
    {
        get => !UseInvertedSubjectMask;
        set
        {
            if (value)
            {
                UseInvertedSubjectMask = false;
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether foreground sorting is selected.
    /// </summary>
    public bool SortForegroundSelected
    {
        get => UseInvertedSubjectMask;
        set
        {
            if (value)
            {
                UseInvertedSubjectMask = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether subtract combination is selected.
    /// </summary>
    public bool UseSubtractMasksSelected
    {
        get => UseSubtractMasks;
        set
        {
            if (value)
            {
                UseSubtractMasks = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether additive combination is selected.
    /// </summary>
    public bool UseAddMasksSelected
    {
        get => !UseSubtractMasks;
        set
        {
            if (value)
            {
                UseSubtractMasks = false;
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the Canny threshold section should be visible.
    /// </summary>
    public bool ShowCannyThreshold => UseCanny;

    /// <summary>
    /// Gets a value indicating whether the subject padding section should be visible.
    /// </summary>
    public bool ShowSubjectPadding => UseSubjectMask;

    /// <summary>
    /// Gets a value indicating whether the foreground/background selection section should be visible.
    /// </summary>
    public bool ShowWhatToSort => UseSubjectMask && !UseCanny;

    /// <summary>
    /// Gets a value indicating whether the mask combination section should be visible.
    /// </summary>
    public bool ShowHowToCombine => UseSubjectMask && UseCanny;

    /// <summary>
    /// Gets the command that requests image sorting.
    /// </summary>
    public IRelayCommand SortCommand => sortCommand;

    /// <summary>
    /// Gets the command that requests saving the focused image.
    /// </summary>
    public IRelayCommand SaveCommand => saveCommand;

    /// <summary>
    /// Gets the command that requests image loading.
    /// </summary>
    public IRelayCommand LoadImageCommand => loadImageCommand;

    [RelayCommand]
    private async Task OpenHelpAsync()
    {
        await helpNavigationService.ShowHelpMenuAsync();
    }

    /// <summary>
    /// Occurs when sorting is requested.
    /// </summary>
    public event Action? SortRequested;

    /// <summary>
    /// Occurs when saving is requested.
    /// </summary>
    public event Action? SaveRequested;

    /// <summary>
    /// Occurs when image loading is requested.
    /// </summary>
    public event Action? LoadImageRequested;

    /// <summary>
    /// Gets the available sort criteria names.
    /// </summary>
    public IReadOnlyList<string> SortByOptions { get; }

    /// <summary>
    /// Gets the available sort direction names for the current masking configuration.
    /// </summary>
    public ObservableCollection<string> SortDirectionOptions { get; } = [];

    /// <summary>
    /// Gets the currently selected sorting criterion delegate.
    /// </summary>
    public Func<Hsl, float>? SortingCriterion =>
        SelectedSortByIndex >= 0 && SelectedSortByIndex < SortByOptions.Count
            ? sortByOptions[SortByOptions[SelectedSortByIndex]]
            : null;

    /// <summary>
    /// Gets the currently selected sorting direction.
    /// </summary>
    public SortDirections SortingDirection =>
        SelectedSortDirectionIndex >= 0 && SelectedSortDirectionIndex < SortDirectionOptions.Count
            ? sortDirectionOptions[SortDirectionOptions[SelectedSortDirectionIndex]]
            : SortDirections.RowRightToLeft;

    /// <summary>
    /// Gets the selected sort criterion display name.
    /// </summary>
    public string SelectedSortByName =>
        SelectedSortByIndex >= 0 && SelectedSortByIndex < SortByOptions.Count
            ? SortByOptions[SelectedSortByIndex]
            : "Unknown";

    /// <summary>
    /// Gets the selected sort direction display name.
    /// </summary>
    public string SelectedSortDirectionName =>
        SelectedSortDirectionIndex >= 0 && SelectedSortDirectionIndex < SortDirectionOptions.Count
            ? SortDirectionOptions[SelectedSortDirectionIndex]
            : "Unknown";

    /// <summary>
    /// Refreshes sort direction options based on current mask settings and preserves selection when possible.
    /// </summary>
    private void RefreshSortDirectionOptions()
    {
        string? previousSelection =
            SelectedSortDirectionIndex >= 0 && SelectedSortDirectionIndex < SortDirectionOptions.Count
                ? SortDirectionOptions[SelectedSortDirectionIndex]
                : null;

        var filtered = sortDirectionOptions.Keys
            .Where(name => UseSubjectMask || UseCanny || !name.Contains("mask", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        SortDirectionOptions.Clear();
        foreach (var option in filtered)
        {
            SortDirectionOptions.Add(option);
        }

        if (SortDirectionOptions.Count == 0)
        {
            SelectedSortDirectionIndex = -1;
            return;
        }

        if (!string.IsNullOrEmpty(previousSelection))
        {
            int previousIndex = SortDirectionOptions.IndexOf(previousSelection);
            SelectedSortDirectionIndex = previousIndex >= 0 ? previousIndex : 0;
            return;
        }

        if (SelectedSortDirectionIndex < 0 || SelectedSortDirectionIndex >= SortDirectionOptions.Count)
        {
            SelectedSortDirectionIndex = 0;
        }
    }

    private async Task LoadPresetDefaultsAsync()
    {
        try
        {
            var tomlContent = await ReadAppPackageTextAsync(BasePresetPath);
            var mapContent = await ReadAppPackageTextAsync(TomlMapPath);

            var map = JsonSerializer.Deserialize<TomlMap>(mapContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });

            if (map is null)
            {
                return;
            }

            ApplyPreset(tomlContent, map);
        }
        catch
        {
        }
    }

    private static async Task<string> ReadAppPackageTextAsync(string path)
    {
        using var stream = await FileSystem.OpenAppPackageFileAsync(path);
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    private void ApplyPreset(string tomlContent, TomlMap map)
    {
        var sanitizedToml = Regex.Replace(
            tomlContent,
            @"(?m)^\s*mode\s+\""(?<value>[^\""\r\n]+)\""\s*$",
            "mode = \"${value}\"");

        if (!TomlSerializer.TryDeserialize(sanitizedToml, out PresetToml? preset, null) || preset is null)
        {
            return;
        }

        if (preset.MaskingOptions is not null)
        {
            UseCanny = preset.MaskingOptions.UseCanny;
            UseSubjectMask = preset.MaskingOptions.UseSubject;
        }

        if (preset.CannyOptions is not null)
        {
            CannyThresholdPercent = preset.CannyOptions.Threshold > 0
                ? preset.CannyOptions.Threshold
                : preset.CannyOptions.LegacyThreshold;
        }

        if (preset.SubjectSettings is not null)
        {
            SubjectMaskPadding = preset.SubjectSettings.Padding;
            if (!string.IsNullOrWhiteSpace(preset.SubjectSettings.WhatToSort))
            {
                if (TryGetMappedValue(map.WhatToSort, preset.SubjectSettings.WhatToSort, out var whatToSortMapped))
                {
                    UseInvertedSubjectMask = string.Equals(
                        whatToSortMapped,
                        nameof(SortForegroundSelected),
                        StringComparison.Ordinal);
                }
                else
                {
                    UseInvertedSubjectMask = string.Equals(preset.SubjectSettings.WhatToSort, "foreground", StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(preset.SortSettings?.SortBy)
            && TryGetMappedValue(map.SortBy, preset.SortSettings.SortBy, out var sortByMapped))
        {
            var sortByName = sortByMapped.Split('.').Last();
            var sortByIndex = FindIndex(SortByOptions, sortByName);
            if (sortByIndex >= 0)
            {
                SelectedSortByIndex = sortByIndex;
            }
        }

        if (!string.IsNullOrWhiteSpace(preset.MaskCombination?.Mode)
            && TryGetMappedValue(map.MaskCombination, preset.MaskCombination.Mode, out var maskCombinationMapped))
        {
            UseSubtractMasks = string.Equals(
                maskCombinationMapped,
                nameof(UseSubtractMasksSelected),
                StringComparison.Ordinal);
        }

        if (!string.IsNullOrWhiteSpace(preset.SortSettings?.Direction)
            && TryGetMappedValue(map.Direction, preset.SortSettings.Direction, out var directionMapped)
            && Enum.TryParse<SortDirections>(directionMapped.Split('.').Last(), out var direction))
        {
            var directionName = sortDirectionOptions
                .FirstOrDefault(option => option.Value == direction)
                .Key;

            if (!string.IsNullOrWhiteSpace(directionName))
            {
                var directionIndex = SortDirectionOptions.IndexOf(directionName);
                if (directionIndex >= 0)
                {
                    SelectedSortDirectionIndex = directionIndex;
                }
            }
        }
    }

    private static int FindIndex(IReadOnlyList<string> items, string value)
    {
        for (var i = 0; i < items.Count; i++)
        {
            if (string.Equals(items[i], value, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    private static bool TryGetMappedValue(IReadOnlyDictionary<string, string>? map, string key, out string value)
    {
        value = string.Empty;

        if (map is null)
        {
            return false;
        }

        foreach (var pair in map)
        {
            if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                value = pair.Value;
                return true;
            }
        }

        return false;
    }

    private sealed class TomlMap
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

    private sealed class PresetToml
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

    private sealed class SortSettings
    {
        [TomlPropertyName("sort_by")]
        public string? SortBy { get; init; }

        [TomlPropertyName("direction")]
        public string? Direction { get; init; }
    }

    private sealed class MaskingOptions
    {
        [TomlPropertyName("use_canny")]
        public bool UseCanny { get; init; }

        [TomlPropertyName("use_subject")]
        public bool UseSubject { get; init; }
    }

    private sealed class CannyOptions
    {
        [TomlPropertyName("threshold")]
        public int Threshold { get; init; } = 30;

        [TomlPropertyName("threashold")]
        public int LegacyThreshold { get; init; }
    }

    private sealed class SubjectSettings
    {
        [TomlPropertyName("padding")]
        public int Padding { get; init; } = 15;

        [TomlPropertyName("what_to_sort")]
        public string? WhatToSort { get; init; }
    }

    private sealed class MaskCombination
    {
        [TomlPropertyName("mode")]
        public string? Mode { get; init; }
    }

    partial void OnUseSubjectMaskChanged(bool value)
    {
        RefreshSortDirectionOptions();
    }

    partial void OnUseCannyChanged(bool value)
    {
        RefreshSortDirectionOptions();
    }

    partial void OnIsSortEnabledChanged(bool value)
    {
        sortCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsSaveEnabledChanged(bool value)
    {
        saveCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsInteractionEnabledChanged(bool value)
    {
        loadImageCommand.NotifyCanExecuteChanged();
    }

    partial void OnCannyThresholdPercentChanged(int value)
    {
        var clamped = Math.Clamp(value, 1, 99);
        if (value != clamped)
        {
            CannyThresholdPercent = clamped;
        }
    }

    partial void OnSubjectMaskPaddingChanged(int value)
    {
        var clamped = Math.Clamp(value, 1, 100);
        if (value != clamped)
        {
            SubjectMaskPadding = clamped;
        }
    }
}
