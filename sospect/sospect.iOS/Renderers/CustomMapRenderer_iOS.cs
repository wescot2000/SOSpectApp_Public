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

        public UIImage ResizeImage(UIImage sourceImage, float width, float height)
        {
            UIGraphics.BeginImageContext(new CGSize(width, height));
            sourceImage.Draw(new CGRect(0, 0, width, height));
            var resultImage = UIGraphics.GetImageFromCurrentImageContext();
            UIGraphics.EndImageContext();
            return resultImage;
        }

        public UIImage OverlayImages(UIImage baseImage, UIImage overlayImage, CGPoint position)
        {
            UIGraphics.BeginImageContext(baseImage.Size);
            baseImage.Draw(new CGRect(0, 0, baseImage.Size.Width, baseImage.Size.Height));
            var overlayRect = new CGRect(position.X, position.Y, overlayImage.Size.Width, overlayImage.Size.Height);
            overlayImage.Draw(overlayRect);
            var resultImage = UIGraphics.GetImageFromCurrentImageContext();
            UIGraphics.EndImageContext();
            return resultImage;
        }



        private UIImage CreateCustomMarker(string iconImageName, int cantidadInteracciones, bool isBeingAttended, bool estadoAlarma)
        {
            UIImage pinImage = UIImage.FromFile(iconImageName);

            if (isBeingAttended)
            {
                string policeIconName = estadoAlarma ? "man_officer_police_icon.png" : "man_officer_police_icon_grey.png";
                UIImage policeImage = UIImage.FromFile(policeIconName);

                // Redimensiona la imagen del ícono de policía
                var resizedPoliceIcon = ResizeImage(policeImage, 30, 30);  // Asume que quieres que el ícono de la policía tenga un tamaño de 30x30.

                // Coloca la imagen redimensionada del ícono de policía en la imagen principal
                pinImage = OverlayImages(pinImage, resizedPoliceIcon, new CGPoint(pinImage.Size.Width - resizedPoliceIcon.Size.Width - 0, -4));
            }

            if (cantidadInteracciones > 0)
            {
                // Crea el badge de notificaciones en una nueva imagen
                var badgeDiameter = 40.0f;
                UIGraphics.BeginImageContext(new CGSize(badgeDiameter, badgeDiameter));
                var context = UIGraphics.GetCurrentContext();

                UIColor badgeColor = estadoAlarma ? UIColor.Red : UIColor.Gray; 
                context.SetFillColor(badgeColor.CGColor);
                context.FillEllipseInRect(new CGRect(0, 0, badgeDiameter, badgeDiameter));

                context.SetFillColor(UIColor.White.CGColor);
                var attributes = new UIStringAttributes
                {
                    Font = UIFont.SystemFontOfSize(30.0f),
                    ForegroundColor = UIColor.White,
                    ParagraphStyle = new NSMutableParagraphStyle { Alignment = UITextAlignment.Center }
                };
                NSString interaccionesString = new NSString(cantidadInteracciones.ToString());
                var size = interaccionesString.GetSizeUsingAttributes(attributes);
                interaccionesString.DrawString(new CGRect((badgeDiameter - size.Width) / 2, (badgeDiameter - size.Height) / 2, size.Width, size.Height), attributes);

                UIImage badgeImage = UIGraphics.GetImageFromCurrentImageContext();
                UIGraphics.EndImageContext();

                // Redimensiona el badge de notificaciones
                var resizedBadge = ResizeImage(badgeImage, 20, 20); // Asume que quieres que el badge tenga un tamaño de 20x20.

                // Coloca el badge redimensionado en la imagen principal
                pinImage = OverlayImages(pinImage, resizedBadge, new CGPoint(1, 0)); // Asume que quieres que el badge esté en la posición (10,10)
            }


            return pinImage;
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

                UIImage finalImage;

                var imagenSeleccionada = "user_location.png"; // Imagen por defecto

                if (customPin.AlarmaCercana != null)
                {
                    if (customPin.AlarmaCercana.estado_alarma)
                    {
                        switch (customPin.TipoAlarma)
                        {
                            case 1:
                                imagenSeleccionada = "WarningPin.png";
                                break;
                            case 2:
                                imagenSeleccionada = "DangerPin.png";
                                break;
                            case 3:
                                imagenSeleccionada = "AmarilloPleitoCallejero.png";
                                break;
                            case 4:
                                imagenSeleccionada = "VerdeAzulMascotaPerdida.png";
                                break;
                            case 5:
                                imagenSeleccionada = "AzulPersonaPerdida.png";
                                break;
                            case 6:
                                imagenSeleccionada = "NegroDisturbiosProtestas.png";
                                break;
                            case 7:
                                imagenSeleccionada = "VerdeAcompanamientoVisual.png";
                                break;
                            case 8:
                                imagenSeleccionada = "PurpuraViolenciaIntrafamiliar.png";
                                break;
                            case 9:
                                imagenSeleccionada = "DireccionEscapeSospechoso.png";
                                break;
                        }
                    }
                    else
                    {
                        if (customPin.TipoAlarma == 9)
                        {
                            imagenSeleccionada = "DireccionEscapeSospechosoCerrada.png";
                        }
                        else if (customPin.TipoAlarma >= 1 && customPin.TipoAlarma <= 9)
                        {
                            imagenSeleccionada = "ClosedAlarmPin.png";
                        }
                    }
                    finalImage = CreateCustomMarker(imagenSeleccionada, customPin.AlarmaCercana.cantidad_interacciones, customPin.AlarmaCercana.flag_alarma_siendo_atendida, customPin.AlarmaCercana.estado_alarma);
                }
                else
                {
                    finalImage = UIImage.FromFile(imagenSeleccionada);
                }

                annotationView.Image = finalImage;

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
