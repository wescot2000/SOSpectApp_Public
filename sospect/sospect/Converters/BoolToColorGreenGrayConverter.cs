using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace sospect.Converters
{
    /// <summary>
    /// Converter para marcos de tarjetas: true=Verde, false=LightGray
    /// </summary>
    public class BoolToColorGreenGrayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && boolValue)
            {
                // Usar un verde brillante similar a los labels (#00C853 - Material Green A700)
                return Color.FromArgb("#00C853");
            }
            else
            {
                return Colors.LightGray; // Gris claro para alarmas normales
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
