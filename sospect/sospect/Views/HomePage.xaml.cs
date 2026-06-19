// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using CommunityToolkit.Maui.Views;
using sospect.CustomRenderers;
using sospect.Helpers;
using sospect.Interfaces;
using sospect.Models;
using sospect.Views.Popups;
using sospect.Services;
using sospect.Utils;
using sospect.ViewModels;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Maps;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls.Shapes;
using CommunityToolkit.Maui;
using Microsoft.Maui.Graphics;
using Location = Microsoft.Maui.Devices.Sensors.Location;
using CommunityToolkit.Maui.Extensions;

namespace sospect.Views
{
    public partial class HomePage : ContentPage
    {
        CancellationTokenSource cts;
        Persona persona;
        private Location _lastLocation;
        private DateTime _lastLocationFetchTime;
        private bool _locationUpdateTimer;
        private bool _shouldTimerRun = true;
        private bool _isVisualizacionAlarmaEspecifica = false;
        private const int INTERVALO_VERIFICACION_SEGUNDOS = 15; // Cada 15 segundos (como en Xamarin)
        private const double DISTANCIA_MINIMA_ACTUALIZACION_METROS = 40; // 40 metros (como en Xamarin)
        private bool _trackingTimerRunning = false;
        private int _trackingTickCount = 0; // ITER3: Contador de ticks para flush periódico
        private bool _hasSentTrackingDiagnostic = false; // ITER3: Flag para flush inicial de logs

        Circle currentCircle;
        CustomPin currentUser;

        // NUEVAS VARIABLES PARA CONTROLAR EL PROBLEMA DE ZOOM
        private bool _isInitialMapSetup = true;
        private bool _isMapMovementInProgress = false;
        private MapSpan _initialMapRegion;
        private readonly object _mapUpdateLock = new object();

        // VARIABLES PARA CLUSTERING ZOOM-AWARE (sección 5 del manual)
        private int _currentZoomLevel = 15; // Default: zoom alto (sin clustering)
        private MapSpan _currentMapSpan;
        private List<AlarmaCercana> _alarmasCacheadas; // Cache de alarmas para re-clustering sin llamar API
        private bool _isClusteringEnabled = false;

        // OPTIMIZACIÓN: Flag estricto para controlar si la página está actualmente visible
        // Usado para detener TODOS los procesos del mapa cuando el usuario navega a otra página
        private bool _isPageCurrentlyVisible = false;

        // Flag para coordinar foreground/background location tracking
        private bool _isAppInForeground = false;

        // Flag para indicar que hay una alarma recién lanzada pendiente de pintar en el mapa.
        // Se activa cuando RefrescarDespuesDeAlarma detecta que la página no es visible
        // y se consume en OnAppearing para repintar desde cache sin llamar al API.
        private bool _pendienteRepintarDespuesDeAlarma = false;

        // Flag para omitir el próximo background refresh del API.
        // Se activa cuando se consume _pendienteRepintarDespuesDeAlarma en OnAppearing,
        // para evitar que EjecutarRefrescoMapa→ObtenerPines→BGAPI sobrescriba el cache
        // que ya contiene la alarma recién lanzada.
        private bool _skipNextBackgroundRefresh = false;

        // OPTIMIZACIÓN: Debouncer para eventos de cambio de región visible (reduce 285 eventos a ~15-20)
        private readonly DebounceHelper _visibleRegionDebouncer = new DebounceHelper();

        public HomePage(ObservableCollection<AlarmaCercana> alarma)
        {
            System.Diagnostics.Debug.WriteLine($"HomePage: Constructor CON ALARMA iniciado - {alarma?.Count ?? 0} alarmas");

            InitializeComponent();
            ConfigurarGestosMapa();

            // CRÍTICO: Marcar que este es un modo de visualización especial
            _isVisualizacionAlarmaEspecifica = true;

            BindingContext = new HomeViewModel(false);

            // CRÍTICO: Suscribir InfoWindowClicked incluso en modo visualización específica
            // para que al tocar el InfoWindow del pin se pueda navegar correctamente
            SuscribirInfoWindowClicked();
            System.Diagnostics.Debug.WriteLine("HomePage: Modo visualización específica - InfoWindowClicked suscrito");

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(500);
                System.Diagnostics.Debug.WriteLine("HomePage: Llamando a PintarAlarma desde constructor");
                await PintarAlarma(alarma);
            });
        }

        private async Task PintarAlarma(ObservableCollection<AlarmaCercana> alarma)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"HomePage: PintarAlarma iniciado con {alarma?.Count ?? 0} alarmas");

                if (alarma == null || !alarma.Any())
                {
                    System.Diagnostics.Debug.WriteLine("HomePage: No hay alarmas para pintar");
                    return;
                }

                // CRÍTICO: Asegurarse de que estamos en el hilo principal
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    try
                    {
                        // Inicializar CustomPins
                        if (map.CustomPins == null)
                        {
                            map.CustomPins = new List<CustomPin>();
                            System.Diagnostics.Debug.WriteLine("HomePage: CustomPins inicializado");
                        }
                        else
                        {
                            map.CustomPins.Clear();
                            System.Diagnostics.Debug.WriteLine("HomePage: CustomPins limpiado");
                        }

                        // NOTA: NO limpiar map.Pins - solo usamos CustomPins para evitar duplicados
                        // El MapHandler base de MAUI procesa map.Pins y crea markers por defecto
                        System.Diagnostics.Debug.WriteLine("HomePage: Solo usando CustomPins (evita pin duplicado de Google)");

                        ParametrosUsuario parametros = JsonConvert.DeserializeObject<ParametrosUsuario>(
                            Preferences.Get("ParametrosUsuario", ""));

                        var LabelAlarma = await TranslateExtension.TranslateAsync("LabelAlarma");
                        var LabelMetros = await TranslateExtension.TranslateAsync("LabelMetros");

                        foreach (var item in alarma)
                        {
                            System.Diagnostics.Debug.WriteLine($"HomePage: Creando pin para alarma {item.alarma_id}");

                            CustomPin AlarmaPin = new CustomPin()
                            {
                                MarkerId = item.alarma_id.ToString(),
                                Id = item.alarma_id.ToString(),
                                Label = (LabelAlarma ?? "Alarma") + " " + item.alarma_id.ToString(),
                                TipoAlarma = item.tipoalarma_id,
                                Type = PinType.Generic,
                                Address = $"{item.descripciontipoalarma}. {item.distancia_en_metros} {LabelMetros ?? "metros"}",
                                Location = new Location((double)item.latitud_alarma, (double)item.longitud_alarma),
                                FlagPropietarioAlarma = item.flag_propietario_alarma,
                                AlarmaCercana = item
                            };

                            // CRÍTICO: Solo agregar a CustomPins (NO a map.Pins para evitar pin duplicado)
                            // El MapHandler base de MAUI procesa map.Pins y crea markers por defecto (pin rojo)
                            map.CustomPins.Add(AlarmaPin);

                            System.Diagnostics.Debug.WriteLine($"HomePage: Pin {item.alarma_id} agregado - Lat: {item.latitud_alarma}, Lng: {item.longitud_alarma}");
                        }

                        System.Diagnostics.Debug.WriteLine($"HomePage: Total pines agregados - CustomPins: {map.CustomPins.Count}");

                        // Forzar actualización del handler si es CustomMap
                        if (map is CustomMap customMap)
                        {
                            var updatedCustomPins = new List<CustomPin>(map.CustomPins);
                            map.CustomPins = updatedCustomPins;
                            System.Diagnostics.Debug.WriteLine("HomePage: CustomMap handler actualizado");
                        }

                        // Mover el mapa al primer pin
                        var primeraAlarma = alarma.First();
                        var location = new Location((double)primeraAlarma.latitud_alarma, (double)primeraAlarma.longitud_alarma);
                        var radioAlarmas = parametros?.radio_alarmas_mts_actual ?? 500;
                        var region = MapSpan.FromCenterAndRadius(location, new Distance(radioAlarmas));

                        map.MoveToRegion(region);
                        System.Diagnostics.Debug.WriteLine($"HomePage: Mapa centrado en {location.Latitude}, {location.Longitude}");

                        // Esperar un momento y verificar
                        await Task.Delay(1000);
                        System.Diagnostics.Debug.WriteLine($"HomePage: VERIFICACIÓN FINAL - CustomPins: {map.CustomPins?.Count}, Pins: {map.Pins?.Count}");
                    }
                    catch (Exception innerEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"HomePage: Error en MainThread de PintarAlarma: {innerEx.Message}");
                        System.Diagnostics.Debug.WriteLine($"HomePage: StackTrace: {innerEx.StackTrace}");
                        CrashlyticsHelper.LogError(innerEx, "HomePage", "PintarAlarma-MainThread");
                    }
                });

                System.Diagnostics.Debug.WriteLine("HomePage: PintarAlarma completado");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HomePage: Error en PintarAlarma: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"HomePage: StackTrace: {ex.StackTrace}");

                var LabelError = await TranslateExtension.TranslateAsync("LabelError");
                await ModernAlerts.ShowError(LabelError, ex.Message);
                CrashlyticsHelper.LogError(ex, "HomePage", "PintarAlarma");
            }
        }

        public HomePage()
        {
            System.Diagnostics.Debug.WriteLine("HomePage: Constructor iniciado");
            System.Diagnostics.Debug.WriteLine($"HomePage: Instancia creada - HashCode: {this.GetHashCode()}");

            this.persona = App.persona;
            InitializeComponent();

            // CRÍTICO: Configurar gestos del mapa (incluye suscripción a VisibleRegionChanged para clustering)
            ConfigurarGestosMapa();

            // Suscribir InfoWindowClicked usando el método compartido
            SuscribirInfoWindowClicked();

            NavigationPage.SetHasNavigationBar(this, false);

            MessagingCenter.Subscribe<object, string>(this, "Refrescar", async (sender, cadenaVacia) =>
            {
                await ObtenerPines();
            });

            MessagingCenter.Subscribe<IBackgroundService, List<AlarmaCercana>>(this, "", async (sender, arg) =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"HomePage: *** MENSAJE DE BACKGROUND SERVICE RECIBIDO *** con {arg?.Count ?? 0} alarmas");
                    System.Diagnostics.Debug.WriteLine($"HomePage: Sender type: {sender?.GetType()?.Name}");

                    if (arg != null && arg.Any())
                    {
                        System.Diagnostics.Debug.WriteLine("HomePage: Pintando alarmas en mapa...");
                        // CRÍTICO: Esperar a que termine de pintar alarmas ANTES de actualizar ubicación
                        await PintarAlarmasEnMapa(arg);
                        System.Diagnostics.Debug.WriteLine("HomePage: Alarmas pintadas exitosamente, ahora actualizando ubicación...");
                        // FIX Iter2: centrarMapa:true porque BackgroundService envía datos cuando el usuario se movió
                        ActualizarUbicacionEnMapa(centrarMapa: true);
                        System.Diagnostics.Debug.WriteLine("HomePage: Ubicación actualizada exitosamente");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("HomePage: Lista de alarmas vacía o null");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"HomePage: Error en suscripción BackgroundService: {ex.Message}");
                    CrashlyticsHelper.LogError(ex, "HomePage", "BackgroundServiceSubscription");
                }
            });

            try
            {
                CheckAndUpdateLocation();
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "HomePage", "Constructor");
            }

            Application.Current.Dispatcher.StartTimer(TimeSpan.FromSeconds(INTERVALO_VERIFICACION_SEGUNDOS), () =>
            {
                try
                {
                    // ITER4: Log seguro via CrashlyticsHelper (no Firebase directo que crashea el timer)
                    try
                    {
                        CrashlyticsHelper.LogDiagnostico("Timer-Tick",
                            $"init={_isInitialMapSetup} run={_shouldTimerRun} tracking={_trackingTimerRunning} fg={_isAppInForeground} lastLoc={_lastLocation != null}");
                    }
                    catch { /* Nunca bloquear el timer por logging */ }

                    // Solo ejecutar si:
                    // 1. Ya terminó la configuración inicial
                    // 2. La página está visible (OnAppearing fue llamado)
                    // 3. No hay otro tracking en progreso
                    if (!_isInitialMapSetup && _shouldTimerRun && !_trackingTimerRunning)
                    {
                        System.Diagnostics.Debug.WriteLine($"Timer: Verificando ubicación cada {INTERVALO_VERIFICACION_SEGUNDOS} segundos");
                        _ = VerificarYActualizarUbicacionSiNecesario();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Timer: Error verificando ubicación: {ex.Message}");
                    CrashlyticsHelper.LogError(ex, "HomePage", "TrackingTimer");
                }

                return true; // FIX Iter5: Timer SIEMPRE vivo. Los guards internos (_shouldTimerRun, _isAppInForeground) controlan si ejecuta o no.
            });

            BindingContext = new HomeViewModel(true);

            MessagingCenter.Subscribe<LanzarAlarmaViewModel, bool>(this, "ModificarVariable", (sender, value) =>
            {
                if (BindingContext is HomeViewModel vm)
                {
                    vm.IsRunning = value;
                }
            });

            MessagingCenter.Subscribe<LanzarAlarmaViewModel, string>(this, "RefrescarConGestos", async (sender, mensaje) =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"HomePage: *** MENSAJE RefrescarConGestos RECIBIDO *** - {mensaje}");

                    // CRÍTICO: Preservar la configuración de gestos ANTES del refresco
                    bool wasInputTransparent = map.InputTransparent;
                    bool hasScrollEnabled = map.HasScrollEnabled;
                    bool hasZoomEnabled = map.HasZoomEnabled;
                    bool hasRotationEnabled = map.HasRotationEnabled;

                    // IMPORTANTE: RefrescarConGestos es para zoom-out/exploración del mapa (viralidad)
                    // DEBE llamar al API porque las alarmas lejanas NO están en el cache local
                    // Ver manual: 0704-zoom-out-behavior-viral-discovery-rule.md y 0620-regla-de-asimetría-intencional-mapa-vs-pestañas.md
                    System.Diagnostics.Debug.WriteLine("HomePage: RefrescarConGestos - llamando API para obtener alarmas lejanas");
                    await ObtenerPines(centrarMapa: false, forceApiRefresh: true);

                    // CRÍTICO: Restaurar inmediatamente la configuración de gestos
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        map.InputTransparent = wasInputTransparent;
                        map.HasScrollEnabled = hasScrollEnabled;
                        map.HasZoomEnabled = hasZoomEnabled;
                        map.HasRotationEnabled = hasRotationEnabled;

                        // Verificar que los gesture recognizers aún estén presentes
                        if (map.GestureRecognizers.Count == 0)
                        {
                            ConfigurarGestosMapa();
                            System.Diagnostics.Debug.WriteLine("HomePage: Gesture recognizers reconfigurados después del refresco");
                        }

                        System.Diagnostics.Debug.WriteLine("HomePage: RefrescarConGestos completado - gestos preservados");
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"HomePage: Error en RefrescarConGestos: {ex.Message}");
                    CrashlyticsHelper.LogError(ex, "HomePage", "RefrescarConGestos");

                    // Como fallback, reconfigurar gestos
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        ConfigurarGestosMapa();
                    });
                }
            });

            // AGREGAR ESTA SUSCRIPCIÓN EN EL CONSTRUCTOR DE HomePage, después de las otras suscripciones:

            MessagingCenter.Subscribe<CustomMap, Location>(this, "MapTapped", async (sender, location) =>
            {
                var LabelError = TranslateExtension.Translate("LabelError");
                var LabelOK = TranslateExtension.Translate("LabelOK");
                try
                {
                    System.Diagnostics.Debug.WriteLine($"HomePage: *** MENSAJE 'MapTapped' RECIBIDO *** en {location.Latitude}, {location.Longitude}");
                    System.Diagnostics.Debug.WriteLine($"HomePage: Instancia que recibe - HashCode: {this.GetHashCode()}");
                    System.Diagnostics.Debug.WriteLine($"HomePage: Sender HashCode: {sender?.GetHashCode()}");

                    // Verificar que tenemos una ubicación válida
                    if (location != null)
                    {
                        System.Diagnostics.Debug.WriteLine("HomePage: Location válida, creando popup");

                        // Crear y mostrar el popup de confirmación usando las coordenadas del tap
                        var latitude = Math.Round(location.Latitude, 6);
                        var longitude = Math.Round(location.Longitude, 6);

                        System.Diagnostics.Debug.WriteLine($"HomePage: Coordenadas redondeadas: {latitude}, {longitude}");

                        var popup = new Views.Popups.ConfirmarLanzarAlarma(latitude, longitude);

                        System.Diagnostics.Debug.WriteLine("HomePage: Popup creado, mostrando...");
                        await this.ShowPopupAsync(popup);
                        System.Diagnostics.Debug.WriteLine("HomePage: Popup mostrado exitosamente");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("HomePage: ERROR - location es null en MapTapped");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"HomePage: *** ERROR CRÍTICO en MapTapped handler: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"HomePage: StackTrace: {ex.StackTrace}");
                    CrashlyticsHelper.LogError(ex, "HomePage", "MapTapped-Constructor");

                    // Como fallback, mostrar un mensaje de error
                    await ModernAlerts.ShowError(LabelError, ex.Message);
                }
            });

            System.Diagnostics.Debug.WriteLine("HomePage: MessagingCenter 'MapTapped' suscrito exitosamente");

            MessagingCenter.Subscribe<object, string>(this, "AlarmaLanzadaExitosamente", async (sender, mensaje) =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"HomePage: *** MENSAJE AlarmaLanzadaExitosamente RECIBIDO (object) *** - {mensaje}");

                    // CRÍTICO: Verificar que esta instancia aún esté válida
                    if (this == null || BindingContext == null)
                    {
                        System.Diagnostics.Debug.WriteLine("HomePage: Instancia o BindingContext es null, saltando refresco");
                        return;
                    }

                    // OPTIMIZADO: Re-pintar desde cache local SIN llamar al API
                    // La alarma nueva ya fue insertada en App.AlarmasCacheadas por LanzarAlarmaViewModel
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        try
                        {
                            if (this != null && BindingContext != null)
                            {
                                System.Diagnostics.Debug.WriteLine("HomePage: AlarmaLanzadaExitosamente - usando cache local (sin API)");
                                await AplicarFiltroSinRecargarAPI();
                                System.Diagnostics.Debug.WriteLine("HomePage: Mapa refrescado desde cache después de lanzar alarma");
                            }
                        }
                        catch (Exception innerEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"HomePage: Error en MainThread de AlarmaLanzadaExitosamente: {innerEx.Message}");
                            CrashlyticsHelper.LogError(innerEx, "HomePage", "AlarmaLanzadaExitosamente-MainThread");
                        }
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"HomePage: Error refrescando después de alarma: {ex.Message}");
                    CrashlyticsHelper.LogError(ex, "HomePage", "AlarmaLanzadaExitosamente-Outer");
                }
            });

            // Suscribirse al mensaje para mostrar alarma en el mapa (desde VerMapaPopup)
            MessagingCenter.Subscribe<VerMapaPopupViewModel, AlarmaCercana>(this, "MostrarAlarmaEnMapa", async (sender, alarma) =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"HomePage: *** MENSAJE MostrarAlarmaEnMapa RECIBIDO *** - Alarma ID: {alarma.alarma_id}");
                    System.Diagnostics.Debug.WriteLine($"HomePage: Posición alarma - Lat: {alarma.latitud_alarma}, Lng: {alarma.longitud_alarma}");

                    // Centrar el mapa en la alarma
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        try
                        {
                            // Actualizar pins del mapa
                            await ObtenerPines();

                            // Centrar el mapa en la ubicación de la alarma
                            if (map != null)
                            {
                                var location = new Location((double)alarma.latitud_alarma, (double)alarma.longitud_alarma);
                                map.MoveToRegion(MapSpan.FromCenterAndRadius(location, new Distance(500)));
                                System.Diagnostics.Debug.WriteLine($"HomePage: Mapa centrado en alarma {alarma.alarma_id}");
                            }
                        }
                        catch (Exception innerEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"HomePage: Error mostrando alarma en mapa: {innerEx.Message}");
                            CrashlyticsHelper.LogError(innerEx, "HomePage", "MostrarAlarmaEnMapa-MainThread");
                        }
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"HomePage: Error en MostrarAlarmaEnMapa: {ex.Message}");
                    CrashlyticsHelper.LogError(ex, "HomePage", "MostrarAlarmaEnMapa");
                }
            });

            DiagnosticarRecepcionMensajes();

            System.Diagnostics.Debug.WriteLine("HomePage: Constructor completado");
        }

        private async Task VerificarYActualizarUbicacionSiNecesario()
        {
            // ITER4: Log seguro via CrashlyticsHelper (no Firebase directo)
            try
            {
                CrashlyticsHelper.LogDiagnostico("Verify-Enter",
                    $"fg={_isAppInForeground} tracking={_trackingTimerRunning} lastLoc={(_lastLocation != null ? $"{_lastLocation.Latitude:F4},{_lastLocation.Longitude:F4}" : "null")}");
            }
            catch { /* Nunca bloquear tracking por logging */ }

            // NUEVO: Salir si la app no está en primer plano (servicio nativo maneja ubicación)
            if (!_isAppInForeground)
            {
                System.Diagnostics.Debug.WriteLine("[HomePage] App en segundo plano, servicio nativo se encarga de ubicación");
                return;
            }

            // Patrón Twitter/X: no refrescar alarmas desde API mientras el usuario está en el feed
            // InsertaUbicacionBackground no pasa por aquí, así que las notificaciones de seguridad no se ven afectadas
            if (App.DescribirPageActiva)
            {
                System.Diagnostics.Debug.WriteLine("[HomePage] Timer: refresco de alarmas suspendido (usuario en DescribirPage)");
                _trackingTimerRunning = false;
                return;
            }

            if (_trackingTimerRunning)
            {
                System.Diagnostics.Debug.WriteLine("Tracking: Ya hay verificación en progreso, saltando");
                return;
            }

            _trackingTimerRunning = true;

            try
            {
                // ITER3: Flush periódico cada 20 ticks (~5 min) para que logs acumulados aparezcan en Crashlytics
                _trackingTickCount++;
                if (_trackingTickCount % 20 == 0)
                {
                    CrashlyticsHelper.LogDiagnostico("Tracking-Flush",
                        $"Tick #{_trackingTickCount}",
                        new Dictionary<string, string>
                        {
                            { "TickCount", _trackingTickCount.ToString() },
                            { "LastLoc", _lastLocation != null ? $"{_lastLocation.Latitude:F4},{_lastLocation.Longitude:F4}" : "null" }
                        });
                }

                System.Diagnostics.Debug.WriteLine("========== TRACKING: INICIO VERIFICACIÓN ==========");

                // Obtener ubicación actual del GPS
                var ubicacionActual = await ObtenerUbicacionGPS();

                if (ubicacionActual == null)
                {
                    System.Diagnostics.Debug.WriteLine("Tracking:  No se pudo obtener ubicación GPS");
                    System.Diagnostics.Debug.WriteLine("========== TRACKING: FIN (SIN GPS) ==========");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"Tracking:  GPS obtenido - Lat: {ubicacionActual.Latitude:F6}, Lng: {ubicacionActual.Longitude:F6}");

                // Si no tenemos ubicación previa, guardarla y actualizar
                if (_lastLocation == null)
                {
                    System.Diagnostics.Debug.WriteLine("Tracking:  Primera ubicación registrada");
                    await ActualizarUbicacionYAlarmas(ubicacionActual);
                    System.Diagnostics.Debug.WriteLine("========== TRACKING: FIN (PRIMERA VEZ) ==========");
                    return;
                }

                // Calcular distancia desde la última ubicación guardada
                var distanciaMovida = Location.CalculateDistance(
                    ubicacionActual,
                    _lastLocation,
                    DistanceUnits.Kilometers) * 1000; // Convertir a metros

                System.Diagnostics.Debug.WriteLine($"Tracking:  Distancia desde última posición: {distanciaMovida:F1}m");
                System.Diagnostics.Debug.WriteLine($"Tracking:  Última posición: Lat: {_lastLocation.Latitude:F6}, Lng: {_lastLocation.Longitude:F6}");
                System.Diagnostics.Debug.WriteLine($"Tracking:  Umbral mínimo: {DISTANCIA_MINIMA_ACTUALIZACION_METROS}m");

                // TEMPORAL: Log diagnostico CADA ejecucion del timer (no solo en movimiento)
                CrashlyticsHelper.LogDiagnostico("Tracking-Timer",
                    $"GPS:{ubicacionActual.Latitude:F6},{ubicacionActual.Longitude:F6} Dist:{distanciaMovida:F1}m Umbral:{DISTANCIA_MINIMA_ACTUALIZACION_METROS}m",
                    new Dictionary<string, string>
                    {
                        { "GPSLat", ubicacionActual.Latitude.ToString("F6") },
                        { "GPSLng", ubicacionActual.Longitude.ToString("F6") },
                        { "LastLat", _lastLocation.Latitude.ToString("F6") },
                        { "LastLng", _lastLocation.Longitude.ToString("F6") },
                        { "Distancia", distanciaMovida.ToString("F1") },
                        { "Actualizar", (distanciaMovida >= DISTANCIA_MINIMA_ACTUALIZACION_METROS).ToString() }
                    });

                // Solo actualizar si se movió más de la distancia mínima
                if (distanciaMovida >= DISTANCIA_MINIMA_ACTUALIZACION_METROS)
                {
                    System.Diagnostics.Debug.WriteLine($"Tracking:  MOVIMIENTO DETECTADO - Actualizando todo");

                    // TEMPORAL: Log diagnostico para depurar GPS en campo
                    CrashlyticsHelper.LogDiagnostico("Tracking-Movimiento",
                        $"Movimiento {distanciaMovida:F1}m detectado, actualizando",
                        new Dictionary<string, string>
                        {
                            { "NuevaLat", ubicacionActual.Latitude.ToString("F6") },
                            { "NuevaLng", ubicacionActual.Longitude.ToString("F6") },
                            { "AnteriorLat", _lastLocation?.Latitude.ToString("F6") ?? "null" },
                            { "AnteriorLng", _lastLocation?.Longitude.ToString("F6") ?? "null" },
                            { "Distancia", distanciaMovida.ToString("F1") }
                        });

                    await ActualizarUbicacionYAlarmas(ubicacionActual);
                    System.Diagnostics.Debug.WriteLine("Tracking:  Actualización completada");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Tracking:  SIN MOVIMIENTO SIGNIFICATIVO - No actualizando (ahorro batería/API)");
                }

                System.Diagnostics.Debug.WriteLine("========== TRACKING: FIN VERIFICACIÓN ==========");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Tracking:  ERROR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Tracking: StackTrace: {ex.StackTrace}");
                CrashlyticsHelper.LogError(ex, "HomePage", "VerificarYActualizarUbicacionSiNecesario");
            }
            finally
            {
                _trackingTimerRunning = false;
            }
        }

        private async Task<Location> ObtenerUbicacionGPS()
        {
            try
            {
                // FIX Iter2: Best accuracy para obtener GPS fresco (Medium devuelve cached en datos moviles)
                var request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10));
                var location = await Geolocation.GetLocationAsync(request);

                return location;
            }
            catch (FeatureNotEnabledException)
            {
                System.Diagnostics.Debug.WriteLine("Tracking: GPS no habilitado, intentando última ubicación conocida");
                // TEMPORAL: Log diagnostico para depurar GPS en campo
                CrashlyticsHelper.LogDiagnostico("ObtenerUbicacionGPS", "GPS no habilitado, usando fallback GetLastKnownLocationAsync");
                return await Geolocation.GetLastKnownLocationAsync();
            }
            catch (PermissionException)
            {
                System.Diagnostics.Debug.WriteLine("Tracking: Sin permisos de ubicación");
                CrashlyticsHelper.LogDiagnostico("ObtenerUbicacionGPS", "Sin permisos de ubicacion");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Tracking: Error obteniendo GPS: {ex.Message}");
                CrashlyticsHelper.LogDiagnostico("ObtenerUbicacionGPS", $"Error: {ex.Message}");
                return null;
            }
        }

        // REEMPLAZAR TODO EL MÉTODO ActualizarUbicacionYAlarmas (línea 423)
        private async Task ActualizarUbicacionYAlarmas(Location nuevaUbicacion)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("========== TRACKING: Actualizando ubicación ==========");
                System.Diagnostics.Debug.WriteLine($"Tracking: Nueva ubicación - Lat: {nuevaUbicacion.Latitude:F6}, Lng: {nuevaUbicacion.Longitude:F6}");

                // Actualizar App.ubicacionActual
                if (App.ubicacionActual == null)
                {
                    App.ubicacionActual = new Ubicaciones();
                }

                App.ubicacionActual.latitud = nuevaUbicacion.Latitude;
                App.ubicacionActual.longitud = nuevaUbicacion.Longitude;

                if (App.persona != null)
                {
                    App.ubicacionActual.p_user_id_thirdparty = App.persona.user_id_thirdparty;
                    App.ubicacionActual.Pais = App.persona.Pais;
                }

                // Guardar como última ubicación
                _lastLocation = nuevaUbicacion;
                _lastLocationFetchTime = DateTime.Now;

                System.Diagnostics.Debug.WriteLine("Tracking: Actualizando alarmas y centrando mapa en nueva ubicación");

                // FIX Iter3: ObtenerPines con centrarMapa:false durante tracking
                // para ELIMINAR el MoveToRegion competidor dentro de ActualizarUbicacionEnMapa.
                // Solo este método (ActualizarUbicacionYAlarmas) centrará el mapa después.
                await ObtenerPines(centrarMapa: false);

                // FIX Iter3: Esperar 300ms para que DrawUserCircle y el pintado de alarmas
                // (que despachan trabajo a MainThread internamente) terminen ANTES del MoveToRegion.
                // Esto evita que las modificaciones a MapElements/CustomPins reseteen la región visible.
                await Task.Delay(300);

                // FIX Iter3: ÚNICO MoveToRegion durante tracking — sin competencia de otros despachos.
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        var parametrosString = Preferences.Get("ParametrosUsuario", "");
                        ParametrosUsuario parametros = null;
                        if (!string.IsNullOrEmpty(parametrosString))
                        {
                            parametros = JsonConvert.DeserializeObject<ParametrosUsuario>(parametrosString);
                        }
                        var valorRadio = parametros?.radio_alarmas_mts_actual ?? 100;
                        var center = new Location(nuevaUbicacion.Latitude, nuevaUbicacion.Longitude);
                        var mapSpan = MapSpan.FromCenterAndRadius(center, new Distance(valorRadio));

                        // ITER4: Log seguro antes de MoveToRegion
                        CrashlyticsHelper.LogDiagnostico("Move-Pre",
                            $"lat={nuevaUbicacion.Latitude:F6} lng={nuevaUbicacion.Longitude:F6} radio={valorRadio}");

                        map.MoveToRegion(mapSpan);

                        // ITER4: Log seguro después de MoveToRegion
                        CrashlyticsHelper.LogDiagnostico("Move-Post",
                            $"MoveToRegion completado en {nuevaUbicacion.Latitude:F6},{nuevaUbicacion.Longitude:F6}");

                        System.Diagnostics.Debug.WriteLine($"Tracking: Mapa centrado explicitamente en {nuevaUbicacion.Latitude:F6},{nuevaUbicacion.Longitude:F6}");

                        // ITER3: Flush inicial — enviar UN non-fatal para que los Log() acumulados aparezcan en Firebase
                        if (!_hasSentTrackingDiagnostic)
                        {
                            _hasSentTrackingDiagnostic = true;
                            CrashlyticsHelper.LogDiagnostico("Tracking-Flush",
                                $"Primer centrado de tracking",
                                new Dictionary<string, string>
                                {
                                    { "CenterLat", nuevaUbicacion.Latitude.ToString("F6") },
                                    { "CenterLng", nuevaUbicacion.Longitude.ToString("F6") },
                                    { "Radio", valorRadio.ToString() }
                                });
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Tracking: Error centrando mapa: {ex.Message}");
                        CrashlyticsHelper.LogError(ex, "HomePage", "ActualizarUbicacionYAlarmas-MoveToRegion");
                    }
                });

                System.Diagnostics.Debug.WriteLine("========== TRACKING: Completado ==========");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Tracking: ERROR: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "HomePage", "ActualizarUbicacionYAlarmas");
            }
        }

        private void AlRecibirMensaje()
        {
            if (BindingContext is HomeViewModel vm)
            {
                vm.NumeroDeNotificaciones += 1;
            }
        }
        // CRÍTICO: Flag para prevenir llamadas concurrentes a PintarAlarmasEnMapa
        private bool _isPintandoAlarmas = false;

        private async Task PintarAlarmasEnMapa(List<AlarmaCercana> arg)
        {
            // DIAGNÓSTICO - INICIO
            System.Diagnostics.Debug.WriteLine($"[DIAG-PINTAR] ====== INICIO PintarAlarmasEnMapa ======");
            System.Diagnostics.Debug.WriteLine($"[DIAG-PINTAR] Alarmas recibidas: {arg?.Count ?? 0}");
            if (arg != null && arg.Any())
            {
                System.Diagnostics.Debug.WriteLine($"[DIAG-PINTAR] Primera a pintar: ID={arg.First().alarma_id}, Lat={arg.First().latitud_alarma}, Lon={arg.First().longitud_alarma}");
                System.Diagnostics.Debug.WriteLine($"[DIAG-PINTAR] IDs primeras 5: {string.Join(",", arg.Take(5).Select(a => a.alarma_id))}");
            }
            // DIAGNÓSTICO - FIN

            try
            {
                // OPTIMIZACIÓN: Verificar si la página está visible antes de procesar
                if (!_isPageCurrentlyVisible)
                {
                    System.Diagnostics.Debug.WriteLine("HomePage: PintarAlarmasEnMapa IGNORADO - Página no visible");
                    System.Diagnostics.Debug.WriteLine($"[DIAG-PINTAR] ====== FIN PintarAlarmasEnMapa (no visible) ======");
                    return;
                }

                if (_isVisualizacionAlarmaEspecifica)
                {
                    System.Diagnostics.Debug.WriteLine("HomePage: PintarAlarmasEnMapa IGNORADO - Modo visualización específica");
                    return;
                }

                // CRÍTICO: Prevenir llamadas concurrentes - solo una actualización a la vez
                if (_isPintandoAlarmas)
                {
                    System.Diagnostics.Debug.WriteLine($"HomePage: PintarAlarmasEnMapa IGNORADO - Ya hay una actualización en progreso");
                    return;
                }

                _isPintandoAlarmas = true;
                System.Diagnostics.Debug.WriteLine($"HomePage: PintarAlarmasEnMapa iniciado con {arg?.Count ?? 0} alarmas");

                var LabelOK = await TranslateExtension.TranslateAsync("LabelOK");
                var LabelInformacion = await TranslateExtension.TranslateAsync("LabelInformacion");
                var LblHabilitaGPSReintenta = await TranslateExtension.TranslateAsync("LblHabilitaGPSReintenta");
                var MensajeError = await TranslateExtension.TranslateAsync("MensajeError");

                Dictionary<long, CustomPin> pins = new Dictionary<long, CustomPin>();
                ParametrosUsuario parametros = JsonConvert.DeserializeObject<ParametrosUsuario>(Preferences.Get("ParametrosUsuario", ""));

                if (map != null)
                {
                    System.Diagnostics.Debug.WriteLine("HomePage: Limpiando mapa existente");

                    // PASO 1: Guardar pin del usuario ANTES del MainThread para poder usarlo después
                    CustomPin userPinToPreserve = null;
                    if (map.CustomPins != null)
                    {
                        userPinToPreserve = map.CustomPins.FirstOrDefault(p => p.Id == "User" || p.MarkerId == "User");
                        if (userPinToPreserve != null)
                        {
                            System.Diagnostics.Debug.WriteLine("HomePage: Pin de usuario guardado para preservar");
                        }
                    }

                    // CRÍTICO: Awaitable para que el flag _isPintandoAlarmas no se libere
                    // hasta que TODA la operación de UI haya terminado (limpiar + redibujar).
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        bool enModoCacheC = App.CacheMapa?.Pines?.Count > 0;
                        System.Diagnostics.Debug.WriteLine($"[PintarAlarmasEnMapa] Limpieza UI — modoCacheC={enModoCacheC}, polylines antes: {map.MapElements.OfType<Microsoft.Maui.Controls.Maps.Polyline>().Count()}");

                        // CRÍTICO: Preservar configuración de gestos ANTES de limpiar
                        var wasInputTransparent = map.InputTransparent;
                        var hasScrollEnabled = map.HasScrollEnabled;
                        var hasZoomEnabled = map.HasZoomEnabled;
                        var hasRotationEnabled = map.HasRotationEnabled;

                        // PASO 2: Remover SOLO pins de alarmas de map.Pins (mantener usuario)
                        var pinsToRemove = map.Pins.Where(pin => pin.MarkerId != "User").ToList();
                        foreach (var pin in pinsToRemove)
                        {
                            map.Pins.Remove(pin);
                        }
                        System.Diagnostics.Debug.WriteLine($"HomePage: Removidos {pinsToRemove.Count} pins de alarmas de map.Pins");

                        // PASO 3: Remover SIEMPRE las polylines al limpiar el mapa.
                        // Aunque en modo viewport PintarPinesMapaDesdeCache las redibuja,
                        // si PintarAlarmasEnMapa reemplaza CustomPins sin limpiar las polylines
                        // existentes, estas quedan huérfanas (líneas rojas flotando sin pins).
                        for (int i = map.MapElements.Count - 1; i >= 0; i--)
                        {
                            if (map.MapElements[i] is Microsoft.Maui.Controls.Maps.Polyline)
                                map.MapElements.RemoveAt(i);
                        }
                        System.Diagnostics.Debug.WriteLine($"HomePage: Polylines removidas (siempre), modo viewport={enModoCacheC}");

                        // PASO 4: NO tocar CustomPins aquí - lo haremos en un solo paso después

                        // CRÍTICO: Restaurar configuración de gestos DESPUÉS de limpiar
                        map.InputTransparent = wasInputTransparent;
                        map.HasScrollEnabled = hasScrollEnabled;
                        map.HasZoomEnabled = hasZoomEnabled;
                        map.HasRotationEnabled = hasRotationEnabled;

                        System.Diagnostics.Debug.WriteLine("HomePage: Configuración de gestos preservada");
                    });

                    // PASO 4: CACHEAR alarmas para re-clustering posterior sin llamar API
                    _alarmasCacheadas = arg.ToList();
                    System.Diagnostics.Debug.WriteLine($"HomePage: {_alarmasCacheadas.Count} alarmas cacheadas para clustering dinámico");

                    // PASO 5: APLICAR FILTRADO CLIENT-SIDE (Regla #18 del Manual)
                    // El MAPA solo muestra alarmas con flag_visible_mapa = true
                    // Este flag viene pre-calculado desde vw_busca_alarmas_por_zona2 y representa:
                    // - Alarmas activas (estado_alarma IS NULL) O cerradas en últimos 90 minutos
                    // SIMPLIFICADO: Ya no necesitamos calcular filtros complejos, la API lo hace por nosotros

                    var alarmasFiltradas = arg.Where(a => a.flag_visible_mapa).ToList();

                    System.Diagnostics.Debug.WriteLine($"HomePage: FILTRADO CLIENT-SIDE aplicado - {arg.Count} alarmas recibidas → {alarmasFiltradas.Count} alarmas visibles en mapa (flag_visible_mapa=true)");

                    // PASO 6: APLICAR CLUSTERING ZOOM-AWARE (Manual sección 5)

                    // Determinar si clustering debe activarse según zoom level actual
                    List<CustomPin> pinesParaRenderizar;

                    // Obtener radio del usuario para clustering adaptativo
                    ParametrosUsuario parametrosParaClustering = JsonConvert.DeserializeObject<ParametrosUsuario>(Preferences.Get("ParametrosUsuario", ""));
                    var radioUsuario = parametrosParaClustering?.radio_alarmas_mts_actual ?? 100;

                    if (GridClusteringHelper.DebeActivarClustering(_currentZoomLevel, alarmasFiltradas.Count, radioUsuario))
                    {
                        System.Diagnostics.Debug.WriteLine($"HomePage: Clustering ACTIVADO (zoom {_currentZoomLevel}, {alarmasFiltradas.Count} alarmas, radio {radioUsuario}m)");
                        _isClusteringEnabled = true;

                        // Clusterizar alarmas usando algoritmo grid-based con radio del usuario
                        pinesParaRenderizar = GridClusteringHelper.ClusterizarAlarmas(alarmasFiltradas, _currentZoomLevel, radioUsuario);

                        System.Diagnostics.Debug.WriteLine($"HomePage: Clustering completado - {pinesParaRenderizar.Count} pines (clusters + individuales)");

                        // CRÍTICO: Establecer textos localizados en clusters
                        var LabelAlarmas = await TranslateExtension.TranslateAsync("LabelAlarmas");
                        var LabelAlarmasAgrupadas = await TranslateExtension.TranslateAsync("LabelAlarmasAgrupadas");

                        // Llenar diccionario con pines resultantes (para polylines)
                        foreach (var pin in pinesParaRenderizar)
                        {
                            // Para clusters, usar la primera alarma del cluster como key
                            if (pin is ClusterPin cluster && cluster.AlarmasAgrupadas.Any())
                            {
                                // Establecer textos localizados para el cluster
                                cluster.Label = $"{cluster.TotalAlarmas} {LabelAlarmas ?? "alarmas"}";
                                cluster.Address = $"{cluster.TotalAlarmas} {LabelAlarmasAgrupadas ?? "alarmas agrupadas"}";

                                // Agregar todas las alarmas del cluster al diccionario
                                foreach (var alarma in cluster.AlarmasAgrupadas)
                                {
                                    pins[alarma.alarma_id] = cluster;
                                }
                            }
                            else if (pin.AlarmaCercana != null)
                            {
                                pins[pin.AlarmaCercana.alarma_id] = pin;
                            }
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"HomePage: Clustering DESACTIVADO (zoom {_currentZoomLevel}, {alarmasFiltradas.Count} alarmas)");
                        _isClusteringEnabled = false;

                        // Crear pines individuales (sin clustering)
                        pinesParaRenderizar = new List<CustomPin>();

                        foreach (var item in alarmasFiltradas)
                        {
                            var LabelAlarma = await TranslateExtension.TranslateAsync("LabelAlarma");
                            var LabelMetros = await TranslateExtension.TranslateAsync("LabelMetros");

                            CustomPin AlarmaPin = new CustomPin()
                            {
                                MarkerId = item.alarma_id.ToString(),
                                Id = item.alarma_id.ToString(),
                                Label = LabelAlarma + " " + item.alarma_id.ToString(),
                                TipoAlarma = item.tipoalarma_id,
                                Type = PinType.Generic,
                                Address = $"{item.descripciontipoalarma}. {item.distancia_en_metros} {LabelMetros}",
                                Location = new Location((double)item.latitud_alarma, (double)item.longitud_alarma),
                                FlagPropietarioAlarma = item.flag_propietario_alarma,
                                AlarmaCercana = item
                            };

                            pinesParaRenderizar.Add(AlarmaPin);
                            pins[item.alarma_id] = AlarmaPin;
                            System.Diagnostics.Debug.WriteLine($"HomePage: Pin individual de alarma {item.alarma_id} preparado - Tipo: {item.tipoalarma_id}");
                        }
                    }

                    // PASO 7: Agregar pins (clusters o individuales) a CustomPins.
                    // Siempre se ejecuta: PintarAlarmasEnMapa es el responsable de pintar
                    // los pines del radio del usuario (cache A). Las polylines se omiten
                    // en modo Cache C, pero los pines siempre deben mostrarse.

                    // FIX-POLYLINE: Capturar antes del lambda si hay Cache C con pines,
                    // para poder llamar PintarPinesMapaDesdeCache DESPUÉS del InvokeOnMainThreadAsync
                    // (el lambda es síncrono y no puede hacer await).
                    var pinesCacheCParaPolyline = App.CacheMapa?.Pines?.Count > 0
                        ? App.CacheMapa.Pines
                        : null;

                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        System.Diagnostics.Debug.WriteLine($"HomePage: Construyendo nueva lista de CustomPins con {pinesParaRenderizar.Count} pines");

                        // CRÍTICO: Construir lista completa PRIMERO, luego asignar en UNA sola operación
                        var newCustomPins = new List<CustomPin>();

                        // Agregar pin de usuario primero (si existe)
                        if (userPinToPreserve != null)
                        {
                            newCustomPins.Add(userPinToPreserve);
                            System.Diagnostics.Debug.WriteLine("HomePage: Pin de usuario agregado a nueva lista");
                        }

                        // Agregar todos los pins de alarmas (clusters o individuales)
                        newCustomPins.AddRange(pinesParaRenderizar);

                        // CRÍTICO: Asignar en UNA sola operación - esto triggerea UpdateCustomPins solo UNA vez
                        map.CustomPins = newCustomPins;

                        System.Diagnostics.Debug.WriteLine($"HomePage: CustomPins asignado en UNA sola operación - Total: {map.CustomPins.Count}");
                        System.Diagnostics.Debug.WriteLine($"HomePage: Total Pins (solo usuario): {map.Pins.Count}");

                        // PASO 8: Forzar actualización del handler
                        if (map is CustomMap customMap)
                        {
                            var updatedCustomPins = new List<CustomPin>(map.CustomPins);
                            map.CustomPins = updatedCustomPins;

                            System.Diagnostics.Debug.WriteLine("HomePage: *** FORZANDO ACTUALIZACIÓN DE CUSTOMMAP HANDLER ***");
                            System.Diagnostics.Debug.WriteLine($"HomePage: CustomPins.Count = {map.CustomPins.Count}");

                            foreach (var pin in map.CustomPins)
                            {
                                if (pin is ClusterPin cluster)
                                {
                                    System.Diagnostics.Debug.WriteLine($"HomePage: CLUSTER Pin - ID: {pin.Id}, Alarmas: {cluster.TotalAlarmas}, Tipo Dominante: {cluster.TipoDominante}");
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($"HomePage: Pin individual - ID: {pin.Id}, MarkerId: {pin.MarkerId}, Tipo: {pin.TipoAlarma}");
                                }
                            }
                        }

                        // PASO 8: Polylines y flechas.
                        // CRÍTICO: Cuando el Cache C está activo (modo viewport-driven), las polylines
                        // son responsabilidad exclusiva de PintarPinesMapaDesdeCache, que conoce exactamente
                        // qué pines están visibles en pantalla. PintarAlarmasEnMapa trabaja con el cache A
                        // completo (radio del usuario, no el viewport), y dibujaría polylines entre pines
                        // que no están visibles — produciendo líneas cruzadas y confusas.
                        bool modoCacheC = App.CacheMapa?.Pines?.Count > 0;
                        if (modoCacheC)
                        {
                            System.Diagnostics.Debug.WriteLine($"[PintarAlarmasEnMapa] PASO 8 omitido — modo viewport activo (Cache C tiene {App.CacheMapa.Pines.Count} pines). Polylines delegadas a PintarPinesMapaDesdeCache.");
                        }
                        else
                        {
                            // Modo legacy (sin Cache C): limpiar y redibujar polylines desde el cache A
                            for (int idx = map.MapElements.Count - 1; idx >= 0; idx--)
                                if (map.MapElements[idx] is Microsoft.Maui.Controls.Maps.Polyline)
                                    map.MapElements.RemoveAt(idx);

                            var arrowPinsToAdd = new List<CustomPin>();
                            foreach (var pin in pins)
                            {
                                if (pin.Value.AlarmaCercana.alarma_id_padre != null &&
                                    pins.ContainsKey(pin.Value.AlarmaCercana.alarma_id_padre.Value))
                                {
                                    var pinHijo  = pin.Value;
                                    var pinPadre = pins[pin.Value.AlarmaCercana.alarma_id_padre.Value];

                                    var lineColor = pinHijo.AlarmaCercana.estado_alarma ? Colors.Red : Colors.Gray;
                                    var polyline = new Microsoft.Maui.Controls.Maps.Polyline
                                    {
                                        StrokeColor = lineColor,
                                        StrokeWidth = 4
                                    };
                                    polyline.Geopath.Add(pinHijo.Location);
                                    polyline.Geopath.Add(pinPadre.Location);
                                    map.MapElements.Add(polyline);
                                    System.Diagnostics.Debug.WriteLine($"HomePage: Polyline agregada entre alarmas {pin.Key} y {pin.Value.AlarmaCercana.alarma_id_padre}");

                                    var midLat = (pinHijo.Location.Latitude  + pinPadre.Location.Latitude)  / 2.0;
                                    var midLon = (pinHijo.Location.Longitude + pinPadre.Location.Longitude) / 2.0;
                                    float bearing = CalcularBearing(pinPadre.Location, pinHijo.Location);
                                    var arrowPin = new CustomPin
                                    {
                                        TipoAlarma   = -1,
                                        ArrowBearing = bearing,
                                        Address      = pinHijo.AlarmaCercana.estado_alarma ? "" : "Cerrada",
                                        Location     = new Location(midLat, midLon),
                                    };
                                    arrowPin.MarkerId = "arrow_" + pin.Key;
                                    arrowPin.Id       = arrowPin.MarkerId;
                                    arrowPinsToAdd.Add(arrowPin);
                                    System.Diagnostics.Debug.WriteLine($"HomePage: Flecha agregada bearing={bearing:F1}° en ({midLat:F5},{midLon:F5})");
                                }
                            }

                            if (arrowPinsToAdd.Count > 0)
                            {
                                var updatedPins = new List<CustomPin>(map.CustomPins);
                                updatedPins.AddRange(arrowPinsToAdd);
                                map.CustomPins = updatedPins;
                            }
                        }
                    });

                    // FIX-POLYLINE: Si estaba en modo Cache C, la limpieza UI de arriba borró
                    // la polyline padre-hijo. Redibujarla ahora que el InvokeOnMainThreadAsync terminó.
                    // PintarAlarmasEnMapa solo pinta los pines del radio (cache A); la polyline
                    // es responsabilidad de PintarPinesMapaDesdeCache que conoce el viewport real.
                    if (pinesCacheCParaPolyline != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[PintarAlarmasEnMapa] FIX-POLYLINE: Redibujando polylines desde Cache C ({pinesCacheCParaPolyline.Count} pines)");
                        await PintarPinesMapaDesdeCache(pinesCacheCParaPolyline);
                    }

                    // PASO 9: Verificar gestos después de actualización (SIN TIMERS ANIDADOS)
                    // CRÍTICO: Usar Task.Delay en lugar de StartTimer para evitar conflictos de threading JNI
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            // Esperar 500ms para que el mapa termine de renderizar
                            await Task.Delay(500);

                            await MainThread.InvokeOnMainThreadAsync(() =>
                            {
                                try
                                {
                                    map.InputTransparent = false;
                                    map.HasScrollEnabled = true;
                                    map.HasZoomEnabled = true;
                                    map.HasRotationEnabled = false;

                                    var circlesInMap = map.MapElements.OfType<Circle>().Count();
                                    System.Diagnostics.Debug.WriteLine($"HomePage: Verificando círculos después de agregar alarmas: {circlesInMap}");

                                    if (circlesInMap == 0)
                                    {
                                        System.Diagnostics.Debug.WriteLine("HomePage: *** INFO: No hay círculos después de agregar alarmas ***");
                                    }
                                    else if (circlesInMap > 1)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"HomePage: *** WARNING: Múltiples círculos después de alarmas: {circlesInMap} ***");

                                        var circles = map.MapElements.OfType<Circle>().ToList();
                                        foreach (var circle in circles)
                                        {
                                            map.MapElements.Remove(circle);
                                        }
                                        currentCircle = null;
                                        System.Diagnostics.Debug.WriteLine("HomePage: Círculos duplicados removidos");
                                    }
                                    else
                                    {
                                        System.Diagnostics.Debug.WriteLine("HomePage: *** CORRECTO: Exactamente 1 círculo presente ***");
                                        // CRÍTICO: FORZAR que el círculo esté encima después de agregar alarmas
                                        EnsureCircleVisibility();
                                    }

                                    // Re-suscribir MessagingCenter INMEDIATAMENTE
                                    MessagingCenter.Unsubscribe<CustomMap, Location>(this, "MapTapped");
                                    MessagingCenter.Subscribe<CustomMap, Location>(this, "MapTapped", async (sender, location) =>
                                    {
                                        try
                                        {
                                            if (location != null)
                                            {
                                                var latitude = Math.Round(location.Latitude, 6);
                                                var longitude = Math.Round(location.Longitude, 6);
                                                var popup = new Views.Popups.ConfirmarLanzarAlarma(latitude, longitude);
                                                await this.ShowPopupAsync(popup);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"HomePage: Error en MapTapped: {ex.Message}");
                                            CrashlyticsHelper.LogError(ex, "HomePage", "MapTapped-PintarAlarmas");
                                        }
                                    });

                                    System.Diagnostics.Debug.WriteLine("HomePage: MessagingCenter RE-SUSCRITO después de actualización de alarmas");
                                    System.Diagnostics.Debug.WriteLine($"HomePage: VERIFICACIÓN FINAL - CustomPins.Count: {map.CustomPins?.Count ?? 0}");
                                    System.Diagnostics.Debug.WriteLine($"HomePage: VERIFICACIÓN FINAL - Pins.Count: {map.Pins?.Count ?? 0}");
                                    System.Diagnostics.Debug.WriteLine($"HomePage: VERIFICACIÓN FINAL - MapElements.Count: {map.MapElements?.Count ?? 0}");
                                }
                                catch (Exception gestureEx)
                                {
                                    System.Diagnostics.Debug.WriteLine($"HomePage: Error en verificación post-alarmas: {gestureEx.Message}");
                                    CrashlyticsHelper.LogError(gestureEx, "HomePage", "PintarAlarmasEnMapa-PostVerificacion");
                                }
                            });

                            // CRÍTICO: Segundo chequeo después de 1 segundo adicional (sin timer anidado)
                            await Task.Delay(1000);

                            await MainThread.InvokeOnMainThreadAsync(() =>
                            {
                                try
                                {
                                    System.Diagnostics.Debug.WriteLine("HomePage: Segundo chequeo de visibilidad del círculo");
                                    EnsureCircleVisibility();

                                    // DIAGNÓSTICO FINAL DETALLADO
                                    var finalCircles = map.MapElements.OfType<Circle>().Count();
                                    var totalElements = map.MapElements.Count;
                                    System.Diagnostics.Debug.WriteLine($"HomePage: DIAGNÓSTICO FINAL - Círculos: {finalCircles}, Total elementos: {totalElements}");

                                    if (finalCircles > 0)
                                    {
                                        var circle = map.MapElements.OfType<Circle>().FirstOrDefault();
                                        if (circle != null)
                                        {
                                            var elementIndex = map.MapElements.IndexOf(circle);
                                            System.Diagnostics.Debug.WriteLine($"HomePage: POSICIÓN CÍRCULO EN CAPAS: {elementIndex} de {totalElements - 1}");
                                            System.Diagnostics.Debug.WriteLine($"HomePage: CÍRCULO - Centro: {circle.Center.Latitude:F6}, {circle.Center.Longitude:F6}");
                                            System.Diagnostics.Debug.WriteLine($"HomePage: CÍRCULO - Radio: {circle.Radius.Kilometers * 1000:F1}m");

                                            // VERIFICAR SI ESTÁ AL FINAL (encima)
                                            if (elementIndex == totalElements - 1)
                                            {
                                                System.Diagnostics.Debug.WriteLine("HomePage: *** ✓ CÍRCULO ESTÁ EN LA CAPA SUPERIOR ***");
                                            }
                                            else
                                            {
                                                System.Diagnostics.Debug.WriteLine($"HomePage: *** ⚠️ CÍRCULO NO ESTÁ ENCIMA - Posición {elementIndex}, debería ser {totalElements - 1} ***");
                                                // FORZAR UNA VEZ MÁS
                                                EnsureCircleVisibility();
                                            }
                                        }
                                    }
                                    else
                                    {
                                        System.Diagnostics.Debug.WriteLine("HomePage: *** ❌ NO HAY CÍRCULOS VISIBLES ***");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"HomePage: Error en segundo chequeo: {ex.Message}");
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"HomePage: Error en Task.Run de verificación: {ex.Message}");
                            CrashlyticsHelper.LogError(ex, "HomePage", "PintarAlarmasEnMapa-TaskDelay");
                        }
                    });

                    App.CustomPins = map.CustomPins;
                    System.Diagnostics.Debug.WriteLine("HomePage: PintarAlarmasEnMapa completado exitosamente - SIN DUPLICACIÓN");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("HomePage: ERROR - map es null");
                    _isPintandoAlarmas = false; // Liberar flag
                    return;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HomePage: Error en PintarAlarmasEnMapa: {ex.Message}");
                Debug.WriteLine($"Error: {ex.Message}");

                var LabelOK = await TranslateExtension.TranslateAsync("LabelOK");
                var LabelInformacion = await TranslateExtension.TranslateAsync("LabelInformacion");
                var MensajeError = await TranslateExtension.TranslateAsync("MensajeError");

                await ModernAlerts.ShowWarning(LabelInformacion, MensajeError);
                CrashlyticsHelper.LogError(ex, "HomePage", "PintarAlarmasEnMapa");
            }
            finally
            {
                // CRÍTICO: Liberar flag SIEMPRE, incluso si hay error
                _isPintandoAlarmas = false;
                System.Diagnostics.Debug.WriteLine("HomePage: Flag _isPintandoAlarmas liberado");
                System.Diagnostics.Debug.WriteLine($"[DIAG-PINTAR] ====== FIN PintarAlarmasEnMapa ======");
            }
        }

        protected override void OnDisappearing()
        {
            System.Diagnostics.Debug.WriteLine("HomePage: OnDisappearing - Desuscribiendo MessagingCenter");

            // OPTIMIZACIÓN: Marcar página como NO visible ANTES de todo
            // Esto detiene inmediatamente todos los procesos del mapa
            _isPageCurrentlyVisible = false;
            _isAppInForeground = false; // NUEVO: Background service toma control

            // OPTIMIZACIÓN: Cancelar cualquier debounce pendiente
            _visibleRegionDebouncer.Cancel();

            // OPTIMIZACIÓN: Desuscribir evento VisibleRegionChanged para evitar procesamiento
            if (map != null)
            {
                map.VisibleRegionChanged -= OnMapVisibleRegionChanged;
                System.Diagnostics.Debug.WriteLine("HomePage: VisibleRegionChanged desuscrito");
            }

            base.OnDisappearing();
            MessagingCenter.Unsubscribe<object, CustomPin>(this, "InfoWindowClicked");

            // CRÍTICO: Asegurar que se desuscriba correctamente
            MessagingCenter.Unsubscribe<CustomMap, Location>(this, "MapTapped");
            System.Diagnostics.Debug.WriteLine("HomePage: MapTapped desuscrito");

            // Nueva suscripción
            MessagingCenter.Unsubscribe<LanzarAlarmaViewModel, string>(this, "RefrescarConGestos");

            // SOLUCIÓN 5: Agregar las desuscripciones faltantes
            MessagingCenter.Unsubscribe<IBackgroundService, List<AlarmaCercana>>(this, "");
            MessagingCenter.Unsubscribe<object, string>(this, "AlarmaLanzadaExitosamente");

            // NUEVA DESUSCRIPCIÓN para mensajes del ViewModel
            MessagingCenter.Unsubscribe<LanzarAlarmaViewModel, string>(this, "AlarmaLanzadaExitosamente");

            // Desuscribir MostrarAlarmaEnMapa
            MessagingCenter.Unsubscribe<VerMapaPopupViewModel, AlarmaCercana>(this, "MostrarAlarmaEnMapa");

            System.Diagnostics.Debug.WriteLine("HomePage: Todas las suscripciones limpiadas en OnDisappearing");

            // Limpiar gestos al salir
            LimpiarGestosDelMapa();

            _shouldTimerRun = false;

            System.Diagnostics.Debug.WriteLine("HomePage: OnDisappearing completado");
        }

        /// <summary>
        /// Suscribe el evento InfoWindowClicked del MessagingCenter.
        /// Se extrae a un método separado para poder reutilizarlo en:
        /// - Constructor normal
        /// - Constructor de modo visualización específica
        /// - OnAppearing
        /// </summary>
        private void SuscribirInfoWindowClicked()
        {
            // Primero desuscribir para evitar duplicados
            MessagingCenter.Unsubscribe<object, CustomPin>(this, "InfoWindowClicked");

            MessagingCenter.Subscribe<object, CustomPin>(this, "InfoWindowClicked", async (sender, customPin) =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"HomePage: *** InfoWindowClicked RECIBIDO *** - Pin: {customPin?.Id}");

                    // Detectar si es un ClusterPin y hacer zoom al área
                    if (customPin is ClusterPin cluster)
                    {
                        System.Diagnostics.Debug.WriteLine($"HomePage: Tap en CLUSTER con {cluster.TotalAlarmas} alarmas - Haciendo zoom al área");

                        try
                        {
                            if (cluster.ClusterBounds != null)
                            {
                                await MainThread.InvokeOnMainThreadAsync(() =>
                                {
                                    map.MoveToRegion(cluster.ClusterBounds);
                                    System.Diagnostics.Debug.WriteLine($"HomePage: Zoom al cluster completado");
                                });

                                await Task.Delay(300);
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine("HomePage: ClusterBounds es null, no se puede hacer zoom");
                            }
                        }
                        catch (Exception clusterEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"HomePage: Error haciendo zoom a cluster: {clusterEx.Message}");
                            CrashlyticsHelper.LogError(clusterEx, "HomePage", "ClusterZoom");
                        }
                    }
                    else
                    {
                        // Pin individual - comportamiento normal
                        System.Diagnostics.Debug.WriteLine($"HomePage: Pin individual tocado - Alarma {customPin.AlarmaCercana?.alarma_id}, Propietario: {customPin.FlagPropietarioAlarma}");

                        var alarma = customPin.AlarmaCercana;

                        // VOTACIÓN DE CIERRE COMUNITARIO (2026-03-29):
                        // - Propietario de la alarma → VerHistorialAlarmaPage (solo lectura, no puede votar)
                        // - Otros usuarios           → CierreEncuestaPage (para votar; ya bloquea si ya votó)
                        if (alarma?.TieneVotacionActiva == true)
                        {
                            if (alarma.flag_propietario_alarma)
                            {
                                System.Diagnostics.Debug.WriteLine($"HomePage: Alarma {alarma.alarma_id} en votación + propietario -> VerHistorialAlarmaPage");
                                await Navigation.PushAsync(new VerHistorialAlarmaPage(alarma));
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"HomePage: Alarma {alarma.alarma_id} en votación -> CierreEncuestaPage");
                                await Navigation.PushAsync(new CierreEncuestaPage(alarma));
                            }
                        }
                        else if (customPin.FlagPropietarioAlarma)
                        {
                            System.Diagnostics.Debug.WriteLine($"HomePage: Es propietario, navegando a DescribirAlarmaPage");
                            await Navigation.PushAsync(new DescribirAlarmaPage(alarma));
                        }
                        else
                        {
                            // NO es propietario - navegar a HistorialPage mostrando solo esta alarma
                            System.Diagnostics.Debug.WriteLine($"HomePage: NO es propietario, navegando a HistorialPage con alarma {alarma?.alarma_id}");
                            await Navigation.PushAsync(new HistorialPage(alarma?.alarma_id));
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"HomePage: Error en InfoWindowClicked: {ex.Message}");
                    CrashlyticsHelper.LogError(ex, "HomePage", "InfoWindowClicked");
                }
            });

            System.Diagnostics.Debug.WriteLine("HomePage: InfoWindowClicked suscrito exitosamente");
        }

        private void DiagnosticarSuscripciones()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== DIAGNÓSTICO DE SUSCRIPCIONES ===");

                // CORRECCIÓN: Verificar si el servicio existe antes de usarlo
                var backgroundService = DependencyService.Get<IBackgroundService>();
                if (backgroundService != null)
                {
                    // Enviar mensaje de prueba para BackgroundService
                    var testAlarms = new List<AlarmaCercana>();
                    MessagingCenter.Send<IBackgroundService, List<AlarmaCercana>>(
                        backgroundService,
                        "",
                        testAlarms);
                    System.Diagnostics.Debug.WriteLine("Mensaje de prueba BackgroundService enviado");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("BackgroundService no disponible");
                }

                // Enviar mensaje de prueba para alarma lanzada
                MessagingCenter.Send<object, string>(this, "AlarmaLanzadaExitosamente", "Test");
                System.Diagnostics.Debug.WriteLine("Mensaje de prueba AlarmaLanzadaExitosamente enviado");

                System.Diagnostics.Debug.WriteLine("=== FIN DIAGNÓSTICO ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en diagnóstico: {ex.Message}");
            }
        }

        private void DiagnosticarMessagingCenter()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== DIAGNÓSTICO MESSAGING CENTER ===");

                // Probar envío manual
                var testLocation = new Location(4.6, -74.1);
                System.Diagnostics.Debug.WriteLine("Enviando mensaje de prueba...");

                MessagingCenter.Send<CustomMap, Location>(map, "MapTapped", testLocation);
                System.Diagnostics.Debug.WriteLine("Mensaje de prueba enviado");

                System.Diagnostics.Debug.WriteLine("=== FIN DIAGNÓSTICO MESSAGING CENTER ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en diagnóstico MessagingCenter: {ex.Message}");
            }
        }

        private void DiagnosticarEstadoGestos()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== DIAGNÓSTICO DE GESTOS ===");
                System.Diagnostics.Debug.WriteLine($"InputTransparent: {map.InputTransparent}");
                System.Diagnostics.Debug.WriteLine($"HasScrollEnabled: {map.HasScrollEnabled}");
                System.Diagnostics.Debug.WriteLine($"HasZoomEnabled: {map.HasZoomEnabled}");
                System.Diagnostics.Debug.WriteLine($"HasRotationEnabled: {map.HasRotationEnabled}");
                System.Diagnostics.Debug.WriteLine($"GestureRecognizers.Count: {map.GestureRecognizers.Count}");

                foreach (var gesture in map.GestureRecognizers)
                {
                    System.Diagnostics.Debug.WriteLine($"Gesture type: {gesture.GetType().Name}");
                }
                System.Diagnostics.Debug.WriteLine("=== FIN DIAGNÓSTICO ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en diagnóstico de gestos: {ex.Message}");
            }
        }

        private void DiagnosticarNavegacion()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== DIAGNÓSTICO DE NAVEGACIÓN ===");
                System.Diagnostics.Debug.WriteLine($"Application.Current: {Application.Current?.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"Application.Current.MainPage: {Application.Current?.MainPage?.GetType().Name}");

                if (Application.Current?.MainPage is NavigationPage navPage)
                {
                    System.Diagnostics.Debug.WriteLine($"NavigationPage.CurrentPage: {navPage.CurrentPage?.GetType().Name}");
                    System.Diagnostics.Debug.WriteLine($"NavigationPage.Navigation.NavigationStack.Count: {navPage.Navigation.NavigationStack.Count}");

                    foreach (var page in navPage.Navigation.NavigationStack)
                    {
                        System.Diagnostics.Debug.WriteLine($"  - Stack page: {page.GetType().Name} (HashCode: {page.GetHashCode()})");
                    }
                }

                System.Diagnostics.Debug.WriteLine($"Esta instancia HomePage HashCode: {this.GetHashCode()}");
                System.Diagnostics.Debug.WriteLine("=== FIN DIAGNÓSTICO NAVEGACIÓN ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en diagnóstico de navegación: {ex.Message}");
            }
        }

        private bool _isAppearing = false;

        protected async override void OnAppearing()
        {
            System.Diagnostics.Debug.WriteLine($"HomePage: OnAppearing - HashCode: {this.GetHashCode()}");
            System.Diagnostics.Debug.WriteLine($"HomePage: Application.Current.MainPage tipo: {Application.Current?.MainPage?.GetType().Name}");

            if (_isVisualizacionAlarmaEspecifica)
            {
                System.Diagnostics.Debug.WriteLine("HomePage: OnAppearing SALTADO - Modo visualización específica");
                base.OnAppearing();
                return;
            }

            if (_isAppearing) return;
            _isAppearing = true;

            base.OnAppearing();
            _shouldTimerRun = true;
            _isPageCurrentlyVisible = true; // OPTIMIZACIÓN: Marcar página como visible
            _isAppInForeground = true; // NUEVO: Foreground timer toma control

            // CRÍTICO: Re-suscribir VisibleRegionChanged para clustering zoom-aware
            // Este evento se desuscribe en OnDisappearing y debe re-suscribirse aquí
            if (map != null)
            {
                map.VisibleRegionChanged -= OnMapVisibleRegionChanged; // Evitar duplicados
                map.VisibleRegionChanged += OnMapVisibleRegionChanged;
                System.Diagnostics.Debug.WriteLine("HomePage: VisibleRegionChanged re-suscrito en OnAppearing");
            }

            // CRÍTICO: Re-suscribir MessagingCenter en OnAppearing para asegurar conexión
            System.Diagnostics.Debug.WriteLine("HomePage: Re-suscribiendo MessagingCenter en OnAppearing");

            // Desuscribir primero para evitar duplicados
            MessagingCenter.Unsubscribe<CustomMap, Location>(this, "MapTapped");

            // Re-suscribir MapTapped
            MessagingCenter.Subscribe<CustomMap, Location>(this, "MapTapped", async (sender, location) =>
            {
                var LabelError = TranslateExtension.Translate("LabelError");
                var LabelOK = TranslateExtension.Translate("LabelOK");
                try
                {
                    System.Diagnostics.Debug.WriteLine($"HomePage: *** MENSAJE 'MapTapped' RECIBIDO (OnAppearing) *** en {location.Latitude}, {location.Longitude}");

                    if (location != null)
                    {
                        var latitude = Math.Round(location.Latitude, 6);
                        var longitude = Math.Round(location.Longitude, 6);
                        var popup = new Views.Popups.ConfirmarLanzarAlarma(latitude, longitude);
                        await this.ShowPopupAsync(popup);
                        System.Diagnostics.Debug.WriteLine("HomePage: Popup mostrado exitosamente (OnAppearing)");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"HomePage: Error en MapTapped (OnAppearing): {ex.Message}");
                    CrashlyticsHelper.LogError(ex, "HomePage", "MapTapped-OnAppearing");
                    await ModernAlerts.ShowError(LabelError, ex.Message);
                }
            });

            System.Diagnostics.Debug.WriteLine("HomePage: MessagingCenter re-suscrito en OnAppearing");

            // Re-suscribir InfoWindowClicked usando el método compartido
            System.Diagnostics.Debug.WriteLine("HomePage: Re-suscribiendo InfoWindowClicked en OnAppearing");
            SuscribirInfoWindowClicked();

            // Re-suscribir BackgroundService
            MessagingCenter.Unsubscribe<IBackgroundService, List<AlarmaCercana>>(this, "");
            MessagingCenter.Subscribe<IBackgroundService, List<AlarmaCercana>>(this, "", async (sender, arg) =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"HomePage: BackgroundService mensaje (OnAppearing) - {arg?.Count ?? 0} alarmas");

                    if (arg != null)
                    {
                        // CRÍTICO: Esperar a que termine de pintar alarmas ANTES de actualizar ubicación
                        await PintarAlarmasEnMapa(arg);
                        // FIX Iter2: centrarMapa:true porque BackgroundService envía datos cuando el usuario se movió
                        ActualizarUbicacionEnMapa(centrarMapa: true);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"HomePage: Error en BackgroundService (OnAppearing): {ex.Message}");
                    CrashlyticsHelper.LogError(ex, "HomePage", "BackgroundService-OnAppearing");
                }
            });

            System.Diagnostics.Debug.WriteLine("HomePage: BackgroundService re-suscrito en OnAppearing");

            // Re-suscribir mensajes del ViewModel
            MessagingCenter.Unsubscribe<LanzarAlarmaViewModel, string>(this, "AlarmaLanzadaExitosamente");
            MessagingCenter.Subscribe<LanzarAlarmaViewModel, string>(this, "AlarmaLanzadaExitosamente", async (sender, mensaje) =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"HomePage: *** MENSAJE AlarmaLanzadaExitosamente de VIEWMODEL RECIBIDO *** - {mensaje}");

                    if (this == null || BindingContext == null)
                    {
                        System.Diagnostics.Debug.WriteLine("HomePage: Instancia o BindingContext es null (ViewModel), saltando refresco");
                        return;
                    }

                    // OPTIMIZADO: Re-pintar desde cache local SIN llamar al API
                    // La alarma nueva ya fue insertada en App.AlarmasCacheadas por LanzarAlarmaViewModel
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        try
                        {
                            if (this != null && BindingContext != null)
                            {
                                System.Diagnostics.Debug.WriteLine("HomePage: AlarmaLanzadaExitosamente (ViewModel) - usando cache local (sin API)");
                                await AplicarFiltroSinRecargarAPI();
                                System.Diagnostics.Debug.WriteLine("HomePage: Mapa refrescado desde cache (ViewModel)");
                            }
                        }
                        catch (Exception innerEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"HomePage: Error en MainThread ViewModel: {innerEx.Message}");
                            CrashlyticsHelper.LogError(innerEx, "HomePage", "ViewModel-MainThread");
                        }
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"HomePage: Error en suscripción ViewModel: {ex.Message}");
                    CrashlyticsHelper.LogError(ex, "HomePage", "ViewModel-Suscripcion");
                }
            });

            System.Diagnostics.Debug.WriteLine("HomePage: Suscripciones ViewModel re-suscritas en OnAppearing");

            if (App.persona == null)
            {
                System.Diagnostics.Debug.WriteLine("HomePage: App.persona es null, intentando recuperar de Preferences");
                var userJson = Preferences.Get("User", "");
                if (!string.IsNullOrEmpty(userJson))
                {
                    try
                    {
                        App.persona = JsonConvert.DeserializeObject<Persona>(userJson);
                        System.Diagnostics.Debug.WriteLine("HomePage: App.persona recuperado exitosamente");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"HomePage: Error recuperando App.persona: {ex.Message}");
                        CrashlyticsHelper.LogError(ex, "HomePage", "OnAppearing-RecuperarPersona");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("HomePage: No hay datos de usuario en Preferences");
                }
            }

            var LabelError = await TranslateExtension.TranslateAsync("LabelError");
            var LabelOK = await TranslateExtension.TranslateAsync("LabelOK");
            var MensajeError = await TranslateExtension.TranslateAsync("MensajeError");
            var LblUsuarioBloqueado = await TranslateExtension.TranslateAsync("LblUsuarioBloqueado");

            HomeViewModel vm = null;
            if (BindingContext is HomeViewModel viewModel)
            {
                vm = viewModel;
                var isInitialized = await HomeViewModel.InicializarParametrosUsuarioAsync();

                if (!isInitialized)
                {
                    await ModernAlerts.ShowError(LabelError, MensajeError);
                    _isAppearing = false;
                    return;
                }
            }

            ParametrosUsuario parametros = null;
            try
            {
                var parametrosString = Preferences.Get("ParametrosUsuario", "");
                System.Diagnostics.Debug.WriteLine($"HomePage: ParametrosUsuario después de inicializar: {parametrosString}");

                parametros = JsonConvert.DeserializeObject<ParametrosUsuario>(parametrosString);

                System.Diagnostics.Debug.WriteLine($"HomePage: FlagUsuarioDebeFirmarCto = {parametros.FlagUsuarioDebeFirmarCto}");
                System.Diagnostics.Debug.WriteLine($"HomePage: FlagBloqueoUsuario = {parametros.FlagBloqueoUsuario}");

                if (parametros.FlagUsuarioDebeFirmarCto)
                {
                    System.Diagnostics.Debug.WriteLine("HomePage: Navegando a TermsAndConditionsPage");
                    Application.Current.MainPage = new NavigationPage(new TermsAndConditionsPage()) { BarBackgroundColor = Colors.Black };
                    return;
                }

                if (parametros.FlagBloqueoUsuario)
                {
                    System.Diagnostics.Debug.WriteLine("HomePage: Usuario bloqueado, navegando a SuspendedAccountPage");
                    await ModernAlerts.ShowError(LabelError, LblUsuarioBloqueado);
                    Application.Current.MainPage = new NavigationPage(new SuspendedAccountPage()) { BarBackgroundColor = Colors.Black };
                    _isAppearing = false;
                    return;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HomePage: Error verificando parámetros: {ex.Message}");
                Application.Current.MainPage = new NavigationPage(new InternetRequiredForApp()) { BarBackgroundColor = Colors.Black };
                Debug.WriteLine($"Error al verificar los términos y condiciones: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "HomePage", "OnAppearing-Parametros");
                _isAppearing = false;
                return;
            }

            var hasSeenTutorial = Preferences.Get("HasSeenTutorial", false);
            if (!hasSeenTutorial)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("HomePage: Mostrando tutorial - ESPERANDO a que mapa esté listo");
                    await Task.Delay(2000);
                    await Navigation.PushAsync(new TutorialOverlayPage());
                    System.Diagnostics.Debug.WriteLine("HomePage: Tutorial cerrado, refrescando alarmas");
                    await Task.Delay(2000);

                    if (App.ubicacionActual == null)
                    {
                        System.Diagnostics.Debug.WriteLine("HomePage: App.ubicacionActual es null, obteniendo ubicación...");
                        await CheckAndUpdateLocation();
                        await Task.Delay(1000);
                    }

                    await ObtenerPines();
                    ActualizarUbicacionEnMapa();
                    System.Diagnostics.Debug.WriteLine("HomePage: Alarmas cargadas y mapa actualizado después del tutorial");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"HomePage: Error mostrando tutorial: {ex.Message}");
                    CrashlyticsHelper.LogError(ex, "HomePage", "OnAppearing-Tutorial");
                    Preferences.Set("HasSeenTutorial", true);
                }
            }

            if (vm != null && !vm.ShowUIButtons)
            {
                _isAppearing = false;
                return;
            }

            // ÚNICO LUGAR donde se cargan alarmas y se ajusta zoom (solo si ya vio tutorial)
            if (hasSeenTutorial)
            {
                await Task.Delay(800);

                // CRÍTICO: Restaurar _isPageCurrentlyVisible aquí porque MAUI puede hacer
                // OnAppearing→OnDisappearing→OnAppearing rápido al cerrar popups/tabs.
                // El guard _isAppearing bloquea el segundo OnAppearing, dejando el flag en false.
                // Después del Task.Delay(800), la página ya está estable y visible.
                _isPageCurrentlyVisible = true;

                RestaurarOrientacionNorte();

                // Si hay una alarma recién lanzada pendiente de pintar, repintar desde cache
                // ANTES de EjecutarRefrescoMapa para que el usuario la vea inmediatamente.
                if (_pendienteRepintarDespuesDeAlarma)
                {
                    System.Diagnostics.Debug.WriteLine($"[DIAG-REFRESH] OnAppearing: Consumiendo _pendienteRepintarDespuesDeAlarma");
                    _pendienteRepintarDespuesDeAlarma = false;

                    // CRÍTICO: Activar flag para que ObtenerPines NO lance BGAPI.
                    // La alarma recién lanzada ya está en cache, no necesitamos sobrescribirla.
                    _skipNextBackgroundRefresh = true;
                    System.Diagnostics.Debug.WriteLine($"[DIAG-REFRESH] OnAppearing: _skipNextBackgroundRefresh activado");

                    // CRÍTICO: Forzar _isPageCurrentlyVisible = true aquí.
                    // MAUI puede hacer OnAppearing→OnDisappearing→OnAppearing rápidamente
                    // al cerrar popups, y el guard _isAppearing bloquea el segundo OnAppearing,
                    // dejando _isPageCurrentlyVisible en false. Como estamos DENTRO de OnAppearing,
                    // la página SÍ está visible y debemos restaurar el flag.
                    _isPageCurrentlyVisible = true;
                    System.Diagnostics.Debug.WriteLine($"[DIAG-REFRESH] OnAppearing: _isPageCurrentlyVisible forzado a true");

                    try
                    {
                        await AplicarFiltroSinRecargarAPI();
                        System.Diagnostics.Debug.WriteLine($"[DIAG-REFRESH] OnAppearing: Alarma reciente pintada desde cache");
                    }
                    catch (Exception exPendiente)
                    {
                        System.Diagnostics.Debug.WriteLine($"[DIAG-REFRESH] OnAppearing: Error pintando alarma pendiente: {exPendiente.Message}");
                    }
                }

                // 21022026: Bifurcación según origen del arranque.
                // - Primer arranque (app venía CERRADA): ejecutar RefrescarAmbosFeeds en background
                //   (Feed A completo primero, luego Feed B), pintando desde caché de inmediato.
                // - Resume desde background o navegación interna: solo Feed A (igual que antes).
                if (App.EsPrimerArranque)
                {
                    App.EsPrimerArranque = false; // Consumir el flag (solo se activa una vez en OnStart)
                    Console.WriteLine("[OnAppearing] Primer arranque desde cerrada: cache-first para los tres feeds...");

                    // 2026-03-01: Cache-first para los TRES feeds (Mapa + Siguiendo + Para Ti)

                    // Cache C — Mapa: cargar desde disco y validar viewport antes de pintar
                    var cacheMapa = await App.CargarMapaDesdeCache();
                    if (cacheMapa != null && _currentMapSpan != null)
                    {
                        // Calcular bounding box del viewport actual
                        double lat    = _currentMapSpan.Center.Latitude;
                        double lon    = _currentMapSpan.Center.Longitude;
                        double dLat   = _currentMapSpan.LatitudeDegrees / 2.0;
                        double dLon   = _currentMapSpan.LongitudeDegrees / 2.0;
                        decimal minLat = (decimal)(lat - dLat);
                        decimal maxLat = (decimal)(lat + dLat);
                        decimal minLon = (decimal)(lon - dLon);
                        decimal maxLon = (decimal)(lon + dLon);

                        if (cacheMapa.IntersectaViewport(minLat, maxLat, minLon, maxLon))
                        {
                            Console.WriteLine($"[OnAppearing] Cache C intersecta viewport actual — pintando {cacheMapa.Pines.Count} pines desde disco");
                            // Pintar pines del caché (los pines ligeros se renderizan via el mecanismo existente de PintarAlarmasEnMapa)
                            // Por ahora el caché C es utilizado por RefrescarTresFeeds; el pintado visual sigue via ObtenerPines
                        }
                        else
                        {
                            Console.WriteLine("[OnAppearing] Cache C es de otra ciudad — NO pintar, esperar API");
                        }
                    }

                    // Cache B — Para Ti: cargar desde disco para que DescribirPage lo vea de inmediato
                    await App.CargarFeedParaTiDesdeCache();
                    Console.WriteLine($"[OnAppearing] Cache B cargado desde disco: {App.AlarmasCacheadasParaTi?.Count ?? 0} alarmas");

                    // Cache A — Siguiendo/Mapa: pintar desde caché existente de inmediato (UX cache-first)
                    // 15-04-2026: soloDesdeCache=true evita que EjecutarRefrescoMapa caiga a ObtenerPines
                    // (que dispara InsertaUbicacion), ya que RefrescarTresFeeds lo hará en background.
                    await EjecutarRefrescoMapa(soloDesdeCache: true);

                    // En background: los TRES feeds (Mapa → Siguiendo → Para Ti), cada uno independiente
                    _ = Task.Run(async () =>
                    {
                        // Capturar viewport en el hilo correcto antes de ir al background
                        decimal bgMinLat = 0, bgMaxLat = 0, bgMinLon = 0, bgMaxLon = 0;
                        int bgZoom = _currentZoomLevel;
                        if (_currentMapSpan != null)
                        {
                            double lat    = _currentMapSpan.Center.Latitude;
                            double lon    = _currentMapSpan.Center.Longitude;
                            double dLat   = _currentMapSpan.LatitudeDegrees / 2.0;
                            double dLon   = _currentMapSpan.LongitudeDegrees / 2.0;
                            bgMinLat = (decimal)(lat - dLat);
                            bgMaxLat = (decimal)(lat + dLat);
                            bgMinLon = (decimal)(lon - dLon);
                            bgMaxLon = (decimal)(lon + dLon);
                        }

                        // Ajustar zoom efectivo según el threshold dinámico del usuario
                        // (igual que en OnMapVisibleRegionChanged).
                        try
                        {
                            var parametrosStr = Preferences.Get("ParametrosUsuario", "");
                            if (!string.IsNullOrEmpty(parametrosStr))
                            {
                                var parametrosZoom = JsonConvert.DeserializeObject<ParametrosUsuario>(parametrosStr);
                                var radioUsuarioZoom = parametrosZoom?.radio_alarmas_mts_actual ?? 100;
                                if (!GridClusteringHelper.DebeActivarClustering(bgZoom, 1, radioUsuarioZoom))
                                    bgZoom = 15;
                            }
                        }
                        catch { /* si falla la lectura de prefs, usar zoom real */ }

                        var (mapaOk, feedAOk, feedBOk) = await App.RefrescarTresFeeds(bgMinLat, bgMaxLat, bgMinLon, bgMaxLon, bgZoom, forzarArranque: true);
                        Console.WriteLine($"[OnAppearing] RefrescarTresFeeds completado: Mapa={mapaOk}, A={feedAOk}, B={feedBOk}");

                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            // Repintar mapa si hay datos frescos del mapa O del feed A
                            // mapaOk → Cache C tiene pines del viewport real (nuevo endpoint)
                            // feedAOk → Cache A tiene alarmas del radio del usuario (sistema anterior)
                            if (mapaOk || feedAOk)
                                await EjecutarRefrescoMapa();

                            // Notificar siempre — aunque algún feed falle, los disponibles ya están en memoria
                            MessagingCenter.Send<object, string>(this, "AlarmasCacheActualizadas", "PrimerArranque");
                        });
                    });
                }
                else
                {
                    // Resume desde background o navegación interna: solo Feed A (comportamiento original)
                    await EjecutarRefrescoMapa();
                }
            }

            _shouldTimerRun = true;
            System.Diagnostics.Debug.WriteLine("Tracking: Timer de seguimiento ACTIVADO en OnAppearing");


            // Marcar que la configuración inicial ha terminado
            await Task.Delay(500);
            _isInitialMapSetup = false;
            _isAppearing = false;
        }

        private bool _isLocationUpdateInProgress = false;
        private async Task CheckAndUpdateLocation()
        {
            if (_isLocationUpdateInProgress)
            {
                System.Diagnostics.Debug.WriteLine("CheckAndUpdateLocation: Ya en progreso, saltando");
                return;
            }
            _isLocationUpdateInProgress = true;

            try
            {
                System.Diagnostics.Debug.WriteLine("CheckAndUpdateLocation: INICIADO");

                if (this == null)
                {
                    System.Diagnostics.Debug.WriteLine("CheckAndUpdateLocation: Esta instancia es null, abortando");
                    return;
                }

                await Task.Run(async () =>
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine("Task.Run: INICIADO");

                        System.Diagnostics.Debug.WriteLine("Llamando GetCurrentLocation...");
                        var currentLocation = await GetCurrentLocation();
                        System.Diagnostics.Debug.WriteLine($"GetCurrentLocation completado: {currentLocation?.Latitude}, {currentLocation?.Longitude}");

                        if (_lastLocation == null)
                        {
                            System.Diagnostics.Debug.WriteLine("_lastLocation es null, llamando UpdateLocationAndFetchData...");
                            UpdateLocationAndFetchData(currentLocation);
                            System.Diagnostics.Debug.WriteLine("UpdateLocationAndFetchData completado");
                            return;
                        }

                        // CORRECCIÓN: Ser más conservador con las actualizaciones
                        // Solo actualizar si hay cambio SIGNIFICATIVO (100m) o han pasado 15 minutos
                        if (currentLocation != null &&
                            !_isInitialMapSetup &&
                            (Location.CalculateDistance(currentLocation, _lastLocation, DistanceUnits.Kilometers) > 0.1 || // 100 metros
                            (DateTime.Now - _lastLocationFetchTime) > TimeSpan.FromMinutes(15))) // 15 minutos
                        {
                            System.Diagnostics.Debug.WriteLine($"Ubicación cambió significativamente: {Location.CalculateDistance(currentLocation, _lastLocation, DistanceUnits.Kilometers) * 1000:F1}m");
                            UpdateLocationAndFetchData(currentLocation);
                            System.Diagnostics.Debug.WriteLine("UpdateLocationAndFetchData completado");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("Cambio de ubicación menor a 100m, no actualizando");
                        }

                        System.Diagnostics.Debug.WriteLine("Task.Run: COMPLETADO");
                    }
                    catch (Exception taskEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"ERROR EN Task.Run: {taskEx.Message}");
                        CrashlyticsHelper.LogError(taskEx, "HomePage", "CheckAndUpdateLocation-TaskRun");
                    }
                });

                System.Diagnostics.Debug.WriteLine("CheckAndUpdateLocation: COMPLETADO");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR EN CheckAndUpdateLocation: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "HomePage", "CheckAndUpdateLocation");
            }
            finally
            {
                _isLocationUpdateInProgress = false;
            }
        }

        private async void UpdateLocationAndFetchData(Location currentLocation)
        {
            if (App.persona is null)
            {
                App.persona = JsonConvert.DeserializeObject<Persona>(Preferences.Get("User", ""));
            }

            // FIX: Actualizar App.ubicacionActual ANTES de ObtenerPines
            // para que ActualizarUbicacionEnMapa use coordenadas frescas
            if (currentLocation != null)
            {
                if (App.ubicacionActual == null)
                {
                    App.ubicacionActual = new Ubicaciones();
                }
                App.ubicacionActual.latitud = currentLocation.Latitude;
                App.ubicacionActual.longitud = currentLocation.Longitude;

                if (App.persona != null)
                {
                    App.ubicacionActual.p_user_id_thirdparty = App.persona.user_id_thirdparty;
                    App.ubicacionActual.Pais = App.persona.Pais;
                }
            }

            _lastLocation = currentLocation;
            _lastLocationFetchTime = DateTime.Now;

            await ObtenerPines();
            await HomeViewModel.InicializarParametrosUsuarioAsync();
        }

        // REEMPLAZAR TODO EL MÉTODO ActualizarUbicacionEnMapa (línea 1091)
        private async void ActualizarUbicacionEnMapa(bool centrarMapa = false)
        {
            var LabelUbicacionNoDisponible = TranslateExtension.Translate("LabelUbicacionNoDisponible");
            var LabelVerificaGPS = TranslateExtension.Translate("LabelVerificaGPS");
            var LabelReintentar = TranslateExtension.Translate("LabelReintentar");
            var LabelReiniciarApp = TranslateExtension.Translate("LabelReiniciarApp");
            try
            {
                System.Diagnostics.Debug.WriteLine("HomePage: ActualizarUbicacionEnMapa iniciado");

                // Verificar que no hay otro movimiento en progreso
                lock (_mapUpdateLock)
                {
                    if (_isMapMovementInProgress)
                    {
                        System.Diagnostics.Debug.WriteLine("HomePage: Movimiento en progreso, saltando");
                        return;
                    }
                    _isMapMovementInProgress = true;
                }

                ParametrosUsuario parametros = JsonConvert.DeserializeObject<ParametrosUsuario>(
                    Preferences.Get("ParametrosUsuario", ""));

                // Verificar que tenemos ubicación
                if (App.ubicacionActual == null)
                {
                    System.Diagnostics.Debug.WriteLine("HomePage: App.ubicacionActual es null, obteniendo ubicación");
                    var currentLocation = await GetCurrentLocation();
                    if (currentLocation != null)
                    {
                        App.ubicacionActual = new Ubicaciones();
                        App.ubicacionActual.latitud = currentLocation.Latitude;
                        App.ubicacionActual.longitud = currentLocation.Longitude;

                        if (App.persona != null)
                        {
                            App.ubicacionActual.p_user_id_thirdparty = App.persona.user_id_thirdparty;
                            App.ubicacionActual.Pais = App.persona.Pais;
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("HomePage: No se pudo obtener ubicación");
                        _isMapMovementInProgress = false;
                        return;
                    }
                }

                if (App.ubicacionActual?.latitud == null || App.ubicacionActual?.longitud == null)
                {
                    System.Diagnostics.Debug.WriteLine("HomePage: Ubicación aún no disponible");
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        bool result = await ModernAlerts.ShowConfirmation(
                            LabelUbicacionNoDisponible,
                            LabelVerificaGPS,
                            LabelReintentar,
                            LabelReiniciarApp,
                            false);

                        if (result)
                        {
                            _isMapMovementInProgress = false;
                            ActualizarUbicacionEnMapa();
                        }
                        else
                        {
                            Application.Current.MainPage = new MainPage();
                        }
                    });
                    return;
                }

                // Dibujar el círculo y centrar el mapa si se solicita
                await Task.Run(() =>
                {
                    // FIX: Usar _lastLocation como fuente primaria si está disponible (es GPS fresco)
                    // App.ubicacionActual puede estar desactualizado por race condition
                    Location center;
                    if (_lastLocation != null)
                    {
                        center = new Location(_lastLocation.Latitude, _lastLocation.Longitude);
                        // Sincronizar App.ubicacionActual con GPS fresco
                        App.ubicacionActual.latitud = _lastLocation.Latitude;
                        App.ubicacionActual.longitud = _lastLocation.Longitude;
                        System.Diagnostics.Debug.WriteLine($"HomePage: ActualizarUbicacionEnMapa usando _lastLocation: {_lastLocation.Latitude:F6}, {_lastLocation.Longitude:F6}");

                        // TEMPORAL: Log diagnostico para depurar GPS en campo
                        CrashlyticsHelper.LogDiagnostico("ActualizarUbicacionEnMapa",
                            $"Fuente: _lastLocation ({_lastLocation.Latitude:F6},{_lastLocation.Longitude:F6})",
                            new Dictionary<string, string>
                            {
                                { "Fuente", "_lastLocation" },
                                { "CenterLat", center.Latitude.ToString("F6") },
                                { "CenterLng", center.Longitude.ToString("F6") },
                                { "CentrarMapa", centrarMapa.ToString() }
                            });
                    }
                    else
                    {
                        center = new Location(App.ubicacionActual.latitud, App.ubicacionActual.longitud);
                        System.Diagnostics.Debug.WriteLine($"HomePage: ActualizarUbicacionEnMapa usando App.ubicacionActual: {App.ubicacionActual.latitud:F6}, {App.ubicacionActual.longitud:F6}");

                        // TEMPORAL: Log diagnostico para depurar GPS en campo
                        CrashlyticsHelper.LogDiagnostico("ActualizarUbicacionEnMapa",
                            $"Fuente: App.ubicacionActual ({App.ubicacionActual.latitud:F6},{App.ubicacionActual.longitud:F6})",
                            new Dictionary<string, string>
                            {
                                { "Fuente", "App.ubicacionActual" },
                                { "CenterLat", center.Latitude.ToString("F6") },
                                { "CenterLng", center.Longitude.ToString("F6") },
                                { "CentrarMapa", centrarMapa.ToString() },
                                { "AVISO", "_lastLocation era null - posible stale data" }
                            });
                    }

                    // GUARD: El mapa nativo crashea con coordenadas 0,0 (GPS sin fix al arranque).
                    // La guarda anterior solo verifica null — 0.0 pasa el check pero crasha el mapa nativo sin VS adjunto.
                    if (center.Latitude == 0 && center.Longitude == 0)
                    {
                        Console.WriteLine("[ActualizarUbicacionEnMapa] Coordenadas 0,0 — GPS aún sin fix. Abortando dibujo del mapa.");
                        _isMapMovementInProgress = false;
                        return;
                    }

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        DrawUserCircle(center);

                        // Centrar el mapa si se solicita o si es la primera vez
                        if (centrarMapa || _isInitialMapSetup)
                        {
                            var valorRadio = parametros?.radio_alarmas_mts_actual ?? 100;
                            var mapSpan = MapSpan.FromCenterAndRadius(center, new Distance(valorRadio));
                            map.MoveToRegion(mapSpan);
                            System.Diagnostics.Debug.WriteLine($"HomePage: Mapa centrado (centrarMapa={centrarMapa}, isInitial={_isInitialMapSetup})");

                            // Después de la primera vez, marcar como false
                            if (_isInitialMapSetup)
                            {
                                _isInitialMapSetup = false;
                                System.Diagnostics.Debug.WriteLine("HomePage: _isInitialMapSetup establecido en false");
                            }
                        }

                        _isMapMovementInProgress = false;
                    });
                });

                System.Diagnostics.Debug.WriteLine("HomePage: ActualizarUbicacionEnMapa completado");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HomePage: Error en ActualizarUbicacionEnMapa: {ex.Message}");
                _isMapMovementInProgress = false;
                CrashlyticsHelper.LogError(ex, "HomePage", "ActualizarUbicacionEnMapa");
            }
        }
        private async void DrawUserCircle(Location center)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"HomePage: DrawUserCircle iniciado para {center.Latitude}, {center.Longitude}");

                // TEMPORAL: Log diagnostico para depurar GPS en campo
                CrashlyticsHelper.LogDiagnostico("DrawUserCircle",
                    $"Pintando pin en: {center.Latitude:F6},{center.Longitude:F6}",
                    new Dictionary<string, string>
                    {
                        { "PinLat", center.Latitude.ToString("F6") },
                        { "PinLng", center.Longitude.ToString("F6") },
                        { "UbicacionActual", App.ubicacionActual != null ? $"{App.ubicacionActual.latitud:F6},{App.ubicacionActual.longitud:F6}" : "null" },
                        { "LastLocation", _lastLocation != null ? $"{_lastLocation.Latitude:F6},{_lastLocation.Longitude:F6}" : "null" }
                    });

                ParametrosUsuario parametros = JsonConvert.DeserializeObject<ParametrosUsuario>(Preferences.Get("ParametrosUsuario", ""));

                var LabelUsuario = await TranslateExtension.TranslateAsync("LabelUsuario");
                var LabelTuUbicacion = await TranslateExtension.TranslateAsync("LabelTuUbicacion");

                var valorRadio = parametros != null && parametros.radio_alarmas_mts_actual != 0
                     ? parametros.radio_alarmas_mts_actual
                     : 100;

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        // PASO 1: Limpiar pins de usuario existentes
                        if (map.CustomPins != null)
                        {
                            var userPinsToRemove = map.CustomPins.Where(p => p.Id == "User" || p.MarkerId == "User").ToList();
                            foreach (var userPin in userPinsToRemove)
                            {
                                map.CustomPins.Remove(userPin);
                            }
                            System.Diagnostics.Debug.WriteLine($"HomePage: {userPinsToRemove.Count} pins de usuario removidos de CustomPins");
                        }

                        if (map.Pins != null)
                        {
                            var userPinsToRemove = map.Pins.Where(p => p.MarkerId == "User").ToList();
                            foreach (var userPin in userPinsToRemove)
                            {
                                map.Pins.Remove(userPin);
                            }
                            System.Diagnostics.Debug.WriteLine($"HomePage: {userPinsToRemove.Count} pins de usuario removidos de map.Pins");
                        }

                        // PASO 2: MANEJO INTELIGENTE DEL CÍRCULO - EVITAR DUPLICACIONES
                        bool needsNewCircle = true;

                        // CRÍTICO: Limpiar TODOS los círculos existentes primero
                        var existingCircles = map.MapElements.OfType<Circle>().ToList();
                        if (existingCircles.Count > 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"HomePage: Encontrados {existingCircles.Count} círculos existentes, limpiando...");
                            foreach (var circle in existingCircles)
                            {
                                map.MapElements.Remove(circle);
                            }
                            System.Diagnostics.Debug.WriteLine("HomePage: Todos los círculos existentes removidos");
                        }

                        // Verificar si currentCircle es válido para reutilizar
                        if (currentCircle != null)
                        {
                            try
                            {
                                var distance = Location.CalculateDistance(currentCircle.Center, center, DistanceUnits.Kilometers) * 1000;
                                var currentRadiusMeters = currentCircle.Radius.Kilometers * 1000;
                                var radiusDifference = Math.Abs(currentRadiusMeters - valorRadio);

                                System.Diagnostics.Debug.WriteLine($"HomePage: Verificando círculo actual - Distancia: {distance:F1}m, Diferencia radio: {radiusDifference:F1}m");

                                // TOLERANCIA: 100 metros de distancia y 20m de radio
                                if (distance < 100 && radiusDifference < 20)
                                {
                                    needsNewCircle = false;
                                    if (distance > 5)
                                    {
                                        currentCircle.Center = center;
                                        System.Diagnostics.Debug.WriteLine($"HomePage: *** CÍRCULO REUTILIZADO - Centro actualizado *** - Distancia: {distance:F1}m");
                                    }
                                    else
                                    {
                                        System.Diagnostics.Debug.WriteLine($"HomePage: *** CÍRCULO REUTILIZADO - Sin cambios *** - Distancia: {distance:F1}m");
                                    }
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($"HomePage: Círculo necesita recrearse - Distancia: {distance:F1}m, Diferencia radio: {radiusDifference:F1}m");
                                    currentCircle = null;
                                }
                            }
                            catch (Exception circleCheckEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"HomePage: Error verificando círculo: {circleCheckEx.Message}");
                                CrashlyticsHelper.LogError(circleCheckEx, "HomePage", "DrawUserCircle-VerificarCirculo");
                                currentCircle = null;
                                needsNewCircle = true;
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("HomePage: No hay círculo actual válido");
                        }

                        // PASO 3: Crear círculo solo si es necesario
                        if (needsNewCircle || currentCircle == null)
                        {
                            currentCircle = new Circle()
                            {
                                FillColor = Color.FromRgba(0, 123, 255, 0.25),
                                Radius = new Distance(valorRadio),
                                Center = center,
                                StrokeWidth = 4,
                                StrokeColor = Color.FromRgb(0, 100, 200)
                            };

                            System.Diagnostics.Debug.WriteLine($"HomePage: *** NUEVO CÍRCULO CREADO *** - Radio: {valorRadio}m");
                        }

                        // PASO 4: SIEMPRE agregar el círculo al mapa
                        map.MapElements.Add(currentCircle);
                        System.Diagnostics.Debug.WriteLine("HomePage: *** CÍRCULO AGREGADO AL MAPA ***");

                        // PASO 5: Crear pin del usuario
                        currentUser = new CustomPin()
                        {
                            MarkerId = "User",
                            Id = "User",
                            Label = LabelUsuario ?? "Usuario",
                            Type = PinType.Generic,
                            Address = LabelTuUbicacion ?? "Tu ubicación",
                            Location = center,
                            TipoAlarma = 0,
                            FlagPropietarioAlarma = false,
                            AlarmaCercana = null
                        };

                        // PASO 6: Agregar pin
                        if (map.CustomPins == null)
                        {
                            map.CustomPins = new List<CustomPin>();
                        }

                        map.CustomPins.Add(currentUser);
                        //map.Pins.Add(currentUser);

                        System.Diagnostics.Debug.WriteLine($"HomePage: Pin de usuario agregado. Total CustomPins: {map.CustomPins.Count}");
                        System.Diagnostics.Debug.WriteLine($"HomePage: Pin de usuario agregado. Total Pins: {map.Pins.Count}");

                        // PASO 7: Forzar actualización del handler
                        if (map is CustomMap customMap)
                        {
                            var customPinsCopy = new List<CustomPin>(map.CustomPins);
                            map.CustomPins = customPinsCopy;
                            System.Diagnostics.Debug.WriteLine("HomePage: Propiedad CustomPins actualizada para trigger handler");
                        }

                        // PASO 8: Solo mover cámara en configuración inicial
                        if (_isInitialMapSetup)
                        {
                            var mapSpan = MapSpan.FromCenterAndRadius(center, new Distance(valorRadio * 1.5));
                            map.MoveToRegion(mapSpan);
                            System.Diagnostics.Debug.WriteLine("HomePage: Mapa centrado en ubicación del usuario (configuración inicial)");
                        }

                        // PASO 9: CRÍTICO - Timer para asegurar visibilidad del círculo
                        Application.Current.Dispatcher.StartTimer(TimeSpan.FromMilliseconds(800), () =>
                        {
                            try
                            {
                                System.Diagnostics.Debug.WriteLine("HomePage: Timer de visibilidad - Forzando círculo encima");
                                EnsureCircleVisibility();
                            }
                            catch (Exception timerEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"HomePage: Error en timer de visibilidad: {timerEx.Message}");
                                CrashlyticsHelper.LogError(timerEx, "HomePage", "TimerVisibilidad");
                            }
                            return false; // Solo ejecutar una vez
                        });

                        // DIAGNÓSTICO FINAL
                        var circlesInMap = map.MapElements.OfType<Circle>().Count();
                        var totalMapElements = map.MapElements.Count;
                        System.Diagnostics.Debug.WriteLine($"HomePage: FINAL - Círculos en mapa: {circlesInMap}, Total MapElements: {totalMapElements}, Pins totales: {map.Pins.Count}");

                        if (circlesInMap != 1)
                        {
                            System.Diagnostics.Debug.WriteLine($"HomePage: *** WARNING: Deberían ser exactamente 1 círculo, pero hay {circlesInMap} ***");
                        }

                        if (circlesInMap > 0)
                        {
                            var circle = map.MapElements.OfType<Circle>().FirstOrDefault();
                            if (circle != null)
                            {
                                System.Diagnostics.Debug.WriteLine($"HomePage: VERIFICACIÓN - Círculo centro: {circle.Center.Latitude:F6}, {circle.Center.Longitude:F6}");
                                System.Diagnostics.Debug.WriteLine($"HomePage: VERIFICACIÓN - Círculo radio: {circle.Radius.Kilometers * 1000:F1}m");
                            }
                        }
                    }
                    catch (Exception mainThreadEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"HomePage: Error en MainThread de DrawUserCircle: {mainThreadEx.Message}");
                        CrashlyticsHelper.LogError(mainThreadEx, "HomePage", "DrawUserCircle-MainThread");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HomePage: Error en DrawUserCircle: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "HomePage", "DrawUserCircle");
            }
        }

        // MÉTODO ADICIONAL: Para forzar visibilidad del círculo cuando sea necesario
        private void ForzarVisibilidadCirculo()
        {
            try
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (currentCircle != null && map.MapElements.Contains(currentCircle))
                    {
                        // TRUCO: Remover y volver a agregar para asegurar que quede visible
                        var circleBackup = currentCircle;
                        map.MapElements.Remove(currentCircle);

                        // Esperar un frame y volver a agregar
                        Application.Current.Dispatcher.StartTimer(TimeSpan.FromMilliseconds(50), () =>
                        {
                            try
                            {
                                map.MapElements.Add(circleBackup);
                                System.Diagnostics.Debug.WriteLine("HomePage: *** CÍRCULO FORZADO A ESTAR VISIBLE ***");
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"HomePage: Error forzando visibilidad: {ex.Message}");
                                CrashlyticsHelper.LogError(ex, "HomePage", "ForzarVisibilidadCirculo-Timer");
                            }
                            return false;
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HomePage: Error en ForzarVisibilidadCirculo: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "HomePage", "ForzarVisibilidadCirculo");
            }
        }

        async Task<Location> GetCurrentLocation()
        {
            var LabelGPSRequerido = TranslateExtension.Translate("LabelGPSRequerido");
            var LabelHabilitarUbicacion = TranslateExtension.Translate("LabelHabilitarUbicacion");
            var LabelIrAConfiguracion = TranslateExtension.Translate("LabelIrAConfiguracion");
            var LabelCancelar = TranslateExtension.Translate("LabelCancelar");
            try
            {
                System.Diagnostics.Debug.WriteLine("GetCurrentLocation: INICIADO");

                var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(5));

                var parametrosString = Preferences.Get("ParametrosUsuario", "");
                System.Diagnostics.Debug.WriteLine($"ParametrosUsuario string: '{parametrosString}'");

                ParametrosUsuario parametros = null;
                if (!string.IsNullOrEmpty(parametrosString))
                {
                    try
                    {
                        parametros = JsonConvert.DeserializeObject<ParametrosUsuario>(parametrosString);
                        System.Diagnostics.Debug.WriteLine("ParametrosUsuario deserializado correctamente");
                    }
                    catch (Exception deserEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error deserializando ParametrosUsuario: {deserEx.Message}");
                        CrashlyticsHelper.LogError(deserEx, "HomePage", "GetCurrentLocation-DeserializarParametros");
                        parametros = null;
                    }
                }

                if (parametros == null)
                {
                    System.Diagnostics.Debug.WriteLine("ParametrosUsuario es null, usando valores por defecto");
                    parametros = new ParametrosUsuario
                    {
                        radio_alarmas_mts_actual = 100
                    };
                }

                System.Diagnostics.Debug.WriteLine("Creando CancellationTokenSource");
                cts = new CancellationTokenSource();

                System.Diagnostics.Debug.WriteLine("Verificando permisos de ubicación");

                try
                {
                    var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                    System.Diagnostics.Debug.WriteLine($"Estado actual de permisos: {status}");

                    if (status != PermissionStatus.Granted)
                    {
                        System.Diagnostics.Debug.WriteLine("Solicitando permisos de ubicación");
                        status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                        System.Diagnostics.Debug.WriteLine($"Resultado de solicitud de permisos: {status}");
                    }

                    if (status != PermissionStatus.Granted)
                    {
                        System.Diagnostics.Debug.WriteLine("Permisos denegados, intentando última ubicación conocida");
                        var lastLocation = await Geolocation.GetLastKnownLocationAsync();

                        if (lastLocation != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"Última ubicación obtenida: {lastLocation.Latitude}, {lastLocation.Longitude}");

                            DrawUserCircle(lastLocation);

                            // CORRECCIÓN: Solo mover mapa si es configuración inicial
                            if (_isInitialMapSetup)
                            {
                                MainThread.BeginInvokeOnMainThread(() =>
                                {
                                    map.MoveToRegion(MapSpan.FromCenterAndRadius(lastLocation, new Distance(parametros.radio_alarmas_mts_actual)));
                                });
                            }

                            // FIX: SIEMPRE actualizar App.ubicacionActual (no solo si != null)
                            if (App.ubicacionActual == null)
                            {
                                App.ubicacionActual = new Ubicaciones();
                            }
                            App.ubicacionActual.latitud = lastLocation.Latitude;
                            App.ubicacionActual.longitud = lastLocation.Longitude;
                            if (App.persona != null)
                            {
                                App.ubicacionActual.p_user_id_thirdparty = App.persona.user_id_thirdparty;
                                App.ubicacionActual.Pais = App.persona.Pais;
                            }

                            return lastLocation;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("No hay última ubicación conocida");
                            return null;
                        }
                    }

                    System.Diagnostics.Debug.WriteLine("Verificando si la ubicación está habilitada");
                    bool isLocationEnabled = true;

                    try
                    {
                        var testLocation = await Geolocation.GetLocationAsync(
                            new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(1)),
                            cts.Token);

                        if (testLocation == null)
                        {
                            isLocationEnabled = false;
                            System.Diagnostics.Debug.WriteLine("GPS parece estar deshabilitado");
                        }
                    }
                    catch (FeatureNotEnabledException)
                    {
                        isLocationEnabled = false;
                        System.Diagnostics.Debug.WriteLine("GPS está deshabilitado");
                    }

                    if (!isLocationEnabled)
                    {
                        System.Diagnostics.Debug.WriteLine("GPS no disponible, obteniendo última ubicación conocida");
                        var location = await Geolocation.GetLastKnownLocationAsync();

                        if (location != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"Última ubicación obtenida: {location.Latitude}, {location.Longitude}");

                            DrawUserCircle(location);

                            // CORRECCIÓN: Solo mover mapa si es configuración inicial
                            if (_isInitialMapSetup)
                            {
                                MainThread.BeginInvokeOnMainThread(() =>
                                {
                                    map.MoveToRegion(MapSpan.FromCenterAndRadius(location, new Distance(parametros.radio_alarmas_mts_actual)));
                                });
                            }

                            // FIX: SIEMPRE actualizar App.ubicacionActual (no solo si != null)
                            if (App.ubicacionActual == null)
                            {
                                App.ubicacionActual = new Ubicaciones();
                            }
                            App.ubicacionActual.latitud = location.Latitude;
                            App.ubicacionActual.longitud = location.Longitude;
                            if (App.persona != null)
                            {
                                App.ubicacionActual.p_user_id_thirdparty = App.persona.user_id_thirdparty;
                                App.ubicacionActual.Pais = App.persona.Pais;
                            }

                            return location;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("No hay última ubicación conocida, sugiriendo habilitar GPS");

                            MainThread.BeginInvokeOnMainThread(async () =>
                            {
                                bool result = await ModernAlerts.ShowConfirmation(
                                            LabelGPSRequerido,
                                            LabelHabilitarUbicacion,
                                            LabelIrAConfiguracion,
                                            LabelCancelar,
                                            false);
                            });

                            return null;
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("GPS disponible, obteniendo ubicación actual");
                        var location = await Geolocation.GetLocationAsync(request, cts.Token);

                        if (location != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"Ubicación actual obtenida: {location.Latitude}, {location.Longitude}");

                            // TEMPORAL: Log diagnostico para depurar GPS en campo
                            CrashlyticsHelper.LogDiagnostico("GetCurrentLocation",
                                $"GPS fresco obtenido: {location.Latitude:F6},{location.Longitude:F6}",
                                new Dictionary<string, string>
                                {
                                    { "Lat", location.Latitude.ToString("F6") },
                                    { "Lng", location.Longitude.ToString("F6") },
                                    { "UbicacionActualAntes", App.ubicacionActual != null ? $"{App.ubicacionActual.latitud:F6},{App.ubicacionActual.longitud:F6}" : "null" },
                                    { "IsInitialSetup", _isInitialMapSetup.ToString() }
                                });

                            var radius = new Distance(parametros.radio_alarmas_mts_actual);
                            DrawUserCircle(location);

                            // CORRECCIÓN: Solo mover mapa si es configuración inicial
                            if (_isInitialMapSetup)
                            {
                                MainThread.BeginInvokeOnMainThread(() =>
                                {
                                    map.MoveToRegion(MapSpan.FromCenterAndRadius(location, radius));
                                });
                            }

                            // FIX: SIEMPRE actualizar App.ubicacionActual (no solo si != null)
                            if (App.ubicacionActual == null)
                            {
                                App.ubicacionActual = new Ubicaciones();
                            }
                            App.ubicacionActual.latitud = location.Latitude;
                            App.ubicacionActual.longitud = location.Longitude;
                            if (App.persona != null)
                            {
                                App.ubicacionActual.p_user_id_thirdparty = App.persona.user_id_thirdparty;
                                App.ubicacionActual.Pais = App.persona.Pais;
                            }

                            return location;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("No se pudo obtener ubicación actual");
                            return null;
                        }
                    }
                }
                catch (FeatureNotSupportedException fnsEx)
                {
                    System.Diagnostics.Debug.WriteLine($"FeatureNotSupportedException: {fnsEx.Message}");
                    return null;
                }
                catch (FeatureNotEnabledException fneEx)
                {
                    System.Diagnostics.Debug.WriteLine($"FeatureNotEnabledException: {fneEx.Message}");
                    return null;
                }
                catch (PermissionException pEx)
                {
                    System.Diagnostics.Debug.WriteLine($"PermissionException: {pEx.Message}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception general en GetCurrentLocation: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");

                CrashlyticsHelper.LogError(ex, "HomePage", "GetCurrentLocation");

                return null;
            }
        }

        private async void AbrirMenu_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new MenuPage());
        }

        private async void AbrirMensajes_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new MensajesPage((HomeViewModel)BindingContext));
        }

        private async void AbrirFiltro_Clicked(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[FiltroAlarmas] Abriendo popup de filtro");

                // Verificar si los tipos están cargados
                if (App.TiposAlarmaDisponibles == null || App.TiposAlarmaDisponibles.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("[FiltroAlarmas] Tipos no cargados, cargando ahora...");

                    try
                    {
                        var cargaExitosa = await App.CargarTiposAlarmaConEstadisticas();

                        if (!cargaExitosa)
                        {
                            var LabelError = TranslateExtension.Translate("LabelError");
                            var LabelErrorCargandoTipos = TranslateExtension.Translate("ErrorCargandoTiposIntenteNuevamente");
                            await ModernAlerts.ShowError(LabelError, LabelErrorCargandoTipos);
                            return;
                        }
                    }
                    catch (Exception exCarga)
                    {
                        System.Diagnostics.Debug.WriteLine($"[FiltroAlarmas] Error al cargar tipos: {exCarga.Message}");
                        CrashlyticsHelper.LogError(exCarga, "HomePage", "AbrirFiltro_Clicked-CargarTipos");

                        var LabelError = TranslateExtension.Translate("LabelError");
                        var LabelErrorCargandoTipos = TranslateExtension.Translate("ErrorCargandoTiposVerifiqueConexion");
                        await ModernAlerts.ShowError(LabelError, LabelErrorCargandoTipos);
                        return;
                    }
                }

                // Mostrar popup
                var popup = new FiltroAlarmasPopup();
                await this.ShowPopupAsync(popup);

                // Si el usuario aplicó cambios, re-filtrar el mapa SIN llamar al API
                if (popup.FiltrosAplicados)
                {
                    System.Diagnostics.Debug.WriteLine("[FiltroAlarmas] Filtros aplicados, re-filtrando caché local (sin API)...");
                    await AplicarFiltroSinRecargarAPI();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[FiltroAlarmas] Filtros cancelados o sin cambios");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FiltroAlarmas] Error abriendo filtro: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "HomePage", "AbrirFiltro_Clicked");

                var LabelError = TranslateExtension.Translate("LabelError");
                await ModernAlerts.ShowError(LabelError, ex.Message);
            }
        }

        bool isBusy = false;

        async void ReportarAlarma_Clicked(System.Object sender, System.EventArgs e)
        {
            if (IsBusy)
            {
                return;
            }

            IsBusy = true;

            var popup = new ConfirmarLanzarAlarmaEnUbicacionActual(App.ubicacionActual.latitud, App.ubicacionActual.longitud, 1);
            await this.ShowPopupAsync(popup);
            IsBusy = false;
        }

        string CuentaRegresivaRefrescar;
        bool IsTimeRunning = false;

        // 21022026: Guard para evitar ejecución concurrente de RefrescarAmbosFeeds desde el botón
        private bool _isRefreshingBothFeeds = false;

        async void RefreshButton_Clicked(System.Object sender, System.EventArgs e)
        {
            // Guard de concurrencia: el botón visual ya se oculta con IsTimeRunning,
            // pero este flag protege contra invocaciones simultáneas en cualquier otro escenario.
            if (_isRefreshingBothFeeds) return;
            _isRefreshingBothFeeds = true;

            // Parte visual (contador)
            IsTimeRunning = true;
            BotonContador.IsVisible = true;
            BotonRefrescar.IsVisible = false;
            CuentaRegresivaRefrescar = "30";
            BotonContador.Text = CuentaRegresivaRefrescar;

            // FIX-BUG2: Arrancar el timer del contador ANTES del await,
            // para que la cuenta regresiva sea visible mientras la API responde.
            Application.Current.Dispatcher.StartTimer(new TimeSpan(0, 0, 1), () =>
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    CuentaRegresivaRefrescar = (int.Parse(CuentaRegresivaRefrescar) - 1).ToString();
                    BotonContador.Text = CuentaRegresivaRefrescar;
                    if (CuentaRegresivaRefrescar == "0")
                    {
                        IsTimeRunning = false;
                        BotonContador.IsVisible = false;
                        BotonRefrescar.IsVisible = true;
                    }
                });
                return IsTimeRunning;
            });

            RestaurarOrientacionNorte();

            try
            {
                // 2026-03-01: Botón Refresh ejecuta los TRES feeds (Mapa → Siguiendo → Para Ti), cada uno independiente
                Console.WriteLine("[RefreshButton] Iniciando RefrescarTresFeeds...");

                // Capturar viewport actual
                decimal rfMinLat = 0, rfMaxLat = 0, rfMinLon = 0, rfMaxLon = 0;
                int rfZoom = _currentZoomLevel;
                if (_currentMapSpan != null)
                {
                    double lat   = _currentMapSpan.Center.Latitude;
                    double lon   = _currentMapSpan.Center.Longitude;
                    double dLat  = _currentMapSpan.LatitudeDegrees / 2.0;
                    double dLon  = _currentMapSpan.LongitudeDegrees / 2.0;
                    rfMinLat = (decimal)(lat - dLat);
                    rfMaxLat = (decimal)(lat + dLat);
                    rfMinLon = (decimal)(lon - dLon);
                    rfMaxLon = (decimal)(lon + dLon);
                }

                // FIX-BUG1: Ajustar el zoom efectivo para la API igual que hace el background task
                // de OnAppearing. Si el zoom visual actual no requiere clustering, forzar zoom=15
                // para que la API devuelva pines individuales en vez de clusters sintéticos.
                // Sin este ajuste, al presionar Refresh con el mapa alejado (zoom < 15) la API
                // devuelve clusters sin alarma_id ni alarma_id_padre, borrando los pines individuales
                // y la polyline padre-hijo del mapa.
                try
                {
                    var parametrosStr = Preferences.Get("ParametrosUsuario", "");
                    if (!string.IsNullOrEmpty(parametrosStr))
                    {
                        var p = JsonConvert.DeserializeObject<ParametrosUsuario>(parametrosStr);
                        var radio = p?.radio_alarmas_mts_actual ?? 100;
                        if (!GridClusteringHelper.DebeActivarClustering(rfZoom, 1, radio))
                            rfZoom = 15;
                    }
                }
                catch { /* si falla la lectura de prefs, usar zoom real */ }

                Console.WriteLine($"[RefreshButton] Zoom efectivo para API: {rfZoom} (zoom visual: {_currentZoomLevel})");

                var (mapaOk, feedAOk, feedBOk) = await App.RefrescarTresFeeds(rfMinLat, rfMaxLat, rfMinLon, rfMaxLon, rfZoom);
                Console.WriteLine($"[RefreshButton] Completado: Mapa={mapaOk}, A={feedAOk}, B={feedBOk}");

                // Repintar el mapa con los datos frescos
                await EjecutarRefrescoMapa();

                // Notificar a DescribirPage que los caches se actualizaron
                MessagingCenter.Send<object, string>(this, "AlarmasCacheActualizadas", "RefreshButton");
            }
            finally
            {
                _isRefreshingBothFeeds = false;
            }
        }

        /// <param name="soloDesdeCache">Si true, NO hace fallback a ObtenerPines cuando no hay Cache C.
        /// Usar true durante primer arranque para evitar InsertaUbicacion duplicado (RefrescarTresFeeds lo hará después).</param>
        private async Task EjecutarRefrescoMapa(bool soloDesdeCache = false)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"HomePage: EjecutarRefrescoMapa iniciado (soloDesdeCache={soloDesdeCache})");

                // ── NUEVO (2026-03-02): Si hay Cache C válido, pintar desde él ──────────────
                // Cache C contiene los pines del nuevo endpoint viewport-driven (/Ubicaciones/PinesMapa).
                // Tiene cobertura geográfica real (todo el viewport, no solo el radio del usuario),
                // por lo que puede mostrar alarmas de otras ciudades cuando el mapa se aleja.
                var cacheMapa = App.CacheMapa;
                if (cacheMapa?.Pines != null && cacheMapa.Pines.Count > 0)
                {
                    int conPadre = cacheMapa.Pines.Count(p => p.alarma_id_padre.HasValue);
                    int conId    = cacheMapa.Pines.Count(p => p.alarma_id > 0);
                    int clusters = cacheMapa.Pines.Count(p => p.cantidad_cluster > 1);
                    System.Diagnostics.Debug.WriteLine($"[EjecutarRefrescoMapa] Usando Cache C: {cacheMapa.Pines.Count} pines (conId={conId} conPadre={conPadre} clusters={clusters}) guardadoEn={cacheMapa.GuardadoEn:HH:mm:ss}");
                    await PintarPinesMapaDesdeCache(cacheMapa.Pines);
                    ActualizarUbicacionEnMapa(centrarMapa: true);
                }
                else if (soloDesdeCache)
                {
                    // 15-04-2026: En primer arranque, NO caer a ObtenerPines (evita InsertaUbicacion duplicado).
                    // RefrescarTresFeeds lo hará en background y llamará EjecutarRefrescoMapa después.
                    System.Diagnostics.Debug.WriteLine("[EjecutarRefrescoMapa] Sin Cache C + soloDesdeCache=true — esperando RefrescarTresFeeds");
                    ActualizarUbicacionEnMapa(centrarMapa: true);
                }
                else
                {
                    // Sin Cache C: caer de vuelta al sistema anterior (caché de InsertaUbicacion)
                    System.Diagnostics.Debug.WriteLine("[EjecutarRefrescoMapa] Sin Cache C — usando ObtenerPines (sistema anterior)");
                    await ObtenerPines(centrarMapa: true);
                }

                Application.Current.Dispatcher.StartTimer(TimeSpan.FromMilliseconds(300), () =>
                {
                    try
                    {
                        var circlesInMap = map.MapElements.OfType<Circle>().Count();
                        System.Diagnostics.Debug.WriteLine($"HomePage: Círculos después de refresh: {circlesInMap}");

                        if (circlesInMap == 0)
                        {
                            System.Diagnostics.Debug.WriteLine("HomePage: *** INFO: No hay círculos después del refresh (normal) ***");
                        }
                        else if (circlesInMap > 1)
                        {
                            System.Diagnostics.Debug.WriteLine($"HomePage: *** WARNING: Múltiples círculos: {circlesInMap} ***");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("HomePage: *** CORRECTO: 1 círculo presente ***");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"HomePage: Error verificando círculos: {ex.Message}");
                        CrashlyticsHelper.LogError(ex, "HomePage", "EjecutarRefrescoMapa-VerificarCirculos");
                    }
                    return false;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HomePage: Error en EjecutarRefrescoMapa: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "HomePage", "EjecutarRefrescoMapa");
            }
        }

        /// <summary>
        /// Pinta en el mapa los pines que vienen del nuevo endpoint /Ubicaciones/PinesMapa (Cache C).
        /// A diferencia de PintarAlarmasEnMapa, estos pines ya vienen filtrados geográficamente
        /// por el viewport y no tienen todos los campos de AlarmaCercana — solo lo mínimo para el mapa.
        /// Creado: 2026-03-02 — Rediseño Viewport-Driven
        /// </summary>
        private async Task PintarPinesMapaDesdeCache(List<Models.PinMapaDto> pines)
        {
            if (_isVisualizacionAlarmaEspecifica)
            {
                System.Diagnostics.Debug.WriteLine("HomePage: PintarPinesMapaDesdeCache IGNORADO - Modo visualización específica");
                return;
            }

            if (_isPintandoAlarmas)
            {
                System.Diagnostics.Debug.WriteLine("HomePage: PintarPinesMapaDesdeCache IGNORADO - Pintado en progreso");
                return;
            }

            // Cache D: fusionar pines persistentes de persecución (tipo-9 + padres de crimen).
            // Esto evita que desaparezcan del mapa cuando el usuario desplaza el viewport.
            // Se hace ANTES de setear el flag para no bloquear llamadas concurrentes durante la fusión.
            var pinesPersistentes = App.PinesPersistentesEscape;
            if (pinesPersistentes.Count > 0)
            {
                var idsEnViewport = new HashSet<long>(pines.Where(p => p.alarma_id > 0).Select(p => p.alarma_id));
                var extras = pinesPersistentes.Where(p => !idsEnViewport.Contains(p.alarma_id)).ToList();
                if (extras.Count > 0)
                {
                    // Crear nueva lista para no mutar la lista del llamador
                    pines = new List<Models.PinMapaDto>(pines);
                    pines.AddRange(extras);
                    System.Diagnostics.Debug.WriteLine($"[PintarPinesMapaDesdeCache] +{extras.Count} pines persistentes inyectados (total={pines.Count})");
                }
            }

            _isPintandoAlarmas = true;
            try
            {
                System.Diagnostics.Debug.WriteLine($"[PintarPinesMapaDesdeCache] Pintando {pines.Count} pines del viewport");

                // Guardar pin del usuario antes de limpiar
                CustomPin userPinToPreserve = null;
                if (map?.CustomPins != null)
                    userPinToPreserve = map.CustomPins.FirstOrDefault(p => p.Id == "User" || p.MarkerId == "User");

                // CRÍTICO: Usar InvokeOnMainThreadAsync (awaitable) para que _isPintandoAlarmas
                // se libere SOLO cuando el MainThread terminó de limpiar y redibujar.
                // Antes se usaba BeginInvokeOnMainThread (fire-and-forget), lo que causaba que
                // el flag se liberara antes del pintado, permitiendo race conditions con polylines
                // de distintos viewports acumulándose en el mapa.
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (map == null) return;

                    System.Diagnostics.Debug.WriteLine($"[PintarPinesMapaDesdeCache] Limpieza UI — polylines antes: {map.MapElements.OfType<Microsoft.Maui.Controls.Maps.Polyline>().Count()}");

                    // Preservar gestos
                    var hasScrollEnabled   = map.HasScrollEnabled;
                    var hasZoomEnabled     = map.HasZoomEnabled;
                    var hasRotationEnabled = map.HasRotationEnabled;

                    // Limpiar pins de alarmas (preservar usuario)
                    var pinsToRemove = map.Pins.Where(p => p.MarkerId != "User").ToList();
                    foreach (var p in pinsToRemove) map.Pins.Remove(p);

                    // Limpiar solo polylines, nunca círculos
                    for (int i = map.MapElements.Count - 1; i >= 0; i--)
                        if (map.MapElements[i] is Microsoft.Maui.Controls.Maps.Polyline)
                            map.MapElements.RemoveAt(i);

                    // Restaurar gestos
                    map.HasScrollEnabled   = hasScrollEnabled;
                    map.HasZoomEnabled     = hasZoomEnabled;
                    map.HasRotationEnabled = hasRotationEnabled;

                    // Construir nueva lista de CustomPins
                    var newPins = new List<CustomPin>();

                    // Re-agregar pin de usuario
                    if (userPinToPreserve != null)
                        newPins.Add(userPinToPreserve);

                    // Agregar pines del viewport
                    foreach (var pin in pines)
                    {
                        if (pin.cantidad_cluster > 1)
                        {
                            // Pin sintético de cluster del viewport (zoom <= 14)
                            // Usar ClusterPin para que CustomMapHandler pinte el badge con la cantidad
                            newPins.Add(new Models.ClusterPin(pin.latitud, pin.longitud, pin.tipoalarma_id, pin.cantidad_cluster));
                            System.Diagnostics.Debug.WriteLine($"[PintarPinesMapaDesdeCache] Cluster viewport tipo={pin.tipoalarma_id} cantidad={pin.cantidad_cluster} en ({pin.latitud},{pin.longitud})");
                        }
                        else
                        {
                            // Pin individual (zoom >= 15) o cluster de 1 sola alarma
                            bool esCerrado = !pin.estado_alarma;

                            // Determinar propietario comparando user_id del creador con el usuario logueado
                            bool esPropietario = !string.IsNullOrEmpty(pin.user_id_creador_alarma)
                                && pin.user_id_creador_alarma == App.persona?.user_id_thirdparty;

                            // Construir AlarmaCercana mínimo para activar badges e InfoWindow completo.
                            // Con AlarmaCercana != null, CustomMapHandler.CreateMarker toma la rama
                            // con badges (interacciones, policía, red de confianza).
                            AlarmaCercana? alarmaCercanaViewport = null;
                            if (pin.alarma_id > 0)
                            {
                                alarmaCercanaViewport = new AlarmaCercana
                                {
                                    alarma_id                   = pin.alarma_id,
                                    tipoalarma_id               = pin.tipoalarma_id,
                                    estado_alarma               = pin.estado_alarma,
                                    descripciontipoalarma       = pin.descripciontipoalarma,
                                    flag_alarma_siendo_atendida = pin.flag_alarma_siendo_atendida,
                                    cantidad_interacciones      = pin.cantidad_interacciones,
                                    flag_red_confianza          = pin.flag_red_confianza,
                                    user_id_creador_alarma      = pin.user_id_creador_alarma,
                                    distancia_en_metros         = pin.distancia_en_metros,
                                    flag_propietario_alarma     = esPropietario,
                                    latitud_alarma              = pin.latitud,
                                    longitud_alarma             = pin.longitud,
                                    alarma_id_padre             = pin.alarma_id_padre,
                                };
                            }

                            // Construir Address del InfoWindow: "Tipo de alarma. 150 m"
                            string LabelMetros = TranslateExtension.Translate("LabelMetros") ?? "m";
                            string address;
                            if (alarmaCercanaViewport != null && !string.IsNullOrEmpty(pin.descripciontipoalarma))
                            {
                                address = pin.distancia_en_metros > 0
                                    ? $"{pin.descripciontipoalarma}. {pin.distancia_en_metros:F0} {LabelMetros}"
                                    : pin.descripciontipoalarma;
                            }
                            else
                            {
                                address = esCerrado ? "Cerrada" : "Activa";
                            }

                            newPins.Add(new CustomPin
                            {
                                MarkerId              = pin.alarma_id > 0 ? pin.alarma_id.ToString() : $"vp_{pin.latitud:F4}_{pin.longitud:F4}",
                                Id                    = pin.alarma_id > 0 ? pin.alarma_id.ToString() : $"vp_{pin.latitud:F4}_{pin.longitud:F4}",
                                Label                 = pin.alarma_id > 0 ? $"Alarma {pin.alarma_id}" : "Alarma",
                                TipoAlarma            = pin.tipoalarma_id,
                                Type                  = PinType.Generic,
                                Address               = address,
                                Location              = new Location((double)pin.latitud, (double)pin.longitud),
                                FlagPropietarioAlarma = esPropietario,
                                AlarmaCercana         = alarmaCercanaViewport
                            });
                            System.Diagnostics.Debug.WriteLine($"[PintarPinesMapaDesdeCache] Pin {pin.alarma_id} tipo={pin.tipoalarma_id} estado={pin.estado_alarma} propietario={esPropietario} padre={pin.alarma_id_padre?.ToString() ?? "null"}");
                        }
                    }

                    // Calcular polylines y flechas ANTES de asignar CustomPins,
                    // pero añadirlas al mapa DESPUÉS (en un timer retardado) para evitar que
                    // las líneas rojas aparezcan en pantalla antes que los markers de Google Maps.
                    var pinIndex = newPins
                        .Where(p => p.AlarmaCercana != null)
                        .ToDictionary(p => p.AlarmaCercana.alarma_id, p => p);

                    // Lista separada para los marcadores de flecha
                    var arrowPins = new List<sospect.CustomRenderers.CustomPin>();

                    // Acumular datos de polylines para dibujarlas después
                    var polylinesToAdd = new List<Microsoft.Maui.Controls.Maps.Polyline>();

                    foreach (var customPin in newPins)
                    {
                        if (customPin.AlarmaCercana?.alarma_id_padre == null) continue;
                        if (!pinIndex.TryGetValue(customPin.AlarmaCercana.alarma_id_padre.Value, out var pinPadre))
                        {
                            System.Diagnostics.Debug.WriteLine($"[PintarPinesMapaDesdeCache] DIAG: alarma {customPin.AlarmaCercana.alarma_id} tipo={customPin.AlarmaCercana.tipoalarma_id} tiene padre={customPin.AlarmaCercana.alarma_id_padre} pero el padre NO está en pinIndex — polyline omitida. IDs en pinIndex: [{string.Join(",", pinIndex.Keys)}]");
                            continue;
                        }

                        var lineColor = customPin.AlarmaCercana.estado_alarma ? Colors.Red : Colors.Gray;
                        var polyline = new Microsoft.Maui.Controls.Maps.Polyline
                        {
                            StrokeColor = lineColor,
                            StrokeWidth = 4
                        };
                        polyline.Geopath.Add(customPin.Location);
                        polyline.Geopath.Add(pinPadre.Location);
                        polylinesToAdd.Add(polyline);
                        System.Diagnostics.Debug.WriteLine($"[PintarPinesMapaDesdeCache] Polyline preparada: alarma {customPin.AlarmaCercana.alarma_id} → padre {customPin.AlarmaCercana.alarma_id_padre}");

                        // Acumular marcador de flecha en punto medio apuntando del padre al hijo
                        var midLat = (customPin.Location.Latitude  + pinPadre.Location.Latitude)  / 2.0;
                        var midLon = (customPin.Location.Longitude + pinPadre.Location.Longitude) / 2.0;
                        float bearing = CalcularBearing(pinPadre.Location, customPin.Location);
                        var arrowPin = new sospect.CustomRenderers.CustomPin
                        {
                            TipoAlarma   = -1,
                            ArrowBearing = bearing,
                            Address      = customPin.AlarmaCercana.estado_alarma ? "" : "Cerrada",
                            Location     = new Location(midLat, midLon),
                        };
                        arrowPin.MarkerId = "arrow_" + customPin.AlarmaCercana.alarma_id;
                        arrowPin.Id       = arrowPin.MarkerId;
                        arrowPins.Add(arrowPin);
                        System.Diagnostics.Debug.WriteLine($"[PintarPinesMapaDesdeCache] Flecha acumulada bearing={bearing:F1}° en ({midLat:F5},{midLon:F5})");
                    }

                    // Asignar pines de alarma + flechas en una sola operación
                    if (arrowPins.Count > 0)
                        newPins.AddRange(arrowPins);

                    // PRIMERO asignar CustomPins para que los markers de Google Maps se creen
                    map.CustomPins = newPins;
                    System.Diagnostics.Debug.WriteLine($"[PintarPinesMapaDesdeCache] CustomPins asignados — {newPins.Count} (alarmas + {arrowPins.Count} flechas)");

                    // DESPUÉS agregar polylines con un pequeño delay para evitar que aparezcan
                    // antes que los markers (condición de carrera visual).
                    if (polylinesToAdd.Count > 0)
                    {
                        var mapRef = map;
                        var polyRef = polylinesToAdd;
                        Application.Current?.Dispatcher.StartTimer(TimeSpan.FromMilliseconds(250), () =>
                        {
                            try
                            {
                                if (mapRef == null) return false;
                                // Limpiar polylines antiguas que pudieran haber quedado
                                for (int k = mapRef.MapElements.Count - 1; k >= 0; k--)
                                    if (mapRef.MapElements[k] is Microsoft.Maui.Controls.Maps.Polyline)
                                        mapRef.MapElements.RemoveAt(k);
                                // Dibujar las nuevas
                                foreach (var pl in polyRef)
                                    mapRef.MapElements.Add(pl);
                                System.Diagnostics.Debug.WriteLine($"[PintarPinesMapaDesdeCache] {polyRef.Count} polylines dibujadas (retardado)");
                            }
                            catch (Exception exPl)
                            {
                                System.Diagnostics.Debug.WriteLine($"[PintarPinesMapaDesdeCache] Error dibujando polylines retardadas: {exPl.Message}");
                            }
                            return false; // ejecutar solo una vez
                        });
                    }
                    System.Diagnostics.Debug.WriteLine($"[PintarPinesMapaDesdeCache] Completado — {newPins.Count} CustomPins (alarmas + {arrowPins.Count} flechas)");
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PintarPinesMapaDesdeCache] Error: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "HomePage", "PintarPinesMapaDesdeCache");
            }
            finally
            {
                _isPintandoAlarmas = false;
                System.Diagnostics.Debug.WriteLine("HomePage: Flag _isPintandoAlarmas liberado (PintarPinesMapaDesdeCache)");
            }
        }

        /// <summary>
        /// Calcula el bearing (ángulo de rumbo) en grados desde el punto origen hacia el punto destino.
        /// 0° = Norte, 90° = Este, 180° = Sur, 270° = Oeste.
        /// Usado para rotar el marcador de flecha de la polyline padre→hijo.
        /// </summary>
        private static float CalcularBearing(Location origen, Location destino)
        {
            double lat1 = origen.Latitude  * Math.PI / 180.0;
            double lat2 = destino.Latitude * Math.PI / 180.0;
            double dLon = (destino.Longitude - origen.Longitude) * Math.PI / 180.0;

            double y = Math.Sin(dLon) * Math.Cos(lat2);
            double x = Math.Cos(lat1) * Math.Sin(lat2) - Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(dLon);

            double bearing = Math.Atan2(y, x) * 180.0 / Math.PI;
            return (float)((bearing + 360.0) % 360.0);
        }

        /// <summary>
        /// Aplica el filtro de tipos de alarma SIN llamar al API.
        /// Solo re-filtra las alarmas ya cacheadas en App.AlarmasCacheadas.
        /// Según diseño: el filtro trabaja exclusivamente sobre el caché local.
        /// Referencia: 0614-filtro-de-tipos-de-alarma-en-el-mapa-cliente.md
        /// </summary>
        private async Task AplicarFiltroSinRecargarAPI()
        {
            // DIAGNÓSTICO - INICIO
            System.Diagnostics.Debug.WriteLine($"[DIAG-FILTRO] ====== INICIO AplicarFiltroSinRecargarAPI ======");
            System.Diagnostics.Debug.WriteLine($"[DIAG-FILTRO] Cache App.AlarmasCacheadas: {App.AlarmasCacheadas?.Count ?? 0}");
            System.Diagnostics.Debug.WriteLine($"[DIAG-FILTRO] Primera en cache: ID={App.AlarmasCacheadas?.FirstOrDefault()?.alarma_id}");

            try
            {
                // Verificar que hay alarmas cacheadas
                if (App.AlarmasCacheadas == null || App.AlarmasCacheadas.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("[DIAG-FILTRO] No hay alarmas cacheadas para filtrar");
                    return;
                }

                // Aplicar filtro sobre el caché existente
                var alarmasFiltradas = Helpers.FiltroAlarmasHelper.FiltrarPorTipo(App.AlarmasCacheadas);

                // DIAGNÓSTICO - DESPUÉS DE FILTRAR
                System.Diagnostics.Debug.WriteLine($"[DIAG-FILTRO] Alarmas filtradas: {alarmasFiltradas?.Count ?? 0}");
                System.Diagnostics.Debug.WriteLine($"[DIAG-FILTRO] Primera filtrada: ID={alarmasFiltradas?.FirstOrDefault()?.alarma_id}");

                // Re-pintar el mapa con las alarmas filtradas
                if (alarmasFiltradas != null && alarmasFiltradas.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[DIAG-FILTRO] Llamando PintarAlarmasEnMapa con {alarmasFiltradas.Count} alarmas");
                    await PintarAlarmasEnMapa(alarmasFiltradas);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[DIAG-FILTRO] Sin alarmas después del filtro, limpiando mapa");
                    await PintarAlarmasEnMapa(new List<AlarmaCercana>());
                }

                System.Diagnostics.Debug.WriteLine($"[DIAG-FILTRO] ====== FIN AplicarFiltroSinRecargarAPI ======");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DIAG-FILTRO] ERROR: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "HomePage", "AplicarFiltroSinRecargarAPI");
            }
        }

        /// <summary>
        /// Obtiene y pinta las alarmas en el mapa.
        /// </summary>
        /// <param name="centrarMapa">Si es true, centra el mapa en la ubicación del usuario</param>
        /// <param name="forceApiRefresh">Si es true, ignora el caché y fuerza recarga desde API (usar después de lanzar alarma)</param>
        private async Task ObtenerPines(bool centrarMapa = true, bool forceApiRefresh = false)
        {
            // DIAGNÓSTICO - CRÍTICO: DETECTAR QUIÉN LLAMA AL API
            System.Diagnostics.Debug.WriteLine($"[DIAG-PINES] ====== INICIO ObtenerPines ======");
            System.Diagnostics.Debug.WriteLine($"[DIAG-PINES] centrarMapa={centrarMapa}, forceApiRefresh={forceApiRefresh}");
            System.Diagnostics.Debug.WriteLine($"[DIAG-PINES] Caller StackTrace:");
            System.Diagnostics.Debug.WriteLine(Environment.StackTrace);

            if (BindingContext is HomeViewModel vm)
            {
                try
                {
                    // Validar usuario
                    if (App.persona == null)
                    {
                        System.Diagnostics.Debug.WriteLine("[DIAG-PINES] App.persona es null, intentando recuperar");
                        var userJson = Preferences.Get("User", "");
                        if (!string.IsNullOrEmpty(userJson))
                        {
                            App.persona = JsonConvert.DeserializeObject<Persona>(userJson);
                            System.Diagnostics.Debug.WriteLine("[DIAG-PINES] App.persona recuperado");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("[DIAG-PINES] ERROR: No se pudo recuperar App.persona");
                            return;
                        }
                    }

                    // ===== FORCE API REFRESH: Usado después de lanzar alarma para obtener la alarma nueva =====
                    if (forceApiRefresh)
                    {
                        System.Diagnostics.Debug.WriteLine($"[DIAG-PINES] ===== FORCE API REFRESH ACTIVADO =====");

                        vm.IsRefreshingInBackground = true;
                        vm.RefreshStatusText = TranslateExtension.Translate("LblActualizandoAlarmas") ?? "Actualizando alarmas...";

                        try
                        {
                            await RefrescarAlarmasDesdeApiEnBackground(vm, centrarMapa: centrarMapa);
                        }
                        finally
                        {
                            vm.IsRefreshingInBackground = false;
                        }
                        return;
                    }

                    // ===== CACHE-FIRST: Cargar desde caché primero (NO BLOQUEANTE) =====
                    System.Diagnostics.Debug.WriteLine($"[HomePage] ===== CACHE-FIRST: Intentando cargar desde caché =====");
                    var alarmasCacheadas = await App.CargarAlarmasDesdeCache();
                    bool hayCache = alarmasCacheadas != null && alarmasCacheadas.Count > 0;

                    if (hayCache)
                    {
                        // ===== HAY CACHÉ: Pintar INMEDIATAMENTE y refrescar en background =====
                        System.Diagnostics.Debug.WriteLine($"[HomePage] Caché encontrado con {alarmasCacheadas.Count} alarmas - Pintando INMEDIATAMENTE");

                        // Actualizar caché en memoria
                        App.AlarmasCacheadas = alarmasCacheadas;

                        // Aplicar filtro y pintar SIN BLOQUEAR
                        var alarmasFiltradas = Helpers.FiltroAlarmasHelper.FiltrarPorTipo(alarmasCacheadas);
                        System.Diagnostics.Debug.WriteLine($"[HomePage] Filtro aplicado (caché): {alarmasCacheadas.Count} → {alarmasFiltradas?.Count ?? 0} alarmas");

                        if (alarmasFiltradas != null && alarmasFiltradas.Count > 0)
                        {
                            await PintarAlarmasEnMapa(alarmasFiltradas);
                        }

                        // Actualizar ubicación inmediatamente
                        ActualizarUbicacionEnMapa(centrarMapa: centrarMapa);

                        // ===== REFRESCAR EN BACKGROUND (no bloqueante) =====
                        // Si _skipNextBackgroundRefresh está activo, la alarma recién lanzada
                        // ya está en cache y no necesitamos sobrescribirla con datos del API.
                        if (_skipNextBackgroundRefresh)
                        {
                            System.Diagnostics.Debug.WriteLine($"[DIAG-PINES] BGAPI OMITIDO: _skipNextBackgroundRefresh activo - alarma recién lanzada ya en cache");
                            _skipNextBackgroundRefresh = false;
                            // NO lanzar BGAPI - preservar cache con alarma local
                        }
                        else
                        {
                            vm.IsRefreshingInBackground = true;
                            vm.RefreshStatusText = TranslateExtension.Translate("LblRefrescandoAlarmas") ?? "Refrescando alarmas...";

                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    await RefrescarAlarmasDesdeApiEnBackground(vm, centrarMapa: false);
                                }
                                catch (Exception exBg)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[HomePage] Error en refresh background: {exBg.Message}");
                                    // Solo loguear a Crashlytics si no es un error de "página no visible"
                                    if (!exBg.Message.Contains("disposed") && !exBg.Message.Contains("null"))
                                    {
                                        CrashlyticsHelper.LogError(exBg, "HomePage", "ObtenerPines-BackgroundRefresh");
                                    }
                                }
                                finally
                                {
                                    MainThread.BeginInvokeOnMainThread(() =>
                                    {
                                        try
                                        {
                                            // Verificar que el ViewModel sigue válido antes de actualizar
                                            if (vm != null)
                                            {
                                                vm.IsRefreshingInBackground = false;
                                            }
                                        }
                                        catch (Exception exFinally)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"[HomePage] Error al ocultar banner (página ya no activa): {exFinally.Message}");
                                        }
                                    });
                                }
                            });
                        }
                    }
                    else
                    {
                        // ===== NO HAY CACHÉ: Primera carga - Mostrar banner y esperar API =====
                        System.Diagnostics.Debug.WriteLine($"[HomePage] Sin caché - Primera carga, llamando API...");

                        vm.IsRefreshingInBackground = true;
                        vm.RefreshStatusText = TranslateExtension.Translate("LblCargandoAlarmas") ?? "Cargando alarmas...";

                        try
                        {
                            await RefrescarAlarmasDesdeApiEnBackground(vm, centrarMapa: centrarMapa);
                        }
                        finally
                        {
                            vm.IsRefreshingInBackground = false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error al obtener pines: {ex.Message}");
                    Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                    CrashlyticsHelper.LogError(ex, "HomePage", "ObtenerPines");
                    vm.IsRefreshingInBackground = false;
                }
            }
        }

        /// <summary>
        /// Refresca las alarmas desde la API (usado en background o primera carga)
        /// </summary>
        private async Task RefrescarAlarmasDesdeApiEnBackground(HomeViewModel vm, bool centrarMapa)
        {
            // DIAGNÓSTICO - INICIO
            System.Diagnostics.Debug.WriteLine($"[DIAG-BGAPI] ====== INICIO RefrescarAlarmasDesdeApiEnBackground ======");
            System.Diagnostics.Debug.WriteLine($"[DIAG-BGAPI] Cache ANTES de API: {App.AlarmasCacheadas?.Count ?? 0}");
            System.Diagnostics.Debug.WriteLine($"[DIAG-BGAPI] Primera ANTES: ID={App.AlarmasCacheadas?.FirstOrDefault()?.alarma_id}");
            System.Diagnostics.Debug.WriteLine($"[DIAG-BGAPI] centrarMapa={centrarMapa}");
            // DIAGNÓSTICO - FIN

            try
            {
                // Preparar ubicación
                if (App.ubicacionActual != null)
                {
                    App.ubicacionActual.p_user_id_thirdparty = App.persona.user_id_thirdparty;
                    App.ubicacionActual.PantallaOrigen = "HomePage";
                    App.ubicacionActual.Pais = App.persona.Pais;
                }
                else
                {
                    App.ubicacionActual = await LocationUtils.ObtenerUbicacionActual();
                    if (App.ubicacionActual != null && App.persona != null)
                    {
                        App.ubicacionActual.p_user_id_thirdparty = App.persona.user_id_thirdparty;
                        App.ubicacionActual.PantallaOrigen = "HomePage";
                        App.ubicacionActual.Pais = App.persona.Pais;
                    }
                }

                if (App.ubicacionActual == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[HomePage] ERROR: App.ubicacionActual es NULL - No se puede refrescar");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[HomePage] ===== LLAMANDO A API (background) =====");
                System.Diagnostics.Debug.WriteLine($"[HomePage] Ubicación: Lat={App.ubicacionActual.latitud}, Lon={App.ubicacionActual.longitud}");

                List<AlarmaCercana> alarmasFrescas = await ApiService.ActualizarUbicacion(App.ubicacionActual);

                System.Diagnostics.Debug.WriteLine($"[HomePage] API retornó: {alarmasFrescas?.Count ?? 0} alarmas");
                // DIAGNÓSTICO - DESPUÉS DEL API
                System.Diagnostics.Debug.WriteLine($"[DIAG-BGAPI] API retornó: {alarmasFrescas?.Count ?? 0} alarmas");
                System.Diagnostics.Debug.WriteLine($"[DIAG-BGAPI] Primera del API: ID={alarmasFrescas?.FirstOrDefault()?.alarma_id}");

                if (alarmasFrescas != null && alarmasFrescas.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[HomePage] API exitosa - Actualizando caché y repintando");

                    // Limpiar y guardar nuevo caché
                    await App.LimpiarCacheAlarmas();
                    App.AlarmasCacheadas = alarmasFrescas;
                    // DIAGNÓSTICO - DESPUÉS DE ASIGNAR CACHÉ
                    System.Diagnostics.Debug.WriteLine($"[DIAG-BGAPI] Cache DESPUES de asignar: {App.AlarmasCacheadas?.Count ?? 0}");
                    System.Diagnostics.Debug.WriteLine($"[DIAG-BGAPI] Primera DESPUES: ID={App.AlarmasCacheadas?.FirstOrDefault()?.alarma_id}");

                    // Aplicar filtro
                    var alarmasFiltradas = Helpers.FiltroAlarmasHelper.FiltrarPorTipo(App.AlarmasCacheadas);
                    System.Diagnostics.Debug.WriteLine($"[HomePage] Filtro aplicado: {App.AlarmasCacheadas.Count} → {alarmasFiltradas?.Count ?? 0} alarmas");

                    // Repintar en main thread SOLO si la página sigue visible
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        try
                        {
                            // VALIDACIÓN DE SEGURIDAD: Verificar que la página sigue activa
                            if (this == null || map == null || BindingContext == null)
                            {
                                System.Diagnostics.Debug.WriteLine($"[HomePage] Background refresh completado pero página ya no está activa - Saltando repintado");
                                return;
                            }

                            // Verificar que HomePage sigue siendo la página visible
                            bool isHomePageVisible = false;
                            if (Application.Current?.MainPage is TabbedPage tabbedPage)
                            {
                                if (tabbedPage.CurrentPage is NavigationPage navPage)
                                {
                                    isHomePageVisible = navPage.CurrentPage is HomePage;
                                }
                            }
                            else if (Application.Current?.MainPage is NavigationPage navPage)
                            {
                                isHomePageVisible = navPage.CurrentPage is HomePage;
                            }

                            if (!isHomePageVisible)
                            {
                                System.Diagnostics.Debug.WriteLine($"[HomePage] Background refresh completado pero HomePage no está visible - Caché actualizado, repintado omitido");
                                // Notificar a otras páginas (DescribirPage) que el caché se actualizó
                                MessagingCenter.Send<object, string>(this, "AlarmasCacheActualizadas", "BackgroundRefresh");
                                return;
                            }

                            if (alarmasFiltradas != null && alarmasFiltradas.Count > 0)
                            {
                                await PintarAlarmasEnMapa(alarmasFiltradas);
                            }

                            if (centrarMapa)
                            {
                                ActualizarUbicacionEnMapa(centrarMapa: true);
                            }

                            // Notificar a otras páginas (DescribirPage) que el caché se actualizó
                            MessagingCenter.Send<object, string>(this, "AlarmasCacheActualizadas", "BackgroundRefresh");
                        }
                        catch (Exception exPintar)
                        {
                            System.Diagnostics.Debug.WriteLine($"[HomePage] Error al repintar después de background refresh: {exPintar.Message}");
                            // No loguear a Crashlytics - es un error esperado si el usuario cambió de pantalla
                        }
                    });
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[HomePage] API retornó lista vacía o null - Manteniendo caché actual");
                    System.Diagnostics.Debug.WriteLine($"[DIAG-BGAPI] API vacía - Cache sin cambios");
                }
                System.Diagnostics.Debug.WriteLine($"[DIAG-BGAPI] ====== FIN RefrescarAlarmasDesdeApiEnBackground ======");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HomePage] Error en RefrescarAlarmasDesdeApiEnBackground: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[DIAG-BGAPI] ====== FIN RefrescarAlarmasDesdeApiEnBackground (con error) ======");
                CrashlyticsHelper.LogError(ex, "HomePage", "RefrescarAlarmasDesdeApiEnBackground");
            }
        }

        async void ReportarCrimen_Clicked(System.Object sender, System.EventArgs e)
        {
            if (IsBusy)
            {
                return;
            }

            IsBusy = true;

            var popup = new ConfirmarLanzarAlarmaEnUbicacionActual(App.ubicacionActual.latitud, App.ubicacionActual.longitud, 2);
            await this.ShowPopupAsync(popup);
            IsBusy = false;
        }

        private void ConfigurarGestosMapa()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("HomePage: ConfigurarGestosMapa iniciado");

                // CORREGIDO: Limpiar gesture recognizers existentes ANTES de agregar nuevos
                map.GestureRecognizers.Clear();

                // CORREGIDO: Usar las propiedades que ahora existen en CustomMap
                map.HasScrollEnabled = true;
                map.HasZoomEnabled = true;
                map.HasRotationEnabled = false;

                // Configurar el comportamiento de entrada táctil
                map.InputTransparent = false;

                // OPCIONAL: Solo agregar gesture recognizers si no existen
                if (map.GestureRecognizers.Count == 0)
                {
                    var panGestureRecognizer = new PanGestureRecognizer();
                    panGestureRecognizer.PanUpdated += OnMapPanUpdated;
                    map.GestureRecognizers.Add(panGestureRecognizer);

                    var pinchGestureRecognizer = new PinchGestureRecognizer();
                    pinchGestureRecognizer.PinchUpdated += OnMapPinchUpdated;
                    map.GestureRecognizers.Add(pinchGestureRecognizer);

                    System.Diagnostics.Debug.WriteLine("HomePage: Gesture recognizers agregados");
                }

                // NUEVO: Suscribirse al evento de cambio de región visible para clustering zoom-aware
                map.VisibleRegionChanged -= OnMapVisibleRegionChanged; // Desuscribir primero (evitar duplicados)
                map.VisibleRegionChanged += OnMapVisibleRegionChanged;
                System.Diagnostics.Debug.WriteLine("HomePage: Suscrito a VisibleRegionChanged para clustering zoom-aware");

                System.Diagnostics.Debug.WriteLine("HomePage: ConfigurarGestosMapa completado - Rotación DESHABILITADA");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HomePage: Error en ConfigurarGestosMapa: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "HomePage", "ConfigurarGestosMapa");
            }
        }

        private void OnMapPanUpdated(object sender, PanUpdatedEventArgs e)
        {
            try
            {
                // Permitir que el mapa maneje el gesto de paneo
                if (e.StatusType == GestureStatus.Started || e.StatusType == GestureStatus.Running)
                {
                    // Marcar que estamos interactuando con el mapa para prevenir otras acciones
                    System.Diagnostics.Debug.WriteLine($"HomePage: Pan gesture permitido - {e.StatusType}");
                    // El usuario puede mover el mapa libremente
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HomePage: Error en OnMapPanUpdated: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "HomePage", "OnMapPanUpdated");
            }
        }

        private void OnMapPinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
        {
            try
            {
                // Permitir que el mapa maneje el gesto de pellizco para zoom
                if (e.Status == GestureStatus.Started || e.Status == GestureStatus.Running)
                {
                    System.Diagnostics.Debug.WriteLine($"HomePage: Pinch gesture permitido - {e.Status}, Scale: {e.Scale}");
                }
                // NUEVO: Cuando termina el pinch, disparar cambio de región para re-clustering
                else if (e.Status == GestureStatus.Completed)
                {
                    System.Diagnostics.Debug.WriteLine("HomePage: Pinch completado, se disparará re-clustering");
                    // El evento VisibleRegionChanged se encargará del clustering
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HomePage: Error en OnMapPinchUpdated: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "HomePage", "OnMapPinchUpdated");
            }
        }

        // NUEVO: Handler para cambios en la región visible del mapa (zoom/pan)
        // Implementa clustering zoom-aware según Manual General SOSpect, sección 5
        // OPTIMIZADO: Usa debounce de 500ms para reducir 285 eventos a ~15-20
        private async void OnMapVisibleRegionChanged(object sender, MapSpanChangedEventArgs e)
        {
            try
            {
                // OPTIMIZACIÓN: Verificaciones tempranas de salida (sin logging excesivo)
                if (!_isPageCurrentlyVisible || !_shouldTimerRun)
                    return;

                if (_isInitialMapSetup || _isVisualizacionAlarmaEspecifica)
                    return;

                if (e?.NewRegion == null)
                    return;

                // Capturar valores para usar dentro del debounce
                var newRegion = e.NewRegion;

                // OPTIMIZACIÓN: Usar debounce de 500ms para evitar procesamiento excesivo
                await _visibleRegionDebouncer.DebounceAsync(async () =>
                {
                    // Verificar de nuevo después del debounce (la página pudo cerrarse)
                    if (!_isPageCurrentlyVisible || !_shouldTimerRun)
                        return;

                    // Calcular nuevo zoom level
                    var newZoomLevel = GridClusteringHelper.CalcularZoomLevel(newRegion);
                    var previousZoomLevel = _currentZoomLevel;
                    _currentZoomLevel = newZoomLevel;
                    _currentMapSpan = newRegion;

                    System.Diagnostics.Debug.WriteLine($"HomePage: VisibleRegionChanged (debounced) - Zoom: {previousZoomLevel} → {newZoomLevel}");

                    // Solo re-clusterizar si el zoom cambió (no por simple pan)
                    var zoomChanged = previousZoomLevel != newZoomLevel;

                    if (zoomChanged && _alarmasCacheadas != null && _alarmasCacheadas.Any())
                    {
                        System.Diagnostics.Debug.WriteLine($"HomePage: Zoom cambió, re-clusterizando {_alarmasCacheadas.Count} alarmas cacheadas");
                        await ReclusterizarAlarmas();
                    }

                    // ── NUEVO (2026-03-02): Solicitar datos del nuevo endpoint para el viewport actual ──
                    // Cuando el usuario mueve o aleja el mapa, el viewport cambia y la caché anterior
                    // puede no cubrir la zona visible. Solicitamos datos frescos en background y pintamos.
                    if (newRegion != null)
                    {
                        double lat  = newRegion.Center.Latitude;
                        double lon  = newRegion.Center.Longitude;
                        double dLat = newRegion.LatitudeDegrees / 2.0;
                        double dLon = newRegion.LongitudeDegrees / 2.0;
                        decimal vMinLat = (decimal)(lat - dLat);
                        decimal vMaxLat = (decimal)(lat + dLat);
                        decimal vMinLon = (decimal)(lon - dLon);
                        decimal vMaxLon = (decimal)(lon + dLon);

                        // Calcular zoom efectivo respetando el threshold dinámico del usuario.
                        // Si el usuario NO debería estar en modo cluster según su radio (DebeActivarClustering
                        // retorna false), se fuerza zoom=15 para que el endpoint devuelva pines individuales,
                        // igual que lo hacía el sistema anterior (GridClusteringHelper client-side).
                        // Esto preserva el comportamiento ya probado: usuario con radio 3000m no ve
                        // clusters aunque el zoom real sea 13 o 14.
                        int zoomEfectivo = newZoomLevel;
                        try
                        {
                            var parametrosStr = Preferences.Get("ParametrosUsuario", "");
                            if (!string.IsNullOrEmpty(parametrosStr))
                            {
                                var parametrosZoom = JsonConvert.DeserializeObject<ParametrosUsuario>(parametrosStr);
                                var radioUsuarioZoom = parametrosZoom?.radio_alarmas_mts_actual ?? 100;
                                // Si el clustering NO debe activarse para este usuario/zoom, pedir pines individuales
                                if (!GridClusteringHelper.DebeActivarClustering(newZoomLevel, 1, radioUsuarioZoom))
                                    zoomEfectivo = 15;
                            }
                        }
                        catch { /* si falla la lectura de prefs, usar zoom real */ }

                        System.Diagnostics.Debug.WriteLine($"[VisibleRegion] Solicitando PinesMapa viewport [{vMinLat},{vMinLon}]-[{vMaxLat},{vMaxLon}] zoomReal={newZoomLevel} zoomEfectivo={zoomEfectivo}");

                        // Llamar en background — no bloquear el hilo UI
                        _ = Task.Run(async () =>
                        {
                            bool ok = await App.RefrescarMapaDesdeAPI(vMinLat, vMaxLat, vMinLon, vMaxLon, zoomEfectivo);
                            if (ok && _isPageCurrentlyVisible)
                            {
                                await MainThread.InvokeOnMainThreadAsync(async () =>
                                {
                                    var pines = App.CacheMapa?.Pines;
                                    if (pines != null && pines.Count > 0)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[VisibleRegion] Pintando {pines.Count} pines frescos del viewport");
                                        await PintarPinesMapaDesdeCache(pines);
                                        ActualizarUbicacionEnMapa(centrarMapa: false);
                                    }
                                });
                            }
                        });
                    }
                }, 500); // 500ms de debounce
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HomePage: Error en OnMapVisibleRegionChanged: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "HomePage", "OnMapVisibleRegionChanged");
            }
        }

        // NUEVO: Re-clusteriza alarmas cacheadas según el zoom level actual
        // NO llama al API, solo re-procesa las alarmas ya descargadas
        private async Task ReclusterizarAlarmas()
        {
            try
            {
                // OPTIMIZACIÓN: Verificar si la página está visible antes de procesar
                if (!_isPageCurrentlyVisible)
                {
                    System.Diagnostics.Debug.WriteLine("HomePage: ReclusterizarAlarmas IGNORADO - Página no visible");
                    return;
                }

                if (_isPintandoAlarmas)
                {
                    System.Diagnostics.Debug.WriteLine("HomePage: ReclusterizarAlarmas IGNORADO - Ya hay actualización en progreso");
                    return;
                }

                if (_alarmasCacheadas == null || !_alarmasCacheadas.Any())
                {
                    System.Diagnostics.Debug.WriteLine("HomePage: No hay alarmas cacheadas para re-clusterizar");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"HomePage: Iniciando re-clustering de {_alarmasCacheadas.Count} alarmas con zoom {_currentZoomLevel}");

                // Aplicar filtrado client-side (solo flag_visible_mapa)
                var alarmasFiltradas = _alarmasCacheadas.Where(a => a.flag_visible_mapa).ToList();

                // Aplicar clustering si está habilitado
                List<CustomPin> pinesResultantes;

                // Obtener radio del usuario para clustering adaptativo
                ParametrosUsuario parametrosParaClustering = JsonConvert.DeserializeObject<ParametrosUsuario>(Preferences.Get("ParametrosUsuario", ""));
                var radioUsuario = parametrosParaClustering?.radio_alarmas_mts_actual ?? 100;

                if (GridClusteringHelper.DebeActivarClustering(_currentZoomLevel, alarmasFiltradas.Count, radioUsuario))
                {
                    System.Diagnostics.Debug.WriteLine($"HomePage: Clustering ACTIVADO (zoom {_currentZoomLevel}, {alarmasFiltradas.Count} alarmas, radio {radioUsuario}m)");
                    _isClusteringEnabled = true;
                    pinesResultantes = GridClusteringHelper.ClusterizarAlarmas(alarmasFiltradas, _currentZoomLevel, radioUsuario);

                    // Establecer textos localizados en clusters
                    var LabelAlarmas = await TranslateExtension.TranslateAsync("LabelAlarmas");
                    var LabelAlarmasAgrupadas = await TranslateExtension.TranslateAsync("LabelAlarmasAgrupadas");

                    foreach (var pin in pinesResultantes)
                    {
                        if (pin is ClusterPin cluster)
                        {
                            cluster.Label = $"{cluster.TotalAlarmas} {LabelAlarmas ?? "alarmas"}";
                            cluster.Address = $"{cluster.TotalAlarmas} {LabelAlarmasAgrupadas ?? "alarmas agrupadas"}";
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"HomePage: Clustering DESACTIVADO (zoom {_currentZoomLevel}, {alarmasFiltradas.Count} alarmas)");
                    _isClusteringEnabled = false;
                    pinesResultantes = await ConvertirAlarmasAPinesIndividuales(alarmasFiltradas);
                }

                // Actualizar mapa con nuevos pines (clusters o individuales)
                await ActualizarPinesEnMapa(pinesResultantes);

                System.Diagnostics.Debug.WriteLine($"HomePage: Re-clustering completado - {pinesResultantes.Count} pines renderizados");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HomePage: Error en ReclusterizarAlarmas: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "HomePage", "ReclusterizarAlarmas");
            }
        }

        // NUEVO: Convierte alarmas a pines individuales (sin clustering)
        private async Task<List<CustomPin>> ConvertirAlarmasAPinesIndividuales(List<AlarmaCercana> alarmas)
        {
            var pines = new List<CustomPin>();

            // Obtener textos localizados una sola vez (fuera del loop para performance)
            var LabelAlarma = await TranslateExtension.TranslateAsync("LabelAlarma");

            foreach (var alarma in alarmas)
            {
                var pin = new CustomPin()
                {
                    MarkerId = alarma.alarma_id.ToString(),
                    Id = alarma.alarma_id.ToString(),
                    Label = $"{LabelAlarma ?? "Alarma"} {alarma.alarma_id}",
                    TipoAlarma = alarma.tipoalarma_id,
                    Type = PinType.Generic,
                    Address = alarma.descripciontipoalarma,
                    Location = new Location((double)alarma.latitud_alarma, (double)alarma.longitud_alarma),
                    FlagPropietarioAlarma = alarma.flag_propietario_alarma,
                    AlarmaCercana = alarma
                };

                pines.Add(pin);
            }

            return pines;
        }

        // NUEVO: Actualiza pines en el mapa (optimizado para no duplicar)
        private async Task ActualizarPinesEnMapa(List<CustomPin> pines)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                try
                {
                    // Preservar pin de usuario
                    var userPin = map.CustomPins?.FirstOrDefault(p => p.Id == "User" || p.MarkerId == "User");

                    // Construir nueva lista
                    var newCustomPins = new List<CustomPin>();

                    if (userPin != null)
                        newCustomPins.Add(userPin);

                    newCustomPins.AddRange(pines);

                    // Asignar en una sola operación
                    map.CustomPins = newCustomPins;

                    System.Diagnostics.Debug.WriteLine($"HomePage: Pines actualizados en mapa - Total: {map.CustomPins.Count}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"HomePage: Error actualizando pines: {ex.Message}");
                    CrashlyticsHelper.LogError(ex, "HomePage", "ActualizarPinesEnMapa");
                }
            });
        }

        private void RestaurarOrientacionNorte()
        {
            try
            {
                // Si necesitas restaurar la orientación Norte programáticamente
                // (útil si el usuario rota accidentalmente en algún escenario)

                if (App.ubicacionActual != null)
                {
                    var center = new Location(App.ubicacionActual.latitud, App.ubicacionActual.longitud);
                    ParametrosUsuario parametros = JsonConvert.DeserializeObject<ParametrosUsuario>(
                        Preferences.Get("ParametrosUsuario", ""));

                    var valorRadio = parametros?.radio_alarmas_mts_actual ?? 100;
                    var mapSpan = MapSpan.FromCenterAndRadius(center, new Distance(valorRadio));

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        map.MoveToRegion(mapSpan);
                        System.Diagnostics.Debug.WriteLine("HomePage: Orientación Norte restaurada");
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HomePage: Error restaurando orientación Norte: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "HomePage", "RestaurarOrientacionNorte");
            }
        }
        private void LimpiarGestosDelMapa()
        {
            try
            {
                if (map?.GestureRecognizers != null)
                {
                    // Desconectar eventos antes de limpiar
                    foreach (var gesture in map.GestureRecognizers)
                    {
                        if (gesture is PanGestureRecognizer panGesture)
                        {
                            panGesture.PanUpdated -= OnMapPanUpdated;
                        }
                        else if (gesture is PinchGestureRecognizer pinchGesture)
                        {
                            pinchGesture.PinchUpdated -= OnMapPinchUpdated;
                        }
                    }

                    map.GestureRecognizers.Clear();
                    System.Diagnostics.Debug.WriteLine("HomePage: Gestos del mapa limpiados");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HomePage: Error limpiando gestos: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "HomePage", "LimpiarGestosDelMapa");
            }
        }
        public async Task RefrescarDespuesDeAlarma()
        {
            // DIAGNÓSTICO - INICIO
            System.Diagnostics.Debug.WriteLine($"[DIAG-REFRESH] ====== INICIO RefrescarDespuesDeAlarma ======");
            System.Diagnostics.Debug.WriteLine($"[DIAG-REFRESH] Cache tiene: {App.AlarmasCacheadas?.Count ?? 0} alarmas");
            System.Diagnostics.Debug.WriteLine($"[DIAG-REFRESH] Primera en cache: ID={App.AlarmasCacheadas?.FirstOrDefault()?.alarma_id}");

            try
            {
                // CRÍTICO: Verificar que la instancia sea válida
                if (this == null)
                {
                    System.Diagnostics.Debug.WriteLine("[DIAG-REFRESH] ERROR: Esta instancia es null");
                    return;
                }

                if (BindingContext is HomeViewModel vm)
                {
                    vm.IsRunning = true;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[DIAG-REFRESH] ERROR: BindingContext no es HomeViewModel válido");
                    return;
                }

                // Pequeño delay para asegurar que la alarma ya está en cache
                await Task.Delay(200);

                // Si la página NO es visible (ej: popup de alarma aún abierto),
                // activar flag para que OnAppearing repinte desde cache al volver.
                if (!_isPageCurrentlyVisible)
                {
                    System.Diagnostics.Debug.WriteLine($"[DIAG-REFRESH] Pagina NO visible - Activando _pendienteRepintarDespuesDeAlarma");
                    _pendienteRepintarDespuesDeAlarma = true;

                    if (BindingContext is HomeViewModel vm2)
                    {
                        vm2.IsRunning = false;
                    }

                    System.Diagnostics.Debug.WriteLine($"[DIAG-REFRESH] ====== FIN RefrescarDespuesDeAlarma (diferido a OnAppearing) ======");
                    return;
                }

                // CRÍTICO: Volver al hilo principal para operaciones de UI
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    try
                    {
                        // Verificar nuevamente antes de cada operación
                        if (this == null || BindingContext == null)
                        {
                            System.Diagnostics.Debug.WriteLine("[DIAG-REFRESH] ERROR: Instancia se volvió null");
                            return;
                        }

                        // DIAGNÓSTICO - ANTES DE APLICAR FILTRO
                        System.Diagnostics.Debug.WriteLine($"[DIAG-REFRESH] Llamando AplicarFiltroSinRecargarAPI...");
                        await AplicarFiltroSinRecargarAPI();

                        // FIX 2026-02-27: Notificar al feed Describir para que re-filtre desde caché.
                        // Esto es necesario cuando la alarma propia ya está en App.AlarmasCacheadas
                        // con flag_visible_siguiendo=true y el usuario ya estaba en el tab Describir.
                        MessagingCenter.Send<object, string>(this, "AlarmaLanzada_RefrescarDescribir", "refresh");
                        System.Diagnostics.Debug.WriteLine($"[DIAG-REFRESH] Mensaje AlarmaLanzada_RefrescarDescribir enviado");

                        if (BindingContext is HomeViewModel vm2)
                        {
                            vm2.IsRunning = false;
                        }

                        System.Diagnostics.Debug.WriteLine($"[DIAG-REFRESH] ====== FIN RefrescarDespuesDeAlarma ======");
                    }
                    catch (Exception mainThreadEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[DIAG-REFRESH] ERROR MainThread: {mainThreadEx.Message}");
                        CrashlyticsHelper.LogError(mainThreadEx, "HomePage", "RefrescarDespuesDeAlarma-MainThread");

                        if (BindingContext is HomeViewModel vm3)
                        {
                            vm3.IsRunning = false;
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DIAG-REFRESH] ERROR: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "HomePage", "RefrescarDespuesDeAlarma");

                try
                {
                    if (BindingContext is HomeViewModel vm)
                    {
                        vm.IsRunning = false;
                    }
                }
                catch
                {
                    System.Diagnostics.Debug.WriteLine("[DIAG-REFRESH] No se pudo acceder a ViewModel");
                }
            }
        }
        private async Task IniciarRefrescoAutomaticoTemporal()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Iniciando refresco automático temporal...");

                // Refrescar inmediatamente
                await Task.Delay(1000);
                await ObtenerPines();

                // Refrescar una segunda vez por si acaso
                await Task.Delay(3000);
                await ObtenerPines();

                System.Diagnostics.Debug.WriteLine("Refresco automático temporal completado");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en refresco automático: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "HomePage", "IniciarRefrescoAutomaticoTemporal");
            }
        }
        private void DiagnosticarRecepcionMensajes()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== DIAGNÓSTICO RECEPCIÓN DE MENSAJES ===");
                System.Diagnostics.Debug.WriteLine($"HomePage HashCode: {this.GetHashCode()}");
                System.Diagnostics.Debug.WriteLine($"BindingContext tipo: {BindingContext?.GetType()?.Name}");

                // TEST CRASH PARA VERIFICAR CRASHLYTICS
                if (false) // Cambiar a true solo para probar
                {
                    throw new Exception("Test crash DiagnosticarRecepcionMensajes - Verificando Firebase Crashlytics");
                }

                // Enviar mensaje de prueba a nosotros mismos
                System.Diagnostics.Debug.WriteLine("Enviando mensaje de prueba a esta instancia...");
                MessagingCenter.Send<object, string>(this, "AlarmaLanzadaExitosamente", "TEST");

                System.Diagnostics.Debug.WriteLine("=== FIN DIAGNÓSTICO ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DiagnosticarRecepcionMensajes: Error: {ex.Message}");

                CrashlyticsHelper.LogError(ex, "HomePage", "DiagnosticarRecepcionMensajes");
            }
        }

        private void EnsureCircleVisibility()
        {
            try
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        if (currentCircle != null && map.MapElements.Contains(currentCircle))
                        {
                            // TRUCO: Remover y volver a agregar para que quede encima
                            map.MapElements.Remove(currentCircle);
                            map.MapElements.Add(currentCircle);
                            System.Diagnostics.Debug.WriteLine("HomePage: *** CÍRCULO FORZADO A CAPA SUPERIOR ***");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("HomePage: Círculo no disponible para forzar visibilidad");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"HomePage: Error forzando visibilidad de círculo: {ex.Message}");
                        CrashlyticsHelper.LogError(ex, "HomePage", "EnsureCircleVisibility-Inner");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HomePage: Error en EnsureCircleVisibility: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "HomePage", "EnsureCircleVisibility");
            }
        }

        /// <summary>
        /// Garantiza que el mapa sea visible y aplica cualquier refresco pendiente post-alarma.
        /// Llamado por SospectTabs cuando el usuario selecciona el tab del mapa.
        /// </summary>
        public async Task EnsureMapVisible()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[HomePage] EnsureMapVisible: inicio");

                // Si hay un refresco diferido pendiente (post-alarma), ejecutarlo ahora
                if (_pendienteRepintarDespuesDeAlarma)
                {
                    System.Diagnostics.Debug.WriteLine("[HomePage] EnsureMapVisible: consumiendo _pendienteRepintarDespuesDeAlarma");
                    _pendienteRepintarDespuesDeAlarma = false;
                    await RefrescarDespuesDeAlarma();
                }

                // Si hay páginas apiladas sobre el mapa, volver a la raíz
                var navPage = Parent as NavigationPage;
                if (navPage != null && navPage.Navigation.NavigationStack.Count > 1)
                {
                    System.Diagnostics.Debug.WriteLine($"[HomePage] EnsureMapVisible: popando {navPage.Navigation.NavigationStack.Count - 1} páginas para volver al mapa");
                    await navPage.Navigation.PopToRootAsync(animated: false);
                }

                System.Diagnostics.Debug.WriteLine("[HomePage] EnsureMapVisible: fin");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HomePage] EnsureMapVisible: error — {ex.Message}");
                CrashlyticsHelper.LogError(ex, "HomePage", "EnsureMapVisible");
            }
        }
    }
}

