// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.SplashScreen;
using AndroidX.Core.View;
using Android.Util;
using sospect.Helpers;

namespace sospect.Platforms.Android
{
    [Activity(Theme = "@style/Maui.SplashTheme",
              MainLauncher = true,
              LaunchMode = LaunchMode.SingleTop,
              Exported = true,
              ScreenOrientation = ScreenOrientation.Portrait,
              ConfigurationChanges = ConfigChanges.ScreenSize |
                                   ConfigChanges.Orientation |
                                   ConfigChanges.UiMode |
                                   ConfigChanges.ScreenLayout |
                                   ConfigChanges.SmallestScreenSize |
                                   ConfigChanges.Density)]
    // DEEP LINKS (2026-02-24): App Links HTTPS — https://www.sospect.com/a/{id}
    // AutoVerify=true permite abrir sin diálogo de selección si assetlinks.json está publicado.
    // IMPORTANTE: Los intent-filter van aquí (atributo C#), NO en AndroidManifest.xml.
    // Si se duplican en el XML con un CRC hardcodeado, Android lanza ClassNotFoundException.
    [IntentFilter(new[] { Intent.ActionView },
        Categories = new[] {
            Intent.CategoryDefault,
            Intent.CategoryBrowsable
        },
        DataScheme = "https",
        DataHost = "www.sospect.com",
        DataPathPrefix = "/a/",
        AutoVerify = true)]
    // DEEP LINKS (2026-02-25): App Links HTTPS DEV — https://dev.sospect.com/a/{id}
    // Sin AutoVerify porque dev.sospect.com no publica assetlinks.json.
    // En desarrollo Android mostrará selector de app (comportamiento aceptable).
    [IntentFilter(new[] { Intent.ActionView },
        Categories = new[] {
            Intent.CategoryDefault,
            Intent.CategoryBrowsable
        },
        DataScheme = "https",
        DataHost = "dev.sospect.com",
        DataPathPrefix = "/a/")]
    // DEEP LINKS (2026-02-24): URL Scheme custom — sospect://alarma/{id}
    // Fallback desde el botón "Abrir en SOSpect" de la página Angular pública.
    [IntentFilter(new[] { Intent.ActionView },
        Categories = new[] {
            Intent.CategoryDefault,
            Intent.CategoryBrowsable
        },
        DataScheme = "sospect",
        DataHost = "alarma")]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== MainActivity.OnCreate START ===");

                // CRITICO: Instalar splash screen ANTES de base.OnCreate
                if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
                {
                    AndroidX.Core.SplashScreen.SplashScreen.InstallSplashScreen(this);
                }
                System.Diagnostics.Debug.WriteLine("Splash Screen instalado");

                // LLAMAR base.OnCreate (esto carga la app MAUI)
                base.OnCreate(savedInstanceState);
                System.Diagnostics.Debug.WriteLine("base.OnCreate completado");

                // MAUI 10 + API 36: Edge-to-edge es obligatorio y no se puede desactivar.
                // Sin esto, el contenido se renderiza detrás de la status bar (arriba) y la
                // navigation/gesture bar (abajo), ocultando títulos y tabs del TabbedPage.
                // Aplicamos los insets del sistema como padding en la vista raíz para que
                // todo el contenido quede dentro del área segura.
                // Ref: https://github.com/dotnet/maui/issues/33340
                if (Window?.DecorView != null)
                {
                    ViewCompat.SetOnApplyWindowInsetsListener(Window.DecorView, new SystemBarInsetsListener());
                    System.Diagnostics.Debug.WriteLine("WindowInsets listener configurado para edge-to-edge");
                }

                // NUEVO: Ocultar el ActionBar de Android
                if (SupportActionBar != null)
                {
                    SupportActionBar.Hide();
                    System.Diagnostics.Debug.WriteLine("ActionBar ocultado");
                }

                // Firebase Push Notifications v4.x maneja intents internamente
                // (FirebasePushNotificationManager fue removido en v4.0)
                System.Diagnostics.Debug.WriteLine("Firebase intent processing delegado al plugin v4.x");

                System.Diagnostics.Debug.WriteLine("=== MainActivity.OnCreate END ===");

                // DEEP LINKS: Si la app se abrió desde un deep link (cold start),
                // el Intent llega aquí en lugar de OnNewIntent
                HandleDeepLinkIntent(Intent);

                // 2026-04-11: Extraer chat_id del intent de notificación local
                // Cuando la notificación local (creada por LocalNotificationService) es tocada,
                // el PendingIntent llega aquí con chat_id como extra.
                HandleNotificationIntent(Intent);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CRASH en MainActivity.OnCreate: {ex}");
                Log.Error("SOSpect", $"MainActivity.OnCreate error: {ex.Message}", ex);
                throw;
            }
        }

        protected override void OnNewIntent(Intent intent)
        {
            try
            {
                base.OnNewIntent(intent);
                // Firebase Push Notifications v4.x maneja intents internamente

                // DEEP LINKS (2026-02-24): Manejar App Links (https://www.sospect.com/a/{id})
                // y URL scheme (sospect://alarma/{id}) cuando la app ya está abierta
                HandleDeepLinkIntent(intent);

                // 2026-04-11: Extraer chat_id del intent de notificación local
                HandleNotificationIntent(intent);
            }
            catch (System.Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "MainActivity", "OnNewIntent");
                System.Diagnostics.Debug.WriteLine($"Error en OnNewIntent: {ex}");
            }
        }

        /// <summary>
        /// Procesa un Intent de deep link y navega a la alarma correspondiente.
        /// Soporta:
        ///   - App Links HTTPS: https://www.sospect.com/a/{id}
        ///   - URL Scheme:      sospect://alarma/{id}
        /// </summary>
        private void HandleDeepLinkIntent(Intent? intent)
        {
            try
            {
                var data = intent?.Data;
                if (data == null) return;

                string? alarmaId = null;

                if (data.Scheme == "https" && (data.Host == "www.sospect.com" || data.Host == "dev.sospect.com"))
                {
                    // Formato: https://www.sospect.com/a/{id}  o  https://dev.sospect.com/a/{id}
                    var segments = data.PathSegments;
                    if (segments != null && segments.Count >= 2 && segments[0] == "a")
                        alarmaId = segments[1];
                }
                else if (data.Scheme == "sospect" && data.Host == "alarma")
                {
                    // Formato: sospect://alarma/{id}
                    alarmaId = data.Path?.TrimStart('/');
                }

                if (!string.IsNullOrEmpty(alarmaId))
                {
                    System.Diagnostics.Debug.WriteLine($"[MainActivity] Deep link detectado → alarmaId={alarmaId}");
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        (Microsoft.Maui.Controls.Application.Current as sospect.App)?.NavigateToAlarma(alarmaId);
                    });
                }
            }
            catch (System.Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "MainActivity", "HandleDeepLinkIntent");
                System.Diagnostics.Debug.WriteLine($"Error en HandleDeepLinkIntent: {ex}");
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            try
            {
                Microsoft.Maui.ApplicationModel.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
                base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            }
            catch (System.Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "MainActivity", "OnRequestPermissionsResult");
                System.Diagnostics.Debug.WriteLine($"Error en OnRequestPermissionsResult: {ex}");
            }
        }

        protected override async void OnResume()
        {
            base.OnResume();

            System.Diagnostics.Debug.WriteLine("========================================");
            System.Diagnostics.Debug.WriteLine($"[MainActivity] OnResume LLAMADO: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            System.Diagnostics.Debug.WriteLine("========================================");

            // CAMBIO: Solo pedir permisos si hay usuario logueado (evita popup antes del login)
            var userId = await SecureStorage.GetAsync("user_id_thirdparty");
            if (string.IsNullOrEmpty(userId))
            {
                System.Diagnostics.Debug.WriteLine("[MainActivity] No hay usuario logueado, omitiendo solicitud de permisos");
                return;
            }

            System.Diagnostics.Debug.WriteLine("[MainActivity] Usuario logueado detectado, verificando permisos...");

            // Solicitar permiso de ubicación en segundo plano (Android 10+)
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
            {
                bool permissionGranted = await RequestBackgroundLocationPermission();

                // Si se acaba de otorgar el permiso, reiniciar el servicio de ubicación
                if (permissionGranted)
                {
                    System.Diagnostics.Debug.WriteLine("[MainActivity] Permiso otorgado, reiniciando servicio de ubicación...");
                    await ReiniciarServicioUbicacion();
                    
                    // NUEVO: Solicitar excepción de optimización de batería después de permisos
                    await SolicitarExcepcionOptimizacionBateria();
                }
            }
        }

        private async System.Threading.Tasks.Task<bool> RequestBackgroundLocationPermission()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[MainActivity] Verificando permiso de ubicación en segundo plano...");

                // PASO 1: Verificar primero el permiso de ubicación básico (FINE_LOCATION)
                var fineLocationStatus = await Microsoft.Maui.ApplicationModel.Permissions.CheckStatusAsync<Microsoft.Maui.ApplicationModel.Permissions.LocationWhenInUse>();
                System.Diagnostics.Debug.WriteLine($"[MainActivity] Permiso LocationWhenInUse: {fineLocationStatus}");

                if (fineLocationStatus != Microsoft.Maui.ApplicationModel.PermissionStatus.Granted)
                {
                    System.Diagnostics.Debug.WriteLine("[MainActivity] Se requiere primero permiso de ubicación básico");
                    fineLocationStatus = await Microsoft.Maui.ApplicationModel.Permissions.RequestAsync<Microsoft.Maui.ApplicationModel.Permissions.LocationWhenInUse>();

                    if (fineLocationStatus != Microsoft.Maui.ApplicationModel.PermissionStatus.Granted)
                    {
                        System.Diagnostics.Debug.WriteLine("[MainActivity] Usuario denegó permiso de ubicación básico");
                        return false;
                    }
                }

                // PASO 2: Ahora verificar el permiso de ubicación en segundo plano (BACKGROUND_LOCATION)
                var status = await Microsoft.Maui.ApplicationModel.Permissions.CheckStatusAsync<Microsoft.Maui.ApplicationModel.Permissions.LocationAlways>();
                System.Diagnostics.Debug.WriteLine($"[MainActivity] Permiso LocationAlways: {status}");

                if (status != Microsoft.Maui.ApplicationModel.PermissionStatus.Granted)
                {
                    // POPUP EDUCATIVO: Explicar al usuario por qué necesitamos este permiso
                    System.Diagnostics.Debug.WriteLine("[MainActivity] Mostrando popup educativo antes de solicitar permiso...");

                    // Obtener textos traducidos (usando método síncrono para evitar race conditions)
                    var tituloPopup = sospect.Helpers.TranslateExtension.Translate("PermisoUbicacionFondoTitulo");
                    var mensajePopup = sospect.Helpers.TranslateExtension.Translate("PermisoUbicacionFondoMensaje");
                    var btnPermitir = sospect.Helpers.TranslateExtension.Translate("LabelPermitir");
                    var btnAhoraNo = sospect.Helpers.TranslateExtension.Translate("LabelAhoraNo");

                    bool shouldRequest = await sospect.Helpers.ModernAlerts.ShowConfirmation(
                        tituloPopup,
                        mensajePopup,
                        btnPermitir,
                        btnAhoraNo
                    );

                    if (!shouldRequest)
                    {
                        System.Diagnostics.Debug.WriteLine("[MainActivity] Usuario seleccionó 'Ahora no' en el popup educativo");
                        return false;
                    }

                    System.Diagnostics.Debug.WriteLine("[MainActivity] Usuario aceptó el popup educativo, solicitando permiso LocationAlways...");

                    // Solicitar permiso LocationAlways directamente (Android maneja el flujo automáticamente)
                    status = await Microsoft.Maui.ApplicationModel.Permissions.RequestAsync<Microsoft.Maui.ApplicationModel.Permissions.LocationAlways>();

                    if (status == Microsoft.Maui.ApplicationModel.PermissionStatus.Granted)
                    {
                        System.Diagnostics.Debug.WriteLine("[MainActivity] ✓ Permiso de ubicación en segundo plano CONCEDIDO");
                        return true; // Permiso recién otorgado
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[MainActivity] ✗ Permiso de ubicación en segundo plano DENEGADO (status: {status})");
                        return false;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[MainActivity] Permiso de ubicación en segundo plano ya concedido previamente");
                    return false; // Ya estaba otorgado, no es nuevo
                }

                return false;
            }
            catch (System.Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "MainActivity", "RequestBackgroundLocationPermission");
                System.Diagnostics.Debug.WriteLine($"[MainActivity] Error solicitando permiso: {ex.Message}");
                return false;
            }
        }

        private async System.Threading.Tasks.Task ReiniciarServicioUbicacion()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[MainActivity] ReiniciarServicioUbicacion INICIADO");

                // Verificar que hay usuario logueado
                var userId = await SecureStorage.GetAsync("user_id_thirdparty");
                if (string.IsNullOrEmpty(userId))
                {
                    System.Diagnostics.Debug.WriteLine("[MainActivity] No hay usuario logueado, no se reinicia servicio");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[MainActivity] Usuario logueado: {userId}");

                // Obtener el servicio de background del DI
                var app = Microsoft.Maui.Controls.Application.Current as sospect.App;
                if (app?.Handler?.MauiContext?.Services == null)
                {
                    System.Diagnostics.Debug.WriteLine("[MainActivity] No se pudo obtener el contexto de la app");
                    return;
                }

                var backgroundService = app.Handler.MauiContext.Services.GetService<sospect.Interfaces.IBackgroundService>();
                if (backgroundService == null)
                {
                    System.Diagnostics.Debug.WriteLine("[MainActivity] IBackgroundService no está registrado");
                    return;
                }

                System.Diagnostics.Debug.WriteLine("[MainActivity] IBackgroundService obtenido, deteniendo servicio existente...");

                // Detener servicio existente
                await backgroundService.StopBackgroundService();
                await System.Threading.Tasks.Task.Delay(500); // Dar tiempo para que se detenga

                System.Diagnostics.Debug.WriteLine("[MainActivity] Iniciando nuevo servicio...");

                // Reiniciar servicio
                await backgroundService.RunCodeInBackgroundMode(
                    sospect.Services.ApiService.ActualizarUbicacion,
                    "LocationTracking"
                );

                System.Diagnostics.Debug.WriteLine("[MainActivity] ✓ Servicio de ubicación REINICIADO correctamente");
            }
            catch (System.Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "MainActivity", "ReiniciarServicioUbicacion");
                System.Diagnostics.Debug.WriteLine($"[MainActivity] ✗ Error reiniciando servicio: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[MainActivity] StackTrace: {ex.StackTrace}");
            }
        }

        private async System.Threading.Tasks.Task SolicitarExcepcionOptimizacionBateria()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[MainActivity] SolicitarExcepcionOptimizacionBateria INICIADO");

                // Android 6.0 (API 23) y superior soporta optimización de batería
                if (Build.VERSION.SdkInt < BuildVersionCodes.M)
                {
                    System.Diagnostics.Debug.WriteLine("[MainActivity] Versión de Android menor a 6.0, no se requiere excepción de batería");
                    return;
                }

                var powerManager = (PowerManager)GetSystemService(PowerService);
                string packageName = PackageName;

                if (powerManager.IsIgnoringBatteryOptimizations(packageName))
                {
                    System.Diagnostics.Debug.WriteLine("[MainActivity] App ya está en whitelist de optimización de batería");
                    return;
                }

                System.Diagnostics.Debug.WriteLine("[MainActivity] App NO está en whitelist, mostrando popup educativo...");

                // POPUP EDUCATIVO: Explicar por qué necesitamos esto
                var tituloPopup = sospect.Helpers.TranslateExtension.Translate("OptimizacionBateriaTitulo");
                var mensajePopup = sospect.Helpers.TranslateExtension.Translate("OptimizacionBateriaMensaje");
                var btnPermitir = sospect.Helpers.TranslateExtension.Translate("LabelPermitir");
                var btnAhoraNo = sospect.Helpers.TranslateExtension.Translate("LabelAhoraNo");

                bool aceptado = await sospect.Helpers.ModernAlerts.ShowConfirmation(
                    tituloPopup,
                    mensajePopup,
                    btnPermitir,
                    btnAhoraNo
                );

                if (!aceptado)
                {
                    System.Diagnostics.Debug.WriteLine("[MainActivity] Usuario seleccionó 'Ahora no' para optimización de batería");
                    return;
                }

                System.Diagnostics.Debug.WriteLine("[MainActivity] Usuario aceptó, abriendo configuración del sistema...");

                // Abrir configuración del sistema para que el usuario agregue la app a la whitelist
                var intent = new Intent();
                intent.SetAction(global::Android.Provider.Settings.ActionRequestIgnoreBatteryOptimizations);
                intent.SetData(global::Android.Net.Uri.Parse("package:" + packageName));
                StartActivity(intent);

                System.Diagnostics.Debug.WriteLine("[MainActivity] ✓ Intent de configuración lanzado");
            }
            catch (System.Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "MainActivity", "SolicitarExcepcionOptimizacionBateria");
                System.Diagnostics.Debug.WriteLine($"[MainActivity] ✗ Error solicitando excepción de batería: {ex.Message}");
            }
        }

        /// <summary>
        /// 2026-04-11: Extrae chat_id del intent de notificaciones locales creadas por LocalNotificationService.
        /// Cuando la app está en foreground y recibe un mensaje de chat, LocalNotificationService crea
        /// una notificación con chat_id como extra en el PendingIntent. Al tocarla, este método
        /// guarda chat_id en Preferences ANTES de que NotificationOpened sea procesado por MAUI.
        /// </summary>
        private void HandleNotificationIntent(Intent? intent)
        {
            try
            {
                if (intent == null) return;
                var chatId = intent.GetStringExtra("chat_id");
                var alarmaId = intent.GetStringExtra("alarma_id");
                System.Diagnostics.Debug.WriteLine($"[MainActivity] HandleNotificationIntent: alarma_id='{alarmaId ?? "null"}', chat_id='{chatId ?? "null"}', Action='{intent.Action}'");

                // DIAGNÓSTICO: Loguear todos los extras del intent para detectar cómo llegan los datos FCM en cold start
                if (intent.Extras != null)
                {
                    foreach (var key in intent.Extras.KeySet())
                    {
                        System.Diagnostics.Debug.WriteLine($"[MainActivity] HandleNotificationIntent extra: key='{key}' value='{intent.Extras.GetString(key) ?? "(no-string)"}'");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[MainActivity] HandleNotificationIntent: intent.Extras es null");
                }

                if (!string.IsNullOrEmpty(chatId) && chatId != "0")
                {
                    Microsoft.Maui.Storage.Preferences.Set("chat_id_notif", chatId);
                    System.Diagnostics.Debug.WriteLine($"[MainActivity] chat_id={chatId} guardado en Preferences desde intent local");
                }

                // COLD START: Si el intent trae alarma_id (notificación FCM tapeada con app cerrada),
                // guardarla en Preferences ANTES de que OnStart verifique si hay alarma pendiente.
                // Esto garantiza que el flujo de OnStart encuentre el alarma_id aunque NotificationOpened
                // se dispare después de que InitializeFirebaseHandlers registra los handlers.
                if (!string.IsNullOrEmpty(alarmaId) && alarmaId != "0")
                {
                    Microsoft.Maui.Storage.Preferences.Set("alarma_id", alarmaId);
                    System.Diagnostics.Debug.WriteLine($"[MainActivity] alarma_id={alarmaId} guardado en Preferences (cold start FCM intent)");
                }
            }
            catch (System.Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "MainActivity", "HandleNotificationIntent");
                System.Diagnostics.Debug.WriteLine($"Error en HandleNotificationIntent: {ex}");
            }
        }

        /// <summary>
        /// Evento estático que se dispara cada vez que el SystemBarInsetsListener aplica
        /// los insets del sistema. Cualquier página puede suscribirse para reaccionar al
        /// relayout que el SetPadding produce en la jerarquía de vistas de Android.
        /// </summary>
        public static event EventHandler WindowInsetsApplied;

        private class SystemBarInsetsListener : Java.Lang.Object, IOnApplyWindowInsetsListener
        {
            public WindowInsetsCompat OnApplyWindowInsets(global::Android.Views.View v, WindowInsetsCompat insets)
            {
                var systemBars = insets.GetInsets(
                    WindowInsetsCompat.Type.SystemBars()
                    | WindowInsetsCompat.Type.DisplayCutout());

                v.SetPadding(systemBars.Left, systemBars.Top, systemBars.Right, systemBars.Bottom);

                System.Diagnostics.Debug.WriteLine(
                    $"[MainActivity] WindowInsets aplicados - Top:{systemBars.Top}, Bottom:{systemBars.Bottom}, Left:{systemBars.Left}, Right:{systemBars.Right}");

                // Notificar a suscriptores (ej. ChatPublicidadPage) para que fuercen relayout.
                // OnApplyWindowInsets ya corre en el main thread de Android.
                MainActivity.WindowInsetsApplied?.Invoke(null, EventArgs.Empty);

                return WindowInsetsCompat.Consumed;
            }
        }
    }
}

