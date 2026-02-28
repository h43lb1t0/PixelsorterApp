using PixelsorterClassLib;
using Image = PixelsorterClassLib.Image;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace PixelsorterApp
{
    public partial class MainPage : ContentPage
    {

        private ImageSource imgSource;
        private string imagePath; // Add field to store the file path
        private readonly Mask mask = new Mask();

        public MainPage()
        {
            InitializeComponent();
            SizeChanged += OnPageSizeChanged;
            sortBtn.IsEnabled = false; // Disable the sort button until an image is loaded
        }

        private void OnPageSizeChanged(object sender, EventArgs e)
        {
            imgShower.WidthRequest = this.Width * 2 / 3;
            imgShower.HeightRequest = this.Height * 0.7;
        }


        private async void LoadImage_Clicked(object sender, EventArgs e)
        {
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

            sortBtn.IsEnabled = false;

            try
            {
                // Run the CPU/IO-bound sorting and save on a background thread so the UI can update immediately.
                await Task.Run(() =>
                {
                    var imgData = Sorter.SortImage(
                        Image.LoadImage(this.imagePath),
                        SortBy.Warmth(),
                        SortDirections.RowRightToLeft,
                        mask.GetMask(this.imagePath)
                    );
                    Image.SaveImage(imgData, "sorted_image.png");
                });

                // Update UI after background work completes
                imgShower.Source = ImageSource.FromFile("sorted_image.png");
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., show an alert)
                await DisplayAlertAsync("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }
    }
}
