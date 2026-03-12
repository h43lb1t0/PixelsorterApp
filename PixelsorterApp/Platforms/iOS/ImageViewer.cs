namespace PixelsorterApp;

public partial class ImageViewer
{
    private ImageButton _image = null!;

    partial void InitializePlatformView()
    {
        _image = new ImageButton
        {
            Source = "uploadplaceholder.png",
            Aspect = Aspect.AspectFit,
            BackgroundColor = Colors.Transparent,
            Padding = 0
        };
        _image.Clicked += (_, _) => OnImageTapped();
        SemanticProperties.SetDescription(_image, "Image preview");
        SemanticProperties.SetHint(_image, "Press Enter to load a new image");
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
