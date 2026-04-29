using System;
using System.Collections.Generic;
using System.Linq;
using CoreGraphics;
using Foundation;
using MapKit;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Maps.Handlers;
using Microsoft.Maui.Maps.Platform;
using sospect.CustomRenderers;
using sospect.Models;
using sospect.Views;
using sospect.Helpers;
using UIKit;
using Location = Microsoft.Maui.Devices.Sensors.Location;

namespace sospect.Platforms.iOS.Handlers
{
    public class CustomMapHandler : MapHandler
    {
        public static new IPropertyMapper<CustomMap, CustomMapHandler> Mapper =
            new PropertyMapper<CustomMap, CustomMapHandler>(MapHandler.Mapper)
            {
                [nameof(CustomMap.CustomPins)] = MapCustomPins
            };

        public CustomMapHandler() : base(Mapper)
        {
        }

        private List<CustomPin> customPins = new();
        private MKMapView nativeMap;

        // OPTIMIZACIÓN: Hash de pines para detectar cambios reales (evita actualizaciones redundantes)
        private string _lastPinsHash = null;

        // Rastreo del pin cuyo callout está actualmente visible.
        // Necesario para distinguir "tap en el cuerpo del callout" de "tap en el mapa".
        private CustomPin _calloutVisibleForPin = null;

        /// <summary>
        /// Anotación personalizada que guarda referencia directa al CustomPin.
        /// Elimina la necesidad de buscar por coordenadas en GetViewForAnnotation.
        /// </summary>
        private class CustomPinAnnotation : MKPointAnnotation
        {
            public CustomPin CustomPin { get; }
            public CustomPinAnnotation(CustomPin pin)
            {
                CustomPin = pin;
                Coordinate = new CoreLocation.CLLocationCoordinate2D(pin.Location.Latitude, pin.Location.Longitude);
                Title = pin.Label;
                Subtitle = pin.Address;
            }
        }

        protected override void ConnectHandler(MauiMKMapView platformView)
        {
            base.ConnectHandler(platformView);
            nativeMap = platformView;

            if (nativeMap != null)
            {
                ApplyMapStyle();
                nativeMap.GetViewForAnnotation = GetViewForAnnotation;

                // Rastrear qué pin tiene el callout abierto para manejar tap en el cuerpo del callout.
                nativeMap.DidSelectAnnotationView += OnAnnotationViewSelected;
                nativeMap.DidDeselectAnnotationView += OnAnnotationViewDeselected;

                var tapGesture = new UITapGestureRecognizer(HandleTapGesture)
                {
                    NumberOfTapsRequired = 1,
                    CancelsTouchesInView = false,
                    ShouldRecognizeSimultaneously = (_, __) => true
                };
                nativeMap.AddGestureRecognizer(tapGesture);

                // CRÍTICO: Resetear hash al reconectar el handler para forzar repintado completo.
                // Sin esto, si los pines no cambiaron entre sesiones, UpdateCustomPins retorna
                // inmediatamente sin agregar anotaciones al mapa recién creado.
                _lastPinsHash = null;

                UpdateCustomPins();
            }
        }

        protected override void DisconnectHandler(MauiMKMapView platformView)
        {
            if (nativeMap != null)
            {
                // CRÍTICO iOS — workaround para SIGSEGV en MauiCALayerAutosizeToSuperLayerBehavior:
                // Remover todas las anotaciones ANTES de desconectar, para que los MKAnnotationView
                // (y sus UILabel/UIButton de callout) sean liberados mientras el mapa aún es válido.
                // Sin esto, el GC finaliza esos objetos en el hilo finalizador e intenta enviar
                // mensajes ObjC a superlayers ya liberados → SIGSEGV (signal 11).
                var annotationsToRemove = nativeMap.Annotations?
                    .Where(a => !(a is MKUserLocation))
                    .ToArray();
                if (annotationsToRemove?.Length > 0)
                    nativeMap.RemoveAnnotations(annotationsToRemove);

                nativeMap.GetViewForAnnotation = null;
                nativeMap.DidSelectAnnotationView -= OnAnnotationViewSelected;
                nativeMap.DidDeselectAnnotationView -= OnAnnotationViewDeselected;

                var gestures = nativeMap.GestureRecognizers?.ToArray();
                if (gestures != null)
                {
                    foreach (var gesture in gestures)
                    {
                        if (gesture is UITapGestureRecognizer)
                            nativeMap.RemoveGestureRecognizer(gesture);
                    }
                }

                _calloutVisibleForPin = null;
                nativeMap = null; // Liberar referencia para que el GC pueda colectarlo
            }
            base.DisconnectHandler(platformView);
        }

        static void MapCustomPins(CustomMapHandler handler, CustomMap map)
        {
            handler.UpdateCustomPins();
        }

        // OPTIMIZADO: UpdateCustomPins con detección de cambios reales
        void UpdateCustomPins()
        {
            if (nativeMap == null || VirtualView is not CustomMap customMap)
                return;

            var pinsToProcess = customMap.CustomPins ?? new List<CustomPin>();

            // OPTIMIZACIÓN: Calcular hash para detectar cambios reales
            var newPinsHash = CalculatePinsHash(pinsToProcess);

            // Si los pines no cambiaron, no hacer nada
            if (newPinsHash == _lastPinsHash && customPins.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine("iOS CustomMapHandler: UpdateCustomPins IGNORADO - Sin cambios");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"iOS CustomMapHandler: UpdateCustomPins - {pinsToProcess.Count} pins");
            _lastPinsHash = newPinsHash;

            // Limpiar anotaciones existentes (excepto la ubicación del usuario)
            var annotationsToRemove = nativeMap.Annotations.Where(a => !(a is MKUserLocation)).ToArray();
            nativeMap.RemoveAnnotations(annotationsToRemove);

            customPins = pinsToProcess;

            foreach (var pin in customPins)
            {
                var annotation = CreateAnnotation(pin);
                if (annotation != null)
                {
                    nativeMap.AddAnnotation(annotation);
                }
            }
        }

        /// <summary>
        /// OPTIMIZACIÓN: Calcula un hash de los pines para detectar cambios reales.
        /// </summary>
        private string CalculatePinsHash(List<CustomPin> pins)
        {
            if (pins == null || !pins.Any())
                return "empty";

            var hashInput = string.Join("|", pins
                .OrderBy(p => p.MarkerId ?? p.Id ?? "")
                .Select(p => $"{p.MarkerId ?? p.Id}:{p.Location?.Latitude:F5}:{p.Location?.Longitude:F5}:{p.TipoAlarma}"));

            return hashInput.GetHashCode().ToString("X");
        }

        MKPointAnnotation CreateAnnotation(CustomPin pin)
        {
            try
            {
                // Usar CustomPinAnnotation para llevar la referencia al CustomPin directamente
                // Esto evita tener que buscar por coordenadas en GetViewForAnnotation
                return new CustomPinAnnotation(pin);
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "CustomMapHandler", "CreateAnnotation");
                System.Diagnostics.Debug.WriteLine($"Error en CreateAnnotation: {ex.Message}");
                return null;
            }
        }

        UIImage ResizeImage(UIImage sourceImage, float width, float height)
        {
            UIGraphics.BeginImageContext(new CGSize(width, height));
            sourceImage.Draw(new CGRect(0, 0, width, height));
            var resultImage = UIGraphics.GetImageFromCurrentImageContext();
            UIGraphics.EndImageContext();
            return resultImage;
        }

        UIImage OverlayImages(UIImage baseImage, UIImage overlayImage, CGPoint position)
        {
            UIGraphics.BeginImageContext(baseImage.Size);
            baseImage.Draw(new CGRect(0, 0, baseImage.Size.Width, baseImage.Size.Height));
            var overlayRect = new CGRect(position.X, position.Y, overlayImage.Size.Width, overlayImage.Size.Height);
            overlayImage.Draw(overlayRect);
            var resultImage = UIGraphics.GetImageFromCurrentImageContext();
            UIGraphics.EndImageContext();
            return resultImage;
        }

        UIImage CreateCustomMarker(string iconImageName, int cantidadInteracciones, bool isBeingAttended, bool estadoAlarma, bool flagRedConfianza)
        {
            try
            {
                UIImage pinImage = UIImage.FromFile(iconImageName);
                if (pinImage == null)
                {
                    System.Diagnostics.Debug.WriteLine($"iOS CreateCustomMarker: ⚠️ imagen '{iconImageName}' no encontrada en bundle, usando warningpin.png");
                    pinImage = UIImage.FromFile("warningpin.png");
                }

                if (isBeingAttended)
                {
                    string policeIconName = estadoAlarma ? "man_officer_police_icon.png" : "man_officer_police_icon_grey.png";
                    UIImage policeImage = UIImage.FromFile(policeIconName);

                    if (policeImage != null)
                    {
                        // Redimensiona la imagen del ícono de policía
                        var resizedPoliceIcon = ResizeImage(policeImage, 30, 30);

                        // Coloca la imagen redimensionada del ícono de policía en la imagen principal
                        pinImage = OverlayImages(pinImage, resizedPoliceIcon, new CGPoint(pinImage.Size.Width - resizedPoliceIcon.Size.Width - 0, -4));
                    }
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
                    var resizedBadge = ResizeImage(badgeImage, 20, 20);

                    // Coloca el badge redimensionado en la imagen principal
                    pinImage = OverlayImages(pinImage, resizedBadge, new CGPoint(1, 0));
                }

                if (flagRedConfianza)
                {
                    UIImage verifiedImage = UIImage.FromFile("verified.png");
                    if (verifiedImage != null)
                    {
                        // Redimensiona la imagen del ícono de verificación
                        var resizedVerifiedIcon = ResizeImage(verifiedImage, 16, 16);

                        // Coloca la imagen redimensionada del ícono de verificación en la imagen principal
                        pinImage = OverlayImages(pinImage, resizedVerifiedIcon, new CGPoint(6, pinImage.Size.Height - resizedVerifiedIcon.Size.Height - 6));
                    }
                }

                return pinImage;
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "CustomMapHandler", "CreateCustomMarker");
                System.Diagnostics.Debug.WriteLine($"Error en CreateCustomMarker: {ex.Message}");
                return UIImage.FromFile("warningpin.png");
            }
        }

        MKAnnotationView GetViewForAnnotation(MKMapView mapView, IMKAnnotation annotation)
        {
            try
            {
                if (annotation is MKUserLocation)
                    return null;

                // Obtener CustomPin directamente de la anotación (sin búsqueda por coordenadas)
                CustomPin customPin = null;
                if (annotation is CustomPinAnnotation customPinAnnotation)
                {
                    customPin = customPinAnnotation.CustomPin;
                }
                else
                {
                    // Fallback: búsqueda por coordenadas para anotaciones legacy
                    customPin = GetCustomPin(annotation as MKPointAnnotation);
                }

                if (customPin == null)
                {
                    System.Diagnostics.Debug.WriteLine("iOS GetViewForAnnotation: customPin no encontrado");
                    return null;
                }

                System.Diagnostics.Debug.WriteLine($"iOS GetViewForAnnotation: pin={customPin.Id}, TipoAlarma={customPin.TipoAlarma}, AlarmaCercana={(customPin.AlarmaCercana != null ? "SÍ" : "NO")}");

                // reuseIdentifier por tipo visual de pin para que el reciclado sea correcto
                bool esUsuario = customPin.Id == "User" || customPin.MarkerId == "User";
                bool esFlecha  = customPin.TipoAlarma == -1;
                var reuseId = esUsuario
                    ? "UserPin"
                    : esFlecha
                        ? "ArrowPin"
                        : GetAlarmIconFileName(customPin.TipoAlarma, customPin.AlarmaCercana?.estado_alarma ?? true);

                // Intentar reutilizar una vista existente del pool con el mismo tipo.
                // IMPORTANTE: NO reasignar annotationView.Annotation en vistas recicladas —
                // hacerlo causa que MauiCALayer quede huérfano y crashee en el finalizador (SIGSEGV).
                var existingView = mapView.DequeueReusableAnnotation(reuseId) as CustomMKAnnotationView;
                CustomMKAnnotationView annotationView;
                if (existingView != null)
                {
                    annotationView = existingView;
                }
                else
                {
                    annotationView = new CustomMKAnnotationView(annotation, reuseId);
                }

                // SIEMPRE actualizar imagen e Id (tanto en vista nueva como reciclada)
                annotationView.Image = GetPinImage(customPin);
                annotationView.CalloutOffset = new CGPoint(0, 0);
                annotationView.Id = customPin.Id;
                // NO reasignar annotationView.Annotation — causa SIGSEGV en el finalizador de MauiCALayer

                // Flechas de dirección: sin callout, centradas en su coordenada
                if (esFlecha)
                {
                    annotationView.CanShowCallout = false;
                    annotationView.CenterOffset = CGPoint.Empty;
                    return annotationView;
                }

                annotationView.CanShowCallout = true;

                // CRÍTICO iOS — workaround SIGSEGV MauiCALayer:
                // Al reciclar una vista, NO hacemos null→new sin más: eso deja el wrapper managed
                // del objeto anterior vivo (referenciado por delegates) mientras el objeto nativo
                // ya fue liberado por UIKit. Cuando el GC lo finaliza en el hilo finalizador,
                // NSObject:Finalize intenta enviar mensajes ObjC a objetos ya liberados → SIGSEGV.
                // Solución: reusar UILabel si existe (solo actualizar texto), y para UIButton
                // primero nulificar (UIKit libera su retención), luego Dispose() del wrapper managed.

                // — Detail label: reusar si ya es UILabel, crear+disponer si no —
                if (annotationView.DetailCalloutAccessoryView is UILabel existingLabel)
                {
                    existingLabel.Text = customPin.Address; // Actualizar texto en lugar de recrear
                }
                else
                {
                    var oldDetail = annotationView.DetailCalloutAccessoryView;
                    annotationView.DetailCalloutAccessoryView = null; // UIKit libera su retención primero
                    oldDetail?.Dispose();                             // Luego limpieza managed

                    var calloutLabel = new UILabel
                    {
                        UserInteractionEnabled = false,
                        BackgroundColor = UIColor.Clear,
                        Font = UIFont.SystemFontOfSize(12),
                        TextColor = UIColor.Label,
                        Lines = 2,
                        Text = customPin.Address
                    };
                    annotationView.DetailCalloutAccessoryView = calloutLabel;
                }

                // — Botón derecho: siempre nuevo (lambda con capturedPin distinto por pin),
                //   pero disponer el anterior correctamente antes de reemplazarlo —
                var capturedPin = customPin;
                var oldRight = annotationView.RightCalloutAccessoryView;
                annotationView.RightCalloutAccessoryView = null; // UIKit libera su retención primero
                oldRight?.Dispose();                             // Luego limpieza managed

                var navButton = UIButton.FromType(UIButtonType.DetailDisclosure);
                navButton.TouchUpInside += (s, e) => NavigateToAlarm(capturedPin);
                annotationView.RightCalloutAccessoryView = navButton;

                return annotationView;
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "CustomMapHandler", "GetViewForAnnotation");
                System.Diagnostics.Debug.WriteLine($"Error en GetViewForAnnotation: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Obtiene el nombre del archivo de icono para un tipo de alarma.
        /// Usa App.TiposAlarmaDisponibles dinámicamente (igual que Android),
        /// con fallback hardcoded para tipos 1-9.
        /// En iOS los archivos están en el bundle con su nombre original (case-sensitive).
        /// </summary>
        private string GetAlarmIconFileName(long tipoAlarma, bool estadoAlarma)
        {
            // MÉTODO DINÁMICO: igual que Android, desde App.TiposAlarmaDisponibles
            if (App.TiposAlarmaDisponibles != null && App.TiposAlarmaDisponibles.Count > 0)
            {
                var tipo = App.TiposAlarmaDisponibles.FirstOrDefault(t => t.TipoalarmaId == tipoAlarma);
                if (tipo != null && !string.IsNullOrEmpty(tipo.Icono))
                {
                    if (!estadoAlarma)
                    {
                        // Alarma cerrada: iconos fijos en minúsculas (como están en el bundle)
                        return tipoAlarma == 9 ? "direccionescapesospechosocerrada.png" : "closedalarmpin.png";
                    }

                    // En iOS los archivos se procesan en minúsculas por el resizetizer de MAUI
                    var iconoMinusculas = tipo.Icono.ToLowerInvariant();
                    System.Diagnostics.Debug.WriteLine($"iOS GetAlarmIconFileName: ✅ Icono dinámico para tipo {tipoAlarma}: '{iconoMinusculas}'");
                    return iconoMinusculas;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"iOS GetAlarmIconFileName: ⚠️ Tipo {tipoAlarma} no encontrado en TiposAlarmaDisponibles, usando fallback");
                }
            }

            // FALLBACK: Mapping hardcodeado completo (por si App.TiposAlarmaDisponibles no está cargado aún).
            // FIX 2026-03-01: Ampliado de tipos 1-9 a 1-14 con nombres de icono correctos según BD.
            // Tipos 1 y 2 tenían nombres incorrectos ("warningpin"/"dangerpin"); corregidos a los nombres reales.
            System.Diagnostics.Debug.WriteLine($"iOS GetAlarmIconFileName: ⚠️ TiposAlarmaDisponibles vacío, usando fallback hardcoded para tipo {tipoAlarma}");
            System.Diagnostics.Debug.WriteLine($"iOS GetAlarmIconFileName: DIAG-TIPOS: Count={App.TiposAlarmaDisponibles?.Count.ToString() ?? "NULL"} al pintar tipo {tipoAlarma}");

            // Nombres en minúsculas — así están en el bundle iOS tras el procesamiento del resizetizer
            if (!estadoAlarma)
                return tipoAlarma == 9 ? "direccionescapesospechosocerrada.png" : "closedalarmpin.png";

            return tipoAlarma switch
            {
                1  => "naranjaadvertenciapeligro.png",
                2  => "rojodelitocrimen.png",
                3  => "amarillopleitocallejero.png",
                4  => "verdeazulmascotaperdida.png",
                5  => "azulpersonaperdida.png",
                6  => "negrodisturbiosprotestas.png",
                7  => "verdeacompanamientovisual.png",
                8  => "purpuraviolenciaintrafamiliar.png",
                9  => "direccionescapesospechoso.png",
                10 => "eventoicon.png",
                11 => "votoicon.png",
                12 => "amenazaicon.png",
                13 => "promocionlocal.png",
                14 => "alertaciudadana.png",
                _  => "warningpin.png"
            };
        }

        /// <summary>
        /// Determina la imagen correcta para un CustomPin según su tipo y estado.
        /// Separado de GetViewForAnnotation para reutilizarse tanto en vistas nuevas como recicladas.
        /// </summary>
        /// <summary>
        /// Crea una imagen de flecha dibujada programáticamente, rotada según el bearing indicado.
        /// Equivalente al CreateArrowMarker de Android.
        /// </summary>
        UIImage CreateArrowImage(float bearing, bool isClosed)
        {
            try
            {
                const float size = 80f;
                UIGraphics.BeginImageContextWithOptions(new CGSize(size, size), false, 0);
                var context = UIGraphics.GetCurrentContext();

                float cx = size / 2f;
                float cy = size / 2f;
                float tipY  = cy - size / 3f;
                float baseY = cy + size / 6f;
                float halfBase = size / 6f;

                // Rotar el contexto alrededor del centro antes de dibujar
                context.TranslateCTM(cx, cy);
                context.RotateCTM((nfloat)(bearing * Math.PI / 180.0));
                context.TranslateCTM(-cx, -cy);

                // Triángulo apuntando hacia arriba (Norte)
                var path = new CGPath();
                path.MoveToPoint(cx, tipY);
                path.AddLineToPoint(cx - halfBase, baseY);
                path.AddLineToPoint(cx + halfBase, baseY);
                path.CloseSubpath();

                // Relleno
                context.AddPath(path);
                context.SetFillColor(isClosed ? UIColor.Gray.CGColor : UIColor.Red.CGColor);
                context.FillPath();

                // Borde blanco para contraste
                context.AddPath(path);
                context.SetStrokeColor(UIColor.White.CGColor);
                context.SetLineWidth(3f);
                context.StrokePath();

                var image = UIGraphics.GetImageFromCurrentImageContext();
                UIGraphics.EndImageContext();

                System.Diagnostics.Debug.WriteLine($"iOS CreateArrowImage OK bearing={bearing}° cerrada={isClosed}");
                return image;
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "CustomMapHandler", "CreateArrowImage");
                System.Diagnostics.Debug.WriteLine($"iOS CreateArrowImage Error: {ex.Message}");
                return null;
            }
        }

        UIImage GetPinImage(CustomPin customPin)
        {
            try
            {
                // Pin del usuario
                if (customPin.Id == "User" || customPin.MarkerId == "User")
                {
                    return UIImage.FromFile("user_location.png");
                }

                // Marcador sintético de flecha de dirección de escape
                if (customPin.TipoAlarma == -1)
                {
                    bool isClosed = customPin.Address == "Cerrada";
                    return CreateArrowImage(customPin.ArrowBearing, isClosed);
                }

                bool estadoAlarma = customPin.AlarmaCercana?.estado_alarma ?? true;
                var nombreArchivo = GetAlarmIconFileName(customPin.TipoAlarma, estadoAlarma);

                System.Diagnostics.Debug.WriteLine($"iOS GetPinImage: tipo={customPin.TipoAlarma}, activa={estadoAlarma}, archivo={nombreArchivo}");

                if (customPin.AlarmaCercana != null)
                {
                    return CreateCustomMarker(nombreArchivo,
                        customPin.AlarmaCercana.cantidad_interacciones,
                        customPin.AlarmaCercana.flag_alarma_siendo_atendida,
                        customPin.AlarmaCercana.estado_alarma,
                        customPin.AlarmaCercana.flag_red_confianza);
                }

                // Sin AlarmaCercana: imagen simple sin overlays
                return UIImage.FromFile(nombreArchivo) ?? UIImage.FromFile("warningpin.png");
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "CustomMapHandler", "GetPinImage");
                System.Diagnostics.Debug.WriteLine($"Error en GetPinImage: {ex.Message}");
                return UIImage.FromFile("warningpin.png");
            }
        }

        void OnAnnotationViewSelected(object sender, MKAnnotationViewEventArgs e)
        {
            if (e.View?.Annotation is CustomPinAnnotation cpa &&
                cpa.CustomPin?.Id != "User" &&
                cpa.CustomPin?.MarkerId != "User")
            {
                _calloutVisibleForPin = cpa.CustomPin;
                System.Diagnostics.Debug.WriteLine($"iOS Callout abierto para pin: {cpa.CustomPin?.Id}");
            }
        }

        void OnAnnotationViewDeselected(object sender, MKAnnotationViewEventArgs e)
        {
            _calloutVisibleForPin = null;
            System.Diagnostics.Debug.WriteLine("iOS Callout cerrado");
        }

        void NavigateToAlarm(CustomPin customPin)
        {
            try
            {
                if (customPin == null || customPin.Id == "User" || customPin.MarkerId == "User")
                    return;

                System.Diagnostics.Debug.WriteLine($"iOS NavigateToAlarm: Pin={customPin.Id}, FlagPropietarioAlarma={customPin.FlagPropietarioAlarma}");

                // Usar MessagingCenter igual que Android para que HomePage maneje la navegación
                // diferenciando si el usuario es propietario o no de la alarma
                MessagingCenter.Send<object, CustomPin>(this, "InfoWindowClicked", customPin);
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "CustomMapHandler", "NavigateToAlarm");
                System.Diagnostics.Debug.WriteLine($"Error en NavigateToAlarm: {ex.Message}");
            }
        }

        void HandleTapGesture(UITapGestureRecognizer gestureRecognizer)
        {
            try
            {
                // Solo actuar cuando el gesto termina (no durante el reconocimiento)
                if (gestureRecognizer.State != UIGestureRecognizerState.Ended)
                    return;

                var touchLocation = gestureRecognizer.LocationInView(nativeMap);

                // FIX iOS: Si hay un callout abierto, el toque fue sobre el cuerpo del callout.
                // En Android esto dispara OnInfoWindowClick → navegación. Replicamos ese comportamiento.
                // Doble fuente: SelectedAnnotations (si MapKit no deseleccionó aún) o _calloutVisibleForPin
                // (si DidDeselectAnnotationView ya disparó por la carrera de eventos).
                var selectedPin = nativeMap.SelectedAnnotations?
                    .OfType<CustomPinAnnotation>()
                    .FirstOrDefault()?.CustomPin
                    ?? _calloutVisibleForPin;

                if (selectedPin != null && selectedPin.Id != "User" && selectedPin.MarkerId != "User")
                {
                    System.Diagnostics.Debug.WriteLine($"iOS HandleTapGesture: tap en callout → NavigateToAlarm({selectedPin.Id})");
                    _calloutVisibleForPin = null;
                    NavigateToAlarm(selectedPin);
                    return;
                }

                // Verificar si el toque fue sobre un pin o su callout (con margen de 20pt)
                bool isTouchOnPin = false;
                foreach (var annotation in nativeMap.Annotations)
                {
                    var annotationView = nativeMap.ViewForAnnotation(annotation);
                    if (annotationView == null) continue;

                    // Ampliar el área de hit del pin en 20 puntos en cada dirección
                    var annotationFrame = annotationView.ConvertRectToView(annotationView.Bounds, nativeMap);
                    var expandedFrame = annotationFrame.Inset(-20, -20);

                    if (expandedFrame.Contains(touchLocation))
                    {
                        isTouchOnPin = true;
                        System.Diagnostics.Debug.WriteLine("iOS HandleTapGesture: toque en pin - ignorando");
                        break;
                    }
                }

                if (!isTouchOnPin)
                {
                    var locationCoordinate = nativeMap.ConvertPoint(touchLocation, nativeMap);
                    var customMap = VirtualView as CustomMap;
                    if (customMap != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"iOS HandleTapGesture: OnTap en {locationCoordinate.Latitude:F5}, {locationCoordinate.Longitude:F5}");
                        var location = new Location(locationCoordinate.Latitude, locationCoordinate.Longitude);
                        customMap.OnTap(location);
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "CustomMapHandler", "HandleTapGesture");
                System.Diagnostics.Debug.WriteLine($"Error en HandleTapGesture: {ex.Message}");
            }
        }

        private void ApplyMapStyle()
        {
            try
            {
                if (nativeMap == null)
                {
                    System.Diagnostics.Debug.WriteLine("iOS: nativeMap es null");
                    return;
                }

                // Para iOS, necesitas aplicar MapType, no JSON
                // iOS no soporta estilos JSON como Android

                if (UIDevice.CurrentDevice.CheckSystemVersion(11, 0))
                {
                    nativeMap.MapType = MKMapType.MutedStandard; // iOS 11+ estilo moderno
                    System.Diagnostics.Debug.WriteLine("iOS: MutedStandard aplicado");
                }
                else
                {
                    nativeMap.MapType = MKMapType.Standard;
                    System.Diagnostics.Debug.WriteLine("iOS: Standard aplicado");
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "CustomMapHandler", "ApplyMapStyle");
                System.Diagnostics.Debug.WriteLine($"iOS Error: {ex.Message}");
            }
        }

        CustomPin GetCustomPin(MKPointAnnotation annotation)
        {
            try
            {
                if (annotation != null)
                {
                    var location = new Location(annotation.Coordinate.Latitude, annotation.Coordinate.Longitude);
                    if (App.CustomPins != null)
                    {
                        foreach (var pin in App.CustomPins)
                        {
                            // Comparación con tolerancia para coordenadas flotantes
                            if (Math.Abs(pin.Location.Latitude - location.Latitude) < 0.000001 &&
                                Math.Abs(pin.Location.Longitude - location.Longitude) < 0.000001)
                            {
                                return pin;
                            }
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "CustomMapHandler", "GetCustomPin");
                System.Diagnostics.Debug.WriteLine($"Error en GetCustomPin: {ex.Message}");
                return null;
            }
        }

        // Clase auxiliar para las anotaciones personalizadas - REMOVIDA
        // Ya existe como archivo separado: CustomMKAnnotationView.cs
    }
}