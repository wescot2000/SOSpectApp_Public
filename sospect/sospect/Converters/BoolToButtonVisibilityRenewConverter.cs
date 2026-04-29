using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace sospect.Converters
{
    public class BoolToButtonVisibilityRenewConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var flags = ((string)value).Split(',');
            var flag_subscr_vencida = bool.Parse(flags[0]);
            var flag_renovable = bool.Parse(flags[1]);
            // Tercer parámetro: es_promocion (oculta botón renovar para promociones)
            var es_promocion = flags.Length > 2 && bool.Parse(flags[2]);

            // Las promociones NO muestran botón de renovar
            if (es_promocion)
            {
                return false;
            }

            if (flag_subscr_vencida && !flag_renovable)
            {
                return false;
            }
            else if (!flag_subscr_vencida && flag_renovable)
            {
                return true;
            }
            else if (flag_subscr_vencida && flag_renovable)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
