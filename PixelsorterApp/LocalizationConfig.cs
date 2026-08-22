using System;
using System.Collections.Generic;
using System.Globalization;

namespace PixelsorterApp
{
    /// <summary>
    /// Configuration for application localization and supported cultures.
    /// The SupportedCultures property is automatically generated at build time in LocalizationConfig.g.cs
    /// based on available .resx files in Resources/Languages.
    /// </summary>
    public static partial class LocalizationConfig
    {
        private static readonly CultureInfo DefaultCulture = CultureInfo.GetCultureInfo("en-GB");
        
        /// <summary>
        /// Applies the base lnaguage of the devices language if the regional part is not supported, 
        /// the exact language if supported and the default language if not.
        /// </summary>
        public static void ApplyBestCulture()
        {
            var systemCulture = CultureInfo.CurrentUICulture;

            // 1. Is there an exact match? (e.g., system is exactly "de-DE")
            if (SupportedCultures.Any(c => c.Name == systemCulture.Name))
            {
                return;
            }
           

            // 2. Find a fallback with the same base language (e.g., system "de-CH" -> matches "de-DE")
            // Because your list is ordered by NativeName, if multiple exist (like en-GB and en-US), 
            // FirstOrDefault will pick en-GB first for any unsupported English region like "en-AU".
            var bestMatch = SupportedCultures
                .FirstOrDefault(c => c.TwoLetterISOLanguageName == systemCulture.TwoLetterISOLanguageName);

            // 3. If no base language matches (e.g., system is "fr-FR"), use the app's absolute default
            var finalCulture = bestMatch ?? DefaultCulture;

            // Apply the chosen culture to all future threads
            CultureInfo.DefaultThreadCurrentCulture = finalCulture;
            CultureInfo.DefaultThreadCurrentUICulture = finalCulture;

            // Apply immediately to the current thread
            CultureInfo.CurrentCulture = finalCulture;
            CultureInfo.CurrentUICulture = finalCulture;
        }
    }
}

