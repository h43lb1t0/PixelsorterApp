using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace PixelsorterApp
{
    public class LocalizationConfig
    {

        private static List<CultureInfo> unsortedInfos = [
                new CultureInfo("en-GB"),
                new CultureInfo("en-US"),
                new CultureInfo("de-DE")
            ];
        public static readonly IReadOnlyList<CultureInfo> SupportedCultures = unsortedInfos.OrderBy(x => x.NativeName).ToList();


    }
}
