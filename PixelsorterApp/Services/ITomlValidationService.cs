using PixelsorterApp.Models.Presets;

namespace PixelsorterApp.Services
{
    public interface ITomlValidationService
    {
        Task<(bool isValid, string errors)> Validate(string content, TomlMap? map);

        string Sanitize(string content);
    }
}
