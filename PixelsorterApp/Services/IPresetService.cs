using PixelsorterApp.Models.Presets;

namespace PixelsorterApp.Services;

/// <summary>
/// Provides methods for discovering, loading, and parsing presets.
/// </summary>
public interface IPresetService
{
    /// <summary>
    /// Gets a dictionary of available preset names to their file paths.
    /// </summary>
    IReadOnlyDictionary<string, string> GetAvailablePresets();

    /// <summary>
    /// Finds the default preset option key.
    /// </summary>
    string? FindDefaultPresetOption(IReadOnlyDictionary<string, string> availablePresets);

    /// <summary>
    /// Asynchronously loads a preset from the specified path and returns its parsed state.
    /// </summary>
    /// <param name="presetPath">The path to the preset file to load.</param>
    /// <returns>The parsed state, or null if loading/parsing fails.</returns>
    Task<PresetState?> LoadPresetAsync(string presetPath);

    /// <summary>
    /// Asynchronously gets the cached TOML map.
    /// </summary>
    /// <returns>The parsed TomlMap, or null if loading fails.</returns>
    Task<TomlMap?> GetTomlMapAsync();
}
