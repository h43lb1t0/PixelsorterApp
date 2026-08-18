using PixelsorterApp.Resources.Languages;

namespace PixelsorterApp.Models;

/// <summary>
/// Maps internal ClassLib sort-by and direction keys to localized display strings
/// from the <see cref="SortStrings"/> resource file.
/// </summary>
public static class SortStringLocalizer
{
    private static readonly Dictionary<string, Func<string>> SortByMap = new(StringComparer.Ordinal)
    {
        ["Hue"] = () => SortStrings.SortStrings_by_hue,
        ["Saturation"] = () => SortStrings.SortStrings_by_sat,
        ["Lightness"] = () => SortStrings.SortStrings_by_light,
        ["Warmth"] = () => SortStrings.SortStrings_by_warm,
        ["Coolness"] = () => SortStrings.SortStrings_by_cool,
        ["Chroma"] = () => SortStrings.SortStrings_by_chroma,
        ["PerceivedVibrancy"] = () => SortStrings.SortStrings_by_perVib,
    };

    private static readonly Dictionary<string, Func<string>> DirectionMap = new(StringComparer.Ordinal)
    {
        ["RowLeftToRight"] = () => SortStrings.SortStrings_direction_rlr,
        ["RowRightToLeft"] = () => SortStrings.SortStrings_direction_rrl,
        ["ColumnTopToBottom"] = () => SortStrings.SortStrings_direction_ctb,
        ["ColumnBottomToTop"] = () => SortStrings.SortStrings_direction_cbt,
        ["IntoMask"] = () => SortStrings.SortStrings_direction_im,
    };

    /// <summary>
    /// Returns the localized display name for a sort-by key (e.g. "Hue" → "Farbton" in German).
    /// Falls back to the raw key if no mapping is found.
    /// </summary>
    public static string LocalizeSortBy(string key)
        => SortByMap.TryGetValue(key, out var getter) ? getter() : key;

    /// <summary>
    /// Returns the localized display name for a direction enum name (e.g. "RowLeftToRight" → "Reihe Links nach Rechts").
    /// Falls back to the raw key if no mapping is found.
    /// </summary>
    public static string LocalizeDirection(string enumName)
        => DirectionMap.TryGetValue(enumName, out var getter) ? getter() : enumName;
}
