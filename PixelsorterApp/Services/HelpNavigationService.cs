using PixelsorterApp.Pages;

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

        var selection = await currentPage.DisplayActionSheetAsync(
            "Help & Info",
            "Cancel",
            null,
            "Help Page",
            "Presets page",
            "Open Source Licenses",
            "Privacy Policy");

        switch (selection)
        {
            case "Help Page":
                await currentPage.Navigation.PushAsync(new HelpPage());
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
