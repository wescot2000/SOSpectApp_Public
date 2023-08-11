using System;
using System.Globalization;
using Xamarin.Forms;

namespace sospect.Converters
{
    public class CalificacionDescripcionAlarmaAColorPositivoConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch (value?.ToString())
            {
                case "Positivo":
                    return Color.Blue;
                case "Negativo":
                    return Color.Gray;
                case "Apagado":
                    return Color.Gray;
                default:
                    return Color.Red;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

