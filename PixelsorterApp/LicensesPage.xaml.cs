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
            OpenUrlCommand = new Command<string>(async url =>
            {
                if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
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
            var files = new[]
            {
                "licenses.PixelsorterApp.json",
                "licenses.PixelsorterClassLib.json",
                "licenses.json"
            };

            var allLicenses = new List<LicenseInfo>();

            foreach (var file in files)
            {
                var licenses = await TryReadLicensesAsync(file);
                allLicenses.AddRange(licenses);
            }

            return allLicenses
                .GroupBy(item => $"{item.PackageName}|{item.PackageVersion}", StringComparer.OrdinalIgnoreCase)
                .Select(group => group
                    .OrderByDescending(item => !string.IsNullOrWhiteSpace(item.LicenseUrl))
                    .ThenByDescending(item => item.LicenseType != "License information unavailable")
                    .First())
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