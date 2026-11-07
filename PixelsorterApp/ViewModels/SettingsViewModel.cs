using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalizationResourceManager.Maui;
using System.Globalization;

namespace PixelsorterApp.ViewModels
{
    public sealed partial class SettingsViewModel : BaseViewModel
    {

        private readonly ILocalizationResourceManager localizationResourceManager;

        [ObservableProperty]
        public partial CultureInfo SelectedLanguage { get; set; }

        [RelayCommand]
        private static async Task OpenUrlAsync(string? url)
        {
            if (!string.IsNullOrWhiteSpace(url) && Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                await Launcher.OpenAsync(uri);
            }
        }

        partial void OnSelectedLanguageChanged(CultureInfo value)
        {
            if (value != null)
            {
                localizationResourceManager.CurrentCulture = value;
            }
        }

        public SettingsViewModel(ILocalizationResourceManager localizationResourceManager) 
        {
            this.localizationResourceManager = localizationResourceManager;
            
            var current = localizationResourceManager.CurrentCulture;
            SelectedLanguage = current;
        }
    }
}
