namespace PixelsorterApp;

public partial class ImageViewer : ContentView
{
    public event EventHandler? ImageTapped;
    public event EventHandler<int>? DisplayedImageIndexChanged;

    public ImageViewer()
    {
        InitializePlatformView();
    }

    partial void InitializePlatformView();

    public partial void ShowImage(string filePath);

    public partial void ClearImages();

    public partial void PrepareForImage();

    protected void OnImageTapped()
    {
        ImageTapped?.Invoke(this, EventArgs.Empty);
    }

    protected void OnDisplayedImageIndexChanged(int index)
    {
        DisplayedImageIndexChanged?.Invoke(this, index);
    }
}
