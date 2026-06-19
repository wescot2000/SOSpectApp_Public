// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

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

