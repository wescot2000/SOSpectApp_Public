using System;
using System.Globalization;
using sospect.Models;
using Xamarin.Forms;

namespace sospect.Converters
{
    public class TipoAlarmaVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is AlarmaCercana alarma)
            {
                return alarma.flag_propietario_alarma || alarma.tipoalarma_id == 9;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
