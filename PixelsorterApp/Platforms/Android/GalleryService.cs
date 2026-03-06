using Android.Content;
using Android.Provider;

namespace PixelsorterApp.Platforms.Android
{
    public class GalleryService : IGalleryService
    {
        public async Task<bool> SaveImageAsync(byte[] imageBytes, string fileName)
        {
            try
            {
                var context = Platform.CurrentActivity;
                if (context == null)
                {
                    Console.WriteLine("Error saving image: CurrentActivity is null");
                    return false;
                }

                var contentResolver = context.ContentResolver;
                if (contentResolver == null)
                {
                    Console.WriteLine("Error saving image: ContentResolver is null");
                    return false;
                }

                var externalContentUri = MediaStore.Images.Media.ExternalContentUri;
                if (externalContentUri == null)
                {
                    Console.WriteLine("Error saving image: ExternalContentUri is null");
                    return false;
                }

                var contentValues = new ContentValues();

                // Set file metadata
                contentValues.Put(MediaStore.IMediaColumns.DisplayName, fileName);
                contentValues.Put(MediaStore.IMediaColumns.MimeType, "image/jpeg");
                // Saves to the 'Pictures/YourAppName' folder
                contentValues.Put(MediaStore.IMediaColumns.RelativePath, "Pictures/Pixelsorter");

                var uri = contentResolver.Insert(externalContentUri, contentValues);

                if (uri == null) return false;

                using (var outputStream = contentResolver.OpenOutputStream(uri))
                {
                    if (outputStream == null)
                    {
                        Console.WriteLine("Error saving image: OpenOutputStream returned null");
                        return false;
                    }

                    await outputStream.WriteAsync(imageBytes);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving image: {ex.Message}");
                return false;
            }
        }
    }
}
