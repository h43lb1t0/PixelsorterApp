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

    public static readonly BindableProperty SortByProperty =
        BindableProperty.Create(nameof(SortBy), typeof(string), typeof(BeforeAfterView), string.Empty);
        
    public static readonly BindableProperty SortDirectionProperty =
        BindableProperty.Create(nameof(SortDirection), typeof(string), typeof(BeforeAfterView), string.Empty);

    public static readonly BindableProperty CannyMaskingProperty =
        BindableProperty.Create(nameof(CannyMasking), typeof(bool), typeof(BeforeAfterView), false, propertyChanged: OnMaskingChanged);
        
    public static readonly BindableProperty CannyThresholdProperty =
        BindableProperty.Create(nameof(CannyThreshold), typeof(string), typeof(BeforeAfterView), string.Empty);

    public static readonly BindableProperty SubjectMaskingProperty =
        BindableProperty.Create(nameof(SubjectMasking), typeof(bool), typeof(BeforeAfterView), false, propertyChanged: OnMaskingChanged);
        
    public static readonly BindableProperty SubjectPaddingProperty =
        BindableProperty.Create(nameof(SubjectPadding), typeof(string), typeof(BeforeAfterView), string.Empty);
        
    public static readonly BindableProperty WhatToSortProperty =
        BindableProperty.Create(nameof(WhatToSort), typeof(string), typeof(BeforeAfterView), string.Empty);

    public static readonly BindableProperty MaskCombineProperty =
        BindableProperty.Create(nameof(MaskCombine), typeof(string), typeof(BeforeAfterView), string.Empty);

    public string SortBy
    {
        get => (string)GetValue(SortByProperty);
        set => SetValue(SortByProperty, value);
    }
    
    public string SortDirection
    {
        get => (string)GetValue(SortDirectionProperty);
        set => SetValue(SortDirectionProperty, value);
    }

    public bool CannyMasking
    {
        get => (bool)GetValue(CannyMaskingProperty);
        set => SetValue(CannyMaskingProperty, value);
    }
    
    public string CannyThreshold
    {
        get => (string)GetValue(CannyThresholdProperty);
        set => SetValue(CannyThresholdProperty, value);
    }

    public bool SubjectMasking
    {
        get => (bool)GetValue(SubjectMaskingProperty);
        set => SetValue(SubjectMaskingProperty, value);
    }
    
    public string SubjectPadding
    {
        get => (string)GetValue(SubjectPaddingProperty);
        set => SetValue(SubjectPaddingProperty, value);
    }
    
    public string WhatToSort
    {
        get => (string)GetValue(WhatToSortProperty);
        set => SetValue(WhatToSortProperty, value);
    }

    public string MaskCombine
    {
        get => (string)GetValue(MaskCombineProperty);
        set => SetValue(MaskCombineProperty, value);
    }
    
    public bool HasAnyMask => SubjectMasking || CannyMasking;
    public bool HasBothMasks => SubjectMasking && CannyMasking;
    public bool HasOnlySubjectMask => SubjectMasking && !CannyMasking;

    /// <summary>
    /// Called when either the CannyMasking or SubjectMasking properties change. This method updates the HasAnyMask, HasBothMasks, and HasOnlySubjectMask properties accordingly.
    /// </summary>
    /// <param name="bindable"></param>
    /// <param name="oldValue"></param>
    /// <param name="newValue"></param>
    private static void OnMaskingChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is BeforeAfterView view)
        {
            view.OnPropertyChanged(nameof(HasAnyMask));
            view.OnPropertyChanged(nameof(HasBothMasks));
            view.OnPropertyChanged(nameof(HasOnlySubjectMask));
        }
    }

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

    /// <summary>
    /// Updates the clip region of the BeforeImage based on the slider value.
    /// </summary>
    /// <param name="value"></param>
    void UpdateClip(double value)
    {
        if (BeforeImage.Width <= 0) return;

        double width = BeforeImage.Width * value;
        double height = BeforeImage.Height;

        BeforeImage.Clip = new RectangleGeometry
        {
            Rect = new Rect(0, 0, width, height)
        };

        // Keep the rounded-corner clip geometry in sync with the actual container size
        ImageClipGeometry.Rect = new Rect(0, 0, BeforeImage.Width, height);

        DividerLine.TranslationX = width - (DividerLine.WidthRequest / 2);
    }
}