using System;
using System.Globalization;
using Xamarin.Forms;

namespace sospect.Converters
{
    public class BooleanToFontAttributeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool valor)
            {
                if (valor)
                {
                    return FontAttributes.Bold;
                }
                else
                {
                    return FontAttributes.None;
                }
            }
            else
            {
                return FontAttributes.None;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

