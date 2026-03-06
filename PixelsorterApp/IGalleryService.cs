namespace PixelsorterApp
{
    public interface IGalleryService
    {
        Task<bool> SaveImageAsync(byte[] imageBytes, string fileName);
    }
}
