using NumSharp;
using PixelsorterClassLib;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Image = PixelsorterClassLib.Image;

namespace PixelsorterApp
{
    public partial class MainPage : ContentPage
    {

        private string imagePath; // Add field to store the file path
        private readonly Mask masker = new Mask();
        private bool useMask = false;
        private readonly Dictionary<string, Func<Rgba32, float>> sortByOptions = SortBy.GetAllSortingCriteria();
        private Dictionary<string, SortDirections> sortDirectionOptions = new();
        private Func<Rgba32, float>? sortingCriterion;
        private SortDirections sortingDirection;
        private string[] sortByOptionNames;
        private string[] sortDirectionOptionNames;
        private readonly List<string> imageCaptions = [];
        private readonly List<string> imagePaths = [];
        private int currentDisplayedImageIndex = -1;
        private int maskPaddingAmount = 15;
        private bool useInvertedMask = false;
        private NDArray? mask = null;
        private NDArray? invertedMask = null;
        private readonly double DESKTOP_IMAGE_HEIGHT = 0.75;


        private void InitializeSortDirectionOptions()
        {
            // Populate the dictionary with every SortDirections enum value
            // keyed by a human-readable name (spaces inserted between capitals).

            foreach (SortDirections dir in Enum.GetValues(typeof(SortDirections)))
            {
                string name = System.Text.RegularExpressions.Regex.Replace(dir.ToString(), "([A-Z])", " $1").Trim();
                sortDirectionOptions[name] = dir;
            }
        }

        private void UpdateSortDirectionPicker()
        {
            string? previousSelection = null;
            if (sortDirection.SelectedIndex >= 0 && sortDirection.SelectedIndex < sortDirectionOptionNames.Length)
            {
                previousSelection = sortDirectionOptionNames[sortDirection.SelectedIndex];
            }

            sortDirectionOptionNames =
            [
                .. sortDirectionOptions.Keys.Where(name => this.useMask || !name.Contains("mask", StringComparison.OrdinalIgnoreCase))
            ];

            sortDirection.ItemsSource = sortDirectionOptionNames;

            int selectedIndex = -1;
            if (!string.IsNullOrEmpty(previousSelection))
            {
                selectedIndex = Array.IndexOf(sortDirectionOptionNames, previousSelection);
            }

            sortDirection.SelectedIndex = selectedIndex >= 0
                ? selectedIndex
                : (sortDirectionOptionNames.Length > 0 ? 0 : -1);

            if (sortDirection.SelectedIndex >= 0)
            {
                sortingDirection = sortDirectionOptions[sortDirectionOptionNames[sortDirection.SelectedIndex]];
            }
        }


        public MainPage()
        {
            InitializeComponent();

            SizeChanged += (_, _) => ApplyImageSizeForCurrentDevice();

            sortBtn.IsVisible = true;
            sortBtn.IsEnabled = false; // Disable the sort button until an image is loaded

            InitializeSortDirectionOptions();
            sortByOptionNames = [.. sortByOptions.Keys];
            sortDirectionOptionNames = [];

            sortBy.ItemsSource = sortByOptionNames;
            sortBy.SelectedIndex = sortByOptionNames.Length > 0 ? 0 : -1;
            sortBy.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(sortBy.SelectedIndex))
                    sortBy_SelectedIndexChanged(s, EventArgs.Empty);
            };

            UpdateSortDirectionPicker();
            sortDirection.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(sortDirection.SelectedIndex))
                    sortDirection_SelectedIndexChanged(s, EventArgs.Empty);
            };

            sortingCriterion = sortByOptionNames.Length > 0 ? sortByOptions[sortByOptionNames[0]] : null;
            sortingDirection = sortDirectionOptionNames.Length > 0 ? sortDirectionOptions[sortDirectionOptionNames[0]] : SortDirections.RowRightToLeft;
            ApplyImageSizeForCurrentDevice();

            imageViewer.DisplayedImageIndexChanged += ImageViewer_DisplayedImageIndexChanged;
        }

        private void ImageViewer_DisplayedImageIndexChanged(object? sender, int index)
        {
            currentDisplayedImageIndex = index;

            if (index >= 0 && index < imageCaptions.Count)
            {
                whatIsThisLabel.Text = imageCaptions[index];
                SemanticProperties.SetDescription(whatIsThisLabel, $"Current image caption: {imageCaptions[index]}");
                SemanticProperties.SetDescription(imageViewer, $"Image preview. {imageCaptions[index]}");
            }
            if (index == 0 && index < imagePaths.Count)
            {
                saveBtn.IsEnabled = false;
            }
            else
            {
                saveBtn.IsEnabled = true;
            }
        }

        private string? GetFocusedImagePath()
        {
            if (imagePaths.Count == 0)
            {
                return null;
            }

            if (currentDisplayedImageIndex >= 0 && currentDisplayedImageIndex < imagePaths.Count)
            {
                return imagePaths[currentDisplayedImageIndex];
            }

            return imagePaths[^1];
        }

        private string BuildSortCaption()
        {
            var sortByText = sortBy.SelectedIndex >= 0 && sortBy.SelectedIndex < sortByOptionNames.Length
                ? sortByOptionNames[sortBy.SelectedIndex]
                : "Unknown";

            var directionText = sortDirection.SelectedIndex >= 0 && sortDirection.SelectedIndex < sortDirectionOptionNames.Length
                ? sortDirectionOptionNames[sortDirection.SelectedIndex]
                : "Unknown";

            return $"Sort by: {sortByText} • Direction: {directionText}";
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            SharedImageBridge.SharedImageReceived += OnSharedImageReceived;

            if (SharedImageBridge.TryConsumePendingImagePath(out var pendingImagePath) && pendingImagePath is not null)
            {
                LoadImageFromPath(pendingImagePath);
            }
        }

        protected override void OnDisappearing()
        {
            SharedImageBridge.SharedImageReceived -= OnSharedImageReceived;
            base.OnDisappearing();
        }

        private void OnSharedImageReceived(string sharedImagePath)
        {
            LoadImageFromPath(sharedImagePath);
        }

        private void LoadImageFromPath(string path)
        {
            this.imagePath = path;
            this.mask = null; // Clear any existing mask when a new image is loaded
            imageCaptions.Clear();
            imagePaths.Clear();
            imageCaptions.Add("Original image");
            imagePaths.Add(path);
            currentDisplayedImageIndex = 0;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                imageViewer.PrepareForImage();
                ApplyImageSizeForCurrentDevice();
                imageViewer.ClearImages();
                imageViewer.ShowImage(path);
                whatIsThisLabel.Text = imageCaptions[0];
                SemanticProperties.SetDescription(whatIsThisLabel, $"Options used for the current image: {imageCaptions[0]}");
                SemanticProperties.SetDescription(imageViewer, "Image preview. Original image. Double tap to load another image.");

                whatIsThisLabel.IsVisible = true;
                sortBtn.IsEnabled = true;
                saveBtn.IsVisible = false;
                SemanticScreenReader.Announce("Image loaded. Ready to sort.");
            });
        }

        private void ApplyImageSizeForCurrentDevice()
        {
            if (DeviceInfo.Idiom == DeviceIdiom.Desktop)
            {
                imagePreviewBorder.MaximumHeightRequest = double.PositiveInfinity;
                imageViewer.MaximumHeightRequest = this.Height > 0
                    ? this.Height * DESKTOP_IMAGE_HEIGHT
                    : double.PositiveInfinity;
                return;
            }

            imagePreviewBorder.MaximumHeightRequest = double.PositiveInfinity;
            imageViewer.MaximumHeightRequest = double.PositiveInfinity;
        }

        private void UseLoadingOverlay(String text)
        {
            loadingOverlayLabel.Text = text;
            loadingIndicator.IsRunning = true;
            loadingOverlay.IsVisible = true;
            SemanticScreenReader.Announce(text);
        }



        private async void LoadImage_Clicked(object sender, EventArgs e)
        {
            var results = await MediaPicker.PickPhotosAsync();

            foreach (var file in results)
            {
                LoadImageFromPath(file.FullPath);
                break;
            }
        }

        private string? sortedImagePath; // Path to the temporarily saved sorted image

        private async void sortBtn_Clicked(object sender, EventArgs e)
        {
            if (this.imagePath is null) // Check if we have a file path
                return;



            sortBtn.IsEnabled = false; // Disable the sort button while sorting is in progress
            saveBtn.IsEnabled = false;
            UseLoadingOverlay("Sorting...");
            imageViewer.IsEnabled = false;

            try
            {
                // Run the CPU/IO-bound sorting on a background thread; save to a temporary file so the UI can be updated safely.
                sortedImagePath = Path.Combine(FileSystem.CacheDirectory, $"sorted_temp_{Guid.NewGuid()}.png");
                await Task.Run(async () =>
                {
                    if ((this.useMask && this.mask is null))
                    {
                        (this.mask, this.invertedMask) = await masker.GetMaskAsync(this.imagePath, this.maskPaddingAmount, this.useInvertedMask);
                    }

                    var imgData = Sorter.SortImage(
                                Image.LoadImage(this.imagePath),
                                sortingCriterion ?? sortByOptions.Values.First(),
                                sortingDirection,
                                this.useMask ? (this.useInvertedMask ? this.invertedMask : this.mask) : null
                            );
                    using var foo = Image.NdarrayToImgData(imgData);
                    foo.SaveAsPng(sortedImagePath);
                });

                // Back on the UI thread — safe to update UI elements.
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    imageViewer.ShowImage(sortedImagePath);
                    var caption = BuildSortCaption();
                    imageCaptions.Add(caption);
                    imagePaths.Add(sortedImagePath);
                    currentDisplayedImageIndex = imagePaths.Count - 1;
                    whatIsThisLabel.Text = caption;
                    SemanticProperties.SetDescription(whatIsThisLabel, $"Current image caption: {caption}");
                    SemanticProperties.SetDescription(imageViewer, $"Image preview. {caption}");
                    saveBtn.IsVisible = true;
                    saveBtn.IsEnabled = true; // Enable the save button now that sorting is complete
                    SemanticScreenReader.Announce("Sorting complete. Preview updated.");
                });
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., show an alert)
                await DisplayAlertAsync("Error", $"An error occurred: {ex.Message}", "OK");
                SemanticScreenReader.Announce($"Error: {ex.Message}");
            }
            finally
            {
                loadingIndicator.IsRunning = false;
                loadingOverlay.IsVisible = false;
                sortBtn.IsEnabled = true; // Re-enable the sort button after sorting is complete
                imageViewer.IsEnabled = true;

            }
        }

        /// <summary>
        /// Saves the image to the Gallery/Photos album on the user's device. This is necessary because the image is currently saved to a temporary location that may not be accessible to the user.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void SaveBtn_Clicked(object sender, EventArgs e)
        {
            var focusedImagePath = GetFocusedImagePath();

            if (string.IsNullOrEmpty(focusedImagePath) || !File.Exists(focusedImagePath))
            {
                await DisplayAlertAsync("Error", "No image available to save.", "OK");
                SemanticScreenReader.Announce("No image available to save.");
                return;
            }

            var imageBytes = await File.ReadAllBytesAsync(focusedImagePath);

            var galleryService = Application.Current?.Handler?.MauiContext?.Services?.GetService<IGalleryService>();

            if (galleryService != null)
            {
                var fileName = $"pixelsorted_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                var result = await galleryService.SaveImageAsync(imageBytes, fileName);

                if (result)
                {
                    await DisplayAlertAsync("Success", "Image saved to gallery", "OK");
                    SemanticScreenReader.Announce("Image saved to gallery.");
                }
                else
                {
                    await DisplayAlertAsync("Error", "Failed to save image to gallery", "OK");
                    SemanticScreenReader.Announce("Failed to save image to gallery.");
                }
            }
            else
            {
                await DisplayAlertAsync("Error", "Gallery service is not available", "OK");
                SemanticScreenReader.Announce("Gallery service is not available.");
            }
        }

        private async void useMasking_Toggled(object sender, ToggledEventArgs e)
        {

            bool netAccess = checkNetworkAcces();
            if (e.Value && !netAccess && !masker.IsModelDownloaded)
            {
                useMasking.IsToggled = false;
                return;
            }
            
            if (!Preferences.Get("MaskingLicenseAccepted", false) && e.Value)
            {
                var response = await DisplayAlertAsync(
                    "Masking Feature License",
                    "The masking feature uses a pre-trained machine learning model that was created by a third party. By enabling this feature, you accept that you won't use pictures created or edited by this tool for any commercial purposes. For further information, go to the license page.",
                    "Accept",
                    "Don't accept"
                    );
                Preferences.Set("MaskingLicenseAccepted", response);

                if (!response)
                {
                    useMasking.IsToggled = false;
                    return;
                }
            }

            if (!masker.IsModelDownloaded && netAccess)
            {
                UseLoadingOverlay("Downloading...");
                try
                {
                    await Task.Run(() =>
                    {
                        _ = masker.DownloadModel();
                    });
                }
                catch (Exception)
                {
                    await DisplayAlertAsync(
                        "Download failed",
                        "The masking model could not be downloaded. Please check your internet connection and try again.",
                        "OK");
                    useMasking.IsToggled = false;
                    return;
                }
                finally
                {
                    loadingIndicator.IsRunning = false;
                    loadingOverlay.IsVisible = false;
                }
            }


            this.useMask = e.Value;
            maskPadding.IsVisible = e.Value;
            UpdateSortDirectionPicker();
        }

        private bool checkNetworkAcces()
        {
            NetworkAccess accessType = Connectivity.Current.NetworkAccess;

            if (accessType != NetworkAccess.Internet)
            {
                _ = DisplayAlertAsync("No Internet Connection", "An internet connection is required to use the masking feature. Please connect to the internet and try again.", "OK");
                return false;
            }
            return true;
        }

        private void sortBy_SelectedIndexChanged(object sender, EventArgs e)
        {
            int selectedIndex = sortBy.SelectedIndex;
            if (selectedIndex >= 0 && selectedIndex < sortByOptionNames.Length)
            {
                var selectedOption = sortByOptionNames[selectedIndex];
                // Update the sorting criterion based on the selected option
                sortingCriterion = sortByOptions[selectedOption];
            }
        }

        private void sortDirection_SelectedIndexChanged(object sender, EventArgs e)
        {
            int selectedIndex = sortDirection.SelectedIndex;
            if (selectedIndex >= 0 && selectedIndex < sortDirectionOptionNames.Length)
            {
                var selectedOption = sortDirectionOptionNames[selectedIndex];
                sortingDirection = sortDirectionOptions[selectedOption];
            }
        }

        private async void LicensesBtn_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new LicensesPage());
        }

        private void maskPadding_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not UraniumUI.Material.Controls.TextField entry) return;

            string newText = e.NewTextValue ?? string.Empty;
            string numericText = new string(newText.Where(char.IsDigit).ToArray());

            if (newText != numericText)
            {
                entry.Text = numericText;
                return;
            }

            if (int.TryParse(numericText, out int padding))
            {
                this.maskPaddingAmount = padding;
            }
        }

        private void whatToSort_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            if (sender == sortBackgroundRadio && e.Value)
            {
                this.useInvertedMask = false;
            }
            else if (sender == sortForegroundRadio && e.Value)
            {
                this.useInvertedMask = true;
            }
        }
    }
}
