// Converters/StarRatingConverter.cs
// Convierte (CalificacionActual, NumeroEstrella) → Color de la estrella.
// Uso en XAML:
//   <Label.TextColor>
//     <Binding Path="CalificacionActual"
//              Converter="{StaticResource StarRatingConverter}"
//              ConverterParameter="3"/>
//   </Label.TextColor>
// El ConverterParameter es el número de estrella (1-5).
// Si CalificacionActual >= NumeroEstrella → dorado (#FFD700)
// Si CalificacionActual  < NumeroEstrella → gris claro (#CCCCCC)
// Creado: 2026-03-28

using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace sospect.Converters
{
    public class StarRatingConverter : IValueConverter
    {
        private static readonly Color StarActivaColor = Color.FromArgb("#FFD700");   // Dorado
        private static readonly Color StarVaciaColor  = Color.FromArgb("#CCCCCC");   // Gris claro

        /// <summary>
        /// Retorna el color de la estrella en función de la calificación actual.
        /// </summary>
        /// <param name="value">CalificacionActual (int 0-5). 0 = ninguna estrella seleccionada.</param>
        /// <param name="parameter">Número de la estrella a evaluar (int 1-5, pasado como string).</param>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int calificacionActual && parameter is string parametroStr
                && int.TryParse(parametroStr, out int numeroEstrella))
            {
                return calificacionActual >= numeroEstrella ? StarActivaColor : StarVaciaColor;
            }

            // Fallback seguro
            return StarVaciaColor;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
