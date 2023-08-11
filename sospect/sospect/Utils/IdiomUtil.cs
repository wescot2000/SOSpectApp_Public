using System;
using System.Globalization;

namespace sospect.Utils
{
    public static class IdiomUtil
    {
        public static string ObtenerCodigoDeIdioma()
        {
            CultureInfo currentCulture = CultureInfo.CurrentCulture;
            string languageCode = currentCulture.TwoLetterISOLanguageName;
            return languageCode;
        }
    }
}

