// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
using System.Globalization;
using Microsoft.Maui.Controls;

#if ANDROID
using Android.Graphics;
#endif

namespace sospect.Converters
{
    /// <summary>
    /// Converter que carga recursos drawable de Android directamente.
    /// REEMPLAZA a: IconPathConverter, AndroidImageHelper, CustomImageHandler en popups
    /// </summary>
    public class ImageResourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || string.IsNullOrEmpty(value.ToString()))
                return null;

            var iconName = value.ToString()
                .Replace(".png", "")
                .Replace(".PNG", "")
                .ToLower();

#if ANDROID
            try
            {
                var context = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity ?? 
                             Microsoft.Maui.ApplicationModel.Platform.AppContext;
                
                if (context == null)
                    return null;

                // Buscar recurso en drawable o mipmap
                var resourceId = context.Resources.GetIdentifier(iconName, "drawable", context.PackageName);
                if (resourceId == 0)
                    resourceId = context.Resources.GetIdentifier(iconName, "mipmap", context.PackageName);

                if (resourceId == 0)
                {
                    // Fallback: sospecticon
                    resourceId = context.Resources.GetIdentifier("sospecticon", "drawable", context.PackageName);
                    if (resourceId == 0)
                        return null;
                }

                // Crear ImageSource desde URI del recurso Android
                return ImageSource.FromUri(new Uri($"android.resource://{context.PackageName}/{resourceId}"));
            }
            catch
            {
                return null;
            }
#else
            // iOS u otras plataformas
            return ImageSource.FromFile($"{iconName}.png");
#endif
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

