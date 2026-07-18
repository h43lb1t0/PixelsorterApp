using System;
using System.Collections.Generic;
using System.Text;

namespace PixelsorterApp.Services
{
    public sealed class ShareService : IShareService
    {
        public async Task ShareImage(string filePath)
        {
            await Share.RequestAsync(new ShareFileRequest
            {
                Title = "Share Image",
                File = new ShareFile(filePath)
            });
        }
    }
}
