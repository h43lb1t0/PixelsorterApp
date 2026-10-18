using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;

namespace PixelsorterApp.Views;

public partial class LanguageSettingsView : ContentView
{
	public IRelayCommand<string> OpenUrlCommand => new RelayCommand<string>(async (url) => await Launcher.OpenAsync(!string.IsNullOrEmpty(url) ? url : string.Empty));
    public LanguageSettingsView()
	{
		InitializeComponent();
        BindingContext = this;
    }
}