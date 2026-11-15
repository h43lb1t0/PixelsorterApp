using PixelsorterApp.Pages;

namespace PixelsorterApp.Services;

public sealed class PresetNavigationService(IServiceProvider serviceProvider) : IPresetNavigationService
{
    private readonly IServiceProvider serviceProvider = serviceProvider;

    public async Task ShowCreatePresetPageAsync()
    {
        var currentPage = Shell.Current?.CurrentPage;
        if (currentPage is null)
        {
            return;
        }

        await currentPage.Navigation.PushAsync(serviceProvider.GetRequiredService<PresetsPage>());
    }
}
