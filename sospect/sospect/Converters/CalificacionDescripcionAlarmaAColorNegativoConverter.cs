using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace sospect.Converters
{
    public class CalificacionDescripcionAlarmaAColorNegativoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch (value?.ToString())
            {
                case "Positivo":
                    return Colors.Gray;
                case "Negativo":
                    return Colors.Blue;
                case "Apagado":
                    return Colors.Gray;
                default:
                    return Colors.Red;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
