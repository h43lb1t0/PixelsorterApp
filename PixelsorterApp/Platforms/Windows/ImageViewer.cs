using Microsoft.Maui.Controls.Shapes;

namespace PixelsorterApp;

public partial class ImageViewer
{
    private ImageButton _image = null!;

    partial void InitializePlatformView()
    {
        _image = new ImageButton
        {
            Source = PlaceholderImage,
            Aspect = Aspect.AspectFit,
            BackgroundColor = Colors.Transparent,
            CornerRadius = (int)ImageCornerRadius,
            Padding = 0
        };
        _image.Clicked += (_, _) => OnImageTapped();
        SemanticProperties.SetDescription(_image, "Image preview");
        SemanticProperties.SetHint(_image, "Press Enter to load a new image");
        Content = new Border
        {
            Padding = 0,
            StrokeThickness = 0,
            Stroke = Brush.Transparent,
            BackgroundColor = Colors.Transparent,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(ImageCornerRadius) },
            Content = _image
        };
    }

    public partial void ShowImage(string filePath)
    {
        _image.Source = ImageSource.FromFile(filePath);
    }

    public partial void ClearImages()
    {
        _image.Source = PlaceholderImage;
    }

    public partial void PrepareForImage()
    {
        HeightRequest = -1;
    }
}
