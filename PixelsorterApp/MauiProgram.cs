using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using PixelsorterApp.Services;
using PixelsorterApp.ViewModels;
using UraniumUI;

namespace PixelsorterApp
{
    /// <summary>
    /// Configures MAUI app services, fonts, and platform integrations.
    /// </summary>
    public static class MauiProgram
    {
        /// <summary>
        /// Creates and configures the MAUI application.
        /// </summary>
        /// <returns>The configured <see cref="MauiApp"/> instance.</returns>
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder.UseMauiApp<App>().ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            })
            .UseMauiCommunityToolkit()
            .UseUraniumUI()
            .UseUraniumUIMaterial();

            builder.Services.AddSingleton<AppShell>();
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<MainPageViewModel>();
            builder.Services.AddSingleton<IImageProcessingService, ImageProcessingService>();
            builder.Services.AddSingleton<IHelpNavigationService, HelpNavigationService>();

#if ANDROID
            builder.Services.AddSingleton<IGalleryService, Platforms.Android.GalleryService>();
#endif
#if WINDOWS
            builder.Services.AddSingleton<IGalleryService, Platforms.Windows.GalleryService>();
#endif
#if DEBUG
            builder.Logging.AddDebug();
#endif
            return builder.Build();
        }
    }
}