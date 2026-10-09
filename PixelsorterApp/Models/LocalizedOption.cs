namespace PixelsorterApp.Models;

/// <summary>
/// Pairs an internal key with its localized display name for use in UI pickers.
/// </summary>
/// <param name="Key">The internal key used for dictionary lookups and logic.</param>
/// <param name="DisplayName">The localized string shown to the user.</param>
public record LocalizedOption(string Key, string DisplayName)
{
    /// <summary>
    /// Returns the localized display name so pickers show the translated text.
    /// </summary>
    public override string ToString() => DisplayName;
}
