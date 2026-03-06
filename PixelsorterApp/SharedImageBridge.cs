namespace PixelsorterApp
{
    /// <summary>
    /// Provides a thread-safe mechanism for sharing and consuming image paths between different components of an
    /// application.
    /// </summary>
    /// <remarks>This static class enables components to set a shared image path and allows other components
    /// to consume it in a synchronized manner. The SharedImageReceived event is raised whenever a new image path is
    /// set, enabling subscribers to react to image sharing events. All access to the shared image path is synchronized
    /// to ensure thread safety.</remarks>
    public static class SharedImageBridge
    {
        private static readonly object SyncRoot = new();
        private static string? pendingImagePath;

        public static event Action<string>? SharedImageReceived;

        /// <summary>
        /// Sets the path of the shared image and notifies subscribers of the updated path.
        /// </summary>
        /// <remarks>This method is thread-safe. It raises the SharedImageReceived event after updating
        /// the shared image path, allowing subscribers to respond to the change.</remarks>
        /// <param name="imagePath">The file system path to the image to be shared. This value must not be null or empty.</param>
        public static void SetSharedImagePath(string imagePath)
        {
            lock (SyncRoot)
            {
                pendingImagePath = imagePath;
            }

            SharedImageReceived?.Invoke(imagePath);
        }

        /// <summary>
        /// Attempts to retrieve and clear the pending image path in a thread-safe manner.
        /// </summary>
        /// <remarks>This method is thread-safe. If no pending image path is available, the <paramref
        /// name="imagePath"/> parameter is set to <see langword="null"/>.</remarks>
        /// <param name="imagePath">When this method returns, contains the pending image path if one was available; otherwise, <see
        /// langword="null"/>.</param>
        /// <returns><see langword="true"/> if a pending image path was successfully retrieved; otherwise, <see
        /// langword="false"/>.</returns>
        public static bool TryConsumePendingImagePath(out string? imagePath)
        {
            lock (SyncRoot)
            {
                imagePath = pendingImagePath;
                pendingImagePath = null;
            }

            return !string.IsNullOrWhiteSpace(imagePath);
        }
    }
}
