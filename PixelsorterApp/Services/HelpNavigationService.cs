using PixelsorterApp.Pages;
using PixelsorterApp.Popups;
using System.Diagnostics;
using UXDivers.Popups.Services;
using Microsoft.Extensions.DependencyInjection;

namespace PixelsorterApp.Services;

public sealed class HelpNavigationService : IHelpNavigationService
{

    private readonly IPresetNavigationService presetNavigationService;
    private readonly IServiceProvider serviceProvider;


    public HelpNavigationService(IPresetNavigationService presetNavigationService, IServiceProvider serviceProvider)
    {
        this.presetNavigationService = presetNavigationService;
        this.serviceProvider = serviceProvider;
    }
    public async Task ShowHelpMenuAsync()
    {
        var currentPage = Shell.Current?.CurrentPage;
        if (currentPage is null)
        {
            return;
        }

        var popup = new NavigationPopup();
        popup.OptionSelected += (sender, selection) =>
        {
            _ = NavigateToSelectionAsync(currentPage, selection);
        };

        var parameters = new Dictionary<string, object?>
        {
            { "Title", PixelsorterApp.Resources.Languages.AppStrings.Navigation_Title },
            { "Options", new List<(string Id, string Label, string Icon)> { 
                ("Help", PixelsorterApp.Resources.Languages.AppStrings.PageName_HelpPage, MaterialSymbolsFont.Help), 
                ("Gallery", PixelsorterApp.Resources.Languages.AppStrings.PageName_ExampleGallery, MaterialSymbolsFont.Collections), 
                ("Presets", PixelsorterApp.Resources.Languages.AppStrings.PageName_PresetsPage, MaterialSymbolsFont.Tune), 
                ("Licenses", PixelsorterApp.Resources.Languages.AppStrings.PageName_OpenSourceLicenses, MaterialSymbolsFont.Gavel), 
                ("Privacy", PixelsorterApp.Resources.Languages.AppStrings.PageName_PrivacyPolicy, MaterialSymbolsFont.PrivacyTip), 
                ("Settings", PixelsorterApp.Resources.Languages.AppStrings.PageName_Settings, MaterialSymbolsFont.SettingsGear),
            } }
        };

        await IPopupService.Current.PushAsync(popup, parameters);
    }

    private async Task NavigateToSelectionAsync(Page currentPage, string selection)
    {
        switch (selection)
        {
            case "Help":
                await currentPage.Navigation.PushAsync(new HelpPage());
                break;
            case "Gallery":
                await currentPage.Navigation.PushAsync(new ExampleGallery());
                break;
            case "Presets":
                await presetNavigationService.ShowCreatePresetPageAsync();
                break;
            case "Licenses":
                await currentPage.Navigation.PushAsync(new LicensesPage());
                break;
            case "Privacy":
                await currentPage.Navigation.PushAsync(new PrivacyPolicyPage());
                break;
            case "Settings":
                await currentPage.Navigation.PushAsync(serviceProvider.GetRequiredService<SettingsPage>());
                break;
        }
    }
}
