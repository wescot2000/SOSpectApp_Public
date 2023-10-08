using System;
using System.Globalization;
using sospect.Models;
using sospect.ViewModels;
using Xamarin.Forms;

namespace sospect.Converters
{
    public class VisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is AlarmaCercana alarma)
            {
                return alarma.flag_propietario_alarma || alarma.flag_es_policia;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

