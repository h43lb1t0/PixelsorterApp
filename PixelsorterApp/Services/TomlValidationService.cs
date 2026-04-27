using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Tomlyn;
using Tomlyn.Model;

namespace PixelsorterApp.Services
{
    public sealed class TomlValidationService : ITomlValidationService
    {
        private const string TomlMapPath = "presets/tomlMap.json";
        private readonly IImageProcessingService imageProcessingService;

        public TomlValidationService(IImageProcessingService imageProcessingService)
        {
            this.imageProcessingService = imageProcessingService;
        }

        public async Task<(bool isValid, string errors)> Validate(string conent)
        {

            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(conent))
            {
                return (false, "TOML content is empty.");
            }

            TomlMap? tomlMap = await LoadTomlMapAsync();
            if (tomlMap is null)
            {
                return (false, "Failed to load toml map.");
            }

            TomlTable? model;
            try
            {
                model = TomlSerializer.Deserialize<TomlTable>(conent, new TomlSerializerOptions());
            }
            catch (Exception ex)
            {
                return (false, $"Invalid TOML syntax: {ex.Message}");
            }

            if (model is null)
            {
                return (false, "Invalid TOML syntax.");
            }

            var sortSettings = GetTable(model, "sort_settings", errors);
            var maskingOptions = GetTable(model, "masking_options", errors);
            var cannyOptions = GetTable(model, "canny_options", errors);
            var subjectSettings = GetTable(model, "subject_settings", errors);
            var maskCombination = GetTable(model, "mask_combination", errors);

            string sortBy = GetString(sortSettings, "sort_by", errors);
            string direction = GetString(sortSettings, "direction", errors);
            bool useSubject = GetBool(maskingOptions, "use_subject", errors);
            bool useCanny = GetBool(maskingOptions, "use_canny", errors);
            int cannyThreashold = GetInt(cannyOptions, "threashold", errors);
            int subjectPadding = GetInt(subjectSettings, "padding", errors);
            string whatToSort = GetString(subjectSettings, "what_to_sort", errors);
            string mode = GetString(maskCombination, "mode", errors);

            ValidateMappedOption(sortBy, tomlMap.SortBy, "sort_settings.sort_by", errors);
            ValidateMappedOption(direction, tomlMap.Direction, "sort_settings.direction", errors);
            ValidateMappedOption(whatToSort, tomlMap.WhatToSort, "subject_settings.what_to_sort", errors);
            ValidateMappedOption(mode, tomlMap.MaskCombination, "mask_combination.mode", errors);

            if (string.Equals(direction, "im", StringComparison.OrdinalIgnoreCase) && !useSubject && !useCanny)
            {
                errors.Add("Direction 'im' (Into Mask) requires at least one mask to be enabled.");
            }

            if (cannyThreashold is <= 0 or >= 100)
            {
                errors.Add("canny_options.threashold must be in range (0, 100).");
            }

            if (subjectPadding is < 1 or > 100)
            {
                errors.Add("subject_settings.padding must be in range [1, 100].");
            }

            if (useSubject)
            {
                bool licenseAccepted = Preferences.Get("MaskingLicenseAccepted", false);
                if (!licenseAccepted)
                {
                    errors.Add("Subject masking cannot be enabled before accepting the masking license.");
                }

                if (!imageProcessingService.IsBackgroundMaskReady)
                {
                    errors.Add("Subject masking cannot be enabled because the background model is not downloaded.");
                }
            }

            return errors.Count == 0
                ? (true, string.Empty)
                : (false, string.Join(Environment.NewLine, errors));
        }

        private static TomlTable GetTable(TomlTable root, string tableName, List<string> errors)
        {
            if (root.TryGetValue(tableName, out object? tableValue) && tableValue is TomlTable table)
            {
                return table;
            }

            errors.Add($"Missing or invalid table: [{tableName}].");
            return new TomlTable();
        }

        private static string GetString(TomlTable table, string key, List<string> errors)
        {
            if (table.TryGetValue(key, out object? value) && value is string s)
            {
                return s;
            }

            errors.Add($"Missing or invalid string value: {key}.");
            return string.Empty;
        }

        private static bool GetBool(TomlTable table, string key, List<string> errors)
        {
            if (table.TryGetValue(key, out object? value) && value is bool b)
            {
                return b;
            }

            errors.Add($"Missing or invalid boolean value: {key}.");
            return false;
        }

        private static int GetInt(TomlTable table, string key, List<string> errors)
        {
            if (table.TryGetValue(key, out object? value) && value is long l)
            {
                return (int)l;
            }

            errors.Add($"Missing or invalid integer value: {key}.");
            return 0;
        }

        private static void ValidateMappedOption(string value, Dictionary<string, string> map, string fieldName, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            if (!map.ContainsKey(value))
            {
                errors.Add($"Invalid value for {fieldName}: '{value}'.");
            }
        }

        private static async Task<TomlMap?> LoadTomlMapAsync()
        {
            try
            {
                using var stream = await FileSystem.OpenAppPackageFileAsync(TomlMapPath);
                using var reader = new StreamReader(stream);
                string content = await reader.ReadToEndAsync();

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

        private sealed class TomlMap
        {
            public Dictionary<string, string> SortBy { get; set; } = [];
            public Dictionary<string, string> Direction { get; set; } = [];
            public Dictionary<string, string> MaskCombination { get; set; } = [];
            public Dictionary<string, string> WhatToSort { get; set; } = [];
        }
    }
}
