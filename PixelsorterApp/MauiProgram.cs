using CommunityToolkit.Maui;
using Indiko.Maui.Controls.Markdown;
using Microsoft.Extensions.Logging;
using PixelsorterApp.Services;
using PixelsorterApp.ViewModels;
using UraniumUI;
using UXDivers.Popups.Maui;

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
                fonts.AddFont("SpaceGrotesk-Regular.ttf", "SpaceGroteskRegular");
                fonts.AddFont("SpaceGrotesk-Bold.ttf", "SpaceGroteskBold");
                fonts.AddFont("JetBrainsMono-Regular.ttf", "JetBrainsMonoRegular");
                fonts.AddFont("DMSans-Regular.ttf", "DMSansRegular");
                fonts.AddFont("DMSans-SemiBold.ttf", "DMSansSemiBold");
                fonts.AddFont("MaterialIcons-Regular.ttf", "MaterialSymbolsFont");
            })
            .UseMauiCommunityToolkit()
            .UseUraniumUI()
            .UseUraniumUIMaterial()
            .UseMarkdownView()
            .UseUXDiversPopups();

            builder.Services.AddSingleton<AppShell>();
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<Pages.PresetsPage>();
            builder.Services.AddSingleton<MainPageViewModel>();
            builder.Services.AddTransient<PresetsPageViewModel>();
            builder.Services.AddSingleton<IImageProcessingService, ImageProcessingService>();
            builder.Services.AddSingleton<ITomlValidationService, TomlValidationService>();
            builder.Services.AddSingleton<IPresetService, PresetService>();
            builder.Services.AddSingleton<IHelpNavigationService, HelpNavigationService>();
            builder.Services.AddSingleton<IPresetNavigationService, PresetNavigationService>();
            builder.Services.AddSingleton<IShareService, ShareService>();

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