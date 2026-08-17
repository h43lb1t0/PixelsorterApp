using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

using System;
using System.Collections.Generic;
using System.Text;
using PixelsorterApp.Services;

namespace PixelsorterApp.ViewModels
{
    public sealed partial class SettingsViewModel : BaseViewModel
    {
        private readonly ILanguageServices languageServices;

        [ObservableProperty]
        public partial string CurrentLanguage { get; set; }

        private readonly Dictionary<string, string> AvilableLanguages;

        public ObservableCollection<String> AvilableLanguagesNames { get; set; }
        public SettingsViewModel(ILanguageServices languageServices) 
        {
            this.languageServices = languageServices;
            AvilableLanguages = this.languageServices.GetAvilableLanguages();
            CurrentLanguage = this.languageServices.GetCurrentLanguage();
            AvilableLanguagesNames = [.. AvilableLanguages.Keys];
        }
    }
}
