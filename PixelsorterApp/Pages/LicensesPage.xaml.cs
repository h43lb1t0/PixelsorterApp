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
            var generatedFiles = new[]
            {
                "licenses.PixelsorterApp.json",
                "licenses.PixelsorterClassLib.json"
            };

            var packageDict = new Dictionary<string, LicenseInfo>(StringComparer.OrdinalIgnoreCase);

            foreach (var file in generatedFiles)
            {
                var licenses = await TryReadLicensesAsync(file);
                foreach (var license in licenses)
                {
                    if (!packageDict.ContainsKey(license.PackageName))
                    {
                        packageDict[license.PackageName] = license;
                    }
                }
            }

            var supplementalLicenses = await TryReadLicensesAsync("licenses.json");
            foreach (var item in supplementalLicenses)
            {
                if (packageDict.TryGetValue(item.PackageName, out var existing))
                {
                    if (string.IsNullOrWhiteSpace(existing.LicenseType) || existing.LicenseType == PixelsorterApp.Resources.Languages.AppStrings.LicensePage_LicenseInformationUnavailable)
                    {
                        if (!string.IsNullOrWhiteSpace(item.LicenseType) && item.LicenseType != PixelsorterApp.Resources.Languages.AppStrings.LicensePage_LicenseInformationUnavailable)
                        {
                            existing.LicenseType = item.LicenseType;
                        }
                    }

                    if (string.IsNullOrWhiteSpace(existing.LicenseUrl) && !string.IsNullOrWhiteSpace(item.LicenseUrl))
                    {
                        existing.LicenseUrl = item.LicenseUrl;
                    }
                }
                else
                {
                    packageDict[item.PackageName] = item;
                }
            }

            return packageDict.Values
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

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true
            };

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
                        Authors = String.Format(PixelsorterApp.Resources.Languages.AppStrings.LicensePage_ByItemAuthors, item.Authors ?? string.Empty),
                        PackageVersion = item.PackageVersion ?? string.Empty,
                        LicenseType = item.License ?? PixelsorterApp.Resources.Languages.AppStrings.LicensePage_LicenseInformationUnavailable,
                        LicenseUrl = ResolveLicenseUrl(item.LicenseUrl, item.License)
                    })];
            }

            try
            {
                var directLicenses = JsonSerializer.Deserialize<List<LicenseInfo>>(contents, options) ?? [];
                foreach (var item in directLicenses)
                {
                    item.LicenseUrl = ResolveLicenseUrl(item.LicenseUrl, item.LicenseType);
                }
                return directLicenses;
            }
            catch (JsonException)
            {
                return [];
            }
        }

        private static string ResolveLicenseUrl(string? licenseUrl, string? license)
        {
            var trimmedUrl = licenseUrl?.Trim().TrimEnd('/');
            if (string.Equals(trimmedUrl, "https://aka.ms/deprecateLicenseUrl", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(license) && !license.Contains('\n') && !license.Contains('\r') && license.Length <= 100)
                {
                    return $"https://licenses.nuget.org/{license.Trim()}";
                }
            }

            return licenseUrl ?? string.Empty;
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