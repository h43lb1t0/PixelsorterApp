namespace PixelsorterApp.Pages;

public partial class HelpPage : ContentPage
{
	public HelpPage()
	{
		InitializeComponent();
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