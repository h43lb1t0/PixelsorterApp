using System.Text.Json;
using System.Text.Json.Serialization;

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
					GalleryContainer.ItemsSource = configRoot.Images;
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
	public required string Id { get; set; }
	public required string SortBy { get; set; }
	public required string SortDirection { get; set; }
	public bool Canny { get; set; }
	public int? CannyThreshold { get; set; }
	public bool SubjectMask { get; set; }
	public int? SubjectPadding { get; set; }
	public string WhatToSort { get; set; } = string.Empty;
	public string MaskCombine { get; set; } = string.Empty;

    [JsonIgnore]
	public ImageSource BeforeImageSource => ImageSource.FromFile($"ExampleImages/before_{Id}.jpg");
	
	[JsonIgnore]
	public ImageSource AfterImageSource => ImageSource.FromFile($"ExampleImages/after_{Id}.png");

	[JsonIgnore]
	public string SortBySafe => SortBy ?? "None";
	
	[JsonIgnore]
	public string SortDirectionSafe => SortDirection ?? "None";
	
	[JsonIgnore]
	public string CannyThresholdFormatted => CannyThreshold.HasValue ? $"{CannyThreshold}%" : "N/A";
	
	[JsonIgnore]
	public string SubjectPaddingFormatted => SubjectPadding.HasValue ? $"{SubjectPadding}px" : "N/A";
	
	[JsonIgnore]
	public string WhatToSortSafe => WhatToSort ?? "None";
	
	[JsonIgnore]
	public string MaskCombineSafe => MaskCombine ?? "None";
}

public class ExampleImageConfigsRoot
{
	[JsonPropertyName("images")]
	public required List<ExampleImageConfig> Images { get; set; }
}
