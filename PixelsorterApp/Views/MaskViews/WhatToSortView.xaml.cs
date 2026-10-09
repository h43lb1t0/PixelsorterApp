namespace PixelsorterApp.Views;

public partial class WhatToSortView : ContentView
{

    public static readonly BindableProperty OptionOneProperty =
        BindableProperty.Create(
            propertyName: nameof(OptionOne),
            returnType: typeof(string),
            declaringType: typeof(WhatToSortView),
            defaultValue: string.Empty);


    public string OptionOne
    {
        get => (string)GetValue(OptionOneProperty);
        set => SetValue(OptionOneProperty, value);
    }

    public static readonly BindableProperty OptionOneSelectedProperty =
        BindableProperty.Create(
            propertyName: nameof(OptionOneSelected),
            returnType: typeof(bool),
            declaringType: typeof(WhatToSortView),
            defaultValue: true);

    public bool OptionOneSelected
    {
        get => (bool)GetValue(OptionOneSelectedProperty);
        set => SetValue(OptionOneSelectedProperty, value);
    }

    public static readonly BindableProperty OptionTwoProperty =
        BindableProperty.Create(
            propertyName: nameof(OptionTwo),
            returnType: typeof(string),
            declaringType: typeof(WhatToSortView),
            defaultValue: string.Empty);

    public string OptionTwo
    {
        get => (string)GetValue(OptionTwoProperty);
        set => SetValue(OptionTwoProperty, value);
    }

    public static readonly BindableProperty OptionTwoSelectedProperty =
        BindableProperty.Create(
            propertyName: nameof(OptionTwoSelected),
            returnType: typeof(bool),
            declaringType: typeof(WhatToSortView),
            defaultValue: false);

    public bool OptionTwoSelected
    {
        get => (bool)GetValue(OptionTwoSelectedProperty);
        set => SetValue(OptionTwoSelectedProperty, value);
    }

    public WhatToSortView()
    {
        InitializeComponent();
    }
}