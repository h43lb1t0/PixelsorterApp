using CommunityToolkit.Mvvm.ComponentModel;
using LocalizationResourceManager.Maui;
using System.Globalization;

namespace PixelsorterApp.ViewModels
{
    public sealed partial class SettingsViewModel : BaseViewModel
    {

        private readonly ILocalizationResourceManager localizationResourceManager;

        public IReadOnlyList<CultureInfo> AvailableLanguages => LocalizationConfig.SupportedCultures;

        [ObservableProperty]
        private CultureInfo selectedLanguage;

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
            this.selectedLanguage = current;
        }
    }
}
