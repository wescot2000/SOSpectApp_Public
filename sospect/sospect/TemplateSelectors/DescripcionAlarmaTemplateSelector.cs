// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
using sospect.Models;
using Microsoft.Maui.Controls;

namespace sospect.TemplateSelectors
{
    public class DescripcionAlarmaTemplateSelector : DataTemplateSelector
    {
        public DataTemplate PropioTemplate { get; set; }
        public DataTemplate AjenoTemplate { get; set; }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            var describirAlarma = item as DetalleDescripcionAlarma;
            if (describirAlarma == null)
                return null;

            return describirAlarma.PropietarioDescripcion ? PropioTemplate : AjenoTemplate;
        }
    }
}



