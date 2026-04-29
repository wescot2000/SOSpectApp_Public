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