using PixelsorterApp.Pages;

namespace PixelsorterApp.Services;

public sealed class HelpNavigationService : IHelpNavigationService
{
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
            "Open Source Licenses",
            "Privacy Policy");

        switch (selection)
        {
            case "Help Page":
                await currentPage.Navigation.PushAsync(new HelpPage());
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
