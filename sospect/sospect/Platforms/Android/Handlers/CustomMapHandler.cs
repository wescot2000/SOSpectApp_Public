// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System.Collections.Generic;
using System.Linq;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Maps.Handlers;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using sospect.CustomRenderers;
using sospect.Helpers;
using sospect.Models;
using sospect.Views;
using AndroidView = Android.Views.View;
using AndroidBitmap = Android.Graphics.Bitmap;
using AndroidCanvas = Android.Graphics.Canvas;
using AndroidPaint = Android.Graphics.Paint;
using AndroidBitmapFactory = Android.Graphics.BitmapFactory;
using AndroidColor = Android.Graphics.Color;
using Location = Microsoft.Maui.Devices.Sensors.Location;

namespace sospect.Platforms.Android.Handlers;

public class CustomMapHandler : MapHandler
{
    public static new IPropertyMapper<CustomMap, CustomMapHandler> Mapper =
        new PropertyMapper<CustomMap, CustomMapHandler>(MapHandler.Mapper)
        {
            [nameof(CustomMap.CustomPins)] = MapCustomPins,
            [nameof(CustomMap.HasScrollEnabled)] = MapHasScrollEnabled,
            [nameof(CustomMap.HasZoomEnabled)] = MapHasZoomEnabled,
            [nameof(CustomMap.HasRotationEnabled)] = MapHasRotationEnabled
        };

    public CustomMapHandler() : base(Mapper)
    {
    }

    private GoogleMap _map;
    private List<CustomPin> customPins = new();
    private bool _pendingPinsUpdate = false;
    private bool _handlersConnected = false;
    private bool _isUpdatingPins = false; // NUEVO: Flag para prevenir actualizaciones concurrentes

    // NUEVO: Diccionario para mantener referencia de markers creados
    private Dictionary<string, Marker> _markerDictionary = new Dictionary<string, Marker>();

    // NUEVO: Lock object para sincronizar operaciones en el mapa
    private readonly object _mapLock = new object();

    // OPTIMIZACIÓN: Cache de IDs de pines actuales para actualización diferencial
    private HashSet<string> _currentPinIds = new HashSet<string>();

    // OPTIMIZACIÓN: Hash de pines para detectar cambios reales (evita 56 actualizaciones redundantes)
    private string _lastPinsHash = null;

    // OPTIMIZACIÓN: Throttle para CameraIdle (reduce 285 eventos a ~15-20)
    private DateTime _lastCameraIdleTime = DateTime.MinValue;
    private const int CAMERA_IDLE_THROTTLE_MS = 300;

    protected override void ConnectHandler(MapView platformView)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("CustomMapHandler: Iniciando ConnectHandler");

            base.ConnectHandler(platformView);
            System.Diagnostics.Debug.WriteLine("CustomMapHandler: base.ConnectHandler ejecutado exitosamente");

            if (platformView != null)
            {
                System.Diagnostics.Debug.WriteLine("CustomMapHandler: platformView no es null, llamando GetMapAsync");
                var callback = new MapReadyCallback(this);
                platformView.GetMapAsync(callback);
                System.Diagnostics.Debug.WriteLine("CustomMapHandler: GetMapAsync llamado exitosamente");
            }
        }
        catch (Exception ex)
        {
            CrashlyticsHelper.LogError(ex, "CustomMapHandler", "ConnectHandler");
            System.Diagnostics.Debug.WriteLine($"CustomMapHandler: EXCEPCI�N en ConnectHandler: {ex.Message}");
            throw;
        }
    }

    protected override void DisconnectHandler(MapView platformView)
    {
        if (_map != null && _handlersConnected)
        {
            _map.MapClick -= GoogleMap_MapClick;
            _map.InfoWindowClick -= OnInfoWindowClick;
            _map.MarkerClick -= GoogleMap_MarkerClick; // NUEVO
            _map.CameraIdle -= GoogleMap_CameraIdle; // NUEVO: Para clustering zoom-aware
            _handlersConnected = false;
        }

        _markerDictionary.Clear();
        base.DisconnectHandler(platformView);
    }

    public void OnMapReady(GoogleMap googleMap)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("CustomMapHandler: OnMapReady llamado");

            _map = googleMap;

            if (_map != null)
            {
                System.Diagnostics.Debug.WriteLine("CustomMapHandler: GoogleMap asignado correctamente");

                ApplyMapStyle();

                // Opciones de tipo de mapa
                _map.MapType = GoogleMap.MapTypeNormal;    // Mapa est�ndar (default)
                // _map.MapType = GoogleMap.MapTypeSatellite; // Vista satelital
                // _map.MapType = GoogleMap.MapTypeHybrid;    // H�brido
                // _map.MapType = GoogleMap.MapTypeTerrain;   // Terreno

                // CR�TICO: Configurar InfoWindowAdapter PRIMERO, antes de todo
                var infoWindowAdapter = new InfoWindowAdapter();
                _map.SetInfoWindowAdapter(infoWindowAdapter);
                System.Diagnostics.Debug.WriteLine("CustomMapHandler: InfoWindowAdapter configurado ANTES de agregar markers");

                ConnectEventHandlers();
                ApplyGestureSettings();

                System.Diagnostics.Debug.WriteLine("CustomMapHandler: Eventos configurados exitosamente");

                DiagnoseResources();

                if (_pendingPinsUpdate)
                {
                    System.Diagnostics.Debug.WriteLine("CustomMapHandler: Aplicando pins pendientes");
                    UpdateCustomPins();
                    _pendingPinsUpdate = false;
                }
            }

            System.Diagnostics.Debug.WriteLine("CustomMapHandler: OnMapReady completado exitosamente");
        }
        catch (Exception ex)
        {
            CrashlyticsHelper.LogError(ex, "CustomMapHandler", "OnMapReady");
            System.Diagnostics.Debug.WriteLine($"CustomMapHandler: EXCEPCI�N en OnMapReady: {ex.Message}");
        }
    }

    private void ApplyMapStyle()
    {
        try
        {
            var context = MauiContext?.Context ?? Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
            if (context == null || _map == null)
            {
                System.Diagnostics.Debug.WriteLine("CustomMapHandler: No se puede aplicar estilo - context o map es null");
                return;
            }

            System.Diagnostics.Debug.WriteLine("CustomMapHandler: Intentando leer map_style.json desde Assets");

            // Leer directamente desde Assets (m�todo m�s confiable)
            using var stream = context.Assets.Open("map_style.json");
            using var reader = new System.IO.StreamReader(stream);
            var jsonStyle = reader.ReadToEnd();

            System.Diagnostics.Debug.WriteLine($"CustomMapHandler:  JSON le�do exitosamente, longitud: {jsonStyle.Length} caracteres");

            var success = _map.SetMapStyle(new MapStyleOptions(jsonStyle));

            if (success)
            {
                System.Diagnostics.Debug.WriteLine("CustomMapHandler: ESTILO DE MAPA APLICADO EXITOSAMENTE");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("CustomMapHandler:  SetMapStyle devolvi� false");
            }
        }
        catch (Java.IO.FileNotFoundException fnfEx)
        {
            System.Diagnostics.Debug.WriteLine($"CustomMapHandler:  ARCHIVO NO ENCONTRADO: {fnfEx.Message}");
            System.Diagnostics.Debug.WriteLine("CustomMapHandler: Verifica que map_style.json est� en Platforms/Android/Assets/");
        }
        catch (Exception ex)
        {
            CrashlyticsHelper.LogError(ex, "CustomMapHandler", "ApplyMapStyle");
            System.Diagnostics.Debug.WriteLine($"CustomMapHandler: Error en ApplyMapStyle: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"CustomMapHandler: StackTrace: {ex.StackTrace}");
        }
    }

    private void ConnectEventHandlers()
    {
        try
        {
            if (_map != null)
            {
                if (_handlersConnected)
                {
                    _map.MapClick -= GoogleMap_MapClick;
                    _map.InfoWindowClick -= OnInfoWindowClick;
                    _map.MarkerClick -= GoogleMap_MarkerClick; // NUEVO
                    _map.CameraIdle -= GoogleMap_CameraIdle; // NUEVO: Para clustering zoom-aware
                }

                _map.MapClick += GoogleMap_MapClick;
                _map.InfoWindowClick += OnInfoWindowClick;
                _map.MarkerClick += GoogleMap_MarkerClick; // NUEVO
                _map.CameraIdle += GoogleMap_CameraIdle; // NUEVO: Para clustering zoom-aware (Manual sección 5)

                _handlersConnected = true;

                System.Diagnostics.Debug.WriteLine("CustomMapHandler: Event handlers conectados exitosamente");
            }
        }
        catch (Exception ex)
        {
            CrashlyticsHelper.LogError(ex, "CustomMapHandler", "ConnectEventHandlers");
            System.Diagnostics.Debug.WriteLine($"CustomMapHandler: Error conectando event handlers: {ex.Message}");
        }
    }

    private void EnsureEventHandlersConnected()
    {
        try
        {
            if (_map != null && !_handlersConnected)
            {
                ConnectEventHandlers();
                System.Diagnostics.Debug.WriteLine("CustomMapHandler: Event handlers reconectados");
            }
        }
        catch (Exception ex)
        {
            CrashlyticsHelper.LogError(ex, "CustomMapHandler", "EnsureEventHandlersConnected");
            System.Diagnostics.Debug.WriteLine($"CustomMapHandler: Error ensuring event handlers: {ex.Message}");
        }
    }

    // M�TODOS PARA MANEJAR PROPIEDADES DE GESTOS (sin cambios)
    static void MapHasScrollEnabled(CustomMapHandler handler, CustomMap map)
    {
        handler.UpdateScrollEnabled();
    }

    static void MapHasZoomEnabled(CustomMapHandler handler, CustomMap map)
    {
        handler.UpdateZoomEnabled();
    }

    static void MapHasRotationEnabled(CustomMapHandler handler, CustomMap map)
    {
        handler.UpdateRotationEnabled();
    }

    void UpdateScrollEnabled()
    {
        if (_map != null && VirtualView is CustomMap customMap)
        {
            _map.UiSettings.ScrollGesturesEnabled = customMap.HasScrollEnabled;
            System.Diagnostics.Debug.WriteLine($"CustomMapHandler: Scroll gestures = {customMap.HasScrollEnabled}");
        }
    }

    void UpdateZoomEnabled()
    {
        if (_map != null && VirtualView is CustomMap customMap)
        {
            _map.UiSettings.ZoomGesturesEnabled = customMap.HasZoomEnabled;
            _map.UiSettings.ZoomControlsEnabled = false;
            System.Diagnostics.Debug.WriteLine($"CustomMapHandler: Zoom gestures = {customMap.HasZoomEnabled}");
        }
    }

    void UpdateRotationEnabled()
    {
        if (_map != null && VirtualView is CustomMap customMap)
        {
            _map.UiSettings.RotateGesturesEnabled = customMap.HasRotationEnabled;
            System.Diagnostics.Debug.WriteLine($"CustomMapHandler: Rotation gestures = {customMap.HasRotationEnabled}");

            if (!customMap.HasRotationEnabled)
            {
                try
                {
                    var cameraPosition = new CameraPosition.Builder()
                        .Target(_map.CameraPosition.Target)
                        .Zoom(_map.CameraPosition.Zoom)
                        .Bearing(0)
                        .Tilt(0)
                        .Build();

                    var cameraUpdate = CameraUpdateFactory.NewCameraPosition(cameraPosition);
                    _map.AnimateCamera(cameraUpdate);

                    System.Diagnostics.Debug.WriteLine("CustomMapHandler: Mapa orientado al Norte (rotaci�n deshabilitada)");
                }
                catch (Exception ex)
                {
                    CrashlyticsHelper.LogError(ex, "CustomMapHandler", "UpdateRotationEnabled");
                    System.Diagnostics.Debug.WriteLine($"CustomMapHandler: Error al orientar mapa al Norte: {ex.Message}");
                }
            }
        }
    }

    void ApplyGestureSettings()
    {
        if (VirtualView is CustomMap customMap && _map?.UiSettings != null)
        {
            try
            {
                UpdateScrollEnabled();
                UpdateZoomEnabled();
                UpdateRotationEnabled();

                _map.UiSettings.TiltGesturesEnabled = false;
                _map.UiSettings.CompassEnabled = true;
                _map.UiSettings.MyLocationButtonEnabled = false;

                EnsureEventHandlersConnected();

                System.Diagnostics.Debug.WriteLine("CustomMapHandler: Configuraciones adicionales de UI aplicadas");
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "CustomMapHandler", "ApplyGestureSettings");
                System.Diagnostics.Debug.WriteLine($"CustomMapHandler: Error en ApplyGestureSettings: {ex.Message}");
            }
        }
    }

    private void DiagnoseResources()
    {
        try
        {
            var context = MauiContext?.Context ?? Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
            if (context == null)
            {
                System.Diagnostics.Debug.WriteLine("CustomMapHandler: DIAGN�STICO - Context es null");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"CustomMapHandler: DIAGN�STICO - Package name: {context.PackageName}");

            var requiredResources = new[]
            {
                "user_location", "userlocation", "pin_user", "marker_user",
                "warningpin", "dangerpin", "amarillopleitocallejero",
                "verdeazulmascotaperdida", "azulpersonaperdida",
                "negrodisturbiosprotestas", "verdeacompanamientovisual",
                "purpuraviolenciaintrafamiliar", "direccionescapesospechoso",
                "closedalarmpin", "direccionescapesospechosocerrada",
                "man_officer_police_icon", "man_officer_police_icon_grey",
                "verified"
            };

            System.Diagnostics.Debug.WriteLine("CustomMapHandler: DIAGN�STICO - Verificando recursos drawable:");

            foreach (var resourceName in requiredResources)
            {
                var resourceId = GetResourceId(resourceName);
                System.Diagnostics.Debug.WriteLine($"CustomMapHandler: DIAGN�STICO - '{resourceName}' = {resourceId}");

                if (resourceId == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"CustomMapHandler: RECURSO FALTANTE - {resourceName}");
                }
            }

            var userLocationId = GetResourceId("user_location");
            if (userLocationId == 0)
            {
                System.Diagnostics.Debug.WriteLine("CustomMapHandler: CR�TICO - user_location no encontrado");

                var alternatives = new[] { "userlocation", "user", "location", "pin_user", "marker_user" };
                foreach (var alt in alternatives)
                {
                    var altId = GetResourceId(alt);
                    if (altId != 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"CustomMapHandler: ALTERNATIVA ENCONTRADA - {alt} = {altId}");
                        break;
                    }
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"CustomMapHandler: �XITO - user_location encontrado = {userLocationId}");
            }
        }
        catch (Exception ex)
        {
            CrashlyticsHelper.LogError(ex, "CustomMapHandler", "DiagnoseResources");
            System.Diagnostics.Debug.WriteLine($"CustomMapHandler: Error en DiagnoseResources: {ex.Message}");
        }
    }

    static void MapCustomPins(CustomMapHandler handler, CustomMap map)
    {
        System.Diagnostics.Debug.WriteLine("CustomMapHandler: MapCustomPins llamado");
        handler.UpdateCustomPins();
    }

    // MÉTODO CRÍTICO OPTIMIZADO: UpdateCustomPins con actualización diferencial
    // OPTIMIZACIÓN: Reduce de 56 actualizaciones completas a solo ~5-8 con cambios reales
    void UpdateCustomPins()
    {
        try
        {
            if (_map == null)
            {
                _pendingPinsUpdate = true;
                return;
            }

            if (VirtualView is not CustomMap customMap)
                return;

            // CRÍTICO: Capturar lista de pins ANTES de entrar al Main Thread
            var pinsToProcess = customMap.CustomPins?.ToList() ?? new List<CustomPin>();

            // OPTIMIZACIÓN: Calcular hash de los pines para detectar cambios reales
            var newPinsHash = CalculatePinsHash(pinsToProcess);

            // CRÍTICO: Si los pines no cambiaron y ya tenemos markers, no hacer nada
            if (newPinsHash == _lastPinsHash && _markerDictionary.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"CustomMapHandler: UpdateCustomPins IGNORADO - Pines sin cambios");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"CustomMapHandler: UpdateCustomPins - {pinsToProcess.Count} pins, hash cambió");
            _lastPinsHash = newPinsHash;

            // CRÍTICO: Usar Task para ejecutar en Main Thread
            _ = MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    if (_isUpdatingPins)
                    {
                        System.Diagnostics.Debug.WriteLine("CustomMapHandler: Actualización ya en progreso");
                        return;
                    }

                    _isUpdatingPins = true;

                    lock (_mapLock)
                    {
                        bool handlersWereConnected = _handlersConnected;

                        // Limpiar diccionario y mapa
                        _markerDictionary.Clear();
                        _map.Clear();

                        var infoWindowAdapter = new InfoWindowAdapter();
                        _map.SetInfoWindowAdapter(infoWindowAdapter);

                        // Reconectar handlers si estaban conectados.
                        // IMPORTANTE: no poner _handlersConnected = false aquí porque
                        // ConnectEventHandlers usa ese flag para desuscribir antes de re-suscribir.
                        // Ponerlo en false antes haría que se acumulen subscripciones a MapClick.
                        if (handlersWereConnected)
                        {
                            ConnectEventHandlers();
                        }

                        ApplyGestureSettings();

                        customPins = pinsToProcess;

                        // Procesar pins (con logging reducido)
                        foreach (var pin in customPins)
                        {
                            try
                            {
#if DEBUG
                                System.Diagnostics.Debug.WriteLine($"CustomMapHandler: Procesando pin ID='{pin.Id}', Tipo={pin.TipoAlarma}");
#endif

                                var marker = CreateMarker(pin);
                                if (marker != null)
                                {
                                    var androidMarker = _map.AddMarker(marker);
                                    if (androidMarker != null)
                                    {
                                        var key = pin.MarkerId ?? pin.Id ?? Guid.NewGuid().ToString();
                                        _markerDictionary[key] = androidMarker;
                                    }
                                }
                            }
                            catch (Exception pinEx)
                            {
                                CrashlyticsHelper.LogError(pinEx, "CustomMapHandler", "UpdateCustomPins_ProcessPin");
                            }
                        }

                        // Actualizar tracking de pines
                        _currentPinIds = new HashSet<string>(customPins.Select(p => p.MarkerId ?? p.Id ?? ""));

                        System.Diagnostics.Debug.WriteLine($"CustomMapHandler: UpdateCustomPins completado - {_markerDictionary.Count} markers");
                    }
                }
                catch (Exception mainThreadEx)
                {
                    CrashlyticsHelper.LogError(mainThreadEx, "CustomMapHandler", "UpdateCustomPins_MainThread");
                }
                finally
                {
                    _isUpdatingPins = false;
                }
            });
        }
        catch (Exception ex)
        {
            CrashlyticsHelper.LogError(ex, "CustomMapHandler", "UpdateCustomPins");
        }
    }

    /// <summary>
    /// OPTIMIZACIÓN: Calcula un hash de los pines para detectar cambios reales.
    /// Evita actualizaciones redundantes cuando los pines no han cambiado.
    /// </summary>
    private string CalculatePinsHash(List<CustomPin> pins)
    {
        if (pins == null || !pins.Any())
            return "empty";

        // Crear hash basado en IDs, posiciones y tipos
        var hashInput = string.Join("|", pins
            .OrderBy(p => p.MarkerId ?? p.Id ?? "")
            .Select(p => $"{p.MarkerId ?? p.Id}:{p.Location?.Latitude:F5}:{p.Location?.Longitude:F5}:{p.TipoAlarma}"));

        return hashInput.GetHashCode().ToString("X");
    }

    // M�TODO CR�TICO CORREGIDO: CreateMarker con mejor diagn�stico y manejo de recursos
    MarkerOptions CreateMarker(CustomPin pin)
    {
        try
        {
            if (pin == null)
            {
                System.Diagnostics.Debug.WriteLine("CustomMapHandler: ERROR - pin es null en CreateMarker");
                return null;
            }

            if (pin.Location == null)
            {
                System.Diagnostics.Debug.WriteLine($"CustomMapHandler: ERROR - pin.Location es null para pin {pin.Id}");
                return null;
            }

            System.Diagnostics.Debug.WriteLine($"CustomMapHandler: CreateMarker para pin ID='{pin.Id}', MarkerId='{pin.MarkerId}', Tipo={pin.TipoAlarma}");

            var marker = new MarkerOptions();
            marker.SetPosition(new LatLng(pin.Location.Latitude, pin.Location.Longitude));
            // CR�TICO: USAR RECURSOS DE IDIOMA existentes y nuevos
            var title = !string.IsNullOrEmpty(pin.Label) ? pin.Label : TranslateExtension.Translate("LabelInformacion");
            var snippet = !string.IsNullOrEmpty(pin.Address) ? pin.Address : TranslateExtension.Translate("LabelTocarParaDetalles");

            marker.SetTitle(title);
            marker.SetSnippet(snippet);

            System.Diagnostics.Debug.WriteLine($"CustomMapHandler: Marker configurado - Title: '{title}', Snippet: '{snippet}'");

            var context = MauiContext?.Context ?? Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
            if (context == null)
            {
                System.Diagnostics.Debug.WriteLine("CustomMapHandler: ERROR - Context is null en CreateMarker");
                return null;
            }

            // LÓGICA PARA MARCADOR SINTÉTICO DE FLECHA DE DIRECCIÓN (TipoAlarma == -1)
            if (pin.TipoAlarma == -1)
            {
                System.Diagnostics.Debug.WriteLine($"CustomMapHandler: Creando marcador de flecha con bearing={pin.ArrowBearing}°");
                marker.SetTitle("");
                marker.SetSnippet("");
                marker.Flat(true);   // El marcador rota con el mapa si se habilita rotación
                marker.Anchor(0.5f, 0.5f); // Centrado en el punto medio de la línea
                var arrowIcon = CreateArrowMarker(pin.ArrowBearing, pin.Address == "Cerrada");
                if (arrowIcon != null)
                    marker.SetIcon(arrowIcon);
                return marker;
            }

            // L�GICA ESPECIAL PARA PIN DEL USUARIO
            if (pin.Id == "User" || pin.MarkerId == "User")
            {
                System.Diagnostics.Debug.WriteLine("CustomMapHandler: Creando marker para USUARIO");

                // CR�TICO: USAR RECURSOS EXISTENTES para el usuario
                var LabelUsuario = TranslateExtension.Translate("LabelUsuario");
                marker.SetTitle(LabelUsuario);
                marker.SetSnippet(""); // Snippet vac�o para que NO muestre InfoWindow

                var userResourceNames = new[]
                {
                "user_location",
                "userlocation",
                "user",
                "location",
                "pin_user",
                "marker_user",
                "user_pin",
                "location_pin"
            };

                BitmapDescriptor userIcon = null;
                string usedResourceName = null;

                foreach (var resourceName in userResourceNames)
                {
                    var resourceId = GetResourceId(resourceName);
                    System.Diagnostics.Debug.WriteLine($"CustomMapHandler: Probando recurso usuario '{resourceName}' = {resourceId}");

                    if (resourceId != 0)
                    {
                        try
                        {
                            userIcon = BitmapDescriptorFactory.FromResource(resourceId);
                            usedResourceName = resourceName;
                            System.Diagnostics.Debug.WriteLine($"CustomMapHandler: �XITO - Icono usuario '{resourceName}' cargado");
                            break;
                        }
                        catch (Exception iconEx)
                        {
                            CrashlyticsHelper.LogError(iconEx, "CustomMapHandler", "CreateMarker_UserIcon");
                            System.Diagnostics.Debug.WriteLine($"CustomMapHandler: Error cargando icono '{resourceName}': {iconEx.Message}");
                        }
                    }
                }

                if (userIcon != null)
                {
                    marker.SetIcon(userIcon);
                    System.Diagnostics.Debug.WriteLine($"CustomMapHandler: Icono usuario aplicado exitosamente usando '{usedResourceName}'");
                }
                else
                {
                    marker.SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueBlue));
                    System.Diagnostics.Debug.WriteLine("CustomMapHandler: Usando marker azul por defecto para usuario");
                }

                return marker;
            }

            // L�GICA PARA PINES DE ALARMAS - ESTOS S� DEBEN TENER InfoWindow
            // NUEVO: LÓGICA PARA CLUSTER PINS (Manual sección 6)
            // Detectar si es un ClusterPin y renderizar con badge de conteo
            if (pin is ClusterPin clusterPin)
            {
                System.Diagnostics.Debug.WriteLine($"CustomMapHandler: Creando marker para CLUSTER con {clusterPin.TotalAlarmas} alarmas, tipo dominante {clusterPin.TipoDominante}");

                // Obtener icono del tipo dominante (siempre estado activo para clusters)
                string iconResourceName = GetAlarmIconResourceName(clusterPin.TipoDominante, true);
                int iconResourceId = GetResourceId(iconResourceName);

                System.Diagnostics.Debug.WriteLine($"CustomMapHandler: Recurso cluster '{iconResourceName}' = {iconResourceId}");

                if (iconResourceId != 0)
                {
                    try
                    {
                        // NUEVO: Crear marker de cluster con badge AMARILLO (diferente al badge rojo de mensajes)
                        var clusterMarker = CreateClusterMarker(
                            iconResourceId,
                            clusterPin.TotalAlarmas);  // Badge muestra total de alarmas agrupadas

                        if (clusterMarker != null)
                        {
                            marker.SetIcon(clusterMarker);
                            System.Diagnostics.Debug.WriteLine($"CustomMapHandler: *** CLUSTER ICON APLICADO con badge AMARILLO {clusterPin.TotalAlarmas} ***");
                        }
                        else
                        {
                            var basicIcon = BitmapDescriptorFactory.FromResource(iconResourceId);
                            marker.SetIcon(basicIcon);
                            System.Diagnostics.Debug.WriteLine("CustomMapHandler: Cluster con icono básico como fallback");
                        }
                    }
                    catch (Exception iconEx)
                    {
                        CrashlyticsHelper.LogError(iconEx, "CustomMapHandler", "CreateMarker_ClusterIcon");
                        System.Diagnostics.Debug.WriteLine($"CustomMapHandler: Error aplicando icono cluster: {iconEx.Message}");
                        marker.SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueOrange));
                        System.Diagnostics.Debug.WriteLine("CustomMapHandler: Usando marker naranja para cluster (fallback)");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"CustomMapHandler: RECURSO CLUSTER NO ENCONTRADO - {iconResourceName}");
                    marker.SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueOrange));
                    System.Diagnostics.Debug.WriteLine("CustomMapHandler: Usando marker naranja para cluster (recurso no encontrado)");
                }

                // CRÍTICO: Retornar el marker del cluster AQUÍ para evitar que se sobrescriba
                System.Diagnostics.Debug.WriteLine($"CustomMapHandler: CreateMarker completado para cluster {pin.Id}");
                return marker;
            }
            // LÓGICA PARA PINES DE ALARMAS INDIVIDUALES
            else
            {
                System.Diagnostics.Debug.WriteLine($"CustomMapHandler: Creando marker para ALARMA INDIVIDUAL tipo {pin.TipoAlarma}");
            }

            if (pin.AlarmaCercana != null && !(pin is ClusterPin))
            {
                string iconResourceName = GetAlarmIconResourceName(pin.TipoAlarma, pin.AlarmaCercana.estado_alarma);
                int iconResourceId = GetResourceId(iconResourceName);

                System.Diagnostics.Debug.WriteLine($"CustomMapHandler: Recurso alarma '{iconResourceName}' = {iconResourceId}");

                if (iconResourceId != 0)
                {
                    try
                    {
                        var customMarker = CreateCustomMarker(iconResourceId, pin.AlarmaCercana.cantidad_interacciones,
                            pin.AlarmaCercana.flag_alarma_siendo_atendida, pin.AlarmaCercana.estado_alarma,
                            pin.AlarmaCercana.flag_red_confianza);

                        if (customMarker != null)
                        {
                            marker.SetIcon(customMarker);
                            System.Diagnostics.Debug.WriteLine("CustomMapHandler: *** ICONO PERSONALIZADO APLICADO EXITOSAMENTE ***");
                        }
                        else
                        {
                            var basicIcon = BitmapDescriptorFactory.FromResource(iconResourceId);
                            marker.SetIcon(basicIcon);
                            System.Diagnostics.Debug.WriteLine("CustomMapHandler: Icono b�sico aplicado como fallback");
                        }
                    }
                    catch (Exception iconEx)
                    {
                        CrashlyticsHelper.LogError(iconEx, "CustomMapHandler", "CreateMarker_CustomIcon");
                        System.Diagnostics.Debug.WriteLine($"CustomMapHandler: Error aplicando icono personalizado: {iconEx.Message}");
                        marker.SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueRed));
                        System.Diagnostics.Debug.WriteLine("CustomMapHandler: Usando marker rojo por defecto como �ltimo fallback");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"CustomMapHandler: RECURSO NO ENCONTRADO - {iconResourceName}");
                    marker.SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueRed));
                    System.Diagnostics.Debug.WriteLine("CustomMapHandler: Usando marker rojo por defecto (recurso no encontrado)");
                }
            }
            else if (pin.TipoAlarma > 0)
            {
                // Pin de viewport (desde PinesMapa) sin AlarmaCercana pero con TipoAlarma válido.
                // Usar ícono según tipo de alarma directamente (estado_alarma = pin.Address != "Cerrada").
                bool estadoActivoViewport = pin.Address != "Cerrada";
                string iconResourceName = GetAlarmIconResourceName(pin.TipoAlarma, estadoActivoViewport);
                int iconResourceId = GetResourceId(iconResourceName);

                System.Diagnostics.Debug.WriteLine($"CustomMapHandler: Pin viewport sin AlarmaCercana — tipo={pin.TipoAlarma}, recurso='{iconResourceName}' = {iconResourceId}");

                if (iconResourceId != 0)
                {
                    try
                    {
                        var basicIcon = BitmapDescriptorFactory.FromResource(iconResourceId);
                        marker.SetIcon(basicIcon);
                        System.Diagnostics.Debug.WriteLine("CustomMapHandler: *** ICONO VIEWPORT APLICADO EXITOSAMENTE ***");
                    }
                    catch (Exception iconEx)
                    {
                        CrashlyticsHelper.LogError(iconEx, "CustomMapHandler", "CreateMarker_ViewportIcon");
                        System.Diagnostics.Debug.WriteLine($"CustomMapHandler: Error aplicando icono viewport: {iconEx.Message}");
                        marker.SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueRed));
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"CustomMapHandler: RECURSO VIEWPORT NO ENCONTRADO - {iconResourceName}");
                    marker.SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueRed));
                }
            }
            else
            {
                marker.SetIcon(BitmapDescriptorFactory.DefaultMarker());
                System.Diagnostics.Debug.WriteLine("CustomMapHandler: Usando marker est�ndar para pin sin alarma");
            }

            System.Diagnostics.Debug.WriteLine($"CustomMapHandler: CreateMarker completado para pin {pin.Id}");
            return marker;
        }
        catch (Exception ex)
        {
            CrashlyticsHelper.LogError(ex, "CustomMapHandler", "CreateMarker");
            System.Diagnostics.Debug.WriteLine($"CustomMapHandler: Error en CreateMarker: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"CustomMapHandler: StackTrace: {ex.StackTrace}");
            return null;
        }
    }

    // M�todo para obtener nombre del recurso de alarma (sin cambios)
    /// <summary>
    /// Obtiene el nombre del recurso de icono de alarma de forma DINÁMICA desde App.TiposAlarmaDisponibles.
    /// Esto permite agregar nuevos tipos de alarma sin modificar código.
    /// Fallback: Si no se encuentra el tipo, usa hardcoded mapping para tipos existentes (1-9).
    /// </summary>
    private string GetAlarmIconResourceName(long tipoAlarma, bool estadoAlarma)
    {
        // MÉTODO DINÁMICO: Buscar icono desde App.TiposAlarmaDisponibles
        if (App.TiposAlarmaDisponibles != null && App.TiposAlarmaDisponibles.Count > 0)
        {
            var tipo = App.TiposAlarmaDisponibles.FirstOrDefault(t => t.TipoalarmaId == tipoAlarma);
            if (tipo != null && !string.IsNullOrEmpty(tipo.IconoPathSinExtension))
            {
                System.Diagnostics.Debug.WriteLine($"CustomMapHandler: ✅ Icono dinámico encontrado para tipo {tipoAlarma}: '{tipo.IconoPathSinExtension}'");

                // Para alarmas cerradas, usar icono específico si existe
                if (!estadoAlarma)
                {
                    // Caso especial: tipo 9 (persecución) tiene icono cerrado específico
                    if (tipoAlarma == 9)
                        return "direccionescapesospechosocerrada";

                    // Otros tipos usan icono genérico de alarma cerrada
                    return "closedalarmpin";
                }

                return tipo.IconoPathSinExtension;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"CustomMapHandler: ⚠️ Tipo {tipoAlarma} NO encontrado en App.TiposAlarmaDisponibles, usando fallback");
            }
        }

        // FALLBACK: Mapping hardcodeado completo (por si App.TiposAlarmaDisponibles no está cargado aún).
        // FIX 2026-03-01: Ampliado de tipos 1-9 a 1-14 con nombres de icono correctos según BD.
        // Tipos 1 y 2 tenían nombres incorrectos ("warningpin"/"dangerpin"); corregidos a los nombres reales.
        System.Diagnostics.Debug.WriteLine($"CustomMapHandler: ⚠️ App.TiposAlarmaDisponibles vacío, usando mapping hardcoded para tipo {tipoAlarma}");
        System.Diagnostics.Debug.WriteLine($"CustomMapHandler: DIAG-TIPOS: Count={App.TiposAlarmaDisponibles?.Count.ToString() ?? "NULL"} al pintar tipo {tipoAlarma}");

        if (estadoAlarma)
        {
            return tipoAlarma switch
            {
                1  => "naranjaadvertenciapeligro",
                2  => "rojodelitocrimen",
                3  => "amarillopleitocallejero",
                4  => "verdeazulmascotaperdida",
                5  => "azulpersonaperdida",
                6  => "negrodisturbiosprotestas",
                7  => "verdeacompanamientovisual",
                8  => "purpuraviolenciaintrafamiliar",
                9  => "direccionescapesospechoso",
                10 => "eventoicon",
                11 => "votoicon",
                12 => "amenazaicon",
                13 => "promocionlocal",
                14 => "alertaciudadana",
                _  => "warningpin" // Fallback final para tipos futuros desconocidos
            };
        }
        else
        {
            if (tipoAlarma == 9)
                return "direccionescapesospechosocerrada";
            else
                return "closedalarmpin";
        }
    }

    int GetResourceId(string resourceName)
    {
        try
        {
            var context = MauiContext?.Context ?? Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
            if (context == null)
            {
                System.Diagnostics.Debug.WriteLine("CustomMapHandler: GetResourceId - Context es null");
                return 0;
            }

            var resourceId = context.Resources.GetIdentifier(resourceName, "drawable", context.PackageName);

            if (resourceId == 0)
            {
                System.Diagnostics.Debug.WriteLine($"CustomMapHandler: GetResourceId - RECURSO NO ENCONTRADO: '{resourceName}' en package '{context.PackageName}'");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"CustomMapHandler: GetResourceId - �XITO: '{resourceName}' = {resourceId}");
            }

            return resourceId;
        }
        catch (Exception ex)
        {
            CrashlyticsHelper.LogError(ex, "CustomMapHandler", "GetResourceId");
            System.Diagnostics.Debug.WriteLine($"CustomMapHandler: Error en GetResourceId para '{resourceName}': {ex.Message}");
            return 0;
        }
    }

    // M�TODO CR�TICO CORREGIDO: CreateCustomMarker con mejor manejo de errores
    BitmapDescriptor CreateCustomMarker(int iconResourceId, int cantidadInteracciones,
        bool isBeingAttended, bool estadoAlarma, bool flagRedConfianza)
    {
        try
        {
            var context = MauiContext?.Context ?? Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
            if (context == null)
            {
                System.Diagnostics.Debug.WriteLine("CustomMapHandler: CreateCustomMarker - Context es null");
                return BitmapDescriptorFactory.DefaultMarker();
            }

            System.Diagnostics.Debug.WriteLine($"CustomMapHandler: CreateCustomMarker iniciado con resourceId={iconResourceId}");

            AndroidBitmap pinBitmap = AndroidBitmapFactory.DecodeResource(context.Resources, iconResourceId);
            if (pinBitmap == null)
            {
                System.Diagnostics.Debug.WriteLine($"CustomMapHandler: No se pudo decodificar bitmap para resourceId {iconResourceId}");
                return BitmapDescriptorFactory.DefaultMarker();
            }

            System.Diagnostics.Debug.WriteLine($"CustomMapHandler: Bitmap base cargado exitosamente: {pinBitmap.Width}x{pinBitmap.Height}");

            AndroidBitmap resultBitmap = pinBitmap.Copy(AndroidBitmap.Config.Argb8888, true);
            AndroidCanvas canvas = new AndroidCanvas(resultBitmap);

            // Icono del polic�a si est� siendo atendida
            if (isBeingAttended)
            {
                var policeResourceName = estadoAlarma ? "man_officer_police_icon" : "man_officer_police_icon_grey";
                var policeResourceId = GetResourceId(policeResourceName);
                System.Diagnostics.Debug.WriteLine($"CustomMapHandler: Agregando icono polic�a: {policeResourceName} = {policeResourceId}");

                if (policeResourceId != 0)
                {
                    AndroidBitmap policeBitmap = AndroidBitmapFactory.DecodeResource(context.Resources, policeResourceId);
                    if (policeBitmap != null)
                    {
                        int newPoliceWidth = policeBitmap.Width * 2 / 3;
                        int newPoliceHeight = policeBitmap.Height * 2 / 3;
                        policeBitmap = ResizeBitmap(policeBitmap, newPoliceWidth, newPoliceHeight);
                        canvas.DrawBitmap(policeBitmap, pinBitmap.Width - policeBitmap.Width - 1, -10, null);
                        System.Diagnostics.Debug.WriteLine("CustomMapHandler: Icono polic�a agregado");
                    }
                }
            }

            // Badge de notificaciones
            if (cantidadInteracciones > 0)
            {
                System.Diagnostics.Debug.WriteLine($"CustomMapHandler: Agregando badge con {cantidadInteracciones} interacciones");

                AndroidPaint paint = new AndroidPaint
                {
                    Color = estadoAlarma ? AndroidColor.Red : AndroidColor.Gray,
                    AntiAlias = true
                };

                float badgeDiameter = 40.0f;
                canvas.DrawCircle(2 + badgeDiameter / 2, 2 + badgeDiameter / 2, badgeDiameter / 2, paint);

                paint.Color = AndroidColor.White;
                paint.TextAlign = AndroidPaint.Align.Center;
                paint.TextSize = 20.0f;
                canvas.DrawText(cantidadInteracciones.ToString(), 2 + badgeDiameter / 2,
                    (2 + badgeDiameter / 2) + (paint.TextSize / 3), paint);

                System.Diagnostics.Debug.WriteLine("CustomMapHandler: Badge de notificaciones agregado");
            }

            // Icono de verificaci�n
            if (flagRedConfianza)
            {
                var verifiedResourceId = GetResourceId("verified");
                System.Diagnostics.Debug.WriteLine($"CustomMapHandler: Agregando icono verificaci�n: verified = {verifiedResourceId}");

                if (verifiedResourceId != 0)
                {
                    AndroidBitmap verifiedBitmap = AndroidBitmapFactory.DecodeResource(context.Resources, verifiedResourceId);
                    if (verifiedBitmap != null)
                    {
                        int newVerifiedWidth = verifiedBitmap.Width / 3;
                        int newVerifiedHeight = verifiedBitmap.Height / 3;
                        verifiedBitmap = ResizeBitmap(verifiedBitmap, newVerifiedWidth, newVerifiedHeight);
                        canvas.DrawBitmap(verifiedBitmap, 12, pinBitmap.Height - verifiedBitmap.Height - 12, null);
                        System.Diagnostics.Debug.WriteLine("CustomMapHandler: Icono verificaci�n agregado");
                    }
                }
            }

            var finalBitmap = BitmapDescriptorFactory.FromBitmap(resultBitmap);
            System.Diagnostics.Debug.WriteLine("CustomMapHandler: *** CUSTOM MARKER CREADO EXITOSAMENTE ***");
            return finalBitmap;
        }
        catch (Exception ex)
        {
            CrashlyticsHelper.LogError(ex, "CustomMapHandler", "CreateCustomMarker");
            System.Diagnostics.Debug.WriteLine($"CustomMapHandler: Error en CreateCustomMarker: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"CustomMapHandler: StackTrace: {ex.StackTrace}");
            return BitmapDescriptorFactory.DefaultMarker();
        }
    }

    // NUEVO: Método especializado para crear markers de CLUSTER con badge amarillo
    // Diferente del badge rojo de mensajes para evitar confusión
    BitmapDescriptor CreateClusterMarker(int iconResourceId, int totalAlarmas)
    {
        try
        {
            var context = MauiContext?.Context ?? Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
            if (context == null)
            {
                System.Diagnostics.Debug.WriteLine("CustomMapHandler: CreateClusterMarker - Context es null");
                return BitmapDescriptorFactory.DefaultMarker();
            }

            System.Diagnostics.Debug.WriteLine($"CustomMapHandler: CreateClusterMarker iniciado - {totalAlarmas} alarmas");

            AndroidBitmap pinBitmap = AndroidBitmapFactory.DecodeResource(context.Resources, iconResourceId);
            if (pinBitmap == null)
            {
                System.Diagnostics.Debug.WriteLine($"CustomMapHandler: No se pudo decodificar bitmap para cluster resourceId {iconResourceId}");
                return BitmapDescriptorFactory.DefaultMarker();
            }

            AndroidBitmap resultBitmap = pinBitmap.Copy(AndroidBitmap.Config.Argb8888, true);
            AndroidCanvas canvas = new AndroidCanvas(resultBitmap);

            // BADGE AMARILLO en la parte SUPERIOR CENTRAL del icono
            // Diferente al badge rojo (top-left) para mensajes
            AndroidPaint paint = new AndroidPaint
            {
                Color = AndroidColor.Rgb(255, 193, 7),  // #FFC107 - Amarillo Material Design
                AntiAlias = true
            };

            // Badge más grande (55px vs 40px del badge de mensajes)
            float badgeDiameter = 55.0f;

            // Posición: TOP CENTER (horizontalmente centrado, verticalmente arriba)
            float badgeX = (pinBitmap.Width / 2.0f);  // Centro horizontal
            float badgeY = 2 + badgeDiameter / 2;      // Top (igual que antes)

            // Dibujar círculo amarillo
            canvas.DrawCircle(badgeX, badgeY, badgeDiameter / 2, paint);

            // Texto ROJO (no blanco) para mayor contraste en fondo amarillo
            paint.Color = AndroidColor.Rgb(211, 47, 47);  // #D32F2F - Rojo Material Design
            paint.TextAlign = AndroidPaint.Align.Center;
            paint.TextSize = 24.0f;  // Más grande (24px vs 20px)
            paint.SetTypeface(global::Android.Graphics.Typeface.DefaultBold);  // Negrita para mejor visibilidad

            canvas.DrawText(totalAlarmas.ToString(), badgeX,
                badgeY + (paint.TextSize / 3), paint);

            var finalBitmap = BitmapDescriptorFactory.FromBitmap(resultBitmap);
            System.Diagnostics.Debug.WriteLine($"CustomMapHandler: *** CLUSTER MARKER CREADO - Badge amarillo con {totalAlarmas} en posición TOP-CENTER ***");
            return finalBitmap;
        }
        catch (Exception ex)
        {
            CrashlyticsHelper.LogError(ex, "CustomMapHandler", "CreateClusterMarker");
            System.Diagnostics.Debug.WriteLine($"CustomMapHandler: Error en CreateClusterMarker: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"CustomMapHandler: StackTrace: {ex.StackTrace}");
            return BitmapDescriptorFactory.DefaultMarker();
        }
    }

    AndroidBitmap ResizeBitmap(AndroidBitmap originalBitmap, int width, int height)
    {
        return AndroidBitmap.CreateScaledBitmap(originalBitmap, width, height, false);
    }

    /// <summary>
    /// Crea un bitmap con una flecha triangular rotada al bearing indicado.
    /// Se usa como marcador sintético en el punto medio de la polyline padre→hijo
    /// para indicar la dirección de escape del sospechoso.
    /// bearing=0 → apunta al Norte; 90 → Este; 180 → Sur; 270 → Oeste.
    /// </summary>
    BitmapDescriptor CreateArrowMarker(float bearing, bool isClosed)
    {
        try
        {
            const int size = 80; // px; suficientemente grande para verse en pantalla
            var bitmap = AndroidBitmap.CreateBitmap(size, size, AndroidBitmap.Config.Argb8888);
            var canvas = new AndroidCanvas(bitmap);

            var paint = new AndroidPaint { AntiAlias = true };
            paint.Color = isClosed ? AndroidColor.Gray : AndroidColor.Red;

            // Dibuja un triángulo isósceles centrado en (size/2, size/2) apuntando hacia ARRIBA (Norte=0°).
            // Vértice superior (punta de la flecha) y dos vértices de la base.
            float cx = size / 2f;
            float cy = size / 2f;
            float tipY   = 5f;           // punta: cerca del borde superior
            float baseY  = size - 10f;   // base: cerca del borde inferior
            float halfBase = 22f;        // semi-ancho de la base

            var path = new global::Android.Graphics.Path();
            path.MoveTo(cx, tipY);                       // punta
            path.LineTo(cx - halfBase, baseY);           // base izquierda
            path.LineTo(cx + halfBase, baseY);           // base derecha
            path.Close();

            // Rotar el canvas alrededor del centro antes de dibujar
            canvas.Rotate(bearing, cx, cy);
            canvas.DrawPath(path, paint);

            // Borde blanco fino para contraste sobre cualquier fondo de mapa
            paint.Color = AndroidColor.White;
            paint.SetStyle(AndroidPaint.Style.Stroke);
            paint.StrokeWidth = 3f;
            canvas.DrawPath(path, paint);

            System.Diagnostics.Debug.WriteLine($"CustomMapHandler: CreateArrowMarker OK bearing={bearing}° cerrada={isClosed}");
            return BitmapDescriptorFactory.FromBitmap(bitmap);
        }
        catch (Exception ex)
        {
            CrashlyticsHelper.LogError(ex, "CustomMapHandler", "CreateArrowMarker");
            System.Diagnostics.Debug.WriteLine($"CustomMapHandler: Error en CreateArrowMarker: {ex.Message}");
            return BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueRed);
        }
    }

    void GoogleMap_MapClick(object sender, GoogleMap.MapClickEventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"CustomMapHandler: GoogleMap_MapClick disparado en {e.Point.Latitude}, {e.Point.Longitude}");

            var customMap = VirtualView as CustomMap;
            if (customMap != null)
            {
                var location = new Location(e.Point.Latitude, e.Point.Longitude);
                System.Diagnostics.Debug.WriteLine("CustomMapHandler: Llamando customMap.OnTap UNA SOLA VEZ");
                customMap.OnTap(location);
                System.Diagnostics.Debug.WriteLine("CustomMapHandler: GoogleMap_MapClick procesado exitosamente");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("CustomMapHandler: ERROR - VirtualView no es CustomMap en GoogleMap_MapClick");
            }
        }
        catch (Exception ex)
        {
            CrashlyticsHelper.LogError(ex, "CustomMapHandler", "GoogleMap_MapClick");
            System.Diagnostics.Debug.WriteLine($"CustomMapHandler: Error en GoogleMap_MapClick: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"CustomMapHandler: StackTrace: {ex.StackTrace}");
        }
    }

    void GoogleMap_MarkerClick(object sender, GoogleMap.MarkerClickEventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"CustomMapHandler: GoogleMap_MarkerClick disparado para marker: {e.Marker.Title}");

            // Mostrar el InfoWindow autom�ticamente
            e.Marker.ShowInfoWindow();
            System.Diagnostics.Debug.WriteLine($"CustomMapHandler: InfoWindow mostrado para {e.Marker.Title}");

            // Marcar el evento como manejado para que no se propague al MapClick
            e.Handled = true;
        }
        catch (Exception ex)
        {
            CrashlyticsHelper.LogError(ex, "CustomMapHandler", "GoogleMap_MarkerClick");
            System.Diagnostics.Debug.WriteLine($"CustomMapHandler: Error en GoogleMap_MarkerClick: {ex.Message}");
        }
    }

    void OnInfoWindowClick(object sender, GoogleMap.InfoWindowClickEventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"CustomMapHandler: OnInfoWindowClick disparado para marker: {e.Marker.Title}");

            if (e.Marker.Title == "User" || string.IsNullOrEmpty(e.Marker.Title))
            {
                System.Diagnostics.Debug.WriteLine("CustomMapHandler: Ignorando clic en marker de usuario");
                return;
            }

            // Extraer el ID de la alarma del t�tulo
            var titleParts = e.Marker.Title.Split(' ');
            if (titleParts.Length < 2 || !long.TryParse(titleParts[1], out long alarmaId))
            {
                System.Diagnostics.Debug.WriteLine($"CustomMapHandler: No se pudo extraer ID de alarma del t�tulo: {e.Marker.Title}");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"CustomMapHandler: Buscando CustomPin para alarma ID: {alarmaId}");

            // Buscar el CustomPin correspondiente
            var customPin = customPins?.FirstOrDefault(p =>
                p.MarkerId == alarmaId.ToString() ||
                p.Id == alarmaId.ToString() ||
                (p.AlarmaCercana?.alarma_id == alarmaId));

            if (customPin != null)
            {
                System.Diagnostics.Debug.WriteLine($"CustomMapHandler: CustomPin encontrado, enviando MessagingCenter para alarma {alarmaId}");
                System.Diagnostics.Debug.WriteLine($"CustomMapHandler: FlagPropietarioAlarma = {customPin.FlagPropietarioAlarma}");

                // ENVIAR el mensaje al MessagingCenter como en Xamarin
                MessagingCenter.Send<object, CustomPin>(this, "InfoWindowClicked", customPin);

                System.Diagnostics.Debug.WriteLine("CustomMapHandler: Mensaje 'InfoWindowClicked' enviado exitosamente");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"CustomMapHandler: CustomPin NO encontrado para alarma {alarmaId}");
                System.Diagnostics.Debug.WriteLine($"CustomMapHandler: CustomPins disponibles: {customPins?.Count ?? 0}");

                // Log de debugging para ver qu� pins est�n disponibles
                if (customPins != null)
                {
                    foreach (var pin in customPins)
                    {
                        System.Diagnostics.Debug.WriteLine($"CustomMapHandler: Pin disponible - ID: {pin.Id}, MarkerId: {pin.MarkerId}, AlarmaId: {pin.AlarmaCercana?.alarma_id}");
                    }
                }

                // Fallback: navegaci�n directa como estaba antes
                System.Diagnostics.Debug.WriteLine("CustomMapHandler: Usando fallback - navegaci�n directa");
                Microsoft.Maui.Controls.Application.Current?.MainPage?.Navigation.PushAsync(new DescribirPage(alarmaId));
            }
        }
        catch (Exception ex)
        {
            CrashlyticsHelper.LogError(ex, "CustomMapHandler", "OnInfoWindowClick");
            System.Diagnostics.Debug.WriteLine($"CustomMapHandler: Error en OnInfoWindowClick: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"CustomMapHandler: StackTrace: {ex.StackTrace}");
        }
    }

    // NUEVO: Listener para cambios de cámara (zoom/pan) - CRÍTICO para clustering zoom-aware
    // OPTIMIZADO: Usa throttle de 300ms para reducir 285 eventos a ~15-20
    void GoogleMap_CameraIdle(object sender, EventArgs e)
    {
        try
        {
            // OPTIMIZACIÓN: Throttle para evitar procesamiento excesivo
            var now = DateTime.Now;
            if ((now - _lastCameraIdleTime).TotalMilliseconds < CAMERA_IDLE_THROTTLE_MS)
            {
                return; // Throttled - ignorar evento
            }
            _lastCameraIdleTime = now;

            if (_map == null || VirtualView is not CustomMap customMap)
                return;

            var projection = _map.Projection;
            if (projection == null)
                return;

            var visibleRegion = projection.VisibleRegion;
            if (visibleRegion == null)
                return;

            // Calcular el centro y los deltas de lat/lng
            var centerLat = (visibleRegion.LatLngBounds.Southwest.Latitude + visibleRegion.LatLngBounds.Northeast.Latitude) / 2;
            var centerLng = (visibleRegion.LatLngBounds.Southwest.Longitude + visibleRegion.LatLngBounds.Northeast.Longitude) / 2;
            var latDelta = Math.Abs(visibleRegion.LatLngBounds.Northeast.Latitude - visibleRegion.LatLngBounds.Southwest.Latitude);
            var lngDelta = Math.Abs(visibleRegion.LatLngBounds.Northeast.Longitude - visibleRegion.LatLngBounds.Southwest.Longitude);

            // Crear MapSpan MAUI
            var center = new Location(centerLat, centerLng);
            var mapSpan = new MapSpan(center, latDelta, lngDelta);

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"CustomMapHandler: CameraIdle (throttled) - LatDelta: {latDelta:F6}");
#endif

            // Disparar evento en CustomMap (esto ejecutará el re-clustering en HomePage)
            customMap.OnVisibleRegionChanged(mapSpan);
        }
        catch (Exception ex)
        {
            CrashlyticsHelper.LogError(ex, "CustomMapHandler", "GoogleMap_CameraIdle");
        }
    }

    class MapReadyCallback : Java.Lang.Object, IOnMapReadyCallback
    {
        private readonly CustomMapHandler _handler;

        public MapReadyCallback(CustomMapHandler handler)
        {
            _handler = handler;
        }

        public void OnMapReady(GoogleMap googleMap)
        {
            _handler.OnMapReady(googleMap);
        }
    }

    class InfoWindowAdapter : Java.Lang.Object, GoogleMap.IInfoWindowAdapter
    {
        public AndroidView GetInfoContents(Marker marker)
        {
            System.Diagnostics.Debug.WriteLine($"CustomMapHandler: GetInfoContents llamado para marker: {marker.Title}");
            return null; // Esto usa el layout por defecto
        }

        public AndroidView GetInfoWindow(Marker marker)
        {
            System.Diagnostics.Debug.WriteLine($"CustomMapHandler: GetInfoWindow llamado para marker: {marker.Title}");
            return null; // Esto usa el InfoWindow por defecto
        }
    }
}

