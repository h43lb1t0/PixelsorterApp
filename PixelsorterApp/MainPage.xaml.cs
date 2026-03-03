using Microsoft.Maui.Storage;
using NumSharp;
using PixelsorterClassLib;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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


        public MainPage()
        {
            InitializeComponent();
            
            SizeChanged += OnPageSizeChanged;
            sortBtn.IsEnabled = false; // Disable the sort button until an image is loaded
            saveBtn.IsVisible = false;

            InitializeSortDirectionOptions();
            sortByOptionNames = [.. sortByOptions.Keys];
            sortDirectionOptionNames = [.. sortDirectionOptions.Keys];

            sortBy.ItemsSource = sortByOptionNames;
            // auto selects first option
            sortBy.SelectedItem = sortByOptionNames.Length > 0 ? sortByOptionNames[0] : null;
            sortDirection.ItemsSource = sortDirectionOptionNames;
            sortDirection.SelectedItem = sortDirectionOptionNames.Length > 0 ? sortDirectionOptionNames[0] : null;

            sortingCriterion = sortByOptionNames.Length > 0 ? sortByOptions[sortByOptionNames[0]] : null;
            sortingDirection = sortDirectionOptionNames.Length > 0 ? sortDirectionOptions[sortDirectionOptionNames[0]] : SortDirections.RowRightToLeft;
        }

        private void OnPageSizeChanged(object sender, EventArgs e)
        {
            imgShower.WidthRequest = this.Width * 2 / 3;
            imgShower.HeightRequest = this.Height * 0.7;
        }



        private async void LoadImage_Clicked(object sender, EventArgs e)
        {

            saveBtn.IsVisible = false; // Hide the save button when loading a new image
            var results = await MediaPicker.PickPhotosAsync();

            foreach (var file in results)
            {
                this.imagePath = file.FullPath; // Store the file path

                // Load the picked file into memory so the original file handle isn't held open.
                using var stream = await file.OpenReadAsync();
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                var imageBytes = ms.ToArray();

                // Provide a fresh MemoryStream each time the ImageSource requests it to avoid file locks.
                this.imgSource = ImageSource.FromStream(() => new MemoryStream(imageBytes));
                break;
            }
            imgShower.Source = this.imgSource;
            sortBtn.IsEnabled = true; // Enable the sort button now that an image is loaded
        }

        private async void sortBtn_Clicked(object sender, EventArgs e)
        {
            if (this.imagePath is null) // Check if we have a file path
                return;

            NDArray? mask = null;

            sortBtn.IsEnabled = false; // Disable the sort button while sorting is in progress

            try
            {
                // Run the CPU/IO-bound sorting on a background thread; return only raw bytes so the UI can be updated safely.
                var imageBytes = await Task.Run(() =>
                {
                    if (this.useMask)
                    {
                        mask = masker.GetMask(this.imagePath);
                    }

                    var imgData = Sorter.SortImage(
                                Image.LoadImage(this.imagePath),
                                sortingCriterion ?? sortByOptions.Values.First(),
                                sortingDirection,
                                mask
                            );
                    using var ms = new MemoryStream();
                    using var foo = Image.NdarrayToImgData(imgData);
                    foo.SaveAsPng(ms);
                    return ms.ToArray();
                });

                // Back on the UI thread — safe to update UI elements.
                imgShower.Source = ImageSource.FromStream(() => new MemoryStream(imageBytes));

            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., show an alert)
                await DisplayAlertAsync("Error", $"An error occurred: {ex.Message}", "OK");
            }
            sortBtn.IsEnabled = true; // Re-enable the sort button after sorting is complete
        }

        /// <summary>
        /// Saves the image to the Gallery/Photos album on the user's device. This is necessary because the image is currently saved to a temporary location that may not be accessible to the user.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveBtn_Clicked(object sender, EventArgs e)
        {
           throw new NotImplementedException("Save functionality is not implemented yet. This will save the sorted image to the user's Gallery/Photos album.");
        }

        private void useMasking_Toggled(object sender, ToggledEventArgs e)
        {
            this.useMask = e.Value;
        }

        private void sortBy_SelectedIndexChanged(object sender, EventArgs e)
        {
            var picker = (Picker)sender;
            int selectedIndex = picker.SelectedIndex;
            if (selectedIndex >= 0 && selectedIndex < sortByOptionNames.Length)
            {
                var selectedOption = sortByOptionNames[selectedIndex];
                // Update the sorting criterion based on the selected option
                sortingCriterion = sortByOptions[selectedOption];
            }
        }

        private void sortDirection_SelectedIndexChanged(object sender, EventArgs e)
        {
            var picker = (Picker)sender;
            int selectedIndex = picker.SelectedIndex;
            if (selectedIndex >= 0 && selectedIndex < sortDirectionOptionNames.Length)
            {
                var selectedOption = sortDirectionOptionNames[selectedIndex];
                sortingDirection = sortDirectionOptions[selectedOption];
            }
        }
    }
}
