// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
using System.Globalization;
using Microsoft.Maui.Controls;

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
                return false; // Solo el botón "Renovar" oculto
            }
            else if (!flag_subscr_vencida && flag_renovable)
            {
                return true; // Ambos botones visibles
            }
            else // flag_subscr_vencida && flag_renovable
            {
                return true; // Solo el botón "Renovar" visible
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}


