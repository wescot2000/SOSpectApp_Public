// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

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

