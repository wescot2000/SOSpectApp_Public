using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace sospect.Converters
{
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && boolValue)
            {
                // Vencida = gris (no rojo, porque rojo indica error/peligro)
                return Color.FromArgb("#7F8C8D");
            }
            else
            {
                // Activa = verde
                return Color.FromArgb("#1E8449");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
