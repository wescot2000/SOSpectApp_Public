using System;
using MapKit;

namespace sospect.iOS.Renderers
{
    public class CustomMKAnnotationView : MKAnnotationView
    {
        public object Id { get; set; }

        public string Url { get; set; }

        public CustomMKAnnotationView(IMKAnnotation annotation, string id)
            : base(annotation, id)
        {
        }
    }
}

