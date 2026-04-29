using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sospect.Models
{
    public class TutorialStep
    {
        public int StepNumber { get; set; }
        public string TitleKey { get; set; }  // Clave para traducción
        public string DescriptionKey { get; set; }  // Clave para traducción
        public string BackgroundImageSource { get; set; }  // Imagen de fondo opcional
        public string HighlightImageSource { get; set; }  // Imagen destacada opcional
        public bool HasHighlightImage => !string.IsNullOrEmpty(HighlightImageSource);
    }
}

