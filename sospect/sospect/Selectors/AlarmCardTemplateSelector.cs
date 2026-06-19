// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using Microsoft.Maui.Controls;
using sospect.Models;

namespace sospect.Selectors
{
    /// <summary>
    /// DataTemplateSelector para tarjetas de alarma estilo Twitter/X.
    /// Selecciona el template adecuado basado en la cantidad de fotos.
    /// CRÍTICO: Evita FlexLayout, MultiBinding, y converters complejos para estabilidad en MAUI.
    /// </summary>
    public class AlarmCardTemplateSelector : DataTemplateSelector
    {
        public DataTemplate NoPhotosTemplate { get; set; }
        public DataTemplate OnePhotoTemplate { get; set; }
        public DataTemplate TwoPhotosTemplate { get; set; }
        public DataTemplate ThreePhotosTemplate { get; set; }
        public DataTemplate FourPhotosTemplate { get; set; }
        public DataTemplate FivePlusPhotosTemplate { get; set; }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            if (item is not AlarmaCercana alarma)
                return NoPhotosTemplate;

            var cantidadFotos = alarma.CantidadFotos;

            return cantidadFotos switch
            {
                0 => NoPhotosTemplate,
                1 => OnePhotoTemplate,
                2 => TwoPhotosTemplate,
                3 => ThreePhotosTemplate,
                4 => FourPhotosTemplate,
                _ => FivePlusPhotosTemplate // 5 o más
            };
        }
    }
}


