using System.Text.Json;
using System.Text.Json.Serialization;
using PixelsorterApp.Views;

namespace PixelsorterApp.Pages;

public partial class ExampleGallery : ContentPage
{
	public ExampleGallery()
	{
		InitializeComponent();
		LoadExamplesAsync();
	}

	private async void LoadExamplesAsync()
	{
		try
		{
			using var stream = await FileSystem.OpenAppPackageFileAsync("ExampleImages/exampleImageConfigs.json");
			using var reader = new StreamReader(stream);
			var json = await reader.ReadToEndAsync();
			
			var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
			var configRoot = JsonSerializer.Deserialize<ExampleImageConfigsRoot>(json, options);

			if (configRoot?.Images != null)
			{
				MainThread.BeginInvokeOnMainThread(() =>
				{
					foreach (var config in configRoot.Images)
					{
						var beforeAfterView = new BeforeAfterView
						{
							HorizontalOptions = LayoutOptions.Center,
							VerticalOptions = LayoutOptions.Start,
							Margin = new Thickness(0, 0, 0, 32),
							BeforeImageSource = ImageSource.FromFile($"ExampleImages/before_{config.Id}.jpg"),
							AfterImageSource = ImageSource.FromFile($"ExampleImages/after_{config.Id}.png"),
							SortBy = config.SortBy ?? "None",
							SortDirection = config.SortDirection ?? "None",
							CannyMasking = config.Canny,
							CannyThreshold = config.CannyThreshold.HasValue ? $"{config.CannyThreshold}%" : "N/A",
							SubjectMasking = config.SubjectMask,
							SubjectPadding = config.SubjectPadding.HasValue ? $"{config.SubjectPadding}px" : "N/A",
							WhatToSort = config.WhatToSort ?? "None",
							MaskCombine = config.MaskCombine ?? "None"
						};

						GalleryContainer.Children.Add(beforeAfterView);
					}
				});
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Error loading examples: {ex.Message}");
		}
	}
}

public class ExampleImageConfig
{
	public string Id { get; set; }
	public string SortBy { get; set; }
	public string SortDirection { get; set; }
	public bool Canny { get; set; }
	public int? CannyThreshold { get; set; }
	public bool SubjectMask { get; set; }
	public int? SubjectPadding { get; set; }
	public string WhatToSort { get; set; }
	public string MaskCombine { get; set; }
}

public class ExampleImageConfigsRoot
{
	[JsonPropertyName("images")]
	public List<ExampleImageConfig> Images { get; set; }
}
