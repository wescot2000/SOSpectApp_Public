// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
using MapKit;

namespace sospect.Platforms.iOS.Handlers
{
    public class CustomMKAnnotationView : MKAnnotationView
    {
        public string Id { get; set; }
        public string Url { get; set; }

        public CustomMKAnnotationView(IMKAnnotation annotation, string id)
            : base(annotation, id)
        {
            Id = id;
        }
    }
}

