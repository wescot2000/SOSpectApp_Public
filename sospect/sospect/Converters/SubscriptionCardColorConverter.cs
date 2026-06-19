// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace sospect.Converters
{
    /// <summary>
    /// Convierte el CombinedFlags de una suscripción al color de borde de la tarjeta
    /// - Promociones: Morado (#9B59B6)
    /// - Activas normales: Verde (#27AE60)
    /// - Vencidas: Gris (#BDC3C7)
    /// </summary>
    public class SubscriptionBorderColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string combinedFlags)
            {
                var parts = combinedFlags.Split(',');
                if (parts.Length >= 3)
                {
                    bool isVencida = parts[0].ToLower() == "true";
                    bool esPromocion = parts[2].ToLower() == "true";

                    if (esPromocion)
                    {
                        // Promociones siempre morado
                        return Color.FromArgb("#9B59B6");
                    }
                    else if (isVencida)
                    {
                        // Vencidas gris
                        return Color.FromArgb("#BDC3C7");
                    }
                    else
                    {
                        // Activas verde
                        return Color.FromArgb("#27AE60");
                    }
                }
            }
            return Color.FromArgb("#E0E0E0");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convierte el CombinedFlags de una suscripción al color de fondo de la tarjeta
    /// - Promociones: Morado muy claro (#F5F0F9)
    /// - Activas normales: Verde muy claro (#F0FFF4)
    /// - Vencidas: Gris muy claro (#F5F5F5)
    /// </summary>
    public class SubscriptionBackgroundColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string combinedFlags)
            {
                var parts = combinedFlags.Split(',');
                if (parts.Length >= 3)
                {
                    bool isVencida = parts[0].ToLower() == "true";
                    bool esPromocion = parts[2].ToLower() == "true";

                    if (esPromocion)
                    {
                        // Promociones morado muy claro
                        return Color.FromArgb("#F5F0F9");
                    }
                    else if (isVencida)
                    {
                        // Vencidas gris muy claro
                        return Color.FromArgb("#F5F5F5");
                    }
                    else
                    {
                        // Activas verde muy claro
                        return Color.FromArgb("#F0FFF4");
                    }
                }
            }
            return Colors.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convierte el CombinedFlags al color del texto de la tarjeta
    /// - Promociones activas: Morado oscuro (#7B3FA0)
    /// - Promociones vencidas: Gris (#7F8C8D)
    /// - Activas normales: Verde oscuro (#1E8449)
    /// - Vencidas normales: Gris (#7F8C8D)
    /// </summary>
    public class SubscriptionTextColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string combinedFlags)
            {
                var parts = combinedFlags.Split(',');
                if (parts.Length >= 3)
                {
                    bool isVencida = parts[0].ToLower() == "true";
                    bool esPromocion = parts[2].ToLower() == "true";

                    if (isVencida)
                    {
                        // Todas las vencidas en gris
                        return Color.FromArgb("#7F8C8D");
                    }
                    else if (esPromocion)
                    {
                        // Promociones activas morado oscuro
                        return Color.FromArgb("#7B3FA0");
                    }
                    else
                    {
                        // Activas normales verde oscuro
                        return Color.FromArgb("#1E8449");
                    }
                }
            }
            return Color.FromArgb("#2C3E50");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}


