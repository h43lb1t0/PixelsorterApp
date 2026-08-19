namespace PixelsorterApp.Pages;

using Indiko.Maui.Controls.Markdown.Theming;
using Microsoft.Maui.Graphics;

public partial class HelpPage : ContentPage
{


	public HelpPage()
	{
		InitializeComponent();
        ApplyMarkdownTheme();
	}

    private void ApplyMarkdownTheme()
    {
        var theme = MarkdownThemeDefaults.GitHub.Clone();
        theme.Palette.TextPrimary = (Color)Application.Current!.Resources["TextPrimaryLight"];
        theme.Palette.Background = (Color)Application.Current!.Resources["SurfaceLight"];
        theme.PaletteDark.TextPrimary = (Color)Application.Current!.Resources["SurfaceLight"];
        theme.PaletteDark.Background = (Color)Application.Current!.Resources["TextPrimaryDark"];
        MarkdownDisplay.Theme = theme;
        MarkdownDisplay.UseAppTheme = true;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Assign to the control
        MarkdownDisplay.MarkdownText = String.Format(PixelsorterApp.Resources.Languages.HelpPageStrings.HelpPage_Content,
            PixelsorterApp.Resources.Languages.SortStrings.SortStrings_by_hue,
            PixelsorterApp.Resources.Languages.SortStrings.SortStrings_by_sat,
            PixelsorterApp.Resources.Languages.SortStrings.SortStrings_by_light,
            PixelsorterApp.Resources.Languages.SortStrings.SortStrings_by_warm,
            PixelsorterApp.Resources.Languages.SortStrings.SortStrings_by_cool,
            PixelsorterApp.Resources.Languages.SortStrings.SortStrings_by_chroma,
            PixelsorterApp.Resources.Languages.SortStrings.SortStrings_by_perVib,

            PixelsorterApp.Resources.Languages.SortStrings.SortStrings_by_light,
            PixelsorterApp.Resources.Languages.SortStrings.SortStrings_by_hue,

            PixelsorterApp.Resources.Languages.SortStrings.SortStrings_direction_rlr,
            PixelsorterApp.Resources.Languages.SortStrings.SortStrings_direction_rrl,
            PixelsorterApp.Resources.Languages.SortStrings.SortStrings_direction_ctb,
            PixelsorterApp.Resources.Languages.SortStrings.SortStrings_direction_cbt,
            PixelsorterApp.Resources.Languages.SortStrings.SortStrings_direction_im,
            PixelsorterApp.Resources.Languages.SortStrings.SortStrings_direction_im);
    }
}