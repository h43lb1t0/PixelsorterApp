#if ANDROID
using Android.Content;
using AndroidX.Core.Content;
using AFile = Java.IO.File;
#endif

namespace PixelsorterApp.Services
{
    public sealed class ShareService : IShareService
    {
        public async Task ShareImage(string filePath, string? text = null)
        {
            // Use platform-specific default message if no custom text is provided
            string title = PixelsorterApp.Resources.Languages.OtherStrings.ShareFeature_ShareDialog_Title;
            string message = text ?? GetPlatformDefaultMessage();

#if ANDROID
            try
            {
                var context = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity
                    ?? Android.App.Application.Context;

                var file = new AFile(filePath);

                // MAUI registers authority as {PackageName}.fileProvider (capital P)
                var authority = $"{context.PackageName}.fileProvider";
                var uri = AndroidX.Core.Content.FileProvider.GetUriForFile(context, authority, file);

                var intent = new Intent(Intent.ActionSend);
                intent.SetType("image/png");
                intent.PutExtra(Intent.ExtraStream, uri);

                if (!string.IsNullOrEmpty(message))
                {
                    intent.PutExtra(Intent.ExtraText, message);
                }

                // Grant read permission to recipient app
                intent.ClipData = ClipData.NewRawUri(title, uri);
                intent.AddFlags(ActivityFlags.GrantReadUriPermission);

                var chooser = Intent.CreateChooser(intent, title);
                if (chooser != null)
                {
                    chooser.AddFlags(ActivityFlags.GrantReadUriPermission);

                    if (Microsoft.Maui.ApplicationModel.Platform.CurrentActivity == null)
                    {
                        chooser.AddFlags(ActivityFlags.NewTask);
                    }

                    context.StartActivity(chooser);
                }
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShareService] Android share intent failed: {ex}");
                // Fallback to MAUI default share if custom intent fails
                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = title,
                    File = new ShareFile(filePath)
                });
            }

#else
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = title,
                File = new ShareFile(filePath)
            });
#endif
        }

        private static string GetPlatformDefaultMessage()
        {
            String PlayStoreLink = "https://play.google.com/store/apps/details?id=org.haelbich.pixelsorter";
            String GithubLink = "https://github.com/h43lb1t0/PixelsorterApp/releases";
#if ANDROID
            return String.Format(PixelsorterApp.Resources.Languages.OtherStrings.ShareFeature_AdTextWithPlatformLink, PlayStoreLink);
#else
            return String.Format(PixelsorterApp.Resources.Languages.OtherStrings.ShareFeature_AdTextWithPlatformLink, GithubLink);
#endif
        }
    }
}
