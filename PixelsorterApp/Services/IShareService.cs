using System;
using System.Collections.Generic;
using System.Text;

namespace PixelsorterApp.Services
{
    public interface IShareService
    {
        Task ShareImage(string filePath, string? text = null);
    }
}
