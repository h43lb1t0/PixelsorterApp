using System.Text.Json;
using System.Windows.Input;

namespace PixelsorterApp
{
    public partial class LicensesPage : ContentPage
    {
        public ICommand OpenUrlCommand { get; }

        public LicensesPage()
        {
            InitializeComponent();
            OpenUrlCommand = new Command(static async parameter =>
            {
                if (parameter is string url && Uri.TryCreate(url, UriKind.Absolute, out var uri))
                {
                    SemanticScreenReader.Announce($"Opening {uri.Host}");
                    await Launcher.OpenAsync(uri);
                }
            });
            BindingContext = this;
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object? sender, EventArgs e)
        {
            var licenses = await GetLicensesAsync();
            BindableLayout.SetItemsSource(LicensesContainer, licenses);
        }

        public async Task<IReadOnlyList<LicenseInfo>> GetLicensesAsync()
        {
            // Load licenses.json first for prioritization
            var prioritizedLicenses = await TryReadLicensesAsync("licenses.json");
            var prioritizedDict = prioritizedLicenses.ToDictionary(
                item => $"{item.PackageName}|{item.PackageVersion}",
                item => item,
                StringComparer.OrdinalIgnoreCase);

            var generatedFiles = new[]
            {
                "licenses.PixelsorterApp.json",
                "licenses.PixelsorterClassLib.json"
            };

            foreach (var file in generatedFiles)
            {
                var licenses = await TryReadLicensesAsync(file);
                foreach (var license in licenses)
                {
                    var key = $"{license.PackageName}|{license.PackageVersion}";
                    // Only add if not already present from licenses.json
                    if (!prioritizedDict.ContainsKey(key))
                    {
                        prioritizedDict[key] = license;
                    }
                }
            }

            return prioritizedDict.Values
                .OrderBy(item => item.PackageName)
                .ToList();
        }

        private async Task<IReadOnlyList<LicenseInfo>> TryReadLicensesAsync(string fileName)
        {
            string contents;

            try
            {
                using var stream = await FileSystem.OpenAppPackageFileAsync(fileName);
                using var reader = new StreamReader(stream);
                contents = await reader.ReadToEndAsync();
            }
            catch (FileNotFoundException)
            {
                return [];
            }

            if (string.IsNullOrWhiteSpace(contents))
            {
                return [];
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            var generatedLicenses = JsonSerializer.Deserialize<List<GeneratedLicenseInfo>>(contents, options);
            if (generatedLicenses is { Count: > 0 }
                && generatedLicenses.Any(item =>
                    !string.IsNullOrWhiteSpace(item.PackageId)
                    || !string.IsNullOrWhiteSpace(item.PackageVersion)
                    || !string.IsNullOrWhiteSpace(item.License)))
            {
                return [.. generatedLicenses
                    .Select(item => new LicenseInfo
                    {
                        PackageName = item.PackageId ?? string.Empty,
                        Authors = $"by {item.Authors ?? string.Empty}",
                        PackageVersion = item.PackageVersion ?? string.Empty,
                        LicenseType = item.License ?? "License information unavailable",
                        LicenseUrl = item.LicenseUrl ?? string.Empty
                    })];
            }

            try
            {
                return JsonSerializer.Deserialize<List<LicenseInfo>>(contents, options) ?? [];
            }
            catch (JsonException)
            {
                return [];
            }
        }

        private sealed class GeneratedLicenseInfo
        {
            public string? PackageId { get; set; }
            public string? Authors { get; set; }
            public string? PackageVersion { get; set; }
            public string? License { get; set; }
            public string? LicenseUrl { get; set; }
        }
    }
}