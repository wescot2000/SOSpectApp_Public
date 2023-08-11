using System;
namespace sospect.Extensions
{
    public static class DoubleExtensions
    {
        public static double Trim(this double num, int decimalPlaces)
        {
            double multiplier = Math.Pow(10, decimalPlaces);
            return Math.Round(num * multiplier) / multiplier;
        }
    }
}

