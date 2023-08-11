using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using Xamarin.Forms;

namespace sospect.Converters
{
    public class BoolToButtonVisibilityRenewConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var flags = ((string)value).Split(',');
            var flag_subscr_vencida = bool.Parse(flags[0]);
            var flag_renovable = bool.Parse(flags[1]);

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
