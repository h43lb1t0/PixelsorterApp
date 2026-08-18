using CommunityToolkit.Maui.Extensions;
using PixelsorterApp.Extensions;
using PixelsorterApp.Models;
using PixelsorterApp.Popups;
using PixelsorterApp.Services;
using PixelsorterApp.ViewModels;
using PixelsorterClassLib.Core;
using UXDivers.Popups.Maui.Controls;
using UXDivers.Popups.Services;
using Color = Microsoft.Maui.Graphics.Color;

namespace PixelsorterApp
{
    /// <summary>
    /// Code-behind host for the main page that bridges platform/UI interactions with <see cref="MainPageViewModel"/>.
    /// </summary>
    public partial class MainPage : ContentPage
    {
        // other
        private readonly double DESKTOP_IMAGE_HEIGHT = 0.75;
        private readonly MainPageViewModel viewModel;
        private readonly IImageProcessingService imageProcessingService;
        private readonly IShareService shareService;

        private readonly int PopupAutoDismissTime = 1500;

        private CancellationTokenSource? _sortMessageCts;


        // image
        private string? imagePath;

        // image viewer
        private class ImageCaptionInfo
        {
            public bool IsOriginal { get; set; }
            public string? SortByKey { get; set; }
            public string? DirectionKey { get; set; }

            public string GetLocalizedText()
            {
                if (IsOriginal)
                    return PixelsorterApp.Resources.Languages.MainPageStrings.OriginalImage;

                var sortByDisplay = SortByKey != null
                    ? SortStringLocalizer.LocalizeSortBy(SortByKey)
                    : PixelsorterApp.Resources.Languages.CommonStrings.common_Unknown;
                var directionDisplay = DirectionKey != null
                    ? SortStringLocalizer.LocalizeDirection(DirectionKey)
                    : PixelsorterApp.Resources.Languages.CommonStrings.common_Unknown;

                return string.Format(PixelsorterApp.Resources.Languages.MainPageStrings.SortCaption,
                    sortByDisplay, directionDisplay);
            }
        }

        private readonly List<ImageCaptionInfo> imageCaptions = [];
        private readonly List<string> imagePaths = [];
        private int currentDisplayedImageIndex = -1;
        private bool suppressSubjectMaskChangeHandling;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainPage"/> class.
        /// </summary>
        /// <param name="viewModel">The view model bound to this page.</param>
        /// <param name="imageProcessingService">Service used for image processing operations.</param>
        /// <param name="shareService">Service used for sharing images.</param>
        public MainPage(MainPageViewModel viewModel, IImageProcessingService imageProcessingService, IShareService shareService, LocalizationResourceManager.Maui.ILocalizationResourceManager localizationResourceManager)
        {
            this.viewModel = viewModel;
            this.imageProcessingService = imageProcessingService;
            this.shareService = shareService;

            InitializeComponent();
            BindingContext = this.viewModel;

            SizeChanged += (_, _) => ApplyImageSizeForCurrentDevice();

            this.viewModel.IsSortEnabled = false;
            this.viewModel.IsSaveVisible = false;
            this.viewModel.IsSaveEnabled = false;
            ApplyImageSizeForCurrentDevice();

            this.viewModel.SortRequested += OnSortRequested;
            this.viewModel.SaveRequested += OnSaveRequested;
            this.viewModel.LoadImageRequested += OnLoadImageRequested;
            this.viewModel.PropertyChanged += OnViewModelPropertyChanged;
            imageViewer.DisplayedImageIndexChanged += ImageViewer_DisplayedImageIndexChanged;
            this.viewModel.ShareRequested += OnShareRequested;

            ((System.ComponentModel.INotifyPropertyChanged)localizationResourceManager).PropertyChanged += (s, e) =>
            {
                if (currentDisplayedImageIndex >= 0 && currentDisplayedImageIndex < imageCaptions.Count)
                {
                    var captionText = imageCaptions[currentDisplayedImageIndex].GetLocalizedText();
                    viewModel.CurrentCaption = captionText;
                    SemanticProperties.SetDescription(whatIsThisLabel, $"Current image caption: {captionText}");
                    SemanticProperties.SetDescription(imageViewer, $"Image preview. {captionText}");
                }
            };
        }

        /// <summary>
        /// Handles view model property changes that require page-level async UI logic.
        /// </summary>
        /// <param name="sender">The event source.</param>
        /// <param name="e">Property change event arguments.</param>
        private async void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainPageViewModel.UseSubjectMask))
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
                if (viewModel.UseSubjectMask)
                {
                    await HandleSubjectMaskEnabledAsync();
                }
            }
            else if (e.PropertyName == nameof(MainPageViewModel.UseCanny))
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            }
            else if (e.PropertyName == nameof(MainPageViewModel.IsSaveEnabled))
            {
                await AnimateShareFabAsync(viewModel.IsSaveEnabled);
            }
        }

        private void OnOpenHelpClicked(object sender, EventArgs e)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        }

        /// <summary>
        /// Animates the Share FAB appearing or disappearing with smooth fade and scale transitions.
        /// </summary>
        /// <param name="show">True to animate in; false to animate out.</param>
        private async Task AnimateShareFabAsync(bool show)
        {
            if (shareFab == null)
            {
                return;
            }

            shareFab.CancelAnimations();

            if (show)
            {
                if (!shareFab.IsVisible)
                {
                    shareFab.Opacity = 0;
                    shareFab.Scale = 0.75;
                    shareFab.IsVisible = true;
                }

                await Task.WhenAll(
                    shareFab.FadeToAsync(1, 250, Easing.CubicOut),
                    shareFab.ScaleToAsync(1, 250, Easing.SpringOut)
                );
            }
            else
            {
                if (!shareFab.IsVisible)
                {
                    return;
                }

                await Task.WhenAll(
                    shareFab.FadeToAsync(0, 200, Easing.CubicIn),
                    shareFab.ScaleToAsync(0.5, 200, Easing.CubicIn)
                );
                shareFab.IsVisible = false;
            }
        }

        /// <summary>
        /// Handles sort requests raised by the view model.
        /// </summary>
        private void OnSortRequested()
        {
            _ = SortAsync();
        }

        /// <summary>
        /// Handles save requests raised by the view model.
        /// </summary>
        private void OnSaveRequested()
        {
            _ = SaveAsync();
        }

        private void OnShareRequested()
        {
            _ = ShareAsync();
        }

        /// <summary>
        /// Handles image load requests raised by the view model.
        /// </summary>
        private void OnLoadImageRequested()
        {
            _ = LoadImageAsync();
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
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            currentDisplayedImageIndex = index;

            if (index >= 0 && index < imageCaptions.Count)
            {
                var captionText = imageCaptions[index].GetLocalizedText();
                viewModel.CurrentCaption = captionText;
                SemanticProperties.SetDescription(whatIsThisLabel, $"Current image caption: {captionText}");
                SemanticProperties.SetDescription(imageViewer, $"Image preview. {captionText}");
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
        private ImageCaptionInfo BuildSortCaption()
        {
            return new ImageCaptionInfo
            {
                IsOriginal = false,
                SortByKey = viewModel.SelectedSortBy?.Key,
                DirectionKey = viewModel.SelectedSortDirection?.Key
            };
        }

        /// <summary>
        /// Subscribes to shared-image events when the page becomes visible.
        /// </summary>
        protected override void OnAppearing()
        {
            base.OnAppearing();
            SharedImageBridge.SharedImageReceived += OnSharedImageReceived;

            _ = InitializeAppearingAsync();
        }

        private async Task InitializeAppearingAsync()
        {
            if (SharedImageBridge.TryConsumePendingImagePath(out var pendingImagePath) && pendingImagePath is not null)
            {
                LoadImageFromPath(pendingImagePath);
            }

            if (!Preferences.Default.Get("HasSeenGallerySuggest", false))
            {
                var popup = new ConfirmationPopup(true);
                var parameters = new Dictionary<string, object?>
                {
                    { "Title", PixelsorterApp.Resources.Languages.MainPageStrings.WelcomeTitle },
                    { "message", PixelsorterApp.Resources.Languages.MainPageStrings.WelcomeMessage },
                    { "ConfirmButton", PixelsorterApp.Resources.Languages.MainPageStrings.WelcomeAction },
                    { "CancelButton", PixelsorterApp.Resources.Languages.CommonStrings.common_Dismiss }
                };

                var response = await IPopupService.Current.PushAsync(popup, parameters);
                Preferences.Default.Set("HasSeenGallerySuggest", true);

                if (response)
                {
                    await Navigation.PushAsync(new Pages.ExampleGallery());
                }
            }
        }

        /// <summary>
        /// Unsubscribes from shared-image events when the page is no longer visible.
        /// </summary>
        protected override void OnDisappearing()
        {
            SharedImageBridge.SharedImageReceived -= OnSharedImageReceived;
            base.OnDisappearing();
        }

        /// <summary>
        /// Handles incoming shared image notifications.
        /// </summary>
        /// <param name="sharedImagePath">Path to the shared image file.</param>
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
        private Boolean LoadImageFromPath(string path)
        {
            var ext = Path.GetExtension(path).ToLower();
            if (ext is ".dng" or ".nef" or ".cr2" or ".arw" or ".rw2" or ".orf")
            {
                return false;
            }
            this.imagePath = path;
            imageCaptions.Clear();
            imagePaths.Clear();
            imageCaptions.Add(new ImageCaptionInfo { IsOriginal = true });
            imagePaths.Add(path);
            currentDisplayedImageIndex = 0;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                imageViewer.PrepareForImage();
                ApplyImageSizeForCurrentDevice();
                imageViewer.ClearImages();
                imageViewer.ShowImage(path);
                
                var originalText = imageCaptions[0].GetLocalizedText();
                viewModel.CurrentCaption = originalText;
                SemanticProperties.SetDescription(whatIsThisLabel, $"Options used for the current image: {originalText}");
                SemanticProperties.SetDescription(imageViewer, "Image preview. Original image. Double tap to load another image.");

                whatIsThisLabel.IsVisible = true;
                viewModel.IsSortEnabled = true;
                viewModel.IsSaveVisible = false;
                viewModel.IsSaveEnabled = false;
                SemanticScreenReader.Announce("Image loaded. Ready to sort.");
            });
            return true;
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
            loadingOverlay.IsVisible = true;
            loadingOverlayLabel.Text = text;
            loadingOverlayLabel.Opacity = 0;

            pulsingDots.IsAnimating = true;
            SemanticScreenReader.Announce(text);

            // Fade the status label in with a short delay so the dots appear first.
            _ = DelayedLabelFadeInAsync();

            async Task DelayedLabelFadeInAsync()
            {
                await Task.Delay(150);
                await loadingOverlayLabel.FadeToAsync(1.0, 200, Easing.CubicOut);
            }
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

        /// <summary>
        /// Updates the loading overlay label text with a brief fade transition.
        /// Call this to show phased status messages during a long operation.
        /// </summary>
        private async Task UpdateLoadingStatusAsync(string text)
        {
            await loadingOverlayLabel.FadeToAsync(0, 120, Easing.CubicIn);
            loadingOverlayLabel.Text = text;
            await loadingOverlayLabel.FadeToAsync(1.0, 180, Easing.CubicOut);
            SemanticScreenReader.Announce(text);
        }

        /// <summary>
        /// Cycles through product-specific status messages during the sorting operation,
        /// making the wait feel purposeful rather than static.
        /// </summary>
        private async Task CycleSortingMessagesAsync(CancellationToken token)
        {
            try
            {
                // Show the mask message once if masking is active
                if (viewModel.UseSubjectMask || viewModel.UseCanny)
                {
                    await Task.Delay(2200, token);
                    if (token.IsCancellationRequested) return;

                    var maskMessage = (viewModel.UseSubjectMask, viewModel.UseCanny) switch
                    {
                        (true, true) => PixelsorterApp.Resources.Languages.MainPageStrings.CycleSortingMessagesCombiningMasks,
                        (true, false) => PixelsorterApp.Resources.Languages.MainPageStrings.CycleSortingMessagesApplyingSubjectMask,
                        _ => PixelsorterApp.Resources.Languages.MainPageStrings.CycleSortingMessagesDetectingEdges
                    };

                    await UpdateLoadingStatusAsync(maskMessage);
                }

                // Cycle only between sort criterion and direction
                var cycleMessages = new[]
                {
                    String.Format(PixelsorterApp.Resources.Languages.MainPageStrings.CycleSortingMessagesSortingBy,viewModel.SelectedSortByName),
                    String.Format(PixelsorterApp.Resources.Languages.MainPageStrings.CycleSortingMessagesArrangingPixels, viewModel.SelectedSortDirectionName)
                };
                var index = 0;

                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(2200, token);
                    if (token.IsCancellationRequested) break;

                    await UpdateLoadingStatusAsync(cycleMessages[index]);
                    index = (index + 1) % cycleMessages.Length;
                }
            }
            catch (TaskCanceledException) { }
        }



        /// <summary>
        /// Handles taps on the image viewer and routes them to the load-image command.
        /// </summary>
        /// <param name="sender">The event source.</param>
        /// <param name="e">Event arguments.</param>
        private void LoadImage_Clicked(object sender, EventArgs e)
        {
            if (viewModel.LoadImageCommand.CanExecute(null))
            {
                viewModel.LoadImageCommand.Execute(null);
            }
        }

        /// <summary>
        /// Opens the platform photo picker and loads the first selected image.
        /// </summary>
        private async Task LoadImageAsync()
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            var results = await MediaPicker.PickPhotosAsync();
            var success = LoadImageFromPath(results[0].FullPath);
            if (!success)
            {
                var popup = new Toast()
                {
                    Title = PixelsorterApp.Resources.Languages.CommonStrings.common_dngErrorMessage,
                    IconText = MaterialSymbolsFont.Error,
                    IconColor = Colors.Red,
                };

                await IPopupService.Current.PushAsync(popup, waitUntilClosed: false);
                SemanticScreenReader.Announce("Can't load RAW Images (dng files)");
                await Task.Delay(PopupAutoDismissTime);
                await IPopupService.Current.PopAsync(popup);
                await LoadImageAsync();
            }
        }

        private string? sortedImagePath; // Path to the temporarily saved sorted image

        /// <summary>
        /// Sorts the currently loaded image using current view model settings.
        /// </summary>
        private async Task SortAsync()
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);
            if (this.imagePath is null) // Check if we have a file path
                return;

            // Sort button press micro-interaction — physical feel on tap
            await sortBtn.ScaleToAsync(0.95, 80, Easing.CubicOut);
            _ = sortBtn.ScaleToAsync(1.0, 100, Easing.SpringOut);

            _sortMessageCts?.Cancel();
            _sortMessageCts = new CancellationTokenSource();
            var messageCts = _sortMessageCts;
            bool sortSucceeded = false;

            using (new BusyScope(
                onStart: () =>
                {
                    ToggleUiForSorting(false);
                    _ = UseLoadingOverlayAsync(PixelsorterApp.Resources.Languages.MainPageStrings.CycleSortingMessagesReadingPixelData);
                    _ = CycleSortingMessagesAsync(messageCts.Token);
                },
                onComplete: () =>
                {
                    messageCts.Cancel();
                    pulsingDots.IsAnimating = false;
                    loadingOverlayLabel.Opacity = 0;
                    loadingOverlay.IsVisible = false;
                    ToggleUiForSorting(true);

                }))


                try
                {
                    var maskToUse = await imageProcessingService.BuildMaskAsync(
                        this.imagePath,
                        viewModel.UseSubjectMask,
                        viewModel.UseCanny,
                        viewModel.UseSubtractMasks,
                        viewModel.UseInvertedSubjectMask,
                        viewModel.SubjectMaskPadding,
                        viewModel.CannyThreshold);

                    sortedImagePath = await imageProcessingService.SortImageAsync(
                        this.imagePath,
                        viewModel.SortingCriterion ?? SortBy.GetAllSortingCriteria().Values.First(),
                        viewModel.SortingDirection,
                        maskToUse);

                    sortSucceeded = true;

                    // Final status — shown once before revealing the result
                    messageCts.Cancel();

                    // Back on the UI thread — safe to update UI elements.
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        imageViewer.ShowImage(sortedImagePath);
                        var captionInfo = BuildSortCaption();
                        imageCaptions.Add(captionInfo);
                        imagePaths.Add(sortedImagePath);
                        currentDisplayedImageIndex = imagePaths.Count - 1;
                        
                        var captionText = captionInfo.GetLocalizedText();
                        viewModel.CurrentCaption = captionText;
                        SemanticProperties.SetDescription(whatIsThisLabel, $"Current image caption: {captionText}");
                        SemanticProperties.SetDescription(imageViewer, $"Image preview. {captionText}");
                        viewModel.IsSaveVisible = true;
                        viewModel.IsSaveEnabled = true;
                        HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);

                        var sortByName = viewModel.SelectedSortByName;
                        var directionName = viewModel.SelectedSortDirectionName;
                        SemanticScreenReader.Announce($"Sorted by {sortByName}, {directionName}. Swipe to compare with original.");
                    });
                }
                catch (Exception ex)
                {
                    // Handle exceptions (e.g., show an alert)
                    await DisplayAlertAsync(PixelsorterApp.Resources.Languages.CommonStrings.common_Error, String.Format(PixelsorterApp.Resources.Languages.MainPageStrings.SortAsync_AnErrorOccurredExMessage, ex.Message), PixelsorterApp.Resources.Languages.CommonStrings.common_OK);
                    SemanticScreenReader.Announce($"Error: {ex.Message}");
                }

            // Completion reveal — after the overlay is dismissed
            if (sortSucceeded)
            {
                // Brief pause to let the overlay dismiss settle
                await Task.Delay(50);
                _ = imagePreviewBorder.PlaySuccessPulseAsync();
            }
        }

        /// <summary>
        /// Saves the currently focused image to the device gallery.
        /// </summary>
        private async Task SaveAsync()
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            var focusedImagePath = GetFocusedImagePath();

            if (string.IsNullOrEmpty(focusedImagePath) || !File.Exists(focusedImagePath))
            {
                await DisplayAlertAsync(PixelsorterApp.Resources.Languages.CommonStrings.common_Error, PixelsorterApp.Resources.Languages.MainPageStrings.SaveAsync_NoImageAvailableToSave, PixelsorterApp.Resources.Languages.CommonStrings.common_OK);
                SemanticScreenReader.Announce(PixelsorterApp.Resources.Languages.MainPageStrings.SaveAsync_NoImageAvailableToSave);
                return;
            }

            var fileName = $"pixelsorted_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            var result = await imageProcessingService.SaveImageToGalleryAsync(focusedImagePath, fileName);

            if (result)
            {
                var popup = new Toast()
                {
                    Title = PixelsorterApp.Resources.Languages.MainPageStrings.SaveAsync_ImageSaved,
                    IconText = MaterialSymbolsFont.Check_circle,
                    IconColor = Colors.Green
                };

                await IPopupService.Current.PushAsync(popup, waitUntilClosed: false);
                SemanticScreenReader.Announce("Image saved to gallery.");
                await Task.Delay(PopupAutoDismissTime);
                await IPopupService.Current.PopAsync(popup);
            }
            else
            {
                var popup = new Toast()
                {
                    Title = PixelsorterApp.Resources.Languages.MainPageStrings.SaveAsync_FailedToSaveImage,
                    IconText = MaterialSymbolsFont.Error,
                    IconColor = Colors.Red
                };

                await IPopupService.Current.PushAsync(popup, waitUntilClosed: false);
                SemanticScreenReader.Announce("Failed to save image");
                await Task.Delay(PopupAutoDismissTime);
                await IPopupService.Current.PopAsync(popup);
            }
        }

        private async Task ShareAsync()
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            try
            {
                var focusedImagePath = GetFocusedImagePath();
                if (string.IsNullOrEmpty(focusedImagePath) || !File.Exists(focusedImagePath))
                {
                    await DisplayAlertAsync(PixelsorterApp.Resources.Languages.CommonStrings.common_Error, PixelsorterApp.Resources.Languages.MainPageStrings.ShareAsync_NoImageAvailableToShare, PixelsorterApp.Resources.Languages.CommonStrings.common_OK);
                    SemanticScreenReader.Announce("No image available to share.");
                    return;
                }
                // Call the share service with the current image path
                await shareService.ShareImage(focusedImagePath);
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(PixelsorterApp.Resources.Languages.CommonStrings.common_Error, String.Format(PixelsorterApp.Resources.Languages.MainPageStrings.ShareAsync_ShareFailed, ex.Message), PixelsorterApp.Resources.Languages.CommonStrings.common_OK);
            }
        }

        /// <summary>
        /// Validates and initializes subject masking when the user enables it.
        /// </summary>
        private async Task HandleSubjectMaskEnabledAsync()
        {
            if (suppressSubjectMaskChangeHandling)
            {
                return;
            }

            if (!viewModel.UseSubjectMask)
            {
                return;
            }

            bool netAccess = await CheckNetworkAccessAsync();
            if (!netAccess && !imageProcessingService.IsBackgroundMaskReady)
            {
                DisableSubjectMaskWithoutReentry();
                return;
            }

            if (!Preferences.Get("MaskingLicenseAccepted", false))
            {
                var popup = new ConfirmationPopup();
                var parameters = new Dictionary<string, object?>
                {
                    {"Title", PixelsorterApp.Resources.Languages.MainPageStrings.MaskingFeatureLicense_Title },
                    { "message", PixelsorterApp.Resources.Languages.MainPageStrings.MaskingFeatureLicense_Message },
                    { "ConfirmButton", PixelsorterApp.Resources.Languages.CommonStrings.common_Accept },
                    { "CancelButton", PixelsorterApp.Resources.Languages.CommonStrings.common_DontAccept }
                };

                bool response = await IPopupService.Current.PushAsync(popup, parameters);
                Preferences.Set("MaskingLicenseAccepted", response);

                if (!response)
                {
                    DisableSubjectMaskWithoutReentry();
                    return;
                }
            }

            if (!imageProcessingService.IsBackgroundMaskReady && netAccess)
            {
                using (new BusyScope(
                onStart: () =>
                {
                    ToggleUiForSorting(false);
                    _ = UseLoadingOverlayAsync(PixelsorterApp.Resources.Languages.MainPageStrings.Downloading);
                },
                onComplete: () =>
                {
                    pulsingDots.IsAnimating = false;
                    loadingOverlayLabel.Opacity = 0;
                    loadingOverlay.IsVisible = false;
                    ToggleUiForSorting(true);

                }))

                    try
                    {
                        await imageProcessingService.DownloadBackgroundModelAsync();
                    }
                    catch (Exception)
                    {
                        await DisplayAlertAsync(
                            PixelsorterApp.Resources.Languages.MainPageStrings.DownloadFailed_Title,
                            PixelsorterApp.Resources.Languages.MainPageStrings.DownloadFailed_Message,
                            PixelsorterApp.Resources.Languages.CommonStrings.common_OK);
                        DisableSubjectMaskWithoutReentry();
                        return;
                    }
                    finally
                    {
                        pulsingDots.IsAnimating = false;
                        loadingOverlayLabel.Opacity = 0;
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
        private async Task<bool> CheckNetworkAccessAsync()
        {
            NetworkAccess accessType = Connectivity.Current.NetworkAccess;

            if (accessType != NetworkAccess.Internet)
            {
                await DisplayAlertAsync(PixelsorterApp.Resources.Languages.MainPageStrings.NoInternetConnection_Title, PixelsorterApp.Resources.Languages.MainPageStrings.NoInternetConnection_Message, PixelsorterApp.Resources.Languages.CommonStrings.common_OK);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Disables subject masking without reentering property-change handling logic.
        /// </summary>
        private void DisableSubjectMaskWithoutReentry()
        {
            suppressSubjectMaskChangeHandling = true;
            viewModel.UseSubjectMask = false;
            suppressSubjectMaskChangeHandling = false;
        }

        /// <summary>
        /// Toggles enabled state for sorting-related UI interactions.
        /// </summary>
        /// <param name="state"><see langword="true"/> to enable interactions; otherwise, <see langword="false"/>.</param>
        private void ToggleUiForSorting(bool state)
        {
            viewModel.IsSortEnabled = state;
            viewModel.IsInteractionEnabled = state;
            viewModel.IsSaveEnabled = state && currentDisplayedImageIndex > 0 && currentDisplayedImageIndex < imagePaths.Count;

        }

            }
        }
