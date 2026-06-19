// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
using System.Globalization;
using System.Linq;
using Microsoft.Maui.Controls;
using sospect.Models;

namespace sospect.Converters
{
    public class TipoAlarmaVisibilityConverter : IValueConverter
    {
        // IDs de tipos de alarma que permiten cambio a "Delito o crimen cometido"
        private static readonly long[] TIPOS_PERMITIDOS_CAMBIO = { 1, 3, 6, 8 };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is AlarmaCercana alarma)
            {
                // Mostrar picker si:
                // 1. Es propietario de la alarma
                // 2. Es tipo 9 (sospechoso huyendo)
                // 3. El tipo permite cambio a "Delito o crimen cometido" (tipos 1, 3, 6, 8)
                return alarma.flag_propietario_alarma ||
                       alarma.tipoalarma_id == 9 ||
                       TIPOS_PERMITIDOS_CAMBIO.Contains(alarma.tipoalarma_id);
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}


