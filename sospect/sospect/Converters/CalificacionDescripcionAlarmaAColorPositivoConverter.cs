using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace sospect.Converters
{
    public class CalificacionDescripcionAlarmaAColorPositivoConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch (value?.ToString())
            {
                case "Positivo":
                    return Colors.Blue;
                case "Negativo":
                    return Colors.Gray;
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
