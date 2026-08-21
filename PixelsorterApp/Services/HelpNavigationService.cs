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
            { "Options", new List<(string Label, string Icon)> { 
                (PixelsorterApp.Resources.Languages.AppStrings.PageName_HelpPage, MaterialSymbolsFont.Help), 
                (PixelsorterApp.Resources.Languages.AppStrings.PageName_ExampleGallery, MaterialSymbolsFont.Collections), 
                (PixelsorterApp.Resources.Languages.AppStrings.PageName_PresetsPage, MaterialSymbolsFont.Tune), 
                (PixelsorterApp.Resources.Languages.AppStrings.PageName_OpenSourceLicenses, MaterialSymbolsFont.Gavel), 
                (PixelsorterApp.Resources.Languages.AppStrings.PageName_PrivacyPolicy, MaterialSymbolsFont.PrivacyTip), 
                (PixelsorterApp.Resources.Languages.AppStrings.PageName_Settings, MaterialSymbolsFont.SettingsGear),
            } }
        };

        await IPopupService.Current.PushAsync(popup, parameters);
    }

    private async Task NavigateToSelectionAsync(Page currentPage, string selection)
    {
        if (selection == PixelsorterApp.Resources.Languages.AppStrings.PageName_HelpPage)
        {
            await currentPage.Navigation.PushAsync(new HelpPage());
        }
        else if (selection == PixelsorterApp.Resources.Languages.AppStrings.PageName_ExampleGallery)
        {
            await currentPage.Navigation.PushAsync(new ExampleGallery());
        }
        else if (selection == PixelsorterApp.Resources.Languages.AppStrings.PageName_PresetsPage)
        {
            await presetNavigationService.ShowCreatePresetPageAsync();
        }
        else if (selection == PixelsorterApp.Resources.Languages.AppStrings.PageName_OpenSourceLicenses)
        {
            await currentPage.Navigation.PushAsync(new LicensesPage());
        }
        else if (selection == PixelsorterApp.Resources.Languages.AppStrings.PageName_PrivacyPolicy)
        {
            await currentPage.Navigation.PushAsync(new PrivacyPolicyPage());
        }
        else if (selection == PixelsorterApp.Resources.Languages.AppStrings.PageName_Settings)
        {
            await currentPage.Navigation.PushAsync(serviceProvider.GetRequiredService<SettingsPage>());
        }
    }
}
