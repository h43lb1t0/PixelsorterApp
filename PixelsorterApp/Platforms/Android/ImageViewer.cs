using System.Collections.ObjectModel;
using Microsoft.Maui.Controls.Shapes;

namespace PixelsorterApp;

public partial class ImageViewer
{
    private readonly ObservableCollection<ImageSource> _images = [];
    private CarouselView _carousel = null!;

    partial void InitializePlatformView()
    {
        _images.Add(ImageSource.FromFile(PlaceholderImage));

        _carousel = new CarouselView
        {
            Loop = false,
            VerticalOptions = LayoutOptions.Fill,
            ItemTemplate = new DataTemplate(() =>
            {
                var image = new Microsoft.Maui.Controls.Image { Aspect = Aspect.AspectFit };
                image.SetBinding(Microsoft.Maui.Controls.Image.SourceProperty, ".");
                SemanticProperties.SetDescription(image, "Image preview");
                SemanticProperties.SetHint(image, "Double tap to load a new image");

                var tap = new TapGestureRecognizer();
                tap.Tapped += (_, _) => OnImageTapped();
                image.GestureRecognizers.Add(tap);

                return new Border
                {
                    Padding = 0,
                    StrokeThickness = 0,
                    Stroke = Brush.Transparent,
                    BackgroundColor = Colors.Transparent,
                    StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(ImageCornerRadius) },
                    Content = image
                };
            }),
            ItemsSource = _images
        };
        _carousel.PositionChanged += (_, e) => OnDisplayedImageIndexChanged(e.CurrentPosition);

        Content = _carousel;
    }

    public partial void ShowImage(string filePath)
    {
        var newImage = ImageSource.FromFile(filePath);
        SetHeightFromImage(filePath);

        Dispatcher.Dispatch(() =>
        {
            _images.Add(newImage);

            if (_carousel is not null)
            {
                _carousel.ItemsSource ??= _images;

                _carousel.CurrentItem = newImage;
            }
        });

    }

    public partial void ClearImages()
    {
        _carousel?.ItemsSource = null;

        _images.Clear();
    }

    public partial void PrepareForImage()
    {
        //Set autosizing
        HeightRequest = -1;
    }

    private void SetHeightFromImage(string filePath)
    {
        try
        {
            var options = new Android.Graphics.BitmapFactory.Options { InJustDecodeBounds = true };
            Android.Graphics.BitmapFactory.DecodeFile(filePath, options);

            if (options.OutWidth > 0 && options.OutHeight > 0)
            {
                var availableWidth = Width > 0
                    ? Width
                    : DeviceDisplay.Current.MainDisplayInfo.Width / DeviceDisplay.Current.MainDisplayInfo.Density;
                HeightRequest = availableWidth * ((double)options.OutHeight / options.OutWidth);
            }
        }
        catch
        {
            // Fall back to a screen-relative default if dimensions cannot be read
            var display = DeviceDisplay.Current.MainDisplayInfo;
            HeightRequest = display.Height / display.Density * 0.4;
        }
    }

}
