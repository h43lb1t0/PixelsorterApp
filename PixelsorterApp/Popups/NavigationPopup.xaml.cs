using System.Windows.Input;
using UXDivers.Popups.Maui;
using UXDivers.Popups.Services;

using System.Collections.Generic;

namespace PixelsorterApp.Popups;

public partial class NavigationPopup : PopupResultPage<String>
{
    private readonly ICommand SetResultCommand;

    public event EventHandler<string>? OptionSelected;

    public NavigationPopup()
    {
        InitializeComponent();
        SetResultCommand = new Command(async result =>
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            var selection = result?.ToString();
            SetResult(selection);
            if (!string.IsNullOrEmpty(selection))
            {
                OptionSelected?.Invoke(this, selection);
            }
            await IPopupService.Current.PopAsync(this);
        });
    }


    public override void OnNavigatedTo(IReadOnlyDictionary<string, object?> parameters)
    {
        base.OnNavigatedTo(parameters);

        navItemsContainer.Children.Clear();

        if (parameters.TryGetValue("Title", out var title))
        {
            TitleLabel.Text = title?.ToString() ?? PixelsorterApp.Resources.Languages.NavigationStrings.Navigation;
        }

        if (parameters.TryGetValue("Options", out var options) && options is IEnumerable<(string Label, string Icon)> navOptions)
        {
            var optionsList = new List<(string Label, string Icon)>(navOptions);
            for (int i = 0; i < optionsList.Count; i++)
            {
                var isLast = i == optionsList.Count - 1;
                navItemsContainer.Add(CreateNavItem(optionsList[i].Label, optionsList[i].Icon, isLast));
            }
        }
    }

    private View CreateNavItem(string label, string icon, bool isLast)
    {
        if (Application.Current == null) return new VerticalStackLayout();

        var container = new VerticalStackLayout { Spacing = 0 };

        // The tappable row
        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition(new GridLength(40)),   // Icon column
                new ColumnDefinition(GridLength.Star),       // Text column
                new ColumnDefinition(new GridLength(32)),   // Chevron column
            },
            Padding = new Thickness(20, 14),
            BackgroundColor = Colors.Transparent,
        };

        // Leading icon
        var iconLabel = new Label
        {
            FontFamily = "MaterialSymbolsFont",
            Text = icon,
            FontSize = 22,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center,
        };
        iconLabel.SetAppThemeColor(Label.TextColorProperty,
            (Color)Application.Current.Resources["Primary"],
            (Color)Application.Current.Resources["PrimaryDark"]);
        Grid.SetColumn(iconLabel, 0);

        // Text label
        var textLabel = new Label
        {
            Text = label,
            FontSize = 15,
            FontFamily = "DMSansRegular",
            VerticalOptions = LayoutOptions.Center,
        };
        textLabel.SetAppThemeColor(Label.TextColorProperty,
            (Color)Application.Current.Resources["TextPrimaryLight"],
            (Color)Application.Current.Resources["TextPrimaryDark"]);
        Grid.SetColumn(textLabel, 1);

        // Trailing chevron
        var chevron = new Label
        {
            FontFamily = "MaterialSymbolsFont",
            Text = MaterialSymbolsFont.ChevronRight,
            FontSize = 20,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center,
        };
        chevron.SetAppThemeColor(Label.TextColorProperty,
            (Color)Application.Current.Resources["TextSecondaryLight"],
            (Color)Application.Current.Resources["TextSecondaryDark"]);
        Grid.SetColumn(chevron, 2);

        row.Add(iconLabel);
        row.Add(textLabel);
        row.Add(chevron);

        // Tap gesture
        var tapGesture = new TapGestureRecognizer
        {
            Command = SetResultCommand,
            CommandParameter = label,
        };
        row.GestureRecognizers.Add(tapGesture);

        // Pointer-over visual feedback via PointerGestureRecognizer
        var pointerGesture = new PointerGestureRecognizer();
        pointerGesture.PointerEntered += (s, e) =>
        {
            if (Application.Current.RequestedTheme == AppTheme.Dark)
                row.BackgroundColor = Color.FromArgb("#15FFFFFF");
            else
                row.BackgroundColor = Color.FromArgb("#0A000000");
        };
        pointerGesture.PointerExited += (s, e) =>
        {
            row.BackgroundColor = Colors.Transparent;
        };
        row.GestureRecognizers.Add(pointerGesture);

        container.Add(row);

        // Separator line (except after the last item)
        if (!isLast)
        {
            var separator = new BoxView
            {
                HeightRequest = 1,
                Margin = new Thickness(60, 0, 20, 0), // Indent to align with text, not icon
            };
            separator.SetAppThemeColor(BoxView.ColorProperty,
                (Color)Application.Current.Resources["DividerLight"],
                (Color)Application.Current.Resources["DividerDark"]);
            separator.Opacity = 0.6;

            container.Add(separator);
        }

        return container;
    }

    private async void CloseButton_Clicked(object? sender, EventArgs e)
    {
        await IPopupService.Current.PopAsync(this);
    }
}