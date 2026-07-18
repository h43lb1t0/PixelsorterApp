using PixelsorterApp.Models.Presets;
using System.Text.Json;

namespace PixelsorterApp.Services;

public class PresetService : IPresetService
{
    private readonly ITomlValidationService _tomlValidationService;

    private readonly string _defaultPresetPreference = Preferences.Get("defaultPreset", "base.toml");
    private readonly string _basePresetPath = "presets/base.toml";
    private readonly string _userPresetsPath = Path.Combine(FileSystem.Current.AppDataDirectory, "Presets");
    private const string TomlMapPath = "presets/tomlMap.json";

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
        string normalizedDefaultPreset = Path.GetFileName(_defaultPresetPreference);

        foreach (var preset in availablePresets)
        {
            if (string.Equals(preset.Key, _defaultPresetPreference, StringComparison.OrdinalIgnoreCase)
                || string.Equals(preset.Value, _defaultPresetPreference, StringComparison.OrdinalIgnoreCase)
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
                : await ReadAppPackageTextAsync(presetPath);

            var mapContent = await ReadAppPackageTextAsync(TomlMapPath);

            var map = JsonSerializer.Deserialize<TomlMap>(mapContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });

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

        var state = new PresetState();

        if (preset.MaskingOptions is not null)
        {
            state.UseCanny = preset.MaskingOptions.UseCanny;
            state.UseSubjectMask = preset.MaskingOptions.UseSubject;
        }

        if (preset.CannyOptions is not null)
        {
            var threshold = preset.CannyOptions.Threshold;
            if (threshold is > 0)
            {
                state.CannyThresholdPercent = threshold.Value;
            }
        }

        if (preset.SubjectSettings is not null)
        {
            if (preset.SubjectSettings.Padding is > 0)
            {
                state.SubjectMaskPadding = preset.SubjectSettings.Padding.Value;
            }

            if (!string.IsNullOrWhiteSpace(preset.SubjectSettings.WhatToSort))
            {
                if (TryGetMappedValue(map.WhatToSort, preset.SubjectSettings.WhatToSort, out var whatToSortMapped))
                {
                    state.UseInvertedSubjectMask = string.Equals(
                        whatToSortMapped,
                        "SortForegroundSelected",
                        StringComparison.Ordinal);
                }
                else
                {
                    state.UseInvertedSubjectMask = string.Equals(preset.SubjectSettings.WhatToSort, "foreground", StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(preset.SortSettings?.SortBy)
            && TryGetMappedValue(map.SortBy, preset.SortSettings.SortBy, out var sortByMapped))
        {
            state.SortByName = sortByMapped.Split('.').Last();
        }

        if (!string.IsNullOrWhiteSpace(preset.MaskCombination?.Mode)
            && TryGetMappedValue(map.MaskCombination, preset.MaskCombination.Mode, out var maskCombinationMapped))
        {
            state.UseSubtractMasks = string.Equals(
                maskCombinationMapped,
                "UseSubtractMasksSelected",
                StringComparison.Ordinal);
        }

        if (!string.IsNullOrWhiteSpace(preset.SortSettings?.Direction)
            && TryGetMappedValue(map.Direction, preset.SortSettings.Direction, out var directionMapped))
        {
            state.DirectionName = directionMapped.Split('.').Last();
        }

        return state;
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

    private static async Task<string> ReadAppPackageTextAsync(string path)
    {
        using var stream = await FileSystem.OpenAppPackageFileAsync(path);
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
}
