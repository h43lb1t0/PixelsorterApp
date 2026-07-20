namespace PixelsorterApp.Services
{
    public interface IShareService
    {
        Task ShareImage(string filePath, string? text = null);
    }
}
