namespace PixelsorterApp;

public partial class ImageViewer : ContentView
{
    public event EventHandler? ImageTapped;

    public ImageViewer()
    {
        InitializePlatformView();
    }

    partial void InitializePlatformView();

    public partial void showImage(string filePath);

    public partial void clearImages();

    public partial void prepareForImage();

    protected void OnImageTapped()
    {
        ImageTapped?.Invoke(this, EventArgs.Empty);
    }
}
