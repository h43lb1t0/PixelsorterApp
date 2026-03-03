using System;
using System.Collections.Generic;
using System.Text;

namespace PixelsorterApp
{
    public interface IGalleryService
    {
        Task<bool> SaveImageAsync(byte[] imageBytes, string fileName);
    }
}
