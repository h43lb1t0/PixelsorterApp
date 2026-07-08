using Microsoft.Maui.Controls.Shapes;

namespace PixelsorterApp.Views;

public partial class BeforeAfterView : ContentView
{

    public ImageSource BeforeImageSource
    {
        get => (ImageSource)GetValue(BeforeImageSourceProperty);
        set => SetValue(BeforeImageSourceProperty, value);
    }

    public ImageSource AfterImageSource
    {
        get => (ImageSource)GetValue(AfterImageSourceProperty);
        set => SetValue(AfterImageSourceProperty, value);
    }

    public static readonly BindableProperty BeforeImageSourceProperty =
        BindableProperty.Create(nameof(BeforeImageSource), typeof(ImageSource), typeof(BeforeAfterView), default(ImageSource));

    public static readonly BindableProperty AfterImageSourceProperty =
        BindableProperty.Create(nameof(AfterImageSource), typeof(ImageSource), typeof(BeforeAfterView), default(ImageSource));
    public BeforeAfterView()
    {
        InitializeComponent();
    }

    void BeforeImage_SizeChanged(object sender, EventArgs e)
    {
        // Width isn't known until layout happens, so set initial clip here
        UpdateClip(CompareSlider.Value);
    }

    void OnSliderValueChanged(object sender, ValueChangedEventArgs e)
    {
        UpdateClip(e.NewValue);
    }

    void UpdateClip(double value)
    {
        if (BeforeImage.Width <= 0) return;

        double width = BeforeImage.Width * value;
        double height = BeforeImage.Height;

        BeforeImage.Clip = new RectangleGeometry
        {
            Rect = new Rect(0, 0, width, height)
        };

        DividerLine.TranslationX = width - (DividerLine.WidthRequest / 2);
    }
}