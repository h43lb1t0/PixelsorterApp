using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using PixelsorterApp.Models;

namespace PixelsorterApp.Pages;

public partial class ExampleGallery : ContentPage
{
	private List<ExampleImageConfig>? _images;

	public ExampleGallery()
	{
		InitializeComponent();
		GalleryContainer.Scrolled += OnGalleryScrolled;
		LoadExamplesAsync();
	}

    /// <summary>
    /// Loads the example image configurations from a JSON file and sets the ItemsSource of the GalleryContainer.
    /// </summary>
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
				_images = configRoot.Images;

				// Initially only the first card shows dots (the first visible card, before any scroll)
				UpdateDotVisibility(lastVisibleIndex: 0);

				MainThread.BeginInvokeOnMainThread(() =>
				{
					GalleryContainer.ItemsSource = _images;
				});
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Error loading examples: {ex.Message}");
		}
	}

	private void OnGalleryScrolled(object? sender, ItemsViewScrolledEventArgs e)
	{
		UpdateDotVisibility(e.LastVisibleItemIndex);
	}

	/// <summary>
	/// Shows dots only on the bottom-most visible card, provided it is not the last card.
	/// The moment a new card scrolls into view, the previous card's dots disappear.
	/// </summary>
	private void UpdateDotVisibility(int lastVisibleIndex)
	{
		if (_images == null) return;
		int lastCardIndex = _images.Count - 1;

		for (int i = 0; i < _images.Count; i++)
		{
			// Dots visible only on the last currently-visible card, and never on the last card overall
			_images[i].ShowDots = (i == lastVisibleIndex) && (i < lastCardIndex);
		}
	}
}

/// <summary>
/// Represents the configuration for an example image, including properties for sorting, masking, and image sources.
/// </summary>
public class ExampleImageConfig : INotifyPropertyChanged
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

	private bool _showDots = false;

	/// <summary>
	/// True when this card is the bottom-most visible card and is not the last card.
	/// Drives the pulsing dots indicator below the card.
	/// </summary>
	[JsonIgnore]
	public bool ShowDots
	{
		get => _showDots;
		set
		{
			if (_showDots == value) return;
			_showDots = value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowDots)));
		}
	}

	public event PropertyChangedEventHandler? PropertyChanged;

    [JsonIgnore]
	public ImageSource BeforeImageSource => ImageSource.FromFile($"ExampleImages/before_{Id}.webp");
	
	[JsonIgnore]
	public ImageSource AfterImageSource => ImageSource.FromFile($"ExampleImages/after_{Id}.webp");

	[JsonIgnore]
	public string SortBySafe => SortBy != null
		? SortStringLocalizer.LocalizeSortBy(SortBy)
		: PixelsorterApp.Resources.Languages.AppStrings.common_None;
	
	[JsonIgnore]
	public string SortDirectionSafe => SortDirection != null
		? SortStringLocalizer.LocalizeDirection(SortDirection.Replace(" ", string.Empty))
		: PixelsorterApp.Resources.Languages.AppStrings.common_None;
	
	[JsonIgnore]
	public string CannyThresholdFormatted => CannyThreshold.HasValue ? $"{CannyThreshold}%" : PixelsorterApp.Resources.Languages.AppStrings.common_NA;
	
	[JsonIgnore]
	public string SubjectPaddingFormatted => SubjectPadding.HasValue ? $"{SubjectPadding}px" : PixelsorterApp.Resources.Languages.AppStrings.common_NA;
	
	[JsonIgnore]
	public string WhatToSortSafe => WhatToSort ?? PixelsorterApp.Resources.Languages.AppStrings.common_None;
	
	[JsonIgnore]
	public string MaskCombineSafe => MaskCombine ?? PixelsorterApp.Resources.Languages.AppStrings.common_None;
}

public class ExampleImageConfigsRoot
{
	[JsonPropertyName("images")]
	public required List<ExampleImageConfig> Images { get; set; }
}
