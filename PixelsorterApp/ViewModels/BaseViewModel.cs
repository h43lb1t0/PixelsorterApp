using CommunityToolkit.Mvvm.ComponentModel;

namespace PixelsorterApp.ViewModels;

/// <summary>
/// Provides basic property change notification helpers for view models.
/// </summary>
public abstract class BaseViewModel : ObservableObject
{
    public static async Task<string> ReadAppPackageTextAsync(string path)
    {
        using var stream = await FileSystem.OpenAppPackageFileAsync(path);
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
}
