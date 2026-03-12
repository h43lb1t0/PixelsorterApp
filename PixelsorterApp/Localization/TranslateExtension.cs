using Microsoft.Maui.Controls.Xaml;

namespace PixelsorterApp.Localization;

[ContentProperty(nameof(Key))]
public class TranslateExtension : IMarkupExtension<string>
{
    public string Key { get; set; } = string.Empty;

    public string ProvideValue(IServiceProvider serviceProvider)
    {
        return LocalizationManager.GetString(Key);
    }

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
    {
        return ProvideValue(serviceProvider);
    }
}
