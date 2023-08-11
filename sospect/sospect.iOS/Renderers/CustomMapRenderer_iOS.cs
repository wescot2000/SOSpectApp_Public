using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.NetworkInformation;
using CoreGraphics;
using Foundation;
using MapKit;
using sospect.CustomRenderers;
using sospect.iOS.Renderers;
using sospect.Models;
using sospect.Views;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Maps;
using Xamarin.Forms.Maps.iOS;
using Xamarin.Forms.Platform.iOS;


[assembly: ExportRenderer(typeof(CustomMap), typeof(CustomMapRenderer))]
namespace sospect.iOS.Renderers
{
    public class CustomMapRenderer : MapRenderer
    {
        List<CustomPin> customPins;
        MKMapView nativeMap;
        UIView customPinView;

        protected override void OnElementChanged(ElementChangedEventArgs<View> e)
        {
            base.OnElementChanged(e);

            if (e.NewElement != null)
            {
                var formsMap = (CustomMap)e.NewElement;
                nativeMap = Control as MKMapView;
                customPins = formsMap.CustomPins;

                nativeMap.GetViewForAnnotation = GetViewForAnnotation;


                var tapGesture = new UITapGestureRecognizer(HandleTapGesture)
                {
                    NumberOfTapsRequired = 1,
                    CancelsTouchesInView = false
                };
                nativeMap.AddGestureRecognizer(tapGesture);
            }
        }


        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            if (e.PropertyName == nameof(CustomMap.Pins))
            {

                    if (((CustomMap)sender).CustomPins.Any())
                    {
                        customPins = (List<CustomPin>)((CustomMap)sender).Pins;
                    }
            }
        }

        MKAnnotationView GetViewForAnnotation(MKMapView mapView, IMKAnnotation annotation)
        {
            MKAnnotationView annotationView = null;

            if (annotation is MKUserLocation)
                return null;

            var customPin = GetCustomPin(annotation as MKPointAnnotation);
            if (customPin == null)
            {
                return null;
            }

            annotationView = mapView.DequeueReusableAnnotation(customPin.Id);
            if (annotationView == null)
            {
                annotationView = new CustomMKAnnotationView(annotation, customPin.Id);

                switch (customPin.TipoAlarma)
                {
                    case 1:
                        annotationView.Image = UIImage.FromFile("WarningPin.png");
                        break;
                    case 2:
                        annotationView.Image = UIImage.FromFile("DangerPin.png");
                        break;
                    case 3:
                        annotationView.Image = UIImage.FromFile("AmarilloPleitoCallejero.png");
                        break;
                    case 4:
                        annotationView.Image = UIImage.FromFile("VerdeAzulMascotaPerdida.png");
                        break;
                    case 5:
                        annotationView.Image = UIImage.FromFile("AzulPersonaPerdida.png");
                        break;
                    case 6:
                        annotationView.Image = UIImage.FromFile("NegroDisturbiosProtestas.png");
                        break;
                    case 7:
                        annotationView.Image = UIImage.FromFile("VerdeAcompanamientoVisual.png");
                        break;
                    case 8:
                        annotationView.Image = UIImage.FromFile("PurpuraViolenciaIntrafamiliar.png");
                        break;
                    case 9:
                        annotationView.Image = UIImage.FromFile("DireccionEscapeSospechoso.png");
                        break;
                    default:
                        annotationView.Image = UIImage.FromFile("user_location.png");
                        break;
                }

                annotationView.CalloutOffset = new CGPoint(0, 0);
                ((CustomMKAnnotationView)annotationView).Id = customPin.Id;

                // Agregamos la vista de callout personalizada.
                annotationView.CanShowCallout = true;
                var calloutView = new UILabel();
                calloutView.UserInteractionEnabled = true;
                calloutView.Text = customPin.Address;
                calloutView.BackgroundColor = UIColor.FromRGB(50, 50, 50);
                calloutView.Font = UIFont.SystemFontOfSize(12);
                calloutView.TextColor = UIColor.LabelColor;
                calloutView.AddGestureRecognizer(new UITapGestureRecognizer(() => NavigateToAlarm(customPin.Id.ToString())));
                annotationView.DetailCalloutAccessoryView = calloutView;
            }

            return annotationView;
        }



        public class TappableView : UIView
        {
            public Action OnTap { get; set; }

            public override void TouchesBegan(NSSet touches, UIEvent evt)
            {
                base.TouchesBegan(touches, evt);
                OnTap?.Invoke();
            }
        }


        private void NavigateToAlarm(string alarmaId)
        {
            if (alarmaId != "User") // Aquí verificamos si el ID del pin es "User"
            {
                App.Current.MainPage.Navigation.PushAsync(new DescribirPage(long.Parse(alarmaId)));
            }
            else
            {
                return; 
            }
        }


        void HandleTapGesture(UITapGestureRecognizer gestureRecognizer)
        {
            var touchLocation = gestureRecognizer.LocationInView(nativeMap);
            var coordinates = nativeMap.ConvertPoint(touchLocation, null);

            var annotations = nativeMap.Annotations;
            bool isTouchOnPin = false;
            foreach (var annotation in annotations)
            {
                var annotationView = nativeMap.ViewForAnnotation(annotation);
                if (annotationView == null) continue;

                var annotationFrame = annotationView.ConvertRectToView(annotationView.Bounds, nativeMap);

                if (annotationFrame.Contains(touchLocation))
                {
                    // Si se toca una anotación, set the flag
                    isTouchOnPin = true;
                    break;
                }
            }

            if (!isTouchOnPin)
            {
                // Si no se toca ninguna anotación, se puede abrir el popup del mapa
                var locationCoordinate = nativeMap.ConvertPoint(touchLocation, nativeMap);
                ((CustomMap)Element).OnTap(new Position(locationCoordinate.Latitude, locationCoordinate.Longitude));
            }
        }


        CustomPin GetCustomPin(MKPointAnnotation annotation)
        {
            if (annotation != null)
            {
                var position = new Position(annotation.Coordinate.Latitude, annotation.Coordinate.Longitude);
                if (App.CustomPins != null) // Comprobación de nulidad agregada aquí
                {
                    foreach (var pin in App.CustomPins)
                    {
                        if (pin.Position == position)
                        {
                            return pin;
                        }
                    }
                }
            }
            else
            {
                return null;
            }
            return null;
        }

    }
}
