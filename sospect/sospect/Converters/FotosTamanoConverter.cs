using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace sospect.Converters
{
    /// <summary>
    /// Converter para calcular el tamaño dinámico de las fotos en el collage estilo Twitter/X
    /// Según la cantidad de fotos, ajusta el ancho de cada una para crear el efecto de grid/collage
    /// </summary>
    public class FotosTamanoConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // Valores esperados:
            // values[0] = CantidadFotos (int)
            // values[1] = Orden de la foto actual (int) - índice basado en 1

            if (values == null || values.Length < 2)
                return 195.0; // Tamaño por defecto

            int cantidadFotos = 0;
            int orden = 0;

            // Parsear cantidad de fotos
            if (values[0] is int cantidad)
                cantidadFotos = cantidad;
            else if (values[0] != null && int.TryParse(values[0].ToString(), out int parsedCantidad))
                cantidadFotos = parsedCantidad;

            // Parsear orden
            if (values[1] is int ord)
                orden = ord;
            else if (values[1] != null && int.TryParse(values[1].ToString(), out int parsedOrden))
                orden = parsedOrden;

            // Lógica de tamaño dinámico estilo Twitter/X
            // Ancho disponible aproximado: 400px (ancho de tarjeta con márgenes)

            if (cantidadFotos == 0)
                return 0.0; // Sin fotos

            if (cantidadFotos == 1)
                return 400.0; // Una foto ocupa todo el ancho

            if (cantidadFotos == 2)
                return 195.0; // Dos fotos lado a lado (195 + 195 + margen = ~400)

            if (cantidadFotos == 3)
            {
                if (orden == 1)
                    return 400.0; // Primera foto grande (toda la fila)
                else
                    return 195.0; // Las otras dos pequeñas en la segunda fila
            }

            if (cantidadFotos == 4)
            {
                // Grid de 2x2
                return 195.0;
            }

            if (cantidadFotos >= 5)
            {
                // Grid de 2x3 (primera fila 2, segunda fila 3)
                // Pero limitamos a 5 fotos, así que:
                // Fila 1: 2 fotos de 195px
                // Fila 2: 3 fotos de 128px cada una
                if (orden <= 2)
                    return 195.0; // Primera fila
                else
                    return 128.0; // Segunda fila (3 fotos más pequeñas)
            }

            return 195.0; // Fallback
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("FotosTamanoConverter no soporta ConvertBack");
        }
    }
}
