

namespace PixelsorterApp.Models.Licenses
{
    public partial class LicenseGroup(string title, IEnumerable<LicenseInfo> licenses) : List<LicenseInfo>(licenses)
    {
        public string Title { get; } = title;

        public bool HasTitle => !string.IsNullOrWhiteSpace(Title);
    }
}
