using System;
using System.Collections.Generic;
using System.Linq;
using CoreGraphics;
using CoreLocation;
using MapKit;
using sospect.CustomRenderers;
using sospect.iOS.Renderers;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Maps;
using Xamarin.Forms.Maps.iOS;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(MiniMapa), typeof(MiniMapRenderer))]
namespace sospect.iOS.Renderers
{
    public class MiniMapRenderer : MapRenderer
    {
        UIView customPinView;
        List<CustomPin> customPins;
        MKPolylineRenderer polylineRenderer;
        MKPolyline polyline;
        CustomMKAnnotationView RutaEscape;

        protected override void OnElementChanged(ElementChangedEventArgs<View> e)
        {
            base.OnElementChanged(e);

            if (e.OldElement != null)
            {
                var nativeMap = Control as MKMapView;
                nativeMap.OverlayRenderer = null;
                //nativeMap.RemoveOverlays(nativeMap.Overlays);
                nativeMap.RemoveAnnotations(nativeMap.Annotations);
                nativeMap.GetViewForAnnotation = null;
                nativeMap.DidSelectAnnotationView -= OnDidSelectAnnotationView;
                nativeMap.DidDeselectAnnotationView -= OnDidDeselectAnnotationView;
                nativeMap.RemoveFromSuperview();
            }

            if (e.NewElement != null)
            {
                var formsMap = (MiniMapa)e.NewElement;
                var nativeMap = Control as MKMapView;
                customPins = formsMap.CustomPins;

                nativeMap.DidSelectAnnotationView += OnDidSelectAnnotationView;
                nativeMap.DidDeselectAnnotationView += OnDidDeselectAnnotationView;

                nativeMap.AddGestureRecognizer(new UITapGestureRecognizer(TapOnMap));
            }
        }

        void OnDidSelectAnnotationView(object sender, MKAnnotationViewEventArgs e)
        {
            var customView = e.View as CustomMKAnnotationView;
            customPinView = new UIView();
        }

        void OnDidDeselectAnnotationView(object sender, MKAnnotationViewEventArgs e)
        {
            if (!e.View.Selected)
            {
                customPinView.RemoveFromSuperview();
                customPinView.Dispose();
                customPinView = null;
            }
        }

        CustomPin GetCustomPin(MKPointAnnotation annotation)
        {
            var position = new Position(annotation.Coordinate.Latitude, annotation.Coordinate.Longitude);
            foreach (var pin in customPins)
            {
                if (pin.Position == position)
                {
                    return pin;
                }
            }
            return null;
        }

        void TapOnMap(UITapGestureRecognizer gestureRecognizer)
        {
            ((MKMapView)Control).RemoveAnnotations(((MKMapView)Control).Annotations);
            var cgPoint = gestureRecognizer.LocationInView(Control);

            var location = ((MKMapView)Control).ConvertPoint(cgPoint, Control);
            var annotation = new MKPointAnnotation()
            {
                Coordinate = new CLLocationCoordinate2D(location.Latitude , location.Longitude), // estas son coordenadas de ejemplo, reemplaza con las tuyas
            };

            ((MKMapView)Control).AddAnnotation(annotation);

            ((MiniMapa)Element).OnTap(new Xamarin.Forms.Maps.Position(location.Latitude, location.Longitude));
        }
    }
}
