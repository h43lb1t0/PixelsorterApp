using NumSharp;
using PixelsorterClassLib.Core;
using PixelsorterClassLib.Masks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.PixelFormats;
using UraniumUI.Material.Controls;
using Image = PixelsorterClassLib.Core.Image;

namespace PixelsorterApp
{
    public partial class MainPage : ContentPage
    {

        private string? imagePath; // Add field to store the file path
        private readonly BackgroundMask backgroundMasker = new();
        private readonly CannyMask cannyMasker = new();
        private bool useSubjectMask = false;
        private bool useCanny = false;
        private readonly Dictionary<string, Func<Hsl, float>> sortByOptions = SortBy.GetAllSortingCriteria();
        private readonly Dictionary<string, SortDirections> sortDirectionOptions = [];
        private Func<Hsl, float>? sortingCriterion;
        private SortDirections sortingDirection;
        private readonly string[] sortByOptionNames;
        private string[] sortDirectionOptionNames;
        private readonly List<string> imageCaptions = [];
        private readonly List<string> imagePaths = [];
        private int currentDisplayedImageIndex = -1;
        private int subjectMaskPaddingAmount = 15;
        private int cannyThreashold = 30;
        private bool useInvertedMask = false;
        private bool useSubtractMasks = true;
        private NDArray? backgroundMask = null;
        private NDArray? invertedBackgroundMask = null;
        private NDArray? cannyMask = null;
        private NDArray? invertedCannyMask = null;
        private readonly double DESKTOP_IMAGE_HEIGHT = 0.75;


        /// <summary>
        /// Initializes the available sort direction options by populating a dictionary with the string representations
        /// of the sort directions.
        /// </summary>
        /// <remarks>This method prepares the sortDirectionOptions dictionary for use in UI elements or
        /// other components that require a mapping between human-readable sort direction names and their corresponding
        /// enumeration values. It should be called before accessing or displaying sort direction options to ensure the
        /// dictionary is properly populated.</remarks>
        private void InitializeSortDirectionOptions()
        {

            foreach (SortDirections dir in Enum.GetValues(typeof(SortDirections)))
            {
                string name = System.Text.RegularExpressions.Regex.Replace(dir.ToString(), "([A-Z])", " $1").Trim();
                sortDirectionOptions[name] = dir;
            }
        }

        /// <summary>
        /// Updates the sort direction picker to reflect the current sorting options and selection state.
        /// </summary>
        /// <remarks>This method filters the available sorting options based on the current mask usage
        /// setting and updates the selected index accordingly. If the previous selection is no longer valid, the first
        /// available option will be selected. The sorting direction is also updated based on the selected
        /// option.</remarks>
        private void UpdateSortDirectionPicker()
        {
            string? previousSelection = null;
            if (sortDirection.SelectedIndex >= 0 && sortDirection.SelectedIndex < sortDirectionOptionNames.Length)
            {
                previousSelection = sortDirectionOptionNames[sortDirection.SelectedIndex];
            }

            sortDirectionOptionNames =
            [
                .. sortDirectionOptions.Keys.Where(name => this.useSubjectMask || this.useCanny || !name.Contains("mask", StringComparison.OrdinalIgnoreCase))
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
                    SortBy_SelectedIndexChanged(s, EventArgs.Empty);
            };

            UpdateSortDirectionPicker();
            sortDirection.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(sortDirection.SelectedIndex))
                    SortDirection_SelectedIndexChanged(s, EventArgs.Empty);
            };

            sortingCriterion = sortByOptionNames.Length > 0 ? sortByOptions[sortByOptionNames[0]] : null;
            sortingDirection = sortDirectionOptionNames.Length > 0 ? sortDirectionOptions[sortDirectionOptionNames[0]] : SortDirections.RowRightToLeft;
            ApplyImageSizeForCurrentDevice();

            imageViewer.DisplayedImageIndexChanged += ImageViewer_DisplayedImageIndexChanged;
        }

        /// <summary>
        /// Handles changes to the displayed image index in the image viewer, updating the current image index and
        /// related UI elements accordingly.
        /// </summary>
        /// <remarks>When the displayed image index changes, this method updates the image caption label
        /// and accessibility descriptions to reflect the new image. The save button is disabled when the first image is
        /// displayed and enabled for all other images.</remarks>
        /// <param name="sender">The source of the event, typically the image viewer control that triggered the index change.</param>
        /// <param name="index">The new index of the displayed image. Must be greater than or equal to 0 and less than the total number of
        /// available images.</param>
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

        /// <summary>
        /// Gets the file path of the currently focused image in the image collection.
        /// </summary>
        /// <remarks>If the current displayed image index is out of range, the path of the last image in
        /// the collection is returned.</remarks>
        /// <returns>The file path of the currently displayed image if the collection is not empty; otherwise, null.</returns>
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

        /// <summary>
        /// Constructs a formatted string that indicates the current sorting criteria and direction.
        /// </summary>
        /// <remarks>If the selected index for either the sort criteria or direction is invalid, 'Unknown'
        /// will be displayed for that part of the caption.</remarks>
        /// <returns>A string representing the sorting criteria and direction, formatted as 'Sort by: {sortByText} • Direction:
        /// {directionText}'.</returns>
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

        /// <summary>
        /// Loads an image from the specified file path and prepares the image viewer to display it.
        /// </summary>
        /// <remarks>This method resets any existing image masks, clears previous image captions and
        /// paths, and updates the user interface to reflect the newly loaded image. It also sets the appropriate UI
        /// elements to indicate that a new image is ready for further actions such as sorting.</remarks>
        /// <param name="path">The file path of the image to load. This must be a valid path to an image file.</param>
        private void LoadImageFromPath(string path)
        {
            this.imagePath = path;
            this.backgroundMask = null; // Clear any existing mask when a new image is loaded
            this.cannyMask = null;
            this.invertedBackgroundMask = null;
            this.invertedCannyMask = null;
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

        /// <summary>
        /// Adjusts the maximum height of the image viewer and its containing border to optimize image display for the
        /// current device type.
        /// </summary>
        /// <remarks>For desktop devices, the image viewer's maximum height is set relative to the current
        /// height of the containing element, providing a tailored viewing experience. For non-desktop devices, both the
        /// image viewer and its border allow unlimited height, enabling flexible image display across various device
        /// form factors.</remarks>
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

        /// <summary>
        /// Displays a loading overlay with the specified message to indicate that a process is in progress.
        /// </summary>
        /// <remarks>This method activates the loading overlay, starts the loading indicator animation,
        /// and announces the message for accessibility purposes.</remarks>
        /// <param name="text">The message to display on the loading overlay, providing context to the user about the ongoing operation.</param>
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


        private async Task<bool> CreateSubjectMask()
        {
            if (this.backgroundMask is not null)
            {
                return true;
            }
            else if (!String.IsNullOrEmpty(this.imagePath))
            {
                if (backgroundMasker.IsReadyToUse)
                {
                    try
                    {
                        (this.backgroundMask, this.invertedBackgroundMask) = await backgroundMasker.GetMaskAsync(this.imagePath, this.subjectMaskPaddingAmount);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlertAsync("Error", $"An error occurred while creating the subject mask: {ex.Message}", "OK");
                        SemanticScreenReader.Announce($"Error creating subject mask: {ex.Message}");
                        return false;
                    }
                }
                return false;
            }
            else
            {
                return false;
            }
        }

        private async Task<bool> CreateCannyMask()
        {
            if (this.cannyMask is not null)
            {
                return true;
            }
            else if (!String.IsNullOrEmpty(this.imagePath))
            {
                try
                {
                    (this.cannyMask, this.invertedCannyMask) = await cannyMasker.GetMaskAsync(this.imagePath, this.cannyThreashold);
                    return true;
                }
                catch (Exception ex)
                {
                    await DisplayAlertAsync("Error", $"An error occurred while creating the Canny mask: {ex.Message}", "OK");
                    SemanticScreenReader.Announce($"Error creating Canny mask: {ex.Message}");
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private async void SortBtn_Clicked(object sender, EventArgs e)
        {
            if (this.imagePath is null) // Check if we have a file path
                return;



            ToggleUiForSorting(false);
            UseLoadingOverlay("Sorting...");

            try
            {
                // Run the CPU/IO-bound sorting on a background thread; save to a temporary file so the UI can be updated safely.
                sortedImagePath = Path.Combine(FileSystem.CacheDirectory, $"sorted_temp_{Guid.NewGuid()}.png");
                await Task.Run(async () =>
                {
                    NDArray? maskToUse = null;
                    if (this.useSubjectMask && this.useCanny)
                    {
                        bool subjectIsReady = await CreateSubjectMask();
                        bool cannyIsReady = await CreateCannyMask();



                        if (this.useSubtractMasks)
                        {
                            maskToUse = (subjectIsReady && cannyIsReady) ? MaskCombiner.SubtractMasks(this.backgroundMask!, this.invertedCannyMask!) : null;
                        }
                        else if (!this.useSubtractMasks)
                        {
                            maskToUse = (subjectIsReady && cannyIsReady) ? MaskCombiner.AddMasks(this.backgroundMask!, this.cannyMask!) : null;
                        }
                    }
                    else if (this.useCanny)
                    {
                        maskToUse = await CreateCannyMask() ? this.cannyMask : null;
                    } else if (this.useSubjectMask)
                    {
                        bool isReady = await CreateSubjectMask();
                        maskToUse = isReady ? (this.useInvertedMask ? this.invertedBackgroundMask : this.backgroundMask) : null;
                    }

                    var imgData = Sorter.SortImage(
                                Image.LoadImage(this.imagePath),
                                sortingCriterion ?? sortByOptions.Values.First(),
                                sortingDirection,
                                maskToUse
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
                ToggleUiForSorting(true);

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

        private async void useSubjectMaskingSwitch_Toggled(object sender, ToggledEventArgs e)
        {

            bool netAccess = CheckNetworkAcces();
            if (e.Value && !netAccess && !backgroundMasker.IsReadyToUse)
            {
                useSubjectMaskingSwitch.IsToggled = false;
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
                    useSubjectMaskingSwitch.IsToggled = false;
                    return;
                }
            }

            if (!backgroundMasker.IsReadyToUse && netAccess && e.Value)
            {
                UseLoadingOverlay("Downloading...");
                sortBtn.IsEnabled = false;
                try
                {
                    await backgroundMasker.DownloadModel();
                }
                catch (Exception)
                {
                    await DisplayAlertAsync(
                        "Download failed",
                        "The masking model could not be downloaded. Please check your internet connection and try again.",
                        "OK");
                    useSubjectMaskingSwitch.IsToggled = false;
                    return;
                }
                finally
                {
                    loadingIndicator.IsRunning = false;
                    loadingOverlay.IsVisible = false;
                    sortBtn.IsEnabled = true;
                }
            }


            this.useSubjectMask = e.Value;
            UpdateSortDirectionPicker();
        }

        /// <summary>
        /// Determines whether the device currently has internet access required for the masking feature.
        /// </summary>
        /// <remarks>If no internet connection is detected, an alert is displayed to inform the user that
        /// internet access is required to use the masking feature.</remarks>
        /// <returns>true if an internet connection is available; otherwise, false.</returns>
        private bool CheckNetworkAcces()
        {
            NetworkAccess accessType = Connectivity.Current.NetworkAccess;

            if (accessType != NetworkAccess.Internet)
            {
                _ = DisplayAlertAsync("No Internet Connection", "An internet connection is required to use the masking feature. Please connect to the internet and try again.", "OK");
                return false;
            }
            return true;
        }


        private void SortBy_SelectedIndexChanged(object sender, EventArgs e)
        {
            int selectedIndex = sortBy.SelectedIndex;
            if (selectedIndex >= 0 && selectedIndex < sortByOptionNames.Length)
            {
                var selectedOption = sortByOptionNames[selectedIndex];
                // Update the sorting criterion based on the selected option
                sortingCriterion = sortByOptions[selectedOption];
            }
        }

        private void SortDirection_SelectedIndexChanged(object sender, EventArgs e)
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

        private async void PrivacyPolicyBtn_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new PrivacyPolicyPage());
        }

        private async void HelpMenuBtn_Clicked(object sender, EventArgs e)
        {
            var selection = await DisplayActionSheetAsync("Help", "Cancel", null, "Open Source Licenses", "Privacy Policy");

            if (selection == "Open Source Licenses")
            {
                await Navigation.PushAsync(new LicensesPage());
            }
            else if (selection == "Privacy Policy")
            {
                await Navigation.PushAsync(new PrivacyPolicyPage());
            }
        }


        private void cannyThreashold_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not UraniumUI.Material.Controls.TextField entry) return;

            var textfield = (TextField)sender;

            if (!textfield.IsValid)
            {
                return;
            }

            string newText = e.NewTextValue ?? string.Empty;
            string numericText = new string(newText.Where(char.IsDigit).ToArray());

            if (newText != numericText)
            {
                entry.Text = numericText;
                return;
            }

            if (int.TryParse(numericText, out int padding))
            {
                this.cannyThreashold = padding;
            }
            this.cannyMask = null; // Clear existing masks to ensure they are regenerated with the new padding
            this.invertedCannyMask = null;

        }

        private void SubjectMaskPadding_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not UraniumUI.Material.Controls.TextField entry) return;
            var textfield = (TextField)sender;

            if (!textfield.IsValid)
            {
                return;
            }

            string newText = e.NewTextValue ?? string.Empty;
            string numericText = new string(newText.Where(char.IsDigit).ToArray());


            if (newText != numericText)
            {
                entry.Text = numericText;
                return;
            }

            if (int.TryParse(numericText, out int padding))
            {
                this.subjectMaskPaddingAmount = padding;
            }
            this.backgroundMask = null; // Clear existing masks to ensure they are regenerated with the new padding
            this.invertedBackgroundMask = null;
  
        }

        private void WhatToSort_CheckedChanged(object sender, CheckedChangedEventArgs e)
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

        private void UseCanny_Toggled(object sender, ToggledEventArgs e)
        {
            this.useCanny = e.Value;
            UpdateSortDirectionPicker();
        }

        private void HowToCombine_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            if (sender == subMasksRadio && e.Value)
            {
                this.useSubtractMasks = true;
            }
            else if (sender == addMasksRadio && e.Value)
            {
                this.useSubtractMasks = false;
            }
        }

        private void ToggleUiForSorting(bool state)
        {
            sortBtn.IsEnabled = state;
            imageViewer.IsEnabled = state;
            sortBy.IsEnabled = state;
            sortDirection.IsEnabled = state;
            useSubjectMaskingSwitch.IsEnabled = state;
            useCannySwitch.IsEnabled = state;
            subjectMaskPadding.IsEnabled = state;
            sortBackgroundRadio.IsEnabled = state;
            sortForegroundRadio.IsEnabled = state;
            subMasksRadio.IsEnabled = state;
            addMasksRadio.IsEnabled = state;
            saveBtn.IsEnabled = state && currentDisplayedImageIndex > 0 && currentDisplayedImageIndex < imagePaths.Count;
            
        }
    }
}
