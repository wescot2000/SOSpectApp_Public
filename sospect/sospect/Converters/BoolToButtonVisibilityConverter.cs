using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Xamarin.Forms;

namespace sospect.Converters
{
    public class BoolToButtonVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var boolValues = ((string)value).Split(',');
            var flag_subscr_vencida = bool.Parse(boolValues[0]);
            var flag_renovable = bool.Parse(boolValues[1]);

            if (flag_subscr_vencida && !flag_renovable)
            {
                return false; // Ambos botones ocultos
            }
            else if (!flag_subscr_vencida && !flag_renovable)
            {
                return false; // Solo el boton "Renovar" oculto
            }
            else if (!flag_subscr_vencida && flag_renovable)
            {
                return true; // Ambos botones visibles
            }
            else // flag_subscr_vencida && flag_renovable
            {
                return true; // Solo el boton "Renovar" visible
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
