using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace sospect.Converters
{
    public class BoolToTextColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool booleanValue)
            {
                return booleanValue ? Colors.Black : Color.FromArgb("#999999");
            }
            return Colors.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
