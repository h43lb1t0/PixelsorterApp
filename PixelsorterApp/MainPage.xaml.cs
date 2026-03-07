using Mopups.PreBaked.PopupPages.DualResponse;
using NumSharp;
using PixelsorterClassLib;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Color = Microsoft.Maui.Graphics.Color;
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
        private Dictionary<string, SortDirections> sortDirectionOptions = new();
        private Func<Rgba32, float>? sortingCriterion;
        private SortDirections sortingDirection;
        private string[] sortByOptionNames;
        private string[] sortDirectionOptionNames;
        private int maskPaddingAmount = 15;
        private bool invertMask = false;
        private bool isHandlingMaskToggle;

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
            UpdateWhatToSortStateLabel();
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

            MainThread.BeginInvokeOnMainThread(() =>
            {
                LoadImageBtn.HeightRequest = -1;
                LoadImageBtn.MaximumHeightRequest = double.PositiveInfinity;
                LoadImageBtn.Source = this.imgSource;
                sortBtn.IsEnabled = true;
                saveBtn.IsVisible = false;
            });
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

            NDArray? mask = null;

            sortBtn.IsEnabled = false; // Disable the sort button while sorting is in progress
            saveBtn.IsEnabled = false;
            sortingOverlay.IsVisible = true;
            sortingIndicator.IsRunning = true;
            LoadImageBtn.IsEnabled = false;

            try
            {
                // Run the CPU/IO-bound sorting on a background thread; save to a temporary file so the UI can be updated safely.
                sortedImagePath = Path.Combine(FileSystem.CacheDirectory, $"sorted_temp_{Guid.NewGuid()}.png");
                await Task.Run(async () =>
                {
                    if (this.useMask)
                    {
                        mask = await masker.GetMaskAsync(this.imagePath, this.maskPaddingAmount, this.invertMask);
                    }

                    var imgData = Sorter.SortImage(
                                Image.LoadImage(this.imagePath),
                                sortingCriterion ?? sortByOptions.Values.First(),
                                sortingDirection,
                                mask
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
                await DisplayAlertAsync("Error", $"An error occurred: {ex.Message}", "OK");
            }
            finally
            {
                sortingIndicator.IsRunning = false;
                sortingOverlay.IsVisible = false;
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
                await DisplayAlertAsync("Error", "No sorted image available to save.", "OK");
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
                    await DisplayAlertAsync("Success", "Image saved to gallery", "OK");
                }
                else
                {
                    await DisplayAlertAsync("Error", "Failed to save image to gallery", "OK");
                }
            }
            else
            {
                await DisplayAlertAsync("Error", "Gallery service is not available", "OK");
            }
        }

        private async void useMasking_Toggled(object sender, ToggledEventArgs e)
        {
            bool maskingLicenseAccepted = Preferences.Get("MaskingLicenseAccepted", false);
            if (!maskingLicenseAccepted && !isHandlingMaskToggle)
            {
                var response = await DisplayAlertAsync(
                    "Masking Feature License",
                    "The masking feature uses a pre-trained machine learning model that was created by a third party. By enabling this feature accept that you won't use picutures created or edited by this tool for any comercial purposes. For further information go to the license page.",
                    "Accept",
                    "Don't accept"
                    );
                Preferences.Set("MaskingLicenseAccepted", response);
                
                if (!response)
                {
                    isHandlingMaskToggle = true;
                    useMasking.IsToggled = false;
                    isHandlingMaskToggle = false;
                    return;
                }
            }

            this.useMask = e.Value;
            maskPadding.IsVisible = e.Value;
            UpdateSortDirectionPicker();
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

        private void whatToSort_Toggled(object sender, ToggledEventArgs e)
        {
            this.invertMask = e.Value;
            UpdateWhatToSortStateLabel();
        }

        private void UpdateWhatToSortStateLabel()
        {
            whatToSortStateLabel.Text = this.invertMask ? "Foreground (subject)" : "Background";
        }
    }
}
