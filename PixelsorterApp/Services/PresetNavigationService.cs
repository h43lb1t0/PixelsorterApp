using Microsoft.Extensions.DependencyInjection;
using PixelsorterApp.Pages;

namespace PixelsorterApp.Services;

public sealed class PresetNavigationService : IPresetNavigationService
{
   private readonly IServiceProvider serviceProvider;

    public PresetNavigationService(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

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
