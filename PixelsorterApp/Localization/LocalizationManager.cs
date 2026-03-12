using System.Globalization;
using System.Resources;

namespace PixelsorterApp.Localization;

public static class LocalizationManager
{
    private static readonly ResourceManager ResourceManager = new("PixelsorterApp.Resources.Localization.AppResources", typeof(LocalizationManager).Assembly);

    public static string GetString(string key)
    {
        return ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;
    }
}
