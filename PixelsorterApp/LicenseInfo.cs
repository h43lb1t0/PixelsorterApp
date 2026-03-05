using System;
using System.Collections.Generic;
using System.Text;

namespace PixelsorterApp
{
    public class LicenseInfo
    {
        public required string PackageName { get; set; }
        public required string PackageVersion { get; set; }
        public required string LicenseType { get; set; }
        public required string LicenseUrl { get; set; }
    }
}
