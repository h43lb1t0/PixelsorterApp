using PixelsorterApp.Models.Presets;
using System.Text.Json;
using PixelsorterApp.ViewModels;

namespace PixelsorterApp.Services;

public class PresetService : IPresetService
{
    private readonly ITomlValidationService _tomlValidationService;

    private readonly string _basePresetPath = "presets/base.toml";
    private readonly string _userPresetsPath = Path.Combine(FileSystem.Current.AppDataDirectory, "Presets");
    private const string TomlMapPath = "presets/tomlMap.json";
    private TomlMap? _cachedMap;

    public PresetService(ITomlValidationService tomlValidationService)
    {
        _tomlValidationService = tomlValidationService;
    }

    public IReadOnlyDictionary<string, string> GetAvailablePresets()
    {
        var availablePresets = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            availablePresets.Add("Base", _basePresetPath);

            if (Directory.Exists(_userPresetsPath))
            {
                var files = Directory.GetFiles(_userPresetsPath, "*.toml");
                foreach (var file in files)
                {
                    availablePresets[Path.GetFileNameWithoutExtension(file)] = file;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading presets: {ex}");
        }

        return availablePresets;
    }

    public string? FindDefaultPresetOption(IReadOnlyDictionary<string, string> availablePresets)
    {
        string defaultPresetPreference = Preferences.Get("defaultPreset", "base.toml");
        string normalizedDefaultPreset = Path.GetFileName(defaultPresetPreference);

        foreach (var preset in availablePresets)
        {
            if (string.Equals(preset.Key, defaultPresetPreference, StringComparison.OrdinalIgnoreCase)
                || string.Equals(preset.Value, defaultPresetPreference, StringComparison.OrdinalIgnoreCase)
                || string.Equals(Path.GetFileName(preset.Value), normalizedDefaultPreset, StringComparison.OrdinalIgnoreCase))
            {
                return preset.Key;
            }
        }

        return null;
    }

    public async Task<PresetState?> LoadPresetAsync(string presetPath)
    {
        try
        {
            var tomlContent = Path.IsPathRooted(presetPath)
                ? await File.ReadAllTextAsync(presetPath)
                : await BaseViewModel.ReadAppPackageTextAsync(presetPath);

            var map = await GetTomlMapAsync();

            if (map is null)
            {
                return null;
            }

            return ApplyPreset(tomlContent, map);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load preset: {ex}");
            return null;
        }
    }

    private PresetState? ApplyPreset(string tomlContent, TomlMap map)
    {
        var sanitizedToml = _tomlValidationService.Sanitize(tomlContent);

        if (!Tomlyn.TomlSerializer.TryDeserialize(sanitizedToml, out PresetToml? preset, null) || preset is null)
        {
            return null;
        }

        bool? useInvertedSubjectMask = null;
        int? subjectMaskPadding = null;

        if (preset.SubjectSettings is not null)
        {
            if (preset.SubjectSettings.Padding is > 0)
            {
                subjectMaskPadding = preset.SubjectSettings.Padding.Value;
            }

            if (!string.IsNullOrWhiteSpace(preset.SubjectSettings.WhatToSort))
            {
                if (TryGetMappedValue(map.WhatToSort, preset.SubjectSettings.WhatToSort, out var whatToSortMapped))
                {
                    useInvertedSubjectMask = string.Equals(whatToSortMapped, "SortForegroundSelected", StringComparison.Ordinal);
                }
                else
                {
                    useInvertedSubjectMask = string.Equals(preset.SubjectSettings.WhatToSort, "foreground", StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        string? sortByName = null;
        if (!string.IsNullOrWhiteSpace(preset.SortSettings?.SortBy)
            && TryGetMappedValue(map.SortBy, preset.SortSettings.SortBy, out var sortByMapped))
        {
            sortByName = sortByMapped.Split('.').Last();
        }

        bool? useSubtractMasks = null;
        if (!string.IsNullOrWhiteSpace(preset.MaskCombination?.Mode)
            && TryGetMappedValue(map.MaskCombination, preset.MaskCombination.Mode, out var maskCombinationMapped))
        {
            useSubtractMasks = string.Equals(maskCombinationMapped, "UseSubtractMasksSelected", StringComparison.Ordinal);
        }

        string? directionName = null;
        if (!string.IsNullOrWhiteSpace(preset.SortSettings?.Direction)
            && TryGetMappedValue(map.Direction, preset.SortSettings.Direction, out var directionMapped))
        {
            directionName = directionMapped.Split('.').Last();
        }

        return new PresetState
        {
            UseCanny = preset.MaskingOptions?.UseCanny,
            UseSubjectMask = preset.MaskingOptions?.UseSubject,
            CannyThresholdPercent = preset.CannyOptions?.Threshold is > 0 ? preset.CannyOptions.Threshold.Value : null,
            SubjectMaskPadding = subjectMaskPadding,
            UseInvertedSubjectMask = useInvertedSubjectMask,
            SortByName = sortByName,
            UseSubtractMasks = useSubtractMasks,
            DirectionName = directionName
        };
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

    public async Task<TomlMap?> GetTomlMapAsync()
    {
        if (_cachedMap is not null)
        {
            return _cachedMap;
        }

        var mapContent = await BaseViewModel.ReadAppPackageTextAsync(TomlMapPath);
        _cachedMap = JsonSerializer.Deserialize<TomlMap>(mapContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });
        return _cachedMap;
    }
}
