using System;
using SkiaSharp;
using System.Globalization;
using Xamarin.Forms;

namespace sospect.Converters
{
    public class SKColorToXamarinColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SKColor skColor)
            {
                return new Color(skColor.Red / 255.0, skColor.Green / 255.0, skColor.Blue / 255.0, skColor.Alpha / 255.0);
            }
            return Color.Default;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}

