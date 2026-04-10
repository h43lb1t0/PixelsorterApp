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

    public async Task<string> LoadMarkdownAsync()
    {
        using var stream = await FileSystem.OpenAppPackageFileAsync("helpPageContent.md");
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Load the file from Resources/Raw
        var content = await LoadMarkdownAsync();

        // Assign to the control
        MarkdownDisplay.MarkdownText = content;
    }
}