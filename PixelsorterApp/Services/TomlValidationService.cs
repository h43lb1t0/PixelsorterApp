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

        /// <summary>
        /// Validates the provided TOML content against required settings and rules.
        /// </summary>
        /// <remarks>This method checks for the presence and validity of required settings, enforces value
        /// constraints, and ensures that dependencies are met before enabling certain features. If validation fails,
        /// the returned error string contains one or more messages describing the issues found.</remarks>
        /// <param name="content">The TOML content to validate. This parameter cannot be null or empty.</param>
        /// <returns>A tuple containing a boolean that indicates whether the TOML content is valid, and a string with error
        /// messages if validation fails.</returns>
        public async Task<(bool isValid, string errors)> Validate(string content)
        {

            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(content))
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
                model = TomlSerializer.Deserialize<TomlTable>(content, new TomlSerializerOptions());
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
            int cannyThreashold = GetInt(cannyOptions, "threshold", errors);
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
                errors.Add("canny_options.threshold must be in range (0, 100).");
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

        /// <summary>
        /// Retrieves a child table with the specified name from the given root TOML table.
        /// </summary>
        /// <remarks>If the specified table name does not exist in the root table or is not a valid TOML
        /// table, an error message is added to the errors list.</remarks>
        /// <param name="root">The root TOML table from which to retrieve the child table. Cannot be null.</param>
        /// <param name="tableName">The name of the child table to retrieve. This value is case-sensitive.</param>
        /// <param name="errors">A list to which an error message is added if the specified table is missing or not a valid TOML table.
        /// Cannot be null.</param>
        /// <returns>The TOML table corresponding to the specified table name if found; otherwise, an empty TOML table.</returns>
        private static TomlTable GetTable(TomlTable root, string tableName, List<string> errors)
        {
            if (root.TryGetValue(tableName, out object? tableValue) && tableValue is TomlTable table)
            {
                return table;
            }

            errors.Add($"Missing or invalid table: [{tableName}].");
            return new TomlTable();
        }

        /// <summary>
        /// Retrieves the string value associated with the specified key from the provided TOML table.
        /// </summary>
        /// <remarks>If the key is missing or the value is not a string, an error message is added to the
        /// errors list.</remarks>
        /// <param name="table">The TOML table from which to retrieve the string value. This parameter must not be null.</param>
        /// <param name="key">The key whose associated string value is to be retrieved. This parameter must not be null.</param>
        /// <param name="errors">A list that will be populated with an error message if the key does not exist in the table or if the value
        /// is not a valid string. This parameter must not be null.</param>
        /// <returns>The string value associated with the specified key if found and valid; otherwise, an empty string.</returns>
        private static string GetString(TomlTable table, string key, List<string> errors)
        {
            if (table.TryGetValue(key, out object? value) && value is string s)
            {
                return s;
            }

            errors.Add($"Missing or invalid string value: {key}.");
            return string.Empty;
        }

        /// <summary>
        /// Retrieves a Boolean value associated with the specified key from the provided TOML table.
        /// </summary>
        /// <remarks>If the key is not found or the value is not a Boolean, an error message is added to
        /// the errors list.</remarks>
        /// <param name="table">The TOML table from which to retrieve the Boolean value.</param>
        /// <param name="key">The key that identifies the Boolean value to retrieve.</param>
        /// <param name="errors">A list to which an error message is added if the key is missing or the value is not a valid Boolean.</param>
        /// <returns>true if the specified key exists in the table and its value is a Boolean; otherwise, false.</returns>
        private static bool GetBool(TomlTable table, string key, List<string> errors)
        {
            if (table.TryGetValue(key, out object? value) && value is bool b)
            {
                return b;
            }

            errors.Add($"Missing or invalid boolean value: {key}.");
            return false;
        }

        /// <summary>
        /// Retrieves the integer value associated with the specified key from the provided TomlTable.
        /// </summary>
        /// <remarks>If the key is not present in the table or the value is not of type long, an error
        /// message is added to the errors list.</remarks>
        /// <param name="table">The TomlTable instance from which to retrieve the integer value.</param>
        /// <param name="key">The key that identifies the integer value to retrieve from the table.</param>
        /// <param name="errors">A list that is populated with an error message if the key is missing or the value is not a valid integer.</param>
        /// <returns>The integer value associated with the specified key, or 0 if the key does not exist or the value is not a
        /// valid integer.</returns>
        private static int GetInt(TomlTable table, string key, List<string> errors)
        {
            if (table.TryGetValue(key, out object? value) && value is long l)
            {
                return (int)l;
            }

            errors.Add($"Missing or invalid integer value: {key}.");
            return 0;
        }

       /// <summary>
       /// Validates that the specified value exists as a key in the provided mapping and records an error if it does
       /// not.
       /// </summary>
       /// <remarks>No validation is performed if the value is null or white space. This method is
       /// typically used to validate user input or configuration values against a predefined set of allowed
       /// options.</remarks>
       /// <param name="value">The value to validate. This parameter is not validated if it is null or consists only of white-space
       /// characters.</param>
       /// <param name="map">A dictionary containing the set of valid values as keys. The method checks whether the specified value exists
       /// in this dictionary.</param>
       /// <param name="fieldName">The name of the field being validated. Used to identify the field in any generated error messages.</param>
       /// <param name="errors">A list to which error messages are added if validation fails. The method appends an error message if the
       /// value is not found in the mapping.</param>
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

       /// <summary>
       /// Sanitizes the specified string by normalizing line endings and formatting mode declarations for consistency.
       /// </summary>
       /// <remarks>This method replaces all Windows-style line endings (\r\n) and carriage returns (\r)
       /// with Unix-style line endings (\n). It also reformats lines declaring a mode so that they use the syntax 'mode
       /// = "value"'.</remarks>
       /// <param name="content">The input string to sanitize. May contain Windows-style line endings and unformatted mode declarations.</param>
       /// <returns>A string with Unix-style line endings and consistently formatted mode declarations.</returns>
        public string Sanitize(string content)
        {
            content = content.Replace("\r\n", "\n").Replace('\r', '\n');

            return Regex.Replace(
            content,
            @"(?m)^\s*mode\s+\""(?<value>[^\""\r\n]+)\""\s*$",
            "mode = \"${value}\"");
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
