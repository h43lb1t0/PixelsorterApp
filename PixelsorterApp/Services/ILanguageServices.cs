using System;
using System.Collections.Generic;
using System.Text;

namespace PixelsorterApp.Services
{
    public interface ILanguageServices
    {
        public Dictionary<string, string> GetAvilableLanguages();

        public String GetCurrentLanguage();

        public void SetLanguage(string languageCode);
    }
}
