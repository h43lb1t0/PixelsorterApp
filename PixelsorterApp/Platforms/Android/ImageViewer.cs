using System.Collections.ObjectModel;

namespace PixelsorterApp;

public partial class ImageViewer
{
    private readonly ObservableCollection<ImageSource> _images = new();

    partial void InitializePlatformView()
    {
        _images.Add(ImageSource.FromFile("uploadplaceholder.png"));

        var carousel = new CarouselView
        {
            Loop = false,
            VerticalOptions = LayoutOptions.Fill,
            ItemTemplate = new DataTemplate(() =>
            {
                var image = new Microsoft.Maui.Controls.Image { Aspect = Aspect.AspectFit };
                image.SetBinding(Microsoft.Maui.Controls.Image.SourceProperty, ".");
                var tap = new TapGestureRecognizer();
                tap.Tapped += (_, _) => OnImageTapped();
                image.GestureRecognizers.Add(tap);
                return image;
            }),
            ItemsSource = _images
        };
        carousel.PositionChanged += (_, e) => OnDisplayedImageIndexChanged(e.CurrentPosition);
        Content = carousel;
    }

    public partial void ShowImage(string filePath)
    {
        _images.Add(ImageSource.FromFile(filePath));

        SetHeightFromImage(filePath);

        if (Content is CarouselView carousel)
        {
            if (carousel.ItemsSource is null)
                carousel.ItemsSource = _images;

            var targetIndex = _images.Count - 1;
            Dispatcher.Dispatch(() => carousel.ScrollTo(targetIndex, animate: true)); //Scrolls to latest image
        }
    }

    public partial void ClearImages()
    {
        if (Content is CarouselView carousel)
            carousel.ItemsSource = null;

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
