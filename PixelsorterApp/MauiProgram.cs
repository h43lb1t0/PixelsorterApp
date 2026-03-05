using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using UraniumUI;

namespace PixelsorterApp
{
    public static class MauiProgram
    {
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