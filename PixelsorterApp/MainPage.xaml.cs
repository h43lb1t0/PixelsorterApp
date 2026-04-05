using CommunityToolkit.Maui.Extensions;
using NumSharp;
using PixelsorterApp.Extensions;
using PixelsorterApp.ViewModels;
using PixelsorterClassLib.Core;
using PixelsorterClassLib.Masks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.ColorSpaces;
using Color = Microsoft.Maui.Graphics.Color;
using Image = PixelsorterClassLib.Core.Image;

namespace PixelsorterApp
{
    public partial class MainPage : ContentPage
    {
        // other
        private readonly double DESKTOP_IMAGE_HEIGHT = 0.75;
        private readonly MainPageViewModel viewModel;

        // image
        private string? imagePath;

        // Masker
        private readonly BackgroundMask backgroundMasker = new();
        private readonly CannyMask cannyMasker = new();

        // Subject/Background mask
        private bool useSubjectMask = false;
        private int subjectMaskPaddingAmount = 15;
        private bool useInvertedSubjectMask = false;
        private NDArray? subjectMask = null;
        private NDArray? invertedSubjectMask = null;

        // Canny mask
        private bool useCanny = false;
        private float cannyThreashold = 0.3f;
        private NDArray? cannyMask = null;
        private NDArray? invertedCannyMask = null;
        
        // Combine masks
        private bool useSubtractMasks = true;

        // image viewer
        private readonly List<string> imageCaptions = [];
        private readonly List<string> imagePaths = [];
        private int currentDisplayedImageIndex = -1;


        public MainPage(MainPageViewModel viewModel)
        {
            this.viewModel = viewModel;

            InitializeComponent();
            BindingContext = this.viewModel;

            SizeChanged += (_, _) => ApplyImageSizeForCurrentDevice();

            sortBtn.IsVisible = true;
            this.viewModel.IsSortEnabled = false;
            this.viewModel.IsSaveVisible = false;
            this.viewModel.IsSaveEnabled = false;
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
                viewModel.CurrentCaption = imageCaptions[index];
                SemanticProperties.SetDescription(whatIsThisLabel, $"Current image caption: {imageCaptions[index]}");
                SemanticProperties.SetDescription(imageViewer, $"Image preview. {imageCaptions[index]}");
            }

            viewModel.IsSaveEnabled = index > 0 && index < imagePaths.Count;
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
            var sortByText = viewModel.SelectedSortByName;
            var directionText = viewModel.SelectedSortDirectionName;

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
            this.subjectMask = null; // Clear any existing mask when a new image is loaded
            this.cannyMask = null;
            this.invertedSubjectMask = null;
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
                viewModel.CurrentCaption = imageCaptions[0];
                SemanticProperties.SetDescription(whatIsThisLabel, $"Options used for the current image: {imageCaptions[0]}");
                SemanticProperties.SetDescription(imageViewer, "Image preview. Original image. Double tap to load another image.");

                whatIsThisLabel.IsVisible = true;
                viewModel.IsSortEnabled = true;
                viewModel.IsSaveVisible = false;
                viewModel.IsSaveEnabled = false;
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
        private async Task UseLoadingOverlayAsync(String text)
        {
            loadingOverlayLabel.Text = text;
            loadingIndicator.IsRunning = true;
            loadingOverlay.IsVisible = true;
            SemanticScreenReader.Announce(text);
            (Color, Color) colors = ((Color)Application.Current!.Resources["SurfaceLight"], (Color)Application.Current!.Resources["SurfaceDark"]);

            if (Application.Current!.RequestedTheme == AppTheme.Light)
            {
                colors = (colors.Item2, colors.Item1);
            }

            while (loadingOverlay.IsVisible)
            {
                await imagePreviewBorder.StrokeColorTo(colors.Item1, rate: 16, length: 850, easing: Easing.SinInOut);
                await imagePreviewBorder.StrokeColorTo(colors.Item2, rate: 16, length: 850, easing: Easing.SinInOut);
            }
            imagePreviewBorder.SetAppTheme<Brush>(
                Border.StrokeProperty,
                new SolidColorBrush((Color)Application.Current!.Resources["SurfaceLight"]), // Light
                new SolidColorBrush((Color)Application.Current!.Resources["SurfaceDark"])  // Dark
            );
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
            if (this.subjectMask is not null)
            {
                return true;
            }
            else if (!String.IsNullOrEmpty(this.imagePath))
            {
                if (backgroundMasker.IsReadyToUse)
                {
                    try
                    {
                        (this.subjectMask, this.invertedSubjectMask) = await backgroundMasker.GetMaskAsync(this.imagePath, new BackgroundMaskOptions(this.subjectMaskPaddingAmount));
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
                    (this.cannyMask, this.invertedCannyMask) = await cannyMasker.GetMaskAsync(this.imagePath, new CannyMaskOptions(this.cannyThreashold));
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

            using (new BusyScope(
                onStart: () =>
                {
                    ToggleUiForSorting(false);
                    _ = UseLoadingOverlayAsync("Sorting...");
                },
                onComplete: () =>
                {
                    loadingIndicator.IsRunning = false;
                    loadingOverlay.IsVisible = false;
                    ToggleUiForSorting(true);

                }))


                try
                {
                    // Run the CPU/IO-bound sorting on a background thread; save to a temporary file so the UI can be updated safely.
                    sortedImagePath = Path.Combine(FileSystem.CacheDirectory, $"sorted_temp_{Guid.NewGuid()}.png");
                    await Task.Run(async () =>
                    {
                        NDArray? maskToUse = null;
                        if (viewModel.UseSubjectMask && viewModel.UseCanny)
                        {
                            bool subjectIsReady = await CreateSubjectMask();
                            bool cannyIsReady = await CreateCannyMask();



                            if (this.useSubtractMasks)
                            {
                                maskToUse = (subjectIsReady && cannyIsReady) ? MaskCombiner.SubtractMasks(this.subjectMask!, this.invertedCannyMask!) : null;
                            }
                            else if (!this.useSubtractMasks)
                            {
                                maskToUse = (subjectIsReady && cannyIsReady) ? MaskCombiner.AddMasks(this.subjectMask!, this.cannyMask!) : null;
                            }
                        }
                        else if (viewModel.UseCanny)
                        {
                            maskToUse = await CreateCannyMask() ? this.cannyMask : null;
                        }
                        else if (viewModel.UseSubjectMask)
                        {
                            bool isReady = await CreateSubjectMask();
                            maskToUse = isReady ? (this.useInvertedSubjectMask ? this.invertedSubjectMask : this.subjectMask) : null;
                        }

                        var imgData = Sorter.SortImage(
                                    Image.LoadImage(this.imagePath),
                                    viewModel.SortingCriterion ?? SortBy.GetAllSortingCriteria().Values.First(),
                                    viewModel.SortingDirection,
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
                        viewModel.CurrentCaption = caption;
                        SemanticProperties.SetDescription(whatIsThisLabel, $"Current image caption: {caption}");
                        SemanticProperties.SetDescription(imageViewer, $"Image preview. {caption}");
                        viewModel.IsSaveVisible = true;
                        viewModel.IsSaveEnabled = true;
                        SemanticScreenReader.Announce("Sorting complete. Preview updated.");
                    });
                }
                catch (Exception ex)
                {
                    // Handle exceptions (e.g., show an alert)
                    await DisplayAlertAsync("Error", $"An error occurred: {ex.Message}", "OK");
                    SemanticScreenReader.Announce($"Error: {ex.Message}");
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
                using (new BusyScope(
                onStart: () =>
                {
                    ToggleUiForSorting(false);
                    _ = UseLoadingOverlayAsync("Downloading...");
                },
                onComplete: () =>
                {
                    loadingIndicator.IsRunning = false;
                    loadingOverlay.IsVisible = false;
                    ToggleUiForSorting(true);

                }))

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
                        viewModel.IsSortEnabled = true;
                    }
            }

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


        private void OnCannySliderValueChanged(object sender, ValueChangedEventArgs e)
        {

            if (e.OldValue == e.NewValue)
            {
                return;
            }

            int value = (int)e.NewValue;
            if (value != e.NewValue)
            {
                cannyValueSlider.Value = value;
            }
            this.cannyThreashold = (int)e.NewValue / 100f;
            cannySliderValue.Text = $"{value}%";

            this.cannyMask = null; // Clear existing masks to ensure they are regenerated with the new padding
            this.invertedCannyMask = null;

        }

        private void OnSubjectMaskingSliderValueChanged(object sender, ValueChangedEventArgs e)
        {

            if (e.OldValue == e.NewValue)
            {
                return;
            }

            int value = (int)e.NewValue;
            if (value != e.NewValue)
            {
                subjectMaskPadding.Value = value;
            }

            subjectPaddingSliderValue.Text = $"{value} px";


            this.subjectMaskPaddingAmount = value;
            this.subjectMask = null; // Clear existing masks to ensure they are regenerated with the new padding
            this.invertedSubjectMask = null;

        }

        private void WhatToSort_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            if (sender == sortBackgroundRadio && e.Value)
            {
                this.useInvertedSubjectMask = false;
            }
            else if (sender == sortForegroundRadio && e.Value)
            {
                this.useInvertedSubjectMask = true;
            }
        }

        private void UseCanny_Toggled(object sender, ToggledEventArgs e)
        {
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
            viewModel.IsSortEnabled = state;
            imageViewer.IsEnabled = state;
            sortBy.IsEnabled = state;
            sortDirection.IsEnabled = state;
            useSubjectMaskingSwitch.IsEnabled = state;
            useCannySwitch.IsEnabled = state;
            subjectMaskPadding.IsEnabled = state;
            cannyValueSlider.IsEnabled = state;
            sortBackgroundRadio.IsEnabled = state;
            sortForegroundRadio.IsEnabled = state;
            subMasksRadio.IsEnabled = state;
            addMasksRadio.IsEnabled = state;
            viewModel.IsSaveEnabled = state && currentDisplayedImageIndex > 0 && currentDisplayedImageIndex < imagePaths.Count;

        }
    }
}
