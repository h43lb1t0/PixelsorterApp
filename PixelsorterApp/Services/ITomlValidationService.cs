namespace PixelsorterApp.Services
{
    public interface ITomlValidationService
    {
        Task<(bool isValid, string errors)> Validate(string content);

        string Sanitize(string content);
    }
}
