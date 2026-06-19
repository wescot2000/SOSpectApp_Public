// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using sospect.Helpers;

namespace sospect.Converters
{
    public class FlagPropietarioAlarmaToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var LblAlertaReportadaPorMi = TranslateExtension.Translate("LblAlertaReportadaPorMi");
            if (value is bool flag_propietario_alarma)
            {
                return flag_propietario_alarma ? LblAlertaReportadaPorMi : "";
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}


