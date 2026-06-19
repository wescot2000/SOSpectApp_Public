// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Maui.Controls;
using sospect.Models;
using sospect.Services;
using sospect.Utils;
using sospect.Views;
using sospect.Helpers;
using sospect.Interfaces;
using sospect.CustomRenderers;
using sospect.ViewModels;
using System.Net.Http;
using System.Net;
using System.IdentityModel.Tokens.Jwt;
#if ANDROID
using Plugin.FirebasePushNotifications;
#endif
#if IOS
using Plugin.AdMob.Services;
#endif
using Microsoft.Maui.Networking;
using System.Diagnostics;
using sospect.DTOs;

namespace sospect
{
    public partial class App : Application
    {
        // Propiedades estaticas completas
        public static Persona persona;
        public static Ubicaciones ubicacionActual { get; set; }
        public static string TokenHubNotification { get; set; }

        public static List<AlarmaCercana>? AlarmasCercanasAMostrar { get; set; }
        public static List<CustomPin> CustomPins { get; set; }
        public static bool justCheckedNotificationPermissions = false;
        public static bool FMCTokenChanged = false;
        public static bool IsFirstLoginInProgress = false; // Flag para evitar cierre de sesión durante race condition del primer login

        // Supresión de notificaciones cuando el usuario está en el chat de publicidad
        // Se setea en ChatPublicidadPage.OnAppearing / OnDisappearing
        public static bool ChatPublicidadAbierto = false;
        public static long ChatPublicidadAlarmaIdActivo = 0;

        // Flag para detectar arranque desde app CERRADA (vs. resume desde background).
        // Se setea en OnStart() y se consume (reset a false) en HomePage.OnAppearing().
        // Solo cuando es true se ejecuta RefrescarAmbosFeeds (Feed A + Feed B secuencial).
        // 21022026: agregado para secuenciación de feeds.
        public static bool EsPrimerArranque = false;

        /// <summary>
        /// Cuando es true, el usuario está viendo el feed de alarmas (DescribirPage).
        /// Mientras sea true, NO se ejecutan refrescos de alarmas desde el API
        /// (patrón Twitter/X: el usuario controla el refresh con pull-to-refresh).
        /// InsertaUbicacionBackground NO se ve afectado.
        /// </summary>
        public static bool DescribirPageActiva = false;

        /// <summary>
        /// IDs de alarmas para las que el usuario actual propuso el cierre comunitario.
        /// Se usa para redirigir al proponente a VerHistorialAlarmaPage en lugar de
        /// CierreEncuestaPage, incluso cuando flag_propietario_alarma = false.
        /// Se limpia al reiniciar la app (volatile, solo en memoria).
        /// </summary>
        public static HashSet<long> AlarmasProponenteCierre { get; } = new HashSet<long>();

        /// <summary>
        /// Reaplica la "verdad local" del cliente sobre la lista de alarmas que viene del API.
        /// Hoy: para cada alarma cuyo id está en <see cref="AlarmasProponenteCierre"/>, fuerza
        /// <c>TieneVotacionActiva = true</c>. Esto cubre la ventana de inconsistencia eventual
        /// entre el momento en que el usuario propone un cierre y el siguiente refresh del feed
        /// (Feed A "Siguiendo" o Feed B "Para ti") — sin esto, el endpoint puede retornar la
        /// alarma con TVA=false y el routing del tap llevaría al usuario a DetalleDescripcionAlarmaPage
        /// en lugar de VerHistorialAlarmaPage. Agregado: 2026-04 (anti-corrupción / UX cierre).
        /// </summary>
        private static void ReaplicarFlagsLocalesProponente(List<AlarmaCercana>? lista)
        {
            if (lista == null || lista.Count == 0) return;
            if (AlarmasProponenteCierre.Count == 0) return;
            try
            {
                foreach (var a in lista)
                {
                    if (a == null) continue;
                    if (AlarmasProponenteCierre.Contains(a.alarma_id))
                    {
                        a.TieneVotacionActiva = true;
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "App", "ReaplicarFlagsLocalesProponente");
            }
        }

        // NUEVO: Caché persistente de alarmas para diseño Twitter/X
        private static readonly string CacheFileName = "alarmas_cache.json";
        private static List<AlarmaCercana>? _alarmasCacheadas = null;

        // Cache B — "Para ti" (04-02-2026): alimentado por endpoint separado, replace en cada refresh
        private static readonly string CacheFileNameParaTi = "alarmas_cache_parati.json";
        private static List<AlarmaCercana>? _alarmasCacheadasParaTi = null;

        // Cache C — Mapa (2026-03-01): pines del último viewport con bounding box guardado
        // Se valida intersección antes de pintar para evitar mostrar datos de otra ciudad
        private static readonly string CacheFileNameMapa = "alarmas_cache_mapa.json";
        private static MapaCacheDto? _cacheMapa = null;

        // Cache D — Pines persistentes de cadenas de persecución (tipo 9 + sus padres de crimen).
        // Una vez que un pin tipo-9 o su padre aparecen en cualquier viewport, se acumulan aquí
        // y se inyectan en cada render de PintarPinesMapaDesdeCache para que no desaparezcan al mover el mapa.
        // TTL: 90 minutos sin aparecer en ningún viewport → se expira (mismo umbral que el endpoint de pines).
        // Si el viewport retorna el pin con estado_alarma=false, se actualiza el estado (pin gris).
        // Solo se limpia completamente al cerrar sesión. En memoria (no disco) por simplicidad.
        private static readonly object _lockPinesPersistentes = new object();
        // Valor: (pin, últimaVezVistoEnViewport)
        private static readonly Dictionary<long, (Models.PinMapaDto pin, DateTime ultimaVista)> _pinesPersistentesEscape
            = new Dictionary<long, (Models.PinMapaDto, DateTime)>();

        // TTL para expirar pines que ya no aparecen en el viewport: 90 min (igual que el endpoint de pines)
        private static readonly TimeSpan _ttlPinesPersistentes = TimeSpan.FromMinutes(90);

        /// <summary>
        /// Retorna los pines persistentes vigentes (no expirados).
        /// Expira internamente los que llevan más de 90 min sin aparecer en ningún viewport.
        /// </summary>
        public static List<Models.PinMapaDto> PinesPersistentesEscape
        {
            get
            {
                lock (_lockPinesPersistentes)
                {
                    var ahora = DateTime.UtcNow;
                    // Expirar los que superaron el TTL
                    var expirados = _pinesPersistentesEscape
                        .Where(kv => (ahora - kv.Value.ultimaVista) > _ttlPinesPersistentes)
                        .Select(kv => kv.Key)
                        .ToList();
                    foreach (var id in expirados)
                    {
                        _pinesPersistentesEscape.Remove(id);
                    }
                    return _pinesPersistentesEscape.Values.Select(v => v.pin).ToList();
                }
            }
        }

        // Alarmas insertadas localmente (recién lanzadas por el usuario).
        // Se almacena la alarma completa + timestamp para poder preservarla cuando el API
        // devuelve datos que aún no la incluyen (race condition con background refresh).
        // Se auto-limpia después de 5 minutos (tiempo suficiente para que el API la indexe).
        private static readonly Dictionary<long, (AlarmaCercana alarma, DateTime insertadaEn)> _alarmasInsertadasLocalmente
            = new Dictionary<long, (AlarmaCercana, DateTime)>();

        public static List<AlarmaCercana>? AlarmasCacheadas
        {
            get => _alarmasCacheadas;
            set
            {
                // Merge: preservar alarmas insertadas localmente que el API aún no conoce
                if (value != null && _alarmasInsertadasLocalmente.Count > 0)
                {
                    // Limpiar alarmas locales que ya tienen más de 5 minutos (ya deberían estar en el API)
                    var expiradas = _alarmasInsertadasLocalmente
                        .Where(kv => (DateTime.Now - kv.Value.insertadaEn).TotalMinutes > 5)
                        .Select(kv => kv.Key)
                        .ToList();
                    foreach (var key in expiradas)
                    {
                        _alarmasInsertadasLocalmente.Remove(key);
                    }

                    // Buscar alarmas locales vigentes que NO están en el nuevo cache del API
                    if (_alarmasInsertadasLocalmente.Count > 0)
                    {
                        var idsEnNuevoCache = new HashSet<long>(value.Select(a => a.alarma_id));
                        var alarmasLocalesFaltantes = _alarmasInsertadasLocalmente
                            .Where(kv => !idsEnNuevoCache.Contains(kv.Key))
                            .Select(kv => kv.Value.alarma)
                            .ToList();

                        if (alarmasLocalesFaltantes.Count > 0)
                        {
                            // Insertar al principio para que aparezcan primero
                            value.InsertRange(0, alarmasLocalesFaltantes);
                        }
                    }
                }

                // Blindaje 2026-04: reaplicar verdad local del proponente de cierre antes de
                // persistir/exponer la lista. Garantiza que tras un refresh del API, las alarmas
                // donde el usuario propuso cierre conservan TieneVotacionActiva=true en cache,
                // de modo que el tap rutea correctamente a VerHistorialAlarmaPage sin esperar
                // a que el backend propague el flag.
                ReaplicarFlagsLocalesProponente(value);

                _alarmasCacheadas = value;
                // Auto-guardar cuando se actualiza
                if (value != null)
                {
                    _ = GuardarAlarmasEnCache(value);
                }
            }
        }

        /// <summary>
        /// Cache C — Mapa (2026-03-01).
        /// Wrapper con pines + viewport guardado. Se valida intersección al arrancar
        /// para no pintar datos de otra ciudad.
        /// </summary>
        public static MapaCacheDto? CacheMapa
        {
            get => _cacheMapa;
            set
            {
                _cacheMapa = value;
                if (value != null)
                    _ = GuardarCacheMapa(value);
            }
        }

        /// <summary>
        /// Cache B — "Para ti". Sin lógica de merge de alarmas locales (eso solo aplica a Cache A).
        /// Pull-to-refresh siempre REEMPLAZA este cache íntegramente.
        /// </summary>
        public static List<AlarmaCercana>? AlarmasCacheadasParaTi
        {
            get => _alarmasCacheadasParaTi;
            set
            {
                // Blindaje 2026-04: reaplicar verdad local del proponente de cierre.
                // Idéntica razón que en el setter de AlarmasCacheadas (Cache A): evita la
                // ventana en la que el API aún no refleja la votación recién propuesta y
                // el feed mostraría TVA=false, ruteando el tap a la pantalla equivocada.
                ReaplicarFlagsLocalesProponente(value);

                _alarmasCacheadasParaTi = value;
                if (value != null)
                {
                    _ = GuardarCacheParaTi(value);
                }
            }
        }

        // NUEVO: Filtro de alarmas - tipos con estadísticas
        private static List<TipoAlarmaConEstadisticas>? _tiposAlarmaDisponibles = null;
        public static List<TipoAlarmaConEstadisticas>? TiposAlarmaDisponibles
        {
            get => _tiposAlarmaDisponibles;
            set => _tiposAlarmaDisponibles = value;
        }

        /// <summary>
        /// Retorna IDs de tipos de alarma habilitados para filtrar
        /// Si no hay filtros configurados, retorna todos
        /// </summary>
        public static HashSet<int> GetTiposHabilitados()
        {
            if (_tiposAlarmaDisponibles == null || _tiposAlarmaDisponibles.Count == 0)
            {
                // Sin filtros configurados = mostrar todos
                return new HashSet<int>();
            }

            var habilitados = _tiposAlarmaDisponibles
                .Where(t => t.EstaHabilitado)
                .Select(t => t.TipoalarmaId)
                .ToHashSet();

            return habilitados;
        }

        /// <summary>
        /// Verifica si un tipo de alarma está habilitado en el filtro
        /// </summary>
        public static bool EsTipoHabilitado(int tipoAlarmaId)
        {
            var habilitados = GetTiposHabilitados();

            // Si no hay filtros (HashSet vacío), todos están habilitados
            if (habilitados.Count == 0)
                return true;

            return habilitados.Contains(tipoAlarmaId);
        }

        /// <summary>
        /// Carga los tipos de alarma con estadísticas desde el API
        /// Debe llamarse después del login exitoso, cuando el JWT token ya está disponible
        /// </summary>
        public static async Task<bool> CargarTiposAlarmaConEstadisticas()
        {
            try
            {
                // Obtener tipos con estadísticas desde API
                var tipos = await ApiService.ObtenerTiposAlarmaConEstadisticas();

                if (tipos == null || tipos.Count == 0)
                    return false;

                // Cargar preferencias guardadas (tipos deshabilitados)
                await CargarPreferenciasFiltro(tipos);

                // Guardar en App
                TiposAlarmaDisponibles = tipos;

                return true;
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "App", "CargarTiposAlarmaConEstadisticas");
                return false;
            }
        }

        /// <summary>
        /// Carga las preferencias de filtro guardadas en Preferences
        /// Aplica el estado habilitado/deshabilitado a la lista de tipos
        /// </summary>
        private static async Task CargarPreferenciasFiltro(List<TipoAlarmaConEstadisticas> tipos)
        {
            try
            {
                var json = await SecureStorage.Default.GetAsync("tipos_alarma_deshabilitados") ?? "[]";
                var deshabilitados = JsonConvert.DeserializeObject<List<int>>(json) ?? new List<int>();

                foreach (var tipo in tipos)
                {
                    // Si está en la lista de deshabilitados, marcarlo como deshabilitado
                    tipo.EstaHabilitado = !deshabilitados.Contains(tipo.TipoalarmaId);
                }

            }
            catch (Exception ex)
            {
                // En caso de error, todos habilitados por defecto
                foreach (var tipo in tipos)
                {
                    tipo.EstaHabilitado = true;
                }
            }
        }

        // Event para compatibilidad con HomeViewModel
        public static event EventHandler FMCTokenChangedEvent;

        // TaskCompletionSource para Firebase token
        public TaskCompletionSource<string> tokenCompletionSource = new TaskCompletionSource<string>();

        // Nueva propiedad para alarmas pendientes
        private string _pendingAlarmaId = null;

        // NUEVA: Flag para evitar inicializar Firebase handlers multiples veces
        private bool _firebaseHandlersInitialized = false;

        public App()
        {
            try
            {
                InitializeComponent();

                // CAMBIO: Separar exception handlers en metodos propios
                AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
                TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

                // MAUI 10 breaking change: ContentPage ahora renderiza edge-to-edge por defecto.
                // Los estilos implícitos de App.xaml pueden no aplicarse a tiempo en algunos casos,
                // así que forzamos SafeAreaEdges.Container en cada ContentPage al aparecer.
                // Ref: https://github.com/dotnet/maui/issues/31925
                this.PageAppearing += OnPageAppearingSafeArea;

                // AdMob se inicializa automáticamente vía Plugin.AdMob (.UseAdMob() en MauiProgram.cs)

                // CAMBIO: Usar Color con RGB explicito en lugar de FromArgb
                if (DeviceInfo.Platform == DevicePlatform.Android)
                {
                    MainPage = new ContentPage
                    {
                        BackgroundColor = Color.FromRgba(26, 35, 126, 255),
                        Content = new ActivityIndicator
                        {
                            IsRunning = true,
                            Color = Color.FromRgba(255, 255, 255, 255),
                            VerticalOptions = LayoutOptions.Center,
                            HorizontalOptions = LayoutOptions.Center
                        }
                    };
                }
                else // iOS
                {
                    var loadingPage = new sospect.Views.LoadingPage();
                    NavigationPage.SetHasNavigationBar(loadingPage, false);
                    MainPage = new NavigationPage(loadingPage);
                }

                Connectivity.ConnectivityChanged += InternetUtil.Connectivity_ConnectivityChanged;

                // REMOVIDO: Ya NO se inicializan los Firebase handlers aqui
                // Se moveran a OnStart() para garantizar que Firebase este listo
            }
            catch (Exception ex)
            {
                // Log a archivo local como fallback
                try
                {
                    var crashLog = System.IO.Path.Combine(
                        FileSystem.AppDataDirectory,
                        $"crash_constructor_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
                    );
                    System.IO.File.WriteAllText(crashLog, $"{DateTime.Now}\n{ex}");
                }
                catch { }

                throw;
            }
        }

        // MAUI 10: Forzar SafeAreaEdges con Container en cada ContentPage que aparece.
        // Esto garantiza que el contenido respete las barras del sistema (status bar, navigation bar)
        // independientemente de si el estilo implícito se aplicó o no.
        // Nota: Usamos el constructor new SafeAreaEdges(SafeAreaRegions.Container) porque
        // la propiedad estática SafeAreaEdges.Container puede no resolver en todas las plataformas.
        private static readonly Microsoft.Maui.SafeAreaEdges _containerSafeArea =
            new Microsoft.Maui.SafeAreaEdges(Microsoft.Maui.SafeAreaRegions.Container);

        private void OnPageAppearingSafeArea(object sender, Page page)
        {
            if (page is ContentPage contentPage &&
                contentPage.SafeAreaEdges != _containerSafeArea)
            {
                contentPage.SafeAreaEdges = _containerSafeArea;
            }
        }

        // NUEVO: Metodo separado para exception handler
        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            try
            {
                var exception = args.ExceptionObject as Exception;
                if (exception != null)
                {
                    var errorLogger = DependencyService.Get<IErrorLogger>();
                    if (errorLogger != null)
                    {
                        errorLogger.LogError(exception, null);
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "App", "OnUnhandledException-Handler");
            }
        }

        // NUEVO: Metodo separado para task exception handler
        private void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs args)
        {
            try
            {
                var errorLogger = DependencyService.Get<IErrorLogger>();
                if (errorLogger != null)
                {
                    errorLogger.LogError(args.Exception, null);
                }
                args.SetObserved();
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "App", "OnUnobservedTaskException-Handler");
            }
        }

        // NUEVO: Inicializar Firebase handlers DESPUES de que Firebase este listo
        private async void InitializeFirebaseHandlers()
        {
            if (_firebaseHandlersInitialized) return;

            try
            {
                #if ANDROID
                // Firebase token management (solo Android - iOS usa SDK nativo)
                IFirebasePushNotification.Current.TokenRefreshed += async (s, p) =>
                {
                    try
                    {
                        App.TokenHubNotification = p.Token;

                        // Guardar en FMC_token, no en access_token
                        await SecureStorage.SetAsync("FMC_token", p.Token);

                        // Logica del TaskCompletionSource
                        if (!tokenCompletionSource.Task.IsCompleted)
                        {
                            tokenCompletionSource.SetResult(p.Token);
                        }

                        // Actualizar flag y disparar event
                        FMCTokenChanged = true;
                        FMCTokenChangedEvent?.Invoke(null, EventArgs.Empty);
                    }
                    catch (Exception ex)
                    {
                        CrashlyticsHelper.LogError(ex, "App", "App.OnTokenRefresh");
                    }
                };

                // Notificacion recibida (app en foreground)
                IFirebasePushNotification.Current.NotificationReceived += (s, p) =>
                {
                    try
                    {
                        if (p.Data != null)
                        {
                            // CRÍTICO: Mostrar notificación del sistema cuando la app está en foreground
                            // Firebase NO muestra notificaciones automáticamente en foreground, solo en background/cerrada
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                try
                                {
                                    // Obtener título, cuerpo, alarma_id, image_url y logo_url de la notificación
                                    var title = p.Data.ContainsKey("title") ? p.Data["title"].ToString() : "⚠️ SOSpect Alert";
                                    var body = p.Data.ContainsKey("body") ? p.Data["body"].ToString() : "Nueva alarma cerca de ti";
                                    var alarmaId = p.Data.ContainsKey("alarma_id") ? p.Data["alarma_id"].ToString() : "0";
                                    var imageUrl = p.Data.ContainsKey("image_url") ? p.Data["image_url"].ToString() : null;
                                    var logoUrl = p.Data.ContainsKey("logo_url") ? p.Data["logo_url"].ToString() : null;
                                    // 2026-04-11: Capturar chat_id del payload para incluirlo en el intent de la notificación local
                                    var chatIdLocal = p.Data.ContainsKey("chat_id") ? p.Data["chat_id"].ToString() : "0";

                                    // Suprimir si el usuario está en el chat de publicidad de esta alarma
                                    if (ChatPublicidadAbierto
                                        && long.TryParse(alarmaId, out long alarmaIdParsed)
                                        && ChatPublicidadAlarmaIdActivo == alarmaIdParsed)
                                    {
                                        return;
                                    }

                                    // Mostrar notificación del sistema usando el servicio nativo
                                    #if ANDROID
                                    var notificationService = new sospect.Platforms.Android.Services.LocalNotificationService();
                                    // Pasar chat_id para que quede en el intent y sea recuperable en NotificationOpened
                                    notificationService.SendNotification(title, body, alarmaId, imageUrl, logoUrl, chatIdLocal);
                                    #endif
                                }
                                catch (Exception ex)
                                {
                                    CrashlyticsHelper.LogError(ex, "App", "NotificationReceived-LocalNotif");
                                }
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        CrashlyticsHelper.LogError(ex, "App", "App.OnNotificationReceived");
                    }
                };

                // CRITICO: Notificacion tocada (app cerrada o background)
                IFirebasePushNotification.Current.NotificationOpened += (s, p) =>
                {
                    try
                    {
                        if (p.Data != null && p.Data.ContainsKey("alarma_id"))
                        {
                            var alarmaId = p.Data["alarma_id"].ToString();

                            // Guardar chat_id si viene en la notificación (notificaciones de chat)
                            var chatIdStr = p.Data.ContainsKey("chat_id") ? p.Data["chat_id"].ToString() : "0";

                            // Guardar para procesamiento posterior
                            Preferences.Set("alarma_id", alarmaId);
                            Preferences.Set("chat_id_notif", chatIdStr);
                            _pendingAlarmaId = alarmaId;

                            // Intentar navegar inmediatamente
                            NavigateToAlarma(alarmaId);
                        }
                    }
                    catch (Exception ex)
                    {
                        CrashlyticsHelper.LogError(ex, "App", "OnNotificationOpened-Processing");
                    }
                };

                // Accion de notificacion (botones personalizados)
                IFirebasePushNotification.Current.NotificationAction += (s, p) =>
                {
                    try
                    {
                        if (p.Data != null && p.Data.ContainsKey("alarma_id"))
                        {
                            var alarmaId = p.Data["alarma_id"].ToString();
                            NavigateToAlarma(alarmaId);
                        }
                    }
                    catch (Exception ex)
                    {
                        CrashlyticsHelper.LogError(ex, "App", "OnNotificationAction-Processing");
                    }
                };

                // Error handler de notificaciones
                // NOTA: NotificationError no existe en Plugin.FirebasePushNotifications
                // Los errores se manejan vía try-catch en cada handler

                _firebaseHandlersInitialized = true;

                // Intentar obtener el token actual si ya está disponible
                try
                {
                    var currentToken = IFirebasePushNotification.Current.Token;
                    if (!string.IsNullOrEmpty(currentToken))
                    {
                        App.TokenHubNotification = currentToken;
                        await SecureStorage.SetAsync("FMC_token", currentToken);

                        if (!tokenCompletionSource.Task.IsCompleted)
                        {
                            tokenCompletionSource.SetResult(currentToken);
                        }

                        FMCTokenChanged = true;
                        FMCTokenChangedEvent?.Invoke(null, EventArgs.Empty);
                    }
                }
                catch (Exception ex)
                {
                    CrashlyticsHelper.LogError(ex, "App", "Firebase-GetCurrentToken");
                }

                #else
                // Otras plataformas (Windows, MacCatalyst, etc.)
                _firebaseHandlersInitialized = true;
                #endif
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "App", "InitializeFirebaseHandlers");
            }
        }

        // Metodo seguro para navegar a alarma (public para acceso desde MainActivity y AppDelegate)
        public void NavigateToAlarma(string alarmaId)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    // Validar que alarmaId sea valido
                    if (string.IsNullOrEmpty(alarmaId))
                    {
                        return;
                    }

                    // Verificar que MainPage este inicializado
                    if (MainPage == null)
                    {
                        _pendingAlarmaId = alarmaId;
                        return;
                    }

                    // Obtener Navigation correctamente desde TabbedPage
                    INavigation navigation = GetCurrentNavigation();
                    if (navigation == null)
                    {
                        _pendingAlarmaId = alarmaId;

                        // iOS cold start: DidReceiveNotificationResponse puede llamarse antes de que
                        // SetupMainPageAsync termine. Hacer polling hasta que Navigation esté disponible
                        // (máximo 5 segundos) en lugar de rendirse y depender de _pendingAlarmaId.
                        const int maxWaitMs = 5000;
                        const int pollIntervalMs = 200;
                        int elapsed = 0;
                        while (elapsed < maxWaitMs)
                        {
                            await Task.Delay(pollIntervalMs);
                            elapsed += pollIntervalMs;
                            navigation = GetCurrentNavigation();
                            if (navigation != null)
                            {
                                _pendingAlarmaId = null;
                                break;
                            }
                        }

                        if (navigation == null)
                            return;
                    }

                    // 2026-04-17: Zona crítica (alarma_id=0) → abrir HomePage (mapa principal)
                    // Estas notificaciones son del tipo "estás llegando a una zona con alertas"
                    // y no corresponden a ninguna alarma específica, por lo que el destino correcto es el mapa.
                    if (alarmaId == "0")
                    {
                        Preferences.Set("alarma_id", "");
                        await navigation.PushAsync(new Views.HomePage());
                        return;
                    }

                    // Verificar si la alarma es una Promoción local (categoría 13)
                    // Si es así, navegar a DetallePromocionVistaPage en lugar de HistorialPage
                    long alarmaIdLong = long.Parse(alarmaId);

                    // 2026-04-11: Usar tipoalarma_id==13 (Promoción local) en lugar de CategoriaAlarmaId==13 (era incorrecto).
                    // tipoalarma_id=13 → categoria_alarma_id=6 (Publicidad); la categoría NO es 13.
                    AlarmaCercana alarmaPromocion = null;
                    if (AlarmasCacheadas != null)
                    {
                        var encontrada = AlarmasCacheadas.FirstOrDefault(a => a.alarma_id == alarmaIdLong);
                        if (encontrada?.tipoalarma_id == 13)
                            alarmaPromocion = encontrada;
                    }
                    if (alarmaPromocion == null && AlarmasCacheadasParaTi != null)
                    {
                        var encontradaParaTi = AlarmasCacheadasParaTi.FirstOrDefault(a => a.alarma_id == alarmaIdLong);
                        if (encontradaParaTi?.tipoalarma_id == 13)
                            alarmaPromocion = encontradaParaTi;
                    }

                    var chatIdNotifStr = Preferences.Get("chat_id_notif", "0");
                    long.TryParse(chatIdNotifStr, out long chatIdNotif);

                    if (alarmaPromocion != null)
                    {
                        // Regla simple: si la notificación trae chat_id → ir directo al chat.
                        // Si no trae chat_id → mostrar el detalle de la promoción.
                        // No importa si el usuario es proveedor o cliente: la regla es la misma.
                        if (chatIdNotif > 0)
                        {
                            Preferences.Set("chat_id_notif", "0");
                            await navigation.PushAsync(new Views.DetallePromocionVistaPage(alarmaPromocion));
                            await navigation.PushAsync(new Views.ChatPublicidadPage(alarmaIdLong, chatIdNotif));
                        }
                        else
                        {
                            Preferences.Set("chat_id_notif", "0");
                            await navigation.PushAsync(new Views.DetallePromocionVistaPage(alarmaPromocion));
                        }
                    }
                    else
                    {
                        if (chatIdNotif > 0)
                        {
                            Preferences.Set("chat_id_notif", "0");
                            await navigation.PushAsync(new Views.ChatPublicidadPage(alarmaIdLong, chatIdNotif));
                        }
                        else
                        {
                            Preferences.Set("chat_id_notif", "0");
                            await navigation.PushAsync(new HistorialPage(alarmaIdLong));
                        }
                    }

                    // Limpiar la alarma pendiente
                    _pendingAlarmaId = null;
                    Preferences.Set("alarma_id", "");
                }
                catch (FormatException ex)
                {
                    CrashlyticsHelper.LogError(ex, "App", "NavigateToAlarma-FormatException");
                }
                catch (Exception ex)
                {
                    CrashlyticsHelper.LogError(ex, "App", "NavigateToAlarma");
                }
            });
        }

        // Helper method para obtener Navigation desde TabbedPage (mismo patrón que DetalleMensajeViewModel)
        private INavigation GetCurrentNavigation()
        {
            try
            {
                if (MainPage is TabbedPage tabbedPage)
                {
                    // Obtener la página actual del TabbedPage
                    var currentPage = tabbedPage.CurrentPage;

                    // Si es NavigationPage, devolver su Navigation
                    if (currentPage is NavigationPage navPage)
                    {
                        return navPage.Navigation;
                    }

                    // Si la página actual tiene Navigation, devolverlo
                    if (currentPage?.Navigation != null)
                    {
                        return currentPage.Navigation;
                    }
                }

                // Fallback: usar MainPage.Navigation si está disponible
                return MainPage?.Navigation;
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "App", "GetCurrentNavigation");
                return null;
            }
        }

        public async Task<string> GetFirebaseTokenAsync()
        {
            try
            {
                if (!string.IsNullOrEmpty(App.TokenHubNotification))
                {
                    return App.TokenHubNotification;
                }

                return await tokenCompletionSource.Task;
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "App", "GetFirebaseTokenAsync");
                throw;
            }
        }

        /// <summary>
        /// Método público para que AppDelegate notifique cuando el token de Firebase está disponible
        /// </summary>
        public void NotifyFirebaseTokenAvailable(string token)
        {
            try
            {
                App.TokenHubNotification = token;

                // Completar TaskCompletionSource
                if (!tokenCompletionSource.Task.IsCompleted)
                    tokenCompletionSource.SetResult(token);

                // Disparar eventos
                App.FMCTokenChanged = true;
                FMCTokenChangedEvent?.Invoke(null, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "App", "NotifyFirebaseTokenAvailable");
            }
        }

        private async Task SetupMainPageAsync()
        {
            try
            {
                string versionCliente = AppInfo.VersionString; // Ej: "2.0.75"

                // CRÍTICO: Extraer solo el último número (el build/patch number)
                string versionNumero = ExtraerNumeroVersion(versionCliente);


                VersionVerificada IsValidVersion = await ApiService.VerificarVersion(int.Parse(versionNumero));

                if (IsValidVersion.flag_soportada)
                {
                    await ConfigureAppBasedOnUserPreferences();
                }
                else
                {
                    await ShowUpdateAlertAndRedirect();
                }
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is WebException || ex is TimeoutException)
            {
                CrashlyticsHelper.LogError(ex, "App", "SetupMainPageAsync-HttpRequestException");
                await ShowConnectivityErrorAlert();
                await ConfigureAppBasedOnUserPreferences();
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "App", "SetupMainPageAsync-Others");
                await ConfigureAppBasedOnUserPreferences();
            }
        }

        // NUEVO MÉTODO: Extraer el número de versión para compatibilidad con el sistema anterior
        private string ExtraerNumeroVersion(string versionCompleta)
        {
            try
            {
                // Dividir la versión por puntos: "2.0.75" -> ["2", "0", "75"]
                var partes = versionCompleta.Split('.');

                // Retornar el último número (el build/patch number)
                if (partes.Length > 0)
                {
                    return partes[partes.Length - 1]; // "75"
                }

                // Si no tiene puntos, asumir que ya es el formato correcto
                return versionCompleta;
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "App", "ExtraerNumeroVersion");
                return versionCompleta;
            }
        }

        private async Task ShowConnectivityErrorAlert()
        {
            var LabelInformacion = TranslateExtension.Translate("LabelInformacion");
            var LabelCheckConnection = TranslateExtension.Translate("LabelCheckConnection");
            var LabelOK = TranslateExtension.Translate("LabelOK");

            await ModernAlerts.ShowWarning(LabelInformacion, LabelCheckConnection);
        }

        private async Task ShowUpdateAlertAndRedirect()
        {
            var LabelInformacion = TranslateExtension.Translate("LabelInformacion");
            var LabelOK = TranslateExtension.Translate("LabelOK");
            var LabelNuevaVersion = TranslateExtension.Translate("LabelNuevaVersion");

            await ModernAlerts.ShowInfo(LabelInformacion, LabelNuevaVersion);

            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                await Launcher.TryOpenAsync("https://play.google.com/store/apps/details?id=com.wescotcorp.sospect");
            }
            else if (DeviceInfo.Platform == DevicePlatform.iOS)
            {
                await Launcher.TryOpenAsync("itms-apps://itunes.apple.com/us/app/apple-store/com.wescotcorp.sospect");
            }
        }

        public async Task<bool> IsTokenExpiredAsync()
        {
            try
            {
                var token = await SecureStorage.GetAsync("access_token");
                if (string.IsNullOrEmpty(token))
                {
                    return true;
                }

                var jwtToken = new JwtSecurityTokenHandler().ReadToken(token) as JwtSecurityToken;
                if (jwtToken == null)
                {
                    Debug.WriteLine("El token no es un JWT valido.");
                    return true;
                }

                return jwtToken.ValidTo < DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "App", "IsTokenExpiredAsync", new Dictionary<string, string> {
                    { "ErrorMessage", ex.Message }
                });
                Debug.WriteLine($"Error al verificar el token: {ex.Message}");
                return true;
            }
        }

        private async Task CerrarSesion()
        {
            var LabelInformacion = TranslateExtension.Translate("LabelInformacion");
            var LabelOK = TranslateExtension.Translate("LabelOK");
            var LabelSessionExpired = TranslateExtension.Translate("LabelSessionExpired");

            // Limpiar caché D (pines persistentes de persecución) al cerrar sesión
            LimpiarPinesPersistentes();

            await SecureStorage.SetAsync("access_token", "");
            Preferences.Set("alarma_id", "");
            Preferences.Set("HasSeenTutorial", false);
            Preferences.Set("ParametrosUsuario", "");
            Preferences.Set("User", "");

            // Limpiar token de Firebase
            #if ANDROID
            await IFirebasePushNotification.Current.UnregisterForPushNotificationsAsync();
            #endif

            var mainPage = new NavigationPage(new LoginPage())
            {
                BarBackgroundColor = Color.FromArgb("#1A237E"),
                BarTextColor = Colors.White
            };

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await ModernAlerts.ShowWarning(LabelInformacion, LabelSessionExpired);
                MainPage = mainPage;
            });
        }

        private async Task ConfigureAppBasedOnUserPreferences()
        {
            var LabelOK = TranslateExtension.Translate("LabelOK");
            var LabelError = TranslateExtension.Translate("LabelError");
            var LblErrorInicializandoParametros = TranslateExtension.Translate("LblErrorInicializandoParametros");

            if (Preferences.Get("User", "") != string.Empty)
            {
                var isTokenExpired = await IsTokenExpiredAsync();
                if (isTokenExpired)
                {
                    await CerrarSesion();
                }
                else
                {
                    try
                    {
                        App.ubicacionActual = new Ubicaciones();
                        persona = JsonConvert.DeserializeObject<Persona>(Preferences.Get("User", ""));
                        ubicacionActual.p_user_id_thirdparty = persona.user_id_thirdparty;
                        ubicacionActual.Idioma = persona.Idioma;
                        ubicacionActual.Pais = persona.Pais;

                        try
                        {
                            var parametrosInicializados = await HomeViewModel.InicializarParametrosUsuarioAsync();
                            if (parametrosInicializados)
                            {
                                MainThread.BeginInvokeOnMainThread(() =>
                                {
                                    MainPage = new SospectTabs();

                                    // Procesar alarma pendiente despues de inicializar
                                    if (!string.IsNullOrEmpty(_pendingAlarmaId))
                                    {
                                        NavigateToAlarma(_pendingAlarmaId);
                                    }
                                });
                            }
                            else
                            {
                                await ModernAlerts.ShowError(LabelError, LblErrorInicializandoParametros);
                                await CerrarSesion();
                            }
                        }
                        catch (Exception ex)
                        {
                            CrashlyticsHelper.LogError(ex, "App", "ConfigureAppBasedOnUserPreferences-InicializarParametros");
                            await CerrarSesion();
                        }
                    }
                    catch (Exception ex)
                    {
                        CrashlyticsHelper.LogError(ex, "App", "ConfigureAppBasedOnUserPreferences-Others");
                        await CerrarSesion();
                    }
                }
            }
            else
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    MainPage = new NavigationPage(new LoginPage())
                    {
                        BarBackgroundColor = Color.FromArgb("#1A237E"),
                        BarTextColor = Colors.White
                    };
                });
            }
        }

        protected async override void OnStart()
        {
            try
            {
                // CAMBIO CRITICO: Dar tiempo a Firebase para inicializarse completamente
                await Task.Delay(500);

                // CAMBIO CRITICO: Inicializar Firebase handlers AQUI (Firebase ya esta listo)
                InitializeFirebaseHandlers();

                // NUEVO: Iniciar servicio de ubicación en segundo plano
                await IniciarServicioUbicacion();

                // 21022026: Marcar que es arranque desde cerrada para que HomePage
                // ejecute RefrescarAmbosFeeds (Feed A + Feed B secuencial) al aparecer.
                // OnResume NO setea este flag, por lo que solo se activa al abrir desde cerrada.
                EsPrimerArranque = true;

#if IOS
                // iOS: Inicializar consentimiento UMP antes de navegar a HomePage.
                // Plugin.AdMob verifica CanRequestAds() antes de cargar ads; si el estado
                // UMP es Unknown (nunca se llamó RequestConsentInfoUpdate), los ads se bloquean
                // silenciosamente. En Colombia (fuera de EEA) resuelve como NotRequired de inmediato.
                await InicializarConsentimientoAdMobiOS();
#endif

                // Continuar con setup normal
                await SetupMainPageAsync();

                // Procesar alarma pendiente de notificacion
                if (!string.IsNullOrEmpty(Preferences.Get("alarma_id", "")) && Preferences.Get("alarma_id", "") != "0")
                {
                    await Task.Delay(500);
                    var alarmaId = Preferences.Get("alarma_id", "");
                    NavigateToAlarma(alarmaId);
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "App", "OnStart");
            }
        }

#if IOS
        /// <summary>
        /// Resuelve el estado de consentimiento UMP en iOS antes de que se muestren anuncios.
        /// Plugin.AdMob bloquea silenciosamente los ads cuando ConsentStatus == Unknown.
        /// En regiones fuera de EEA (Colombia), resuelve como NotRequired de inmediato.
        /// </summary>
        private async Task InicializarConsentimientoAdMobiOS()
        {
            try
            {

                var consentService = Microsoft.Maui.IPlatformApplication.Current?.Services?
                    .GetService<IAdConsentService>();

                if (consentService == null)
                    return;

                var tcs = new TaskCompletionSource<bool>();

                // Suscribirse a OnConsentInfoUpdated para saber cuando UMP resuelve el estado.
                // Plugin.AdMob 3.0.2 usa EventHandler<IConsentInformation?> para este evento.
                EventHandler<Plugin.AdMob.IConsentInformation?> onUpdated = null;
                onUpdated = (sender, info) =>
                {
                    consentService.OnConsentInfoUpdated -= onUpdated;
                    tcs.TrySetResult(true);
                };
                consentService.OnConsentInfoUpdated += onUpdated;

                // Disparar el flujo de consentimiento UMP
                consentService.LoadAndShowConsentFormIfRequired();

                // Esperar hasta 3 segundos (fuera de EEA resuelve en ~200ms)
                await Task.WhenAny(tcs.Task, Task.Delay(3000));

                if (!tcs.Task.IsCompleted)
                    consentService.OnConsentInfoUpdated -= onUpdated;
            }
            catch (Exception)
            {
                // No bloquear el arranque si hay error en consentimiento
            }
        }
#endif

        private async Task IniciarServicioUbicacion()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("========================================");
                System.Diagnostics.Debug.WriteLine($"[App] IniciarServicioUbicacion LLAMADO: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

                // CAMBIO: Intentar obtener userId de múltiples fuentes
                var userId = await SecureStorage.GetAsync("user_id_thirdparty");
                if (string.IsNullOrEmpty(userId) && persona != null)
                {
                    userId = persona.user_id_thirdparty;
                    System.Diagnostics.Debug.WriteLine($"[App] UserId obtenido desde App.persona: {userId}");

                    // Guardar en SecureStorage para el servicio en segundo plano
                    if (!string.IsNullOrEmpty(userId))
                    {
                        await SecureStorage.SetAsync("user_id_thirdparty", userId);
                        System.Diagnostics.Debug.WriteLine("[App] UserId guardado en SecureStorage");
                    }
                }
                else if (!string.IsNullOrEmpty(userId))
                {
                    System.Diagnostics.Debug.WriteLine($"[App] UserId obtenido desde SecureStorage: OK");
                }

                if (string.IsNullOrEmpty(userId))
                {
                    System.Diagnostics.Debug.WriteLine("[App] No hay usuario logueado (ni en SecureStorage ni en App.persona), no se inicia servicio de ubicación");
                    System.Diagnostics.Debug.WriteLine("========================================");
                    return;
                }

                // Obtener servicio del DI
                var backgroundService = Handler?.MauiContext?.Services.GetService<IBackgroundService>();
                if (backgroundService == null)
                {
                    System.Diagnostics.Debug.WriteLine("[App] ERROR: IBackgroundService no está registrado");
                    System.Diagnostics.Debug.WriteLine("========================================");
                    return;
                }

                System.Diagnostics.Debug.WriteLine("[App] IBackgroundService obtenido, iniciando servicio...");

                // Iniciar seguimiento en segundo plano
                await backgroundService.RunCodeInBackgroundMode(
                    ApiService.ActualizarUbicacion,
                    "LocationTracking"
                );

                System.Diagnostics.Debug.WriteLine("[App] ✓ Servicio de ubicación en segundo plano iniciado correctamente");
                System.Diagnostics.Debug.WriteLine("========================================");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("========================================");
                System.Diagnostics.Debug.WriteLine("[App] ✗ ERROR iniciando servicio");
                System.Diagnostics.Debug.WriteLine($"[App] Message: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[App] StackTrace: {ex.StackTrace}");
                System.Diagnostics.Debug.WriteLine("========================================");
                CrashlyticsHelper.LogError(ex, "App", "IniciarServicioUbicacion");
            }
        }

        protected override void OnSleep()
        {
        }

        protected async override void OnResume()
        {

            try
            {
                bool shouldRecreateApp = false;

                // Verificar si necesitamos recrear la app
                if (MainPage == null || Preferences.Get("User", "") == string.Empty)
                {
                    shouldRecreateApp = true;
                }
                else
                {
                    // Verificar si el token expiro (solo si hay usuario)
                    var isTokenExpired = await IsTokenExpiredAsync();
                    if (isTokenExpired)
                        shouldRecreateApp = true;
                }

                if (shouldRecreateApp)
                {
                    await SetupMainPageAsync();
                }
                else
                {

                    // CIERRE-ENCUESTA FIX: Al volver del background, limpiar el stack del tab
                    // "Describir" (índice 1) para que CierreEncuestaPage u otras páginas de votación
                    // que hayan quedado en el stack no interfieran con la navegación posterior.
                    // Esto es crítico en iOS donde MAUI restaura el stack al volver al foreground.
                    await LimpiarStackTabDescribirAsync();
                }

                // IMPORTANTE: Manejar alarma pendiente al volver al foreground.
                // iOS: DidReceiveNotificationResponse (AppDelegate) ya llama NavigateToAlarma directamente
                //      cuando el usuario toca una notificación. OnResume NO debe interferir porque
                //      causaría doble navegación o navegación con alarma_id ya limpiado.
                // Android: OnResume se dispara ANTES que NotificationOpened (Firebase), por eso
                //          se usa el delay de 800ms para dar tiempo al handler de guardar chat_id_notif.
#if !IOS
                var alarmaIdPref = Preferences.Get("alarma_id", "");

                if (!string.IsNullOrEmpty(alarmaIdPref) && alarmaIdPref != "0")
                {
                    // 2026-04-10: En Android, OnResume se dispara ANTES que NotificationOpened.
                    // Esperamos 800ms para dar tiempo al handler NotificationOpened de guardar
                    // chat_id_notif en Preferences antes de que NavigateToAlarma lo lea.
                    await Task.Delay(800);
                    NavigateToAlarma(Preferences.Get("alarma_id", ""));
                }
#else
                // iOS: alarma pendiente manejada por DidReceiveNotificationResponse, OnResume no interfiere
#endif

                // Manejar permisos de notificacion iOS
                if (!shouldRecreateApp && DeviceInfo.Platform == DevicePlatform.iOS && justCheckedNotificationPermissions)
                {
                    var hasNotificationPermission = await HasNotificationPermission();

                    if (!hasNotificationPermission)
                    {
                        var LblDebesHabilitarNotif = TranslateExtension.Translate("LblDebesHabilitarNotif");
                        await ModernAlerts.ShowWarning("SOSpect", LblDebesHabilitarNotif);
                        DependencyService.Get<ISettingsService>().OpenSettings();
                        justCheckedNotificationPermissions = false;
                    }
                    else
                    {
                        DependencyService.Get<ISettingsService>().RegisterDeviceAgain();
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "App", "OnResume");

                // En caso de error, recrear la app
                await SetupMainPageAsync();
            }
        }

        public Task<bool> HasNotificationPermission()
        {
            return DependencyService.Get<IPermissionManager>().CheckNotificationPermission();
        }

        /// <summary>
        /// Limpia el stack del tab "Describir" (índice 1) en SospectTabs al volver del background.
        /// Evita que CierreEncuestaPage, VerHistorialAlarmaPage u otras páginas de votación
        /// queden en el stack y se restauren automáticamente al volver al foreground (crítico en iOS).
        /// </summary>
        private async Task LimpiarStackTabDescribirAsync()
        {
            try
            {
                if (MainPage is SospectTabs tabbedPage && tabbedPage.Children.Count > 1)
                {
                    var describirNavPage = tabbedPage.Children[1] as NavigationPage;
                    if (describirNavPage != null && describirNavPage.Navigation.NavigationStack.Count > 1)
                    {
                        // 2026-04-10: NO limpiar si el tope del stack es ChatPublicidadPage
                        // (el usuario estaba en un chat activo y salió 5 segundos - no debemos cerrarlo)
                        var topPage = describirNavPage.Navigation.NavigationStack.LastOrDefault();
                        if (topPage is Views.ChatPublicidadPage)
                            return;

                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            await describirNavPage.Navigation.PopToRootAsync(animated: false);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "App", "LimpiarStackTabDescribirAsync");
            }
        }

        // ==========================================
        // NUEVO: Métodos para caché persistente de alarmas (Diseño Twitter/X)
        // ==========================================

        /// <summary>
        /// Carga las alarmas del caché local (archivo JSON)
        /// </summary>
        public static async Task<List<AlarmaCercana>?> CargarAlarmasDesdeCache()
        {
            try
            {
                var cacheFilePath = Path.Combine(FileSystem.CacheDirectory, CacheFileName);

                if (!File.Exists(cacheFilePath))
                    return null;

                var jsonContent = await File.ReadAllTextAsync(cacheFilePath);

                if (string.IsNullOrWhiteSpace(jsonContent))
                    return null;

                // PATRÓN DTO: Deserializar a DTOs desde caché (objetos planos, sin referencias nativas)
                var dtos = JsonConvert.DeserializeObject<List<AlarmaCercanaDto>>(jsonContent);

                // Convertir DTOs a ViewModels para uso en UI
                var alarmas = dtos?.Select(dto => new AlarmaCercana(dto)).ToList();

                // Blindaje 2026-04: este path escribe al field directo (bypassa el setter público),
                // así que reaplicamos manualmente la verdad local del proponente de cierre.
                ReaplicarFlagsLocalesProponente(alarmas);

                _alarmasCacheadas = alarmas;
                return alarmas;
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "App", "CargarAlarmasDesdeCache");
                return null;
            }
        }

        /// <summary>
        /// Guarda las alarmas en el caché local (archivo JSON)
        /// IMPORTANTE: Convierte ViewModels a DTOs antes de serializar para evitar crash JNI
        /// </summary>
        public static async Task GuardarAlarmasEnCache(List<AlarmaCercana> alarmas)
        {
            try
            {
                if (alarmas == null || alarmas.Count == 0)
                    return;

                var cacheFilePath = Path.Combine(FileSystem.CacheDirectory, CacheFileName);

                // PATRÓN DTO: Convertir ViewModels a DTOs antes de serializar
                // Esto evita serializar objetos con INotifyPropertyChanged y referencias nativas JNI
                var dtos = alarmas.Select(a => a.ToDto()).ToList();
                var jsonContent = JsonConvert.SerializeObject(dtos);

                await File.WriteAllTextAsync(cacheFilePath, jsonContent);
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "App", "GuardarAlarmasEnCache");
            }
        }

        /// <summary>
        /// Limpia el caché de alarmas
        /// </summary>
        public static async Task LimpiarCacheAlarmas()
        {
            try
            {
                var cacheFilePath = Path.Combine(FileSystem.CacheDirectory, CacheFileName);

                if (File.Exists(cacheFilePath))
                    File.Delete(cacheFilePath);

                _alarmasCacheadas = null;
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "App", "LimpiarCacheAlarmas");
            }
        }

        /// <summary>
        /// Refresca las alarmas desde el API y actualiza el caché
        /// Puede ser llamado desde cualquier parte de la app (ej: pull-to-refresh)
        /// </summary>
        /// <summary>
        /// Refresca Cache A ("Siguiendo" + Mapa). Siempre REEMPLAZA — sin LIMIT en backend (04-02-2026).
        /// </summary>
        public static async Task<bool> RefrescarAlarmasDesdeAPI()
        {
            try
            {
                if (DescribirPageActiva)
                    return false;

                if (ubicacionActual == null || persona == null)
                    return false;

                ubicacionActual.p_user_id_thirdparty = persona.user_id_thirdparty;
                ubicacionActual.PantallaOrigen = "HomePage";
                ubicacionActual.Pais = persona.Pais;

                var alarmasFrescas = await ApiService.ObtenerFeedSiguiendo(ubicacionActual);

                if (alarmasFrescas != null && alarmasFrescas.Count > 0)
                {
                    await LimpiarCacheAlarmas();
                    AlarmasCacheadas = alarmasFrescas;
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "App", "RefrescarAlarmasDesdeAPI");
                return false;
            }
        }

        /// <summary>
        /// Refresca Cache B ("Para ti") desde el endpoint separado.
        /// Siempre REEMPLAZA (no append). El backend retorna el top fresco con decay temporal.
        /// </summary>
        public static async Task<bool> RefrescarFeedParaTi()
        {
            try
            {
                if (DescribirPageActiva)
                    return false;

                if (ubicacionActual == null || persona == null)
                    return false;

                ubicacionActual.p_user_id_thirdparty = persona.user_id_thirdparty;
                ubicacionActual.Idioma = IdiomUtil.ObtenerCodigoDeIdioma();

                int seed = Random.Shared.Next();
                var alarmasFrescas = await ApiService.ObtenerFeedParaTi(ubicacionActual, seed);

                if (alarmasFrescas != null && alarmasFrescas.Count > 0)
                {
                    AlarmasCacheadasParaTi = alarmasFrescas; // auto-save via property
                    // 21022026: Renovar seed cliente para que AplicarDiversidad produzca orden distinto en cada carga
                    Helpers.RankingDiversidadHelper.RegenerarSeed();
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "App", "RefrescarFeedParaTi");
                return false;
            }
        }

        /// <summary>
        /// Orquestador secuencial 21022026: ejecuta Feed A completo primero, luego Feed B.
        ///
        /// Usar ÚNICAMENTE en tres situaciones:
        ///   1. App abre desde CERRADA (HomePage.OnAppearing con EsPrimerArranque=true)
        ///   2. Botón Refresh en HomePage
        ///   3. Pull-to-refresh en lista "Para ti" de DescribirPage
        ///
        /// NO usar en: timer 15s, tras lanzar alarma, pull-to-refresh de "Siguiendo".
        ///
        /// Si Feed A falla (sin red), Feed B NO se ejecuta (comportamiento conservador).
        /// </summary>
        public static async Task<(bool feedA, bool feedB)> RefrescarAmbosFeeds(bool ejecutarFeedBSiFeedAFalla = false)
        {
            bool feedAExitoso = false;
            bool feedBExitoso = false;
            try
            {
                feedAExitoso = await RefrescarAlarmasDesdeAPI();

                // 2026-03-01: Feed B desacoplado de Feed A. Para Ti tiene su propia vista/endpoint
                // independiente. Un fallo de Siguiendo no debe impedir cargar Para Ti.
                feedBExitoso = await RefrescarFeedParaTi();
                _ = ejecutarFeedBSiFeedAFalla; // parámetro conservado por compatibilidad, ya no controla ejecución
                return (feedAExitoso, feedBExitoso);
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "App", "RefrescarAmbosFeeds");
                return (feedAExitoso, feedBExitoso);
            }
        }

        // ─── CACHE C: MAPA ────────────────────────────────────────────────────────
        // Creado: 2026-03-01 — Rediseño Viewport-Driven del mapa

        /// <summary>
        /// Carga el Cache C (mapa) desde disco.
        /// Retorna null si no existe o el JSON está corrupto.
        /// </summary>
        public static async Task<MapaCacheDto?> CargarMapaDesdeCache()
        {
            try
            {
                var cacheFilePath = Path.Combine(FileSystem.CacheDirectory, CacheFileNameMapa);
                if (!File.Exists(cacheFilePath)) return null;

                var jsonContent = await File.ReadAllTextAsync(cacheFilePath);
                if (string.IsNullOrWhiteSpace(jsonContent)) return null;

                _cacheMapa = JsonConvert.DeserializeObject<MapaCacheDto>(jsonContent);

                // Descartar el cache si tiene más de 90 minutos de antigüedad.
                // Los pines del viewport son válidos solo mientras la BD los devuelva activos;
                // pasada esa ventana, los datos son obsoletos y causarían pines fantasma en el mapa.
                if (_cacheMapa?.GuardadoEn != null
                    && (DateTime.UtcNow - _cacheMapa.GuardadoEn.ToUniversalTime()) > TimeSpan.FromMinutes(90))
                {
                    _cacheMapa = null;
                    return null;
                }

                return _cacheMapa;
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "App", "CargarCacheMapa");
                return null;
            }
        }

        /// <summary>Guarda Cache C en archivo persistente.</summary>
        private static async Task GuardarCacheMapa(MapaCacheDto cache)
        {
            try
            {
                if (cache?.Pines == null || cache.Pines.Count == 0) return;
                var cacheFilePath = Path.Combine(FileSystem.CacheDirectory, CacheFileNameMapa);
                var jsonContent = JsonConvert.SerializeObject(cache);
                await File.WriteAllTextAsync(cacheFilePath, jsonContent);
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "App", "GuardarCacheMapa");
            }
        }

        /// <summary>
        /// Llama GET /Ubicaciones/PinesMapa con el viewport dado, actualiza CacheMapa
        /// y guarda en disco. Retorna true si tuvo éxito.
        /// </summary>
        public static async Task<bool> RefrescarMapaDesdeAPI(
            decimal minLat, decimal maxLat, decimal minLon, decimal maxLon, int zoom)
        {
            try
            {
                // Leer coordenadas del usuario para calcular distancia_en_metros en el servidor
                decimal userLat = ubicacionActual != null ? (decimal)ubicacionActual.latitud : 0;
                decimal userLon = ubicacionActual != null ? (decimal)ubicacionActual.longitud : 0;

                var response = await ApiService.ObtenerPinesMapa(minLat, maxLat, minLon, maxLon, zoom, userLat, userLon);
                if (response == null)
                {
                    return false;
                }

                List<Models.PinMapaDto> pines;

                if (response.tipo == "pines")
                {
                    // Zoom >= 15: pines individuales — usar directo
                    pines = response.Pines ?? new List<Models.PinMapaDto>();
                }
                else
                {
                    // Zoom <= 14: la API devuelve clusters (ST_SnapToGrid).
                    // Convertir cada ClusterMapaDto a un PinMapaDto sintético usando
                    // el centroide del cluster como posición y tipoalarma_id para el ícono.
                    // estado_alarma = true (activa) porque vw_pines_mapa solo incluye activas
                    // o cerradas en los últimos 90 min, y los clusters los agrupamos como activos.
                    var clusters = response.Clusters ?? new List<Models.ClusterMapaDto>();
                    pines = clusters.Select(c => new Models.PinMapaDto
                    {
                        alarma_id     = 0,              // sintético — no tiene alarma_id individual
                        latitud       = c.latitud_centro,
                        longitud      = c.longitud_centro,
                        tipoalarma_id = c.tipoalarma_id,
                        estado_alarma = true,
                        cantidad_cluster = c.cantidad_total  // para que PintarPinesMapaDesdeCache sepa que es cluster
                    }).ToList();
                }

                // Si el resultado es 0 pines, NO sobreescribir el caché anterior.
                // Esto evita el parpadeo cuando el viewport se mueve ligeramente y una
                // alarma queda momentáneamente fuera del bounding box: en vez de borrar
                // los pines actuales, conservamos lo que ya está pintado hasta que llegue
                // un resultado no vacío.
                if (pines.Count == 0)
                {
                    return true;
                }

                // Acumular tipo-9 y sus padres en el caché persistente ANTES de reemplazar CacheMapa,
                // para que no desaparezcan del mapa al mover el viewport.
                AgregarAPinesPersistentes(pines);

                // Si el viewport actual cubre un pin que estaba en el Cache D pero ya no lo devuelve
                // la API, significa que la alarma cerró y expiró su ventana de 90 min en BD.
                // En ese caso lo eliminamos del Cache D para que no quede flotando indefinidamente.
                DepurarPinesPersistentesAusentes(pines, minLat, maxLat, minLon, maxLon);

                CacheMapa = new MapaCacheDto
                {
                    Pines           = pines,
                    ViewportMinLat  = minLat,
                    ViewportMaxLat  = maxLat,
                    ViewportMinLon  = minLon,
                    ViewportMaxLon  = maxLon,
                    GuardadoEn      = DateTime.UtcNow
                };
                return true;
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "App", "RefrescarMapaDesdeAPI");
                return false;
            }
        }

        // ─── ORQUESTADOR TRES FEEDS ───────────────────────────────────────────────
        // Creado: 2026-03-01 — Reemplaza RefrescarAmbosFeeds en los puntos principales

        /// <summary>
        /// Actualiza los tres feeds en orden de prioridad visual: Mapa → Siguiendo → Para Ti.
        /// Cada feed es independiente: si uno falla, los demás continúan.
        /// (Para Ti ya no depende del éxito de Siguiendo.)
        /// </summary>
        public static async Task<(bool mapaOk, bool feedAOk, bool feedBOk)> RefrescarTresFeeds(
            decimal minLat = 0, decimal maxLat = 0, decimal minLon = 0, decimal maxLon = 0, int zoom = 15, bool forzarArranque = false)
        {
            bool mapaOk  = false;
            bool feedAOk = false;
            bool feedBOk = false;
            try
            {
                if (!forzarArranque && DescribirPageActiva)
                {
                    return (false, false, false);
                }

                // Si es arranque forzado, desactivar temporalmente el guard de DescribirPage
                // para que los refrescos individuales no se cancelen por el flag de la tab activa
                bool descPageOriginal = false;
                if (forzarArranque && DescribirPageActiva)
                {
                    descPageOriginal = true;
                    DescribirPageActiva = false;
                }

                try
                {
                    // 1. Mapa (si hay viewport disponible)
                    if (minLat != 0 || maxLat != 0 || minLon != 0 || maxLon != 0)
                    {
                        mapaOk = await RefrescarMapaDesdeAPI(minLat, maxLat, minLon, maxLon, zoom);
                    }

                    // 2. Feed A — Siguiendo
                    feedAOk = await RefrescarAlarmasDesdeAPI();

                    // 3. Feed B — Para Ti (INDEPENDIENTE de Feed A)
                    feedBOk = await RefrescarFeedParaTi();
                }
                finally
                {
                    // Restaurar el flag si lo desactivamos temporalmente
                    if (descPageOriginal)
                    {
                        DescribirPageActiva = true;
                    }
                }

                return (mapaOk, feedAOk, feedBOk);
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "App", "RefrescarTresFeeds");
                return (mapaOk, feedAOk, feedBOk);
            }
        }

        // ─── CACHE D: PINES PERSISTENTES DE PERSECUCIÓN ──────────────────────────
        // Creado: 2026-03-21 — Pines tipo-9 y sus padres de crimen persisten al mover el mapa

        /// <summary>
        /// Analiza los pines recién recibidos del viewport y acumula en el caché persistente
        /// todos los de tipo 9 (sospechoso huyendo) y sus alarmas padre.
        /// Llama SIEMPRE después de actualizar CacheMapa con pines no vacíos.
        /// </summary>
        public static void AgregarAPinesPersistentes(List<Models.PinMapaDto> pinesViewport)
        {
            if (pinesViewport == null || pinesViewport.Count == 0) return;
            try
            {
                // Identificar tipo-9 y sus padres en el viewport actual
                var tipo9 = pinesViewport.Where(p => p.tipoalarma_id == 9 && p.alarma_id > 0).ToList();
                if (tipo9.Count == 0) return;

                // IDs de padres referenciados por los tipo-9
                var idsPadres = tipo9
                    .Where(p => p.alarma_id_padre.HasValue)
                    .Select(p => p.alarma_id_padre!.Value)
                    .ToHashSet();

                // Pines padre presentes en este viewport
                var padres = pinesViewport
                    .Where(p => idsPadres.Contains(p.alarma_id) && p.alarma_id > 0)
                    .ToList();

                var candidatos = tipo9.Concat(padres).ToList();
                var ahora = DateTime.UtcNow;

                lock (_lockPinesPersistentes)
                {
                    int nuevos = 0, actualizados = 0;
                    foreach (var pin in candidatos)
                    {
                        if (_pinesPersistentesEscape.TryGetValue(pin.alarma_id, out var entrada))
                        {
                            // Ya existe: actualizar estado (puede haber cambiado de activa→cerrada)
                            // y renovar el timestamp para reiniciar el TTL de 90 min.
                            if (entrada.pin.estado_alarma != pin.estado_alarma)
                            {
                                entrada.pin.estado_alarma = pin.estado_alarma;
                                actualizados++;
                            }
                            _pinesPersistentesEscape[pin.alarma_id] = (entrada.pin, ahora);
                        }
                        else
                        {
                            // Nuevo: agregar con timestamp actual
                            _pinesPersistentesEscape[pin.alarma_id] = (pin, ahora);
                            nuevos++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "App", "AgregarAPinesPersistentes");
            }
        }

        /// <summary>
        /// Limpia el caché persistente de pines de persecución.
        /// Llamar al cerrar sesión o al cambiar de usuario.
        /// </summary>
        public static void LimpiarPinesPersistentes()
        {
            lock (_lockPinesPersistentes)
            {
                _pinesPersistentesEscape.Clear();
            }
        }

        /// <summary>
        /// Elimina del Cache D los pines que estaban dentro del viewport actual pero que la API
        /// ya no devolvió. Esto ocurre cuando una alarma cerró y superó la ventana de 90 min
        /// en BD — la vista vw_pines_mapa la excluye, pero el Cache D la seguiría mostrando
        /// indefinidamente si no se limpia aquí.
        /// Solo se actúa sobre pines cuya posición geográfica cae dentro del bounding box
        /// actual, para no eliminar pines que simplemente están fuera del viewport visible.
        /// </summary>
        public static void DepurarPinesPersistentesAusentes(
            List<Models.PinMapaDto> pinesViewport,
            decimal minLat, decimal maxLat, decimal minLon, decimal maxLon)
        {
            try
            {
                var idsEnViewport = new HashSet<long>(
                    pinesViewport.Where(p => p.alarma_id > 0).Select(p => p.alarma_id));

                lock (_lockPinesPersistentes)
                {
                    int totalPersistentes = _pinesPersistentesEscape.Count;

                    // Separar: ¿cuántos están dentro del bounding box pero ausentes de la respuesta?
                    var dentroDelBbox = _pinesPersistentesEscape
                        .Where(kv =>
                            (decimal)kv.Value.pin.latitud  >= minLat &&
                            (decimal)kv.Value.pin.latitud  <= maxLat &&
                            (decimal)kv.Value.pin.longitud >= minLon &&
                            (decimal)kv.Value.pin.longitud <= maxLon)
                        .ToList();

                    var ausentes = dentroDelBbox
                        .Where(kv => !idsEnViewport.Contains(kv.Key))
                        .Select(kv => kv.Key)
                        .ToList();

                    foreach (var id in ausentes)
                    {
                        _pinesPersistentesEscape.Remove(id);
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "App", "DepurarPinesPersistentesAusentes");
            }
        }

        /// <summary>Carga Cache B desde archivo persistente.</summary>
        public static async Task<List<AlarmaCercana>?> CargarFeedParaTiDesdeCache()
        {
            try
            {
                var cacheFilePath = Path.Combine(FileSystem.CacheDirectory, CacheFileNameParaTi);
                if (!File.Exists(cacheFilePath)) return null;

                var jsonContent = await File.ReadAllTextAsync(cacheFilePath);
                if (string.IsNullOrWhiteSpace(jsonContent)) return null;

                var dtos = JsonConvert.DeserializeObject<List<AlarmaCercanaDto>>(jsonContent);
                _alarmasCacheadasParaTi = dtos?.Select(dto => new AlarmaCercana(dto)).ToList();

                // Blindaje 2026-04: este path escribe al field directo (bypassa el setter público),
                // así que reaplicamos manualmente la verdad local del proponente de cierre.
                ReaplicarFlagsLocalesProponente(_alarmasCacheadasParaTi);

                return _alarmasCacheadasParaTi;
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "App", "CargarFeedParaTiDesdeCache");
                return null;
            }
        }

        /// <summary>Guarda Cache B en archivo persistente.</summary>
        private static async Task GuardarCacheParaTi(List<AlarmaCercana> alarmas)
        {
            try
            {
                if (alarmas == null || alarmas.Count == 0) return;
                var cacheFilePath = Path.Combine(FileSystem.CacheDirectory, CacheFileNameParaTi);
                var dtos = alarmas.Select(a => a.ToDto()).ToList();
                var jsonContent = JsonConvert.SerializeObject(dtos);
                await File.WriteAllTextAsync(cacheFilePath, jsonContent);
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "App", "GuardarCacheParaTi");
            }
        }

        /// <summary>
        /// Inserta una alarma real (consultada del API) en el cache local
        /// Se inserta al principio para que aparezca primero en el mapa
        /// </summary>
        public static void InsertarAlarmaEnCacheLocal(AlarmaCercana alarma)
        {
            if (alarma == null) return;

            try
            {
                if (_alarmasCacheadas == null)
                {
                    _alarmasCacheadas = new List<AlarmaCercana>();
                }

                // Verificar si ya existe para evitar duplicados
                var existente = _alarmasCacheadas.FirstOrDefault(a => a.alarma_id == alarma.alarma_id);
                if (existente != null) return;

                // FIX 2026-02-27: Una alarma propia recién lanzada debe ser visible en el feed
                // "Siguiendo/En tu área" (flag_visible_siguiendo=true) y en el mapa (flag_propietario_alarma=true).
                // El endpoint TraerAlarma usa vw_busca_alarma_por_id que NO calcula estos flags,
                // así que los forzamos aquí antes de insertar en caché.
                alarma.flag_visible_siguiendo = true;
                alarma.flag_propietario_alarma = true;

                // Insertar al principio de la lista
                _alarmasCacheadas.Insert(0, alarma);

                // Registrar como alarma insertada localmente para preservarla cuando BGAPI sobrescriba
                _alarmasInsertadasLocalmente[alarma.alarma_id] = (alarma, DateTime.Now);

                // Guardar el cache actualizado
                _ = GuardarAlarmasEnCache(_alarmasCacheadas);
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "App", "InsertarAlarmaEnCacheLocal");
            }
        }
    }
}

