using PixelsorterApp.Pages;
using PixelsorterApp.Popups;
using System.Diagnostics;
using UXDivers.Popups.Services;

namespace PixelsorterApp.Services;

public sealed class HelpNavigationService : IHelpNavigationService
{

    private readonly IPresetNavigationService presetNavigationService;


    public HelpNavigationService(IPresetNavigationService presetNavigationService)
    {
        this.presetNavigationService = presetNavigationService;
    }
    public async Task ShowHelpMenuAsync()
    {
        var currentPage = Shell.Current?.CurrentPage;
        if (currentPage is null)
        {
            return;
        }

        var popup = new NavigationPopup();
        var parameters = new Dictionary<string, object?>
        {
            { "Message", "Help & Info" },
            { "Options", new List<string> { 
                "Help Page", 
                "Example Gallery", 
                "Presets page", 
                "Open Source Licenses", 
                "Privacy Policy", 
                "Cancel" 
            } }
        };

        var selection = await IPopupService.Current.PushAsync(popup, parameters);

        Debug.WriteLine("Nav choise: " + selection);

        switch (selection)
        {
            case "Help Page":
                await currentPage.Navigation.PushAsync(new HelpPage());
                break;
            case "Example Gallery":
                await currentPage.Navigation.PushAsync(new ExampleGallery());
                break;
            case "Presets page":
                await presetNavigationService.ShowCreatePresetPageAsync();
                break;
            case "Open Source Licenses":
                await currentPage.Navigation.PushAsync(new LicensesPage());
                break;
            case "Privacy Policy":
                await currentPage.Navigation.PushAsync(new PrivacyPolicyPage());
                break;
        }
    }
}
