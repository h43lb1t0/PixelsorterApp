namespace PixelsorterApp.Models.Licenses
{
    public class LicenseInfo
    {
        public required string PackageName { get; set; }
        public required string Authors { get; set; }
        public required string PackageVersion { get; set; }
        public required string LicenseType { get; set; }
        public required string LicenseUrl { get; set; }
    }
}
