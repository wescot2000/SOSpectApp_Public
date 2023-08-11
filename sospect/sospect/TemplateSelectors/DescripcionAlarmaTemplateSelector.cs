using System;
using sospect.Models;
using Xamarin.Forms;

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

