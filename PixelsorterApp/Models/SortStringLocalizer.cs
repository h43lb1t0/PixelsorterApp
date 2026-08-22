using PixelsorterApp.Resources.Languages;

namespace PixelsorterApp.Models;

/// <summary>
/// Maps internal ClassLib sort-by and direction keys to localized display strings
/// from the <see cref="AppStrings"/> resource file.
/// </summary>
public static class SortStringLocalizer
{
    private static readonly Dictionary<string, Func<string>> SortByMap = new(StringComparer.Ordinal)
    {
        ["Hue"] = () => AppStrings.SortStrings_by_hue,
        ["Saturation"] = () => AppStrings.SortStrings_by_sat,
        ["Lightness"] = () => AppStrings.SortStrings_by_light,
        ["Warmth"] = () => AppStrings.SortStrings_by_warm,
        ["Coolness"] = () => AppStrings.SortStrings_by_cool,
        ["Chroma"] = () => AppStrings.SortStrings_by_chroma,
        ["PerceivedVibrancy"] = () => AppStrings.SortStrings_by_perVib,
    };

    private static readonly Dictionary<string, Func<string>> DirectionMap = new(StringComparer.Ordinal)
    {
        ["RowLeftToRight"] = () => AppStrings.SortStrings_direction_rlr,
        ["RowRightToLeft"] = () => AppStrings.SortStrings_direction_rrl,
        ["ColumnTopToBottom"] = () => AppStrings.SortStrings_direction_ctb,
        ["ColumnBottomToTop"] = () => AppStrings.SortStrings_direction_cbt,
        ["IntoMask"] = () => AppStrings.SortStrings_direction_im,
    };

    private static readonly Dictionary<string, Func<string>> MaskMap = new(StringComparer.Ordinal)
    {
        ["Background"] = () => AppStrings.SubjectMaskOptionsView_WhatToSort_Background,
        ["Foreground"] = () => AppStrings.SubjectMaskOptionsView_WhatToSort_Foreground,
        ["Add"] = () => AppStrings.SubjectMaskOptionsView_HowToCombineMasks_Add,
        ["Subtract"] = () => AppStrings.SubjectMaskOptionsView_HowToCombineMasks_Subtract,
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

    /// <summary>
    /// Returns the localized displayname for all the strings about masks
    /// </summary>
    /// <returns></returns>
    public static string LocalizeMasks(string key)
        => MaskMap.TryGetValue(key, out var getter) ? getter() : key;
}
