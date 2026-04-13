using PixelsorterApp.Pages;

namespace PixelsorterApp.Services;

public sealed class PresetNavigationService : IPresetNavigationService
{
    public async Task ShowCreatePresetPageAsync()
    {
        var currentPage = Shell.Current?.CurrentPage;
        if (currentPage is null)
        {
            return;
        }

        await currentPage.Navigation.PushAsync(new PresetPage());
    }
}
