namespace PixelsorterApp;

public partial class ImageViewer
{
    private Microsoft.Maui.Controls.Image _image = null!;

    partial void InitializePlatformView()
    {
        _image = new Microsoft.Maui.Controls.Image
        {
            Source = "uploadplaceholder.png",
            Aspect = Aspect.AspectFit,
        };
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => OnImageTapped();
        _image.GestureRecognizers.Add(tap);
        Content = _image;
    }

    public partial void ShowImage(string filePath)
    {
        _image.Source = ImageSource.FromFile(filePath);
    }

    public partial void ClearImages()
    {
        _image.Source = "uploadplaceholder.png";
    }

    public partial void PrepareForImage()
    {
        HeightRequest = -1;
    }
}
