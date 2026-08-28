using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PixelsorterApp.Models.Presets;
using PixelsorterApp.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
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
        private readonly bool lumMasking;
        private readonly int lumThreshold;
        private readonly bool lumInverted;
        private readonly string UserPresetsPath = Path.Combine(FileSystem.Current.AppDataDirectory, "Presets");

        private readonly bool subtractMask;

        private readonly TomlMap? tomlMap;

        [ObservableProperty]
        public partial string PresetToml { get; set; }

        [ObservableProperty]
        public partial string TomlMapString { get; set; }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SavePresetCommand))]
        public partial string PresetName { get; set; }

        [ObservableProperty]
        public partial bool MakeDefaultPreset { get; set; } = false;

        private bool isTomlMapVisible;

        /// <summary>
        /// Gets or sets a value indicating whether the TOML map is visible.
        /// </summary>
        /// <remarks>Changing this property raises the PropertyChanged event for the TomlMapToggleText
        /// property, allowing the user interface to update accordingly.</remarks>
        public bool IsTomlMapVisible
        {
            get => isTomlMapVisible;
            set
            {
                if (SetProperty(ref isTomlMapVisible, value))
                {
                    OnPropertyChanged(nameof(TomlMapToggleText));
                }
            }
        }

        [ObservableProperty]
        public partial string SavePresetValidationMessage { get; set; }

        public string TomlMapToggleText => IsTomlMapVisible ? PixelsorterApp.Resources.Languages.AppStrings.ToggleTomlMap_Hide : PixelsorterApp.Resources.Languages.AppStrings.ToggleTomlMap_Show;

        /// <summary>
        /// Validates the current preset and saves it asynchronously if all validation criteria are met.
        /// </summary>
        /// <remarks>This method first performs asynchronous validation of the preset. If validation
        /// succeeds, the preset is saved. Ensure that the preset is in a valid state before invoking this method to
        /// avoid unnecessary operations.</remarks>
        /// <returns></returns>
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

        [RelayCommand]
        private void ToggleTomlMapVisibility()
        {
            IsTomlMapVisible = !IsTomlMapVisible;
        }

        [RelayCommand]
        private void SetBaseAsDefaultPreset()
        {
            Preferences.Set("defaultPreset", "base.toml");
            MakeDefaultPreset = false;
            SavePresetValidationMessage = PixelsorterApp.Resources.Languages.AppStrings.BasePresetSetAsDefault;
        }

        public ObservableCollection<PresetListItem> AvailablePresets { get; set; }


        public PresetsPageViewModel(MainPageViewModel mainViewModel, ITomlValidationService tomlValidationService, IPresetService presetService)
        {
            _mainViewModel = mainViewModel;
            this.tomlValidationService = tomlValidationService;

            sortBy = _mainViewModel.SelectedSortBy?.Key ?? string.Empty;
            sortDirection = _mainViewModel.SelectedSortDirection?.Key ?? string.Empty;

            cannyMasking = _mainViewModel.UseCanny;
            cannyThreashold = _mainViewModel.CannyThresholdPercent;

            subjectMasking = _mainViewModel.UseSubjectMask;
            subjectPadding = _mainViewModel.SubjectMaskPadding;
            subjectBackground = _mainViewModel.UseInvertedSubjectMask;

            lumMasking = _mainViewModel.UseLumMask;
            lumThreshold = _mainViewModel.LumMaskThresholdPercent;
            lumInverted = _mainViewModel.UseInvertedLumMask;

            subtractMask = _mainViewModel.UseSubtractMasks;

            tomlMap = Task.Run(() => presetService.GetTomlMapAsync()).GetAwaiter().GetResult();
            TomlMapString = FormatTomlMap(tomlMap);

            PresetToml = CreateToml();
            SavePresetValidationMessage = string.Empty;

            PresetName = String.Format(PixelsorterApp.Resources.Languages.AppStrings.PresetsPageViewModel_PresetsPageViewModel_NewPresetName, _mainViewModel.PresetOptions.Count);

            AvailablePresets = new ObservableCollection<PresetListItem>();
            RefreshAvailablePresets();

        }


        /// <summary>
        /// Validates the current TOML preset asynchronously and indicates whether it is valid.
        /// </summary>
        /// <remarks>This method sanitizes the TOML preset before validation. It also updates the preset
        /// validation message to reflect the result, including any validation errors.</remarks>
        /// <returns>true if the TOML preset is valid; otherwise, false.</returns>
        private async Task<bool> ValidatePresetAsync()
        {
            PresetToml = tomlValidationService.Sanitize(PresetToml);
            (bool isValid, string errors) = await tomlValidationService.Validate(PresetToml, tomlMap);

            SavePresetValidationMessage = isValid
                ? PixelsorterApp.Resources.Languages.AppStrings.TOMLValidation_Valid
                : string.IsNullOrWhiteSpace(errors)
                    ? PixelsorterApp.Resources.Languages.AppStrings.TOMLValidation_Invalid
                    : String.Format(PixelsorterApp.Resources.Languages.AppStrings.TOMLValidation_InvalidError, errors);
            Debug.WriteLine($"Errors: {errors}");
            return isValid;
        }

        /// <summary>
        /// Asynchronously saves the current preset to a file in TOML format, creating the necessary directory if it
        /// does not exist.
        /// </summary>
        /// <remarks>If the preset name is not provided, a default name is generated based on the count of
        /// existing presets. The method updates the default preset if the 'MakeDefaultPreset' flag is set to <see
        /// langword="true"/>. Any errors encountered during the save operation are captured and reflected in the
        /// 'SavePresetValidationMessage'.</remarks>
        /// <returns>A task that represents the asynchronous save operation.</returns>
        private async Task SavePresetAsync()
        {
            string presetName = string.IsNullOrWhiteSpace(PresetName) ? $"Preset {_mainViewModel.PresetOptions.Count}" : PresetName;
            if (!TryGetPresetFilePath(presetName, out string fileName, out string filePath))
            {
                SavePresetValidationMessage = PixelsorterApp.Resources.Languages.AppStrings.InvalidPresetName;
                return;
            }
            try
            {
                if (!Directory.Exists(UserPresetsPath))
                {
                    Directory.CreateDirectory(UserPresetsPath);
                }
                await File.WriteAllTextAsync(filePath, PresetToml);
                SavePresetValidationMessage = PixelsorterApp.Resources.Languages.AppStrings.PresetSavedSuccessfully;
                if (MakeDefaultPreset)
                {
                    Preferences.Set("defaultPreset", fileName);
                }
                _mainViewModel.RefreshAvailablePresets();
                RefreshAvailablePresets();
                _mainViewModel.SelectedPresetOption = presetName;
            }
            catch (Exception ex)
            {
                SavePresetValidationMessage = String.Format(PixelsorterApp.Resources.Languages.AppStrings.ErrorSavingPreset, ex.Message);
            }
        }

        private bool TryGetPresetFilePath(string presetName, out string fileName, out string filePath)
        {
            fileName = string.Empty;
            filePath = string.Empty;

            if (!TryValidatePresetName(presetName, out string trimmedName))
            {
                return false;
            }

            fileName = $"{trimmedName}.toml";
            string combinedPath = Path.Combine(UserPresetsPath, fileName);
            string fullPath = Path.GetFullPath(combinedPath);
            string basePath = Path.GetFullPath(UserPresetsPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;

            if (!fullPath.StartsWith(basePath, StringComparison.Ordinal))
            {
                return false;
            }

            filePath = fullPath;
            return true;
        }

        private static bool TryValidatePresetName(string presetName, out string trimmedName)
        {
            trimmedName = string.Empty;
            if (string.IsNullOrWhiteSpace(presetName))
            {
                return false;
            }

            trimmedName = presetName.Trim();
            if (trimmedName.Length == 0)
            {
                return false;
            }

            if (trimmedName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                return false;
            }

            if (trimmedName.Contains(Path.DirectorySeparatorChar) || trimmedName.Contains(Path.AltDirectorySeparatorChar))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Loads the specified preset from a file and updates the validation message to reflect the result of the
        /// operation.
        /// </summary>
        /// <remarks>If an error occurs during the loading process, the validation message is updated with
        /// the error details.</remarks>
        /// <param name="preset">The preset to load. Must not be null; otherwise, the method returns without performing any action.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        [RelayCommand]
        private async Task EditPresetAsync(PresetListItem? preset)
        {
            if (preset is null)
            {
                return;
            }

            try
            {
                await LoadPresetTomlFromFileAsync(preset.Name);
                SavePresetValidationMessage = String.Format(PixelsorterApp.Resources.Languages.AppStrings.LoadedPresetPresetName, preset.Name);
            }
            catch (Exception ex)
            {
                SavePresetValidationMessage = String.Format(PixelsorterApp.Resources.Languages.AppStrings.ErrorLoadingPresetPresetNameExMessage, preset.Name, ex.Message);
            }
        }

        /// <summary>
        /// Deletes the specified preset asynchronously and updates the list of available presets upon completion.
        /// </summary>
        /// <remarks>After deleting the preset, this method refreshes the available presets and updates
        /// the validation message to reflect the deletion status.</remarks>
        /// <param name="preset">The preset to delete. This parameter must not be null; if null, the method performs no action.</param>
        /// <returns>A task that represents the asynchronous delete operation.</returns>
        [RelayCommand]
        private async Task DeletePresetAsync(PresetListItem? preset)
        {
            if (preset is null)
            {
                return;
            }

            await DeletePresetFileAsync(preset.Name);
            _mainViewModel.RefreshAvailablePresets();
            RefreshAvailablePresets();
            SavePresetValidationMessage = String.Format(PixelsorterApp.Resources.Languages.AppStrings.DeletedPresetPresetName, preset.Name);
        }


        /// <summary>
        /// Formats the specified TomlMap into a human-readable string that lists the available sorting options by
        /// category.
        /// </summary>
        /// <remarks>The output includes headers for each category (Sort By, Direction, What To Sort, and
        /// Mask Combination) followed by the available options in each. This method is useful for displaying the
        /// current sorting configuration in a readable format, such as in a UI or log.</remarks>
        /// <param name="map">The TomlMap containing sorting configuration options to format. If null, the method returns an empty string.</param>
        /// <returns>A string representation of the TomlMap's options, organized by category. Returns an empty string if the map
        /// is null.</returns>
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
            AppendOptions("What To Sort Luminance Options:", map.WhatToSortLum);
            AppendOptions("Mask Combination Options:", map.MaskCombination);

            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Generates a TOML configuration string that represents the current sorting, masking, and subject settings.
        /// </summary>
        /// <remarks>The generated TOML string reflects the current state of the relevant settings,
        /// including sorting preferences, masking options, and subject configuration. This method is useful for
        /// exporting or persisting the application's configuration in a standardized format.</remarks>
        /// <returns>A string containing the TOML-formatted configuration, structured with sections for sort settings, masking
        /// options, canny options, subject settings, and mask combination.</returns>
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

            string whatToSortLumKey = lumInverted
                ? ResolveBooleanMapKey(tomlMap?.WhatToSortLum, "SortLumInvertedSelected", "SortLumNormalSelected", true, "inverted", "normal")
                : ResolveBooleanMapKey(tomlMap?.WhatToSortLum, "SortLumInvertedSelected", "SortLumNormalSelected", false, "inverted", "normal");

            sb.AppendLine("[masking_options]");
            sb.AppendLine($"use_canny = {cannyMasking.ToString().ToLowerInvariant()}");
            sb.AppendLine($"use_subject = {subjectMasking.ToString().ToLowerInvariant()}");
            sb.AppendLine($"use_luminance = {lumMasking.ToString().ToLowerInvariant()}");
            sb.AppendLine("");

            sb.AppendLine("[canny_options]");
            sb.AppendLine($"threshold = {cannyThreashold}");
            sb.AppendLine("");

            sb.AppendLine("[subject_settings]");
            sb.AppendLine($"padding = {subjectPadding}");
            sb.AppendLine($"what_to_sort = \"{whatToSortKey}\"");
            sb.AppendLine("");

            sb.AppendLine("[luminance_options]");
            sb.AppendLine($"threshold = {lumThreshold}");
            sb.AppendLine($"what_to_sort = \"{whatToSortLumKey}\"");
            sb.AppendLine("");

            sb.AppendLine("[mask_combination]");
            sb.AppendLine($"mode = \"{maskCombinationKey}\"");

            return sb.ToString();


        }

        /// <summary>
        /// Retrieves the key from the specified map whose mapped value matches the provided selected value after
        /// normalization. Returns a fallback value if no matching key is found or if the map is null.
        /// </summary>
        /// <remarks>Both the selected value and the mapped values are normalized before comparison. If a
        /// mapped value contains a period ('.'), only the substring after the last period is used for matching. This
        /// method does not throw exceptions for null maps.</remarks>
        /// <param name="map">A dictionary mapping string keys to string values. The method searches this map for a value that matches the
        /// normalized selected value. Can be null.</param>
        /// <param name="selectedValue">The value to match against the mapped values in the dictionary. This value is normalized before comparison.</param>
        /// <param name="fallback">The value to return if the map is null or if no matching key is found.</param>
        /// <returns>The key from the map that corresponds to the normalized selected value, or the fallback value if no match is
        /// found.</returns>
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

        /// <summary>
        /// Resolves and returns the key from a boolean mapping dictionary that corresponds to the specified boolean
        /// value and its associated symbol. If no matching key is found or the mapping is null, returns the provided
        /// fallback key for the selected value.
        /// </summary>
        /// <remarks>Comparison of symbols is case-sensitive using ordinal comparison. If multiple keys
        /// map to the same symbol, the first matching key encountered is returned.</remarks>
        /// <param name="map">A dictionary mapping string keys to string values representing boolean symbols. May be null, in which case
        /// the appropriate fallback key is returned.</param>
        /// <param name="trueSymbol">The string symbol in the mapping that represents a true value.</param>
        /// <param name="falseSymbol">The string symbol in the mapping that represents a false value.</param>
        /// <param name="selectedValue">A boolean value indicating which symbol to resolve in the mapping. If true, the method searches for the true
        /// symbol; otherwise, it searches for the false symbol.</param>
        /// <param name="trueFallback">The fallback key to return if the mapping is null or no key is found for the true symbol.</param>
        /// <param name="falseFallback">The fallback key to return if the mapping is null or no key is found for the false symbol.</param>
        /// <returns>The key from the mapping that corresponds to the selected value and its symbol, or the appropriate fallback
        /// key if no match is found.</returns>
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
            AvailablePresets.Clear();
            foreach (string preset in _mainViewModel.PresetOptions)
            {
                if (string.Equals(preset, PixelsorterApp.Resources.Languages.AppStrings.NewPresetAction, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(preset, "base", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                AvailablePresets.Add(new PresetListItem { Name = preset });
            }
        }

        public sealed class PresetListItem
        {
            public required string Name { get; init; }
        }

        private async Task LoadPresetTomlFromFileAsync(string presetName)
        {
            string fileName = $"{presetName}.toml";
            string filePath = Path.Combine(UserPresetsPath, fileName);
            string rawToml = Path.IsPathRooted(filePath)
                ? await File.ReadAllTextAsync(filePath)
                : await ReadAppPackageTextAsync(filePath);
            PresetToml = EnsureLuminanceSections(rawToml);
            PresetName = presetName;
            MakeDefaultPreset = string.Equals(
                Preferences.Get("defaultPreset", string.Empty),
                fileName,
                StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Injects default luminance sections into preset TOML text if they are missing.
        /// This ensures old presets get the new fields when opened for editing.
        /// </summary>
        private static string EnsureLuminanceSections(string toml)
        {
            var sb = new StringBuilder(toml);

            if (!toml.Contains("use_luminance", StringComparison.OrdinalIgnoreCase))
            {
                // Insert after the last existing masking_options line
                int maskingEnd = toml.IndexOf("[canny_options]", StringComparison.OrdinalIgnoreCase);
                if (maskingEnd >= 0)
                {
                    sb.Insert(maskingEnd, "use_luminance = false\r\n\r\n");
                }
                else
                {
                    sb.AppendLine("use_luminance = false");
                }
            }

            if (!toml.Contains("[luminance_options]", StringComparison.OrdinalIgnoreCase))
            {
                // Insert before [mask_combination] if present, otherwise append
                string current = sb.ToString();
                int maskCombIdx = current.IndexOf("[mask_combination]", StringComparison.OrdinalIgnoreCase);
                if (maskCombIdx >= 0)
                {
                    sb.Insert(maskCombIdx, "[luminance_options]\r\nthreshold = 50\r\nwhat_to_sort = \"normal\"\r\n\r\n");
                }
                else
                {
                    sb.AppendLine();
                    sb.AppendLine("[luminance_options]");
                    sb.AppendLine("threshold = 50");
                    sb.AppendLine("what_to_sort = \"normal\"");
                }
            }

            return sb.ToString();
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
