// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Maps.Handlers;
using sospect.CustomRenderers;
using System.Linq;
using sospect.Helpers;

#if ANDROID
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Microsoft.Maui.Platform;
#elif IOS || MACCATALYST
using MapKit;
using UIKit;
using CoreLocation;
using CoreGraphics;
using Microsoft.Maui.Maps.Platform;
#endif

namespace sospect.Handlers
{
    public class MiniMapaHandler : MapHandler
    {
        public static IPropertyMapper<Microsoft.Maui.Maps.IMap, IMapHandler> CustomMapper =
            new PropertyMapper<Microsoft.Maui.Maps.IMap, IMapHandler>(Mapper)
            {
                [nameof(Microsoft.Maui.Maps.IMap.Pins)] = MapPins
            };

        public MiniMapaHandler() : base(CustomMapper, CommandMapper)
        {
        }

        public MiniMapaHandler(IPropertyMapper mapper, CommandMapper commandMapper = null)
            : base(mapper ?? CustomMapper, commandMapper ?? CommandMapper)
        {
        }

#if ANDROID
        private GoogleMap _googleMap;
        private bool _handlersConnected = false;

        protected override void ConnectHandler(Android.Gms.Maps.MapView platformView)
        {
            base.ConnectHandler(platformView);

            if (platformView != null)
            {
                // CRÍTICO: Asegurar que el MapView puede recibir toques
                platformView.Clickable = true;
                platformView.Focusable = true;
                platformView.FocusableInTouchMode = true;

                platformView.GetMapAsync(new MapReadyCallback(this));
            }
        }

        protected override void DisconnectHandler(Android.Gms.Maps.MapView platformView)
        {
            if (_googleMap != null && _handlersConnected)
            {
                _googleMap.MarkerClick -= GoogleMap_MarkerClick;
                _handlersConnected = false;
            }

            base.DisconnectHandler(platformView);
        }

        public void OnMapReady(GoogleMap googleMap)
        {
            try
            {
                _googleMap = googleMap;

                if (_googleMap != null)
                {
                    // CRÍTICO: Configuración MINIMALISTA de gestos
                    var uiSettings = _googleMap.UiSettings;

                    // Desactivar TODO primero
                    uiSettings.ZoomControlsEnabled = false;
                    uiSettings.CompassEnabled = false;
                    uiSettings.MyLocationButtonEnabled = false;
                    uiSettings.IndoorLevelPickerEnabled = false;
                    uiSettings.MapToolbarEnabled = false;

                    // Ahora activar SOLO lo necesario
                    uiSettings.ZoomGesturesEnabled = true;
                    uiSettings.ScrollGesturesEnabled = true;
                    uiSettings.RotateGesturesEnabled = false;
                    uiSettings.TiltGesturesEnabled = false;

                    // CRÍTICO: Bloquear InfoWindows
                    _googleMap.SetOnMarkerClickListener(new MarkerClickListener());

                    ConnectEventHandlers();

                    System.Diagnostics.Debug.WriteLine("MiniMapaHandler: Configuración aplicada exitosamente");

                    // FIX 2026-02-28: Dibujar pines que ya se agregaron ANTES de que el mapa estuviera listo.
                    // MapPins() solo se ejecuta cuando _googleMap != null, pero el code-behind puede haber
                    // llamado a Pins.Add() antes de que OnMapReady se disparara (race condition).
                    if (VirtualView != null && VirtualView.Pins.Count > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"MiniMapaHandler: Dibujando {VirtualView.Pins.Count} pin(es) pendiente(s) al inicio del mapa");
                        UpdatePins(_googleMap, VirtualView);
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "MiniMapaHandler", "OnMapReady");
                System.Diagnostics.Debug.WriteLine($"MiniMapaHandler: Error en OnMapReady: {ex.Message}");
            }
        }

        private void ConnectEventHandlers()
        {
            if (_googleMap != null && !_handlersConnected)
            {
                _googleMap.MarkerClick += GoogleMap_MarkerClick;
                _handlersConnected = true;
            }
        }

        void GoogleMap_MarkerClick(object sender, GoogleMap.MarkerClickEventArgs e)
        {
            e.Handled = true;
        }

        private class MarkerClickListener : Java.Lang.Object, GoogleMap.IOnMarkerClickListener
        {
            public bool OnMarkerClick(Marker marker)
            {
                return true;
            }
        }

        private class MapReadyCallback : Java.Lang.Object, IOnMapReadyCallback
        {
            private readonly MiniMapaHandler _handler;

            public MapReadyCallback(MiniMapaHandler handler)
            {
                _handler = handler;
            }

            public void OnMapReady(GoogleMap googleMap)
            {
                _handler.OnMapReady(googleMap);
            }
        }

        public static void MapPins(IMapHandler handler, Microsoft.Maui.Maps.IMap map)
        {
            if (handler is MiniMapaHandler miniHandler && miniHandler._googleMap != null)
            {
                UpdatePins(miniHandler._googleMap, map);
            }
        }

        private static void UpdatePins(GoogleMap googleMap, Microsoft.Maui.Maps.IMap map)
{
    MainThread.BeginInvokeOnMainThread(() =>
    {
        googleMap.Clear();

        foreach (var pin in map.Pins)
        {
            var markerOptions = new MarkerOptions();
            markerOptions.SetPosition(new LatLng(pin.Location.Latitude, pin.Location.Longitude));
            markerOptions.SetTitle(pin.Label ?? "");
            markerOptions.SetSnippet("");
            
            // CRÍTICO: Verificar si es un CustomPin con imagen personalizada
            if (pin is CustomPin customPin && !string.IsNullOrEmpty(customPin.ImageSource))
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"MiniMapaHandler: Intentando cargar imagen: {customPin.ImageSource}");
                    
                    var context = Android.App.Application.Context;
                    
                    // Remover la extensión .png del nombre
                    var imageName = customPin.ImageSource.Replace(".png", "").Replace(".jpg", "");
                    
                    var resourceId = context.Resources.GetIdentifier(
                        imageName, 
                        "drawable", 
                        context.PackageName);
                    
                    if (resourceId != 0)
                    {
                        markerOptions.InvokeIcon(BitmapDescriptorFactory.FromResource(resourceId));
                        System.Diagnostics.Debug.WriteLine($"MiniMapaHandler: Imagen cargada exitosamente: {imageName}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"MiniMapaHandler: Recurso no encontrado, usando pin verde por defecto");
                        markerOptions.InvokeIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueGreen));
                    }
                }
                catch (Exception ex)
                {
                    CrashlyticsHelper.LogError(ex, "MiniMapaHandler", "UpdatePins");
                    System.Diagnostics.Debug.WriteLine($"MiniMapaHandler: Error cargando imagen: {ex.Message}");
                    markerOptions.InvokeIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueGreen));
                }
            }
            else
            {
                // Pin verde por defecto si no hay imagen personalizada
                markerOptions.InvokeIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueGreen));
            }

            googleMap.AddMarker(markerOptions);
        }
    });
}
#elif IOS || MACCATALYST
        private MauiMKMapView _mapView;

        protected override void ConnectHandler(MauiMKMapView platformView)
        {
            base.ConnectHandler(platformView);

            _mapView = platformView;

            if (_mapView != null)
            {
                // CRÍTICO: Configurar gestos EXACTAMENTE como CustomMap
                _mapView.ZoomEnabled = true;
                _mapView.ScrollEnabled = true;
                _mapView.RotateEnabled = false;
                _mapView.PitchEnabled = false;

                _mapView.GetViewForAnnotation = GetViewForAnnotation;

                Console.WriteLine("[MINIMAP-DIAG] iOS ConnectHandler: Mapa configurado. GetViewForAnnotation asignado.");
            }
            else
            {
                Console.WriteLine("[MINIMAP-DIAG] iOS ConnectHandler: platformView es NULL");
            }
        }

        protected override void DisconnectHandler(MauiMKMapView platformView)
        {
            if (_mapView != null)
            {
                _mapView.GetViewForAnnotation = null;
            }
            base.DisconnectHandler(platformView);
        }

        public static void MapPins(IMapHandler handler, Microsoft.Maui.Maps.IMap map)
        {
            Console.WriteLine($"[MINIMAP-DIAG] iOS MapPins llamado. handler={handler?.GetType()?.Name} PlatformView={handler?.PlatformView?.GetType()?.Name ?? "null"}");

            if (handler.PlatformView is not MauiMKMapView mapView)
            {
                Console.WriteLine("[MINIMAP-DIAG] iOS MapPins: PlatformView NO es MauiMKMapView, saliendo.");
                return;
            }

            Console.WriteLine($"[MINIMAP-DIAG] iOS MapPins: Pins.Count={map.Pins.Count} GetViewForAnnotation={mapView.GetViewForAnnotation != null}");
            foreach (var p in map.Pins)
            {
                Console.WriteLine($"[MINIMAP-DIAG] iOS MapPins pin: Label='{p.Label}' Address='{p.Address}' EsCustomPin={p is CustomPin} ImageSource='{(p is CustomPin cp ? cp.ImageSource : "N/A")}'");
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                var annotationsToRemove = mapView.Annotations
                    .Where(a => a is not MKUserLocation)
                    .ToArray();
                Console.WriteLine($"[MINIMAP-DIAG] iOS MapPins MainThread: removiendo={annotationsToRemove.Length}");
                mapView.RemoveAnnotations(annotationsToRemove);

                foreach (var pin in map.Pins)
                {
                    Console.WriteLine($"[MINIMAP-DIAG] iOS MapPins MainThread: agregando anotación Label='{pin.Label}'");
                    var annotation = new MKPointAnnotation
                    {
                        Title = pin.Label,
                        Coordinate = new CLLocationCoordinate2D(
                            pin.Location.Latitude,
                            pin.Location.Longitude)
                    };
                    mapView.AddAnnotation(annotation);
                }
            });
        }

        MKAnnotationView GetViewForAnnotation(MKMapView mapView, IMKAnnotation annotation)
        {
            Console.WriteLine($"[MINIMAP-DIAG] GetViewForAnnotation LLAMADO. tipo={annotation?.GetType()?.Name}");

            if (annotation is MKUserLocation)
            {
                Console.WriteLine("[MINIMAP-DIAG] GetViewForAnnotation: es MKUserLocation, retornando null");
                return null;
            }

            const string annotationId = "miniMapPin";
            var annotationView = mapView.DequeueReusableAnnotation(annotationId) as MKAnnotationView;

            if (annotationView == null)
            {
                annotationView = new MKAnnotationView(annotation, annotationId);
                Console.WriteLine("[MINIMAP-DIAG] GetViewForAnnotation: nueva MKAnnotationView creada");
            }
            else
            {
                Console.WriteLine("[MINIMAP-DIAG] GetViewForAnnotation: reutilizando MKAnnotationView del pool");
            }

            annotationView.Annotation = annotation;
            annotationView.CanShowCallout = false;

            var image = UIImage.FromBundle("direccionescapesospechoso");
            Console.WriteLine($"[MINIMAP-DIAG] FromBundle('direccionescapesospechoso')={image != null}");

            if (image == null)
            {
                image = UIImage.FromBundle("direccionescapesospechoso.png");
                Console.WriteLine($"[MINIMAP-DIAG] FromBundle('direccionescapesospechoso.png')={image != null}");
            }

            if (image == null)
            {
                image = UIImage.FromFile("direccionescapesospechoso.png");
                Console.WriteLine($"[MINIMAP-DIAG] FromFile('direccionescapesospechoso.png')={image != null}");
            }

            if (image != null)
            {
                Console.WriteLine($"[MINIMAP-DIAG] Imagen cargada OK. Size={image.Size.Width}x{image.Size.Height}");
                annotationView.Image = image;
                // Anclar la punta inferior del icono al punto exacto tocado.
                // Por defecto iOS centra la imagen en la coordenada; desplazamos
                // la vista hacia arriba la mitad de la altura para que la punta quede abajo.
                annotationView.CenterOffset = new CGPoint(0, -image.Size.Height / 2);
            }
            else
            {
                Console.WriteLine("[MINIMAP-DIAG] TODAS las búsquedas de imagen fallaron. Usando mappin del sistema.");
                annotationView.Image = UIImage.GetSystemImage("mappin");
                annotationView.CenterOffset = new CGPoint(0, -12);
            }

            return annotationView;
        }
#endif
    }
}

