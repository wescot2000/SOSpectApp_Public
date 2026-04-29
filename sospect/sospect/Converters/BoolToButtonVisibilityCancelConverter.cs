using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace sospect.Converters
{
    public class BoolToButtonVisibilityCancelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var flags = ((string)value).Split(',');
            var flag_subscr_vencida = bool.Parse(flags[0]);
            var flag_renovable = bool.Parse(flags[1]);
            // Tercer parámetro: es_promocion (oculta botón cancelar para promociones)
            var es_promocion = flags.Length > 2 && bool.Parse(flags[2]);

            // Las promociones NO muestran botón de cancelar
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
                return false;
            }
            else
            {
                return true;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
