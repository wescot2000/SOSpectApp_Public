using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace sospect.Converters
{
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool booleanValue)
            {
                return !booleanValue;
            }
            throw new InvalidOperationException("The target must be a boolean");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
