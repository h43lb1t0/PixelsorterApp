using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace PixelsorterApp
{
    public class LocalizationConfig
    {
        public static readonly IReadOnlyList<CultureInfo> SupportedCultures =
            [
                new CultureInfo("en-GB"),
                new CultureInfo("de-DE")
            ];
    }
}
