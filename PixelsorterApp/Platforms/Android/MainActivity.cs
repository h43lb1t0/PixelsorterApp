using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Webkit;

namespace PixelsorterApp
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    [IntentFilter([Android.Content.Intent.ActionSend],
              Categories = new[] { Android.Content.Intent.CategoryDefault },
              DataMimeType = "image/*")]
    public class MainActivity : MauiAppCompatActivity
    {
        /// <summary>
        /// Initializes the activity and processes the incoming intent when the activity is created.
        /// </summary>
        /// <remarks>Overrides the base implementation to perform additional setup specific to this
        /// activity. Always calls the base method to ensure proper initialization.</remarks>
        /// <param name="savedInstanceState">An optional bundle containing the activity's previously saved state, or null if no state was saved.</param>
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            HandleIntent(Intent);
        }

        /// <summary>
        /// Handles a new intent delivered to the activity while it is running.
        /// </summary>
        /// <remarks>This method is called when the activity receives a new intent via an existing
        /// instance, such as when launched with the singleTop launch mode. It is important to call the base
        /// implementation to ensure the intent is properly handled by the Android framework.</remarks>
        /// <param name="intent">The intent containing the new data to be processed. This parameter can be null if no intent is provided.</param>
        protected override void OnNewIntent(Android.Content.Intent? intent)
        {
            base.OnNewIntent(intent);
            HandleIntent(intent);
        }

        /// <summary>
        /// Processes an incoming intent to handle images shared from other applications.
        /// </summary>
        /// <remarks>If the intent contains a valid image, the method retrieves its URI and saves a copy
        /// to the application's cache directory. The path to the cached image is then made available to the
        /// application. Ensure that the intent's type is supported and that the image can be accessed by the
        /// application.</remarks>
        /// <param name="intent">The intent containing the shared image data. This parameter must not be null and should have an action of
        /// <see cref="Android.Content.Intent.ActionSend"/> with a MIME type that starts with "image/".</param>
        private void HandleIntent(Android.Content.Intent? intent)
        {
            if (intent?.Action == Android.Content.Intent.ActionSend && intent.Type != null)
            {
                if (intent.Type.StartsWith("image/"))
                {
                    // Get the URI of the shared image
                    if (intent.GetParcelableExtra(Android.Content.Intent.ExtraStream, Java.Lang.Class.FromType(typeof(Android.Net.Uri))) is Android.Net.Uri imageUri)
                    {
                        var extension = MimeTypeMap.Singleton?.GetExtensionFromMimeType(intent.Type);
                        var cacheFileName = string.IsNullOrWhiteSpace(extension)
                            ? $"shared_{Guid.NewGuid()}"
                            : $"shared_{Guid.NewGuid()}.{extension}";
                        var cachePath = Path.Combine(FileSystem.CacheDirectory, cacheFileName);

                        using var input = ContentResolver?.OpenInputStream(imageUri);
                        if (input is null)
                        {
                            return;
                        }

                        using var output = File.Create(cachePath);
                        input.CopyTo(output);

                        MainThread.BeginInvokeOnMainThread(() => SharedImageBridge.SetSharedImagePath(cachePath));
                    }
                }
            }
        }
    }
}
