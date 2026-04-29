using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using SkiaSharp;

namespace sospect.Converters
{
    public class SKColorToXamarinColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SKColor skColor)
            {
                return new Color((float)(skColor.Red / 255.0), (float)(skColor.Green / 255.0), (float)(skColor.Blue / 255.0), (float)(skColor.Alpha / 255.0));
            }
            return Colors.Transparent; // Cambié a Colors en lugar de Color.Default
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
