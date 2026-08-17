using System;
using System.Collections.Generic;
using System.Text;

namespace PixelsorterApp.Services
{
    public class LanguageServices : ILanguageServices
    {
        public Dictionary<string, string> GetAvilableLanguages()
        {
            var languages = new Dictionary<string, string>
            {
                { "English", "en-US" },
                { "Deutsch", "de-DE" }
            };

            return languages;
        }

        public string GetCurrentLanguage()
        {
            return "German";
        }


        public void SetLanguage(string languageCode)
        {
            return;
        }
    }
}
