using System.ComponentModel;
using Microsoft.Maui.Controls.Shapes;
using UraniumUI.Controls;
using UraniumUI.Material.Controls;

namespace PixelsorterApp.Extensions;

/// <summary>
/// Attached behavior that adds a bottom fade gradient and down-chevron pill indicator
/// to <see cref="SelectField"/> dropdown popups when additional options are scrollable below.
/// </summary>
public static class SelectFieldScrollIndicator
{
    private const double IndicatorHeight = 42.0;
    private const double FadeScrollRange = 24.0;
    private const double BottomScrollTolerance = 2.0;

    /// <summary>
    /// Attached bindable property to enable the bottom scroll indicator on a <see cref="SelectField"/>.
    /// </summary>
    public static readonly BindableProperty IsEnabledProperty =
        BindableProperty.CreateAttached(
            "IsEnabled",
            typeof(bool),
            typeof(SelectFieldScrollIndicator),
            false,
            propertyChanged: OnIsEnabledChanged);

    /// <summary>
    /// Gets whether the bottom scroll indicator is enabled for the specified <paramref name="view"/>.
    /// </summary>
    public static bool GetIsEnabled(BindableObject view) =>
        (bool)view.GetValue(IsEnabledProperty);

    /// <summary>
    /// Sets whether the bottom scroll indicator is enabled for the specified <paramref name="view"/>.
    /// </summary>
    public static void SetIsEnabled(BindableObject view, bool value) =>
        view.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not SelectField selectField || selectField.SelectView is not Select selectView)
        {
            return;
        }

        selectView.PropertyChanged -= OnSelectViewPropertyChanged;

        if (newValue is true)
        {
            selectView.PropertyChanged += OnSelectViewPropertyChanged;
        }
    }

    private static void OnSelectViewPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(Select.IsDropDownOpen) || sender is not Select selectView)
        {
            return;
        }

        if (selectView.IsDropDownOpen)
        {
            AttachScrollIndicator(selectView);
        }
    }

    private static void AttachScrollIndicator(Select selectView)
    {
        var page = FindParentPage(selectView);
        if (page is null)
        {
            return;
        }

        var dropDownBorder = page.GetVisualTreeDescendants()
            .OfType<Border>()
            .LastOrDefault(b => b.GetType().Name == "SelectDropDownBorder" && b.Content is ScrollView);

        if (dropDownBorder?.Content is not ScrollView scrollView)
        {
            return;
        }

        var indicatorContainer = CreateIndicatorOverlay(selectView);

        dropDownBorder.Content = null;

        var wrapperGrid = new Grid
        {
            VerticalOptions = LayoutOptions.Start
        };
        wrapperGrid.Children.Add(scrollView);
        wrapperGrid.Children.Add(indicatorContainer);

        dropDownBorder.Content = wrapperGrid;

        void UpdateVisibility()
        {
            double viewportHeight = scrollView.Height;
            double contentHeight = Math.Max(
                scrollView.ContentSize.Height,
                (scrollView.Content as VisualElement)?.Height ?? 0);

            if (viewportHeight <= 0 || contentHeight <= 0)
            {
                indicatorContainer.IsVisible = false;
                indicatorContainer.Opacity = 0;
                return;
            }

            double remainingScroll = contentHeight - (scrollView.ScrollY + viewportHeight);
            if (remainingScroll <= BottomScrollTolerance)
            {
                indicatorContainer.Opacity = 0;
                indicatorContainer.IsVisible = false;
            }
            else
            {
                double opacity = Math.Clamp((remainingScroll - BottomScrollTolerance) / FadeScrollRange, 0.0, 1.0);
                indicatorContainer.Opacity = opacity;
                indicatorContainer.IsVisible = true;
            }
        }

        scrollView.Scrolled += (_, _) => UpdateVisibility();
        scrollView.SizeChanged += (_, _) => UpdateVisibility();

        if (scrollView.Content is VisualElement contentElement)
        {
            contentElement.SizeChanged += (_, _) => UpdateVisibility();
        }

        scrollView.Dispatcher.Dispatch(UpdateVisibility);
    }

    private static View CreateIndicatorOverlay(Select selectView)
    {
        var app = Application.Current;
        bool isDark = app?.RequestedTheme == AppTheme.Dark;

        Color bgColor = selectView.DropDownBackgroundColor
            ?? (isDark
                ? GetResourceColor(app, "SurfaceDark", Color.FromArgb("#201E1C"))
                : Colors.White);

        Color borderColor = selectView.DropDownBorderColor
            ?? (isDark
                ? GetResourceColor(app, "DividerDark", Color.FromArgb("#303030"))
                : GetResourceColor(app, "DividerLight", Color.FromArgb("#E0E0E0")));

        Color accentColor = isDark
            ? GetResourceColor(app, "PrimaryDark", Color.FromArgb("#E53935"))
            : GetResourceColor(app, "Primary", Color.FromArgb("#D32F2F"));

        Color pillBackgroundColor = isDark
            ? Color.FromArgb("#E62A2624")
            : Color.FromArgb("#F2F5EFE6");

        var gradientBox = new BoxView
        {
            HeightRequest = IndicatorHeight,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            InputTransparent = true,
            Background = new LinearGradientBrush(
                new GradientStopCollection
                {
                    new GradientStop(bgColor.WithAlpha(0.0f), 0.0f),
                    new GradientStop(bgColor.WithAlpha(0.82f), 0.55f),
                    new GradientStop(bgColor.WithAlpha(0.98f), 1.0f)
                },
                new Point(0, 0),
                new Point(0, 1))
        };

        var chevronPill = new Border
        {
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.End,
            Margin = new Thickness(0, 0, 0, 4),
            Padding = new Thickness(10, 1),
            BackgroundColor = pillBackgroundColor,
            Stroke = new SolidColorBrush(borderColor),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 10 },
            InputTransparent = true,
            Content = new Label
            {
                FontFamily = "MaterialSymbolsFont",
                Text = MaterialSymbolsFont.ExpandMore,
                FontSize = 16,
                TextColor = accentColor,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                InputTransparent = true
            }
        };

        var container = new Grid
        {
            HeightRequest = IndicatorHeight,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.End,
            InputTransparent = true,
            CascadeInputTransparent = true,
            IsVisible = false,
            Opacity = 0
        };

        container.Children.Add(gradientBox);
        container.Children.Add(chevronPill);

        return container;
    }

    private static Color GetResourceColor(Application? app, string key, Color fallback)
    {
        if (app?.Resources != null && app.Resources.TryGetValue(key, out var value) && value is Color color)
        {
            return color;
        }

        return fallback;
    }

    private static Page? FindParentPage(Element? element)
    {
        while (element is not null)
        {
            if (element is Page page)
            {
                return page;
            }

            element = element.Parent;
        }

        return null;
    }
}
