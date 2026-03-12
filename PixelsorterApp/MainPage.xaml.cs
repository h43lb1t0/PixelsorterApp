using NumSharp;
using PixelsorterApp.Localization;
using PixelsorterClassLib;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Image = PixelsorterClassLib.Image;

namespace PixelsorterApp
{
    public partial class MainPage : ContentPage
    {

        private ImageSource imgSource;
        private string imagePath; // Add field to store the file path
        private readonly Mask masker = new Mask();
        private bool useMask = false;
        private readonly Dictionary<string, Func<Rgba32, float>> sortByOptions = SortBy.GetAllSortingCriteria();
        private readonly Dictionary<string, string> localizedSortByOptions = new();
        private readonly Dictionary<string, SortDirections> sortDirectionOptions = new();
        private Func<Rgba32, float>? sortingCriterion;
        private SortDirections sortingDirection;
        private string[] sortByOptionNames;
        private string[] sortDirectionOptionNames;
        private int maskPaddingAmount = 15;
        private bool useInvertedMask = false;
        private NDArray? mask = null;
        private NDArray? invertedMask = null;
        private readonly double DESKTOP_IMAGE_HEIGHT = 0.75;

        private static string T(string key) => LocalizationManager.GetString(key);

        private static readonly IReadOnlyDictionary<SortDirections, string> sortDirectionResourceKeys =
            new Dictionary<SortDirections, string>
            {
                [SortDirections.RowLeftToRight] = "SortDirection_RowLeftToRight",
                [SortDirections.RowRightToLeft] = "SortDirection_RowRightToLeft",
                [SortDirections.ColumnTopToBottom] = "SortDirection_ColumnTopToBottom",
                [SortDirections.ColumnBottomToTop] = "SortDirection_ColumnBottomToTop",
                [SortDirections.IntoMask] = "SortDirection_IntoMask"
            };

        private static readonly IReadOnlyDictionary<string, string> sortByResourceKeys =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [nameof(SortBy.Hue)] = "SortBy_Hue",
                [nameof(SortBy.Brightness)] = "SortBy_Brightness",
                [nameof(SortBy.Saturation)] = "SortBy_Saturation",
                [nameof(SortBy.Lightness)] = "SortBy_Lightness",
                [nameof(SortBy.Warmth)] = "SortBy_Warmth",
                [nameof(SortBy.Coolness)] = "SortBy_Coolness"
            };


        private void InitializeSortDirectionOptions()
        {
            foreach (SortDirections dir in Enum.GetValues(typeof(SortDirections)))
            {
                sortDirectionOptions[GetSortDirectionDisplayName(dir)] = dir;
            }
        }

        private void InitializeSortByOptions()
        {
            foreach (var key in sortByOptions.Keys)
            {
                localizedSortByOptions[key] = GetSortByDisplayName(key);
            }
        }

        private static string GetSortByDisplayName(string key)
        {
            if (sortByResourceKeys.TryGetValue(key, out var resourceKey))
            {
                return T(resourceKey);
            }

            return key;
        }

        private static string GetSortDirectionDisplayName(SortDirections direction)
        {
            if (sortDirectionResourceKeys.TryGetValue(direction, out var resourceKey))
            {
                return T(resourceKey);
            }

            return System.Text.RegularExpressions.Regex.Replace(direction.ToString(), "([A-Z])", " $1").Trim();
        }

        private void UpdateSortDirectionPicker()
        {
            SortDirections? previousSelection = null;
            if (sortDirection.SelectedIndex >= 0 && sortDirection.SelectedIndex < sortDirectionOptionNames.Length)
            {
                previousSelection = sortDirectionOptions[sortDirectionOptionNames[sortDirection.SelectedIndex]];
            }

            sortDirectionOptionNames =
            [
                .. sortDirectionOptions
                    .Where(option => this.useMask || option.Value != SortDirections.IntoMask)
                    .Select(option => option.Key)
            ];

            sortDirection.ItemsSource = sortDirectionOptionNames;

            int selectedIndex = -1;
            if (previousSelection.HasValue)
            {
                var previousDisplayName = GetSortDirectionDisplayName(previousSelection.Value);
                selectedIndex = Array.IndexOf(sortDirectionOptionNames, previousDisplayName);
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

            InitializeSortByOptions();
            InitializeSortDirectionOptions();
            sortByOptionNames = [.. localizedSortByOptions.Values];
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

            sortingCriterion = sortByOptionNames.Length > 0
                ? sortByOptions[localizedSortByOptions.First(x => x.Value == sortByOptionNames[0]).Key]
                : null;
            sortingDirection = sortDirectionOptionNames.Length > 0 ? sortDirectionOptions[sortDirectionOptionNames[0]] : SortDirections.RowRightToLeft;
            ApplyImageSizeForCurrentDevice();
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
            this.imgSource = ImageSource.FromFile(path);
            this.mask = null; // Clear any existing mask when a new image is loaded

            MainThread.BeginInvokeOnMainThread(() =>
            {
                LoadImageBtn.HeightRequest = -1;
                ApplyImageSizeForCurrentDevice();
                LoadImageBtn.Source = this.imgSource;
                sortBtn.IsEnabled = true;
                saveBtn.IsVisible = false;
            });
        }

        private void ApplyImageSizeForCurrentDevice()
        {
            if (DeviceInfo.Idiom == DeviceIdiom.Desktop)
            {
                imagePreviewBorder.MaximumHeightRequest = this.Height > 0
                    ? this.Height * DESKTOP_IMAGE_HEIGHT
                    : double.PositiveInfinity;
                LoadImageBtn.MaximumHeightRequest = double.PositiveInfinity;
                return;
            }

            imagePreviewBorder.MaximumHeightRequest = double.PositiveInfinity;
            LoadImageBtn.MaximumHeightRequest = double.PositiveInfinity;
        }

        private void UseLoadingOverlay(String text)
        {
            loadingOverlayLabel.Text = text;
            loadingIndicator.IsRunning = true;
            loadingOverlay.IsVisible = true;
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
            UseLoadingOverlay(T("SortingOverlay"));
            LoadImageBtn.IsEnabled = false;

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
                    LoadImageBtn.Source = ImageSource.FromFile(sortedImagePath);
                    saveBtn.IsVisible = true;
                    saveBtn.IsEnabled = true; // Enable the save button now that sorting is complete
                });
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., show an alert)
                await DisplayAlertAsync(T("ErrorTitle"), string.Format(T("ErrorOccurredMessageFormat"), ex.Message), T("OkButton"));
            }
            finally
            {
                loadingIndicator.IsRunning = false;
                loadingOverlay.IsVisible = false;
                sortBtn.IsEnabled = true; // Re-enable the sort button after sorting is complete
                LoadImageBtn.IsEnabled = true;

            }
        }

        /// <summary>
        /// Saves the image to the Gallery/Photos album on the user's device. This is necessary because the image is currently saved to a temporary location that may not be accessible to the user.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void SaveBtn_Clicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(sortedImagePath) || !File.Exists(sortedImagePath))
            {
                await DisplayAlertAsync(T("ErrorTitle"), T("NoSortedImageToSave"), T("OkButton"));
                return;
            }

            var imageBytes = await File.ReadAllBytesAsync(sortedImagePath);

            var galleryService = Application.Current?.Handler?.MauiContext?.Services?.GetService<IGalleryService>();

            if (galleryService != null)
            {
                var fileName = $"pixelsorted_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                var result = await galleryService.SaveImageAsync(imageBytes, fileName);

                if (result)
                {
                    await DisplayAlertAsync(T("SuccessTitle"), T("ImageSavedToGallery"), T("OkButton"));
                }
                else
                {
                    await DisplayAlertAsync(T("ErrorTitle"), T("FailedToSaveImageToGallery"), T("OkButton"));
                }
            }
            else
            {
                await DisplayAlertAsync(T("ErrorTitle"), T("GalleryServiceNotAvailable"), T("OkButton"));
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
                    T("MaskingFeatureLicenseTitle"),
                    T("MaskingFeatureLicenseMessage"),
                    T("AcceptButton"),
                    T("DontAcceptButton")
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
                UseLoadingOverlay(T("DownloadingOverlay"));
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
                        T("DownloadFailedTitle"),
                        T("DownloadFailedMessage"),
                        T("OkButton"));
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
                _ = DisplayAlertAsync(T("NoInternetTitle"), T("NoInternetMessage"), T("OkButton"));
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
                var selectedOptionInternalKey = localizedSortByOptions.First(item => item.Value == selectedOption).Key;
                // Update the sorting criterion based on the selected option
                sortingCriterion = sortByOptions[selectedOptionInternalKey];
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
