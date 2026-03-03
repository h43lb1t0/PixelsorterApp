using System;
using System.Collections.Generic;
using System.Text;

namespace PixelsorterApp.Platforms.Windows
{
    public class GalleryService : IGalleryService
    {
        // For simplicity, we'll save images to the user's Pictures folder in a subfolder named "Pixelsorter".
        private readonly string userImageDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Pixelsorter");
        public Task<bool> SaveImageAsync(byte[] imageBytes, string fileName)
        {
            imageBytes = imageBytes ?? throw new ArgumentNullException(nameof(imageBytes));
            fileName = fileName ?? throw new ArgumentNullException(nameof(fileName));

            try
            {
                // Ensure the directory exists
                if (!System.IO.Directory.Exists(userImageDir))
                {
                    System.IO.Directory.CreateDirectory(userImageDir);
                }

                // Combine the directory with the filename to get the full path
                string filePath = System.IO.Path.Combine(userImageDir, fileName);

                // Write the image bytes to the file
                System.IO.File.WriteAllBytes(filePath, imageBytes);

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving image: {ex.Message}");
                return Task.FromResult(false);
            }
        }
    }
}
