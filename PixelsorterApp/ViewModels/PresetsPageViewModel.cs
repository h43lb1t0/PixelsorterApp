using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PixelsorterApp.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace PixelsorterApp.ViewModels
{
    public sealed partial class PresetsPageViewModel : BaseViewModel
    {
        private readonly MainPageViewModel _mainViewModel;
        private readonly ITomlValidationService tomlValidationService;
        private readonly string sortBy;
        private readonly string sortDirection;
        private readonly bool cannyMasking;
        private readonly int cannyThreashold;
        private readonly bool subjectMasking;
        private readonly int subjectPadding;
        private readonly bool subjectBackground;
        private readonly string UserPresetsPath = Path.Combine(FileSystem.Current.AppDataDirectory, "Presets");

        private readonly bool subtractMask;

        private readonly string tomlMapPath;

        private readonly TomlMap? tomlMap;

        [ObservableProperty]
        public partial string PresetToml {  get; set; }

        [ObservableProperty]
        public partial string TomlMapString { get; set; }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SavePresetCommand))]
        public partial string PresetName { get; set; }

        [ObservableProperty]
        public partial bool MakeDefaultPreset { get; set; } = false;

        [ObservableProperty]
        public partial string SavePresetValidationMessage { get; set; }

        [RelayCommand(CanExecute = nameof(CanSubmit))]
        private async Task SavePreset()
        {
            bool isValid = await ValidatePresetAsync();
            if (isValid)
            {
                await SavePresetAsync();
            }
        }

        private bool CanSubmit() => !string.IsNullOrWhiteSpace(PresetName);

        public ObservableCollection<PresetListItem> AvilablePresets { get; set; }


        public PresetsPageViewModel(MainPageViewModel mainViewModel, ITomlValidationService tomlValidationService)
        {
            _mainViewModel = mainViewModel;
            this.tomlValidationService = tomlValidationService;

            sortBy = _mainViewModel.SelectedSortByName;
            sortDirection = _mainViewModel.SelectedSortDirectionName;

            cannyMasking = _mainViewModel.UseCanny;
            cannyThreashold = _mainViewModel.CannyThresholdPercent;

            subjectMasking = _mainViewModel.UseSubjectMask;
            subjectPadding = _mainViewModel.SubjectMaskPadding;
            subjectBackground = _mainViewModel.UseInvertedSubjectMask;

            subtractMask = _mainViewModel.UseSubtractMasks;

            tomlMapPath = MainPageViewModel.TomlMapPath;
            tomlMap = LoadTomlMap();
            TomlMapString = FormatTomlMap(tomlMap);

            PresetToml = CreateToml();
            SavePresetValidationMessage = string.Empty;

            PresetName = $"Preset {_mainViewModel.PresetOptions.Count + 1}";

            AvilablePresets = new ObservableCollection<PresetListItem>();
            RefreshAvailablePresets();

        }


        private async Task<bool> ValidatePresetAsync()
        {
            PresetToml = PresetToml.Replace("\r\n", "\n").Replace('\r', '\n');

            PresetToml = Regex.Replace(
            PresetToml,
            @"(?m)^\s*mode\s+\""(?<value>[^\""\r\n]+)\""\s*$",
            "mode = \"${value}\"");
            (bool isValid, string errors) = await tomlValidationService.Validate(PresetToml);

            SavePresetValidationMessage = isValid
                ? "TOML is valid."
                : string.IsNullOrWhiteSpace(errors)
                    ? "TOML is invalid."
                    : $"TOML is invalid: {errors}";
            Debug.WriteLine($"Errors: {errors}");
            return isValid;
        }

        private async Task SavePresetAsync()
        {
            string presetName = string.IsNullOrWhiteSpace(PresetName) ? $"Preset {_mainViewModel.PresetOptions.Count + 1}" : PresetName;
            string fileName = $"{presetName}.toml";
            string filePath = Path.Combine(UserPresetsPath, fileName);
            try
            {
                if (!Directory.Exists(UserPresetsPath))
                {
                    Directory.CreateDirectory(UserPresetsPath);
                }
                await File.WriteAllTextAsync(filePath, PresetToml);
                SavePresetValidationMessage = "Preset saved successfully.";
                if (MakeDefaultPreset)
                {
                    Preferences.Set("defaultPreset", fileName);
                }
                _mainViewModel.GetAvilablePresets();
                RefreshAvailablePresets();
            }
            catch (Exception ex)
            {
                SavePresetValidationMessage = $"Error saving preset: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task EditPresetAsync(PresetListItem? preset)
        {
            if (preset is null)
            {
                return;
            }

            await LoadPresetTomlFromFileAsync(preset.Name);
            SavePresetValidationMessage = $"Loaded preset '{preset.Name}'.";
        }

        [RelayCommand]
        private async Task DeletePresetAsync(PresetListItem? preset)
        {
            if (preset is null)
            {
                return;
            }

            await DeletePresetFileAsync(preset.Name);
            _mainViewModel.GetAvilablePresets();
            RefreshAvailablePresets();
            SavePresetValidationMessage = $"Deleted preset '{preset.Name}'.";
        }


        private static string FormatTomlMap(TomlMap? map)
        {
            if (map == null)
            {
                return string.Empty;
            }

            StringBuilder sb = new();

            void AppendOptions(string header, Dictionary<string, string>? options)
            {
                if (options is { Count: > 0 })
                {
                    sb.AppendLine(header);
                    foreach (string key in options.Keys)
                    {
                        sb.AppendLine($"  - {key}");
                    }
                    sb.AppendLine();
                }
            }

            AppendOptions("Sort By Options:", map.SortBy);
            AppendOptions("Direction Options:", map.Direction);
            AppendOptions("What To Sort Options:", map.WhatToSort);
            AppendOptions("Mask Combination Options:", map.MaskCombination);

            return sb.ToString().TrimEnd();
        }

        private string CreateToml()
        {
            string sortByKey = ResolveMapKey(tomlMap?.SortBy, sortBy, "cool");
            string directionKey = ResolveMapKey(tomlMap?.Direction, sortDirection, "lr");
            string whatToSortKey = subjectBackground
                ? ResolveBooleanMapKey(tomlMap?.WhatToSort, "SortForegroundSelected", "SortBackgroundSelected", true, "foreground", "background")
                : ResolveBooleanMapKey(tomlMap?.WhatToSort, "SortForegroundSelected", "SortBackgroundSelected", false, "foreground", "background");
            string maskCombinationKey = subtractMask
                ? ResolveBooleanMapKey(tomlMap?.MaskCombination, "UseSubtractMasksSelected", "UseAddMasksSelected", true, "sub", "add")
                : ResolveBooleanMapKey(tomlMap?.MaskCombination, "UseSubtractMasksSelected", "UseAddMasksSelected", false, "sub", "add");

            StringBuilder sb = new();

            sb.AppendLine("[sort_settings]");
            sb.AppendLine($"sort_by = \"{sortByKey}\"");
            sb.AppendLine($"direction = \"{directionKey}\"");
            sb.AppendLine("");

            sb.AppendLine("[masking_options]");
            sb.AppendLine($"use_canny = {cannyMasking.ToString().ToLowerInvariant()}");
            sb.AppendLine($"use_subject = {subjectMasking.ToString().ToLowerInvariant()}");
            sb.AppendLine("");

            sb.AppendLine("[canny_options]");
            sb.AppendLine($"threashold = {cannyThreashold}");
            sb.AppendLine("");

            sb.AppendLine("[subject_settings]");
            sb.AppendLine($"padding = {subjectPadding}");
            sb.AppendLine($"what_to_sort = \"{whatToSortKey}\"");
            sb.AppendLine("");

            sb.AppendLine("[mask_combination]");
            sb.AppendLine($"mode = \"{maskCombinationKey}\"");

            return sb.ToString();


        }

        private TomlMap? LoadTomlMap()
        {
            try
            {
                using Stream stream = FileSystem.OpenAppPackageFileAsync(tomlMapPath).GetAwaiter().GetResult();
                using StreamReader reader = new(stream);
                string content = reader.ReadToEnd();

                return JsonSerializer.Deserialize<TomlMap>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch
            {
                return null;
            }
        }

        private static string ResolveMapKey(Dictionary<string, string>? map, string selectedValue, string fallback)
        {
            if (map is null)
            {
                return fallback;
            }

            string normalizedSelected = Normalize(selectedValue);
            foreach ((string key, string mappedValue) in map)
            {
                string symbolName = mappedValue.Contains('.') ? mappedValue[(mappedValue.LastIndexOf('.') + 1)..] : mappedValue;
                if (Normalize(symbolName) == normalizedSelected)
                {
                    return key;
                }
            }

            return fallback;
        }

        private static string ResolveBooleanMapKey(
            Dictionary<string, string>? map,
            string trueSymbol,
            string falseSymbol,
            bool selectedValue,
            string trueFallback,
            string falseFallback)
        {
            if (map is null)
            {
                return selectedValue ? trueFallback : falseFallback;
            }

            foreach ((string key, string mappedValue) in map)
            {
                if (string.Equals(mappedValue, trueSymbol, StringComparison.Ordinal))
                {
                    if (selectedValue)
                    {
                        return key;
                    }
                }
                else if (string.Equals(mappedValue, falseSymbol, StringComparison.Ordinal))
                {
                    if (!selectedValue)
                    {
                        return key;
                    }
                }
            }

            return selectedValue ? trueFallback : falseFallback;
        }

        private static string Normalize(string value)
        {
            return Regex.Replace(value, "[^A-Za-z0-9]", string.Empty).ToLowerInvariant();
        }

        private void RefreshAvailablePresets()
        {
            AvilablePresets.Clear();
            foreach (string preset in _mainViewModel.PresetOptions)
            {
                if (string.Equals(preset, "new preset", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(preset, "base", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                AvilablePresets.Add(new PresetListItem { Name = preset });
            }
        }

        public sealed class PresetListItem
        {
            public required string Name { get; init; }
        }

        private sealed class TomlMap
        {
            [JsonPropertyName("sortBy")]
            public Dictionary<string, string>? SortBy { get; set; }

            [JsonPropertyName("direction")]
            public Dictionary<string, string>? Direction { get; set; }

            [JsonPropertyName("maskCombination")]
            public Dictionary<string, string>? MaskCombination { get; set; }

            [JsonPropertyName("whatToSort")]
            public Dictionary<string, string>? WhatToSort { get; set; }
        }

        private async Task LoadPresetTomlFromFileAsync(string presetName)
        {
            string fileName = $"{presetName}.toml";
            string filePath = Path.Combine(UserPresetsPath, fileName);
            PresetToml = await ReadAppPackageTextAsync(filePath);
            PresetName = presetName;
            if (Preferences.Get("defaultPreset", String.Empty) == fileName)
            {
                MakeDefaultPreset = true;
            }
        }

        private Task DeletePresetFileAsync(string presetName)
        {
            string fileName = $"{presetName}.toml";
            string filePath = Path.Combine(UserPresetsPath, fileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                if (Preferences.Get("defaultPreset", String.Empty) == fileName)
                {
                    Preferences.Set("defaultPreset", "base.toml");
                }
            }

            return Task.CompletedTask;
        }
    }
}
