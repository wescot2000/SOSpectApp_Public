using CoreLocation;
using Foundation;
using Microsoft.Maui;
using sospect.Models;
using sospect.Services;
using UIKit;
using UserNotifications;

namespace sospect.Platforms.iOS;

// TODO: Firebase.CloudMessaging bindings (Xamarin.Firebase.iOS.CloudMessaging) fueron removidos
// porque son incompatibles con .NET 10. Cuando exista un binding compatible,
// restaurar FirebaseMessagingDelegate y la configuración de Firebase Core.
// Por ahora, se usa APNs token directamente como fallback.

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    public override bool FinishedLaunching(UIApplication app, NSDictionary options)
    {
        Console.WriteLine("[AppDelegate] FinishedLaunching START");

        // TODO: Firebase Core - descomentar cuando haya binding .NET 10 compatible
        // Firebase.Core.App.Configure();
        Console.WriteLine("[AppDelegate] Firebase iOS SDK NO disponible en .NET 10 (bindings incompatibles)");

        // Solicitar permisos de notificaciones (funciona sin Firebase binding)
        UNUserNotificationCenter.Current.Delegate = new UserNotificationCenterDelegate();
        UNUserNotificationCenter.Current.RequestAuthorization(
            UNAuthorizationOptions.Alert | UNAuthorizationOptions.Badge | UNAuthorizationOptions.Sound,
            (granted, error) =>
            {
                Console.WriteLine($"[AppDelegate] Notification permission granted: {granted}");
                if (error != null)
                {
                    Console.WriteLine($"[AppDelegate] Notification permission error: {error.Description}");
                }

                if (granted && error == null)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        UIApplication.SharedApplication.RegisterForRemoteNotifications();
                        Console.WriteLine("[AppDelegate] RegisterForRemoteNotifications called");
                    });
                }
            });

        // AdMob: Inicializar el SDK nativo de Google Mobile Ads
        Google.MobileAds.MobileAds.SharedInstance.Start(completionHandler: null);
        Console.WriteLine("[AppDelegate] Google Ads initialization OK");

        // Limpiar insignia al abrir la app
        UIApplication.SharedApplication.ApplicationIconBadgeNumber = 0;
        Console.WriteLine("[AppDelegate] Badge cleared");

        // Cold start: si la app fue abierta al tocar una notificación, guardar alarma_id
        // para que App.NavigateToAlarma la procese una vez la navegación esté lista
        if (options != null && options.ContainsKey(UIApplication.LaunchOptionsRemoteNotificationKey))
        {
            var remoteNotif = options[UIApplication.LaunchOptionsRemoteNotificationKey] as NSDictionary;
            var alarmaId = ExtraerAlarmaId(remoteNotif);
            if (!string.IsNullOrEmpty(alarmaId))
            {
                Console.WriteLine($"[AppDelegate] Cold start con alarma_id: {alarmaId}");
                Preferences.Set("alarma_id", alarmaId);
            }
        }

        var result = base.FinishedLaunching(app, options);

        // Re-lanzamiento por cambio significativo de ubicación (survive force-close, como Life360)
        // iOS llama a FinishedLaunching con LaunchOptionsLocationKey cuando el usuario cerró la app
        // pero StartMonitoringSignificantLocationChanges detectó movimiento (~300-500m)
        if (options != null && options.ContainsKey(UIApplication.LaunchOptionsLocationKey))
        {
            Console.WriteLine("[AppDelegate] Re-lanzado por iOS por cambio de ubicación significativa");
            _ = Task.Run(async () => await HandleLocationRelaunch());
        }

        Console.WriteLine($"[AppDelegate] FinishedLaunching END - result: {result}");
        return result;
    }

    // Maneja el re-lanzamiento en background por cambio significativo de ubicación.
    // Se ejecuta cuando el usuario tenía la app force-cerrada y iOS la relanza automáticamente.
    private async Task HandleLocationRelaunch()
    {
        nint bgTaskId = UIApplication.BackgroundTaskInvalid;
        CLLocationManager relaunchManager = null;

        try
        {
            // Solo proceder si el usuario tenía tracking activo cuando cerró la app
            var wasTracking = Preferences.Get("ios_tracking_active", false);
            if (!wasTracking)
            {
                Console.WriteLine("[AppDelegate] ios_tracking_active=false, no se relanza el tracking");
                return;
            }

            // Solo proceder si hay un usuario logueado
            var userId = await SecureStorage.GetAsync("user_id_thirdparty");
            if (string.IsNullOrEmpty(userId))
            {
                Console.WriteLine("[AppDelegate] No hay usuario logueado, no se relanza el tracking");
                return;
            }

            // Solicitar tiempo extra de ejecución en background (~30 segundos)
            MainThread.BeginInvokeOnMainThread(() =>
            {
                bgTaskId = UIApplication.SharedApplication.BeginBackgroundTask("LocationRelaunch", () =>
                {
                    Console.WriteLine("[AppDelegate] Background task expirado antes de completar");
                });
            });

            await Task.Delay(200); // Pequeña espera para que BeginBackgroundTask se registre

            // Obtener país e idioma del usuario desde Preferences
            string paisId = "CO";
            string idiomaId = "es";
            try
            {
                var userJson = Preferences.Get("User", "");
                if (!string.IsNullOrEmpty(userJson))
                {
                    var persona = Newtonsoft.Json.JsonConvert.DeserializeObject<Persona>(userJson);
                    if (persona != null)
                    {
                        if (!string.IsNullOrEmpty(persona.Pais)) paisId = persona.Pais;
                        if (!string.IsNullOrEmpty(persona.Idioma)) idiomaId = persona.Idioma;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AppDelegate] Error obteniendo datos usuario: {ex.Message}, usando defaults");
            }

            var taskCompletionSource = new TaskCompletionSource<bool>();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                relaunchManager = new CLLocationManager
                {
                    AllowsBackgroundLocationUpdates = true,
                    PausesLocationUpdatesAutomatically = false,
                    DesiredAccuracy = CLLocation.AccuracyHundredMeters
                };

                // Re-registrar el monitoreo de cambios significativos para futuros re-lanzamientos
                relaunchManager.StartMonitoringSignificantLocationChanges();

                relaunchManager.LocationsUpdated += async (sender, e) =>
                {
                    try
                    {
                        var loc = e.Locations?.LastOrDefault();
                        if (loc != null)
                        {
                            Console.WriteLine($"[AppDelegate] Ubicación en re-lanzamiento: {loc.Coordinate.Latitude}, {loc.Coordinate.Longitude}");

                            var resultado = await ApiService.ActualizarUbicacionBackground(
                                (decimal)loc.Coordinate.Latitude,
                                (decimal)loc.Coordinate.Longitude,
                                paisId,
                                idiomaId
                            );

                            if (resultado.success)
                                Console.WriteLine($"[AppDelegate] API OK: {resultado.response?.CantidadAlarmas} alarmas, notif: {resultado.response?.NotificacionEnviada}");
                            else
                                Console.WriteLine($"[AppDelegate] API error: {resultado.error}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[AppDelegate] Error en LocationsUpdated del re-lanzamiento: {ex.Message}");
                    }
                    finally
                    {
                        // Detener el GPS continuo (el monitoreo significativo sigue activo para futuros re-lanzamientos)
                        relaunchManager?.StopUpdatingLocation();
                        taskCompletionSource.TrySetResult(true);
                    }
                };

                relaunchManager.Failed += (sender, e) =>
                {
                    Console.WriteLine($"[AppDelegate] CLLocationManager falló en re-lanzamiento: {e.Error?.Description}");
                    taskCompletionSource.TrySetResult(false);
                };

                // Solicitar una sola lectura de ubicación
                relaunchManager.RequestLocation();
            });

            // Esperar hasta que se obtenga la ubicación o timeout de 25 segundos
            var completed = await Task.WhenAny(taskCompletionSource.Task, Task.Delay(25000));
            if (completed != taskCompletionSource.Task)
            {
                Console.WriteLine("[AppDelegate] Timeout esperando ubicación en re-lanzamiento");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AppDelegate] Error en HandleLocationRelaunch: {ex.Message}");
        }
        finally
        {
            if (bgTaskId != UIApplication.BackgroundTaskInvalid)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    UIApplication.SharedApplication.EndBackgroundTask(bgTaskId);
                });
            }
        }
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    // APNs: Manejar registro exitoso de notificaciones
    [Export("application:didRegisterForRemoteNotificationsWithDeviceToken:")]
    public void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
    {
        Console.WriteLine("[AppDelegate] RegisteredForRemoteNotifications called");

        // Convertir NSData a string hexadecimal para logging
        var tokenBytes = deviceToken.ToArray();
        var apnsToken = BitConverter.ToString(tokenBytes).Replace("-", "");
        Console.WriteLine($"[AppDelegate] APNs Token: {apnsToken}");

        // Sin Firebase iOS binding, usar APNs token directamente como fallback
        // Formato: "ios-apns-[token]" para identificarlo en el backend
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var fallbackToken = $"ios-apns-{apnsToken}";
            SecureStorage.SetAsync("FMC_token", fallbackToken).Wait();

            if (App.Current is App app)
            {
                app.NotifyFirebaseTokenAvailable(fallbackToken);
            }

            Console.WriteLine($"[AppDelegate] APNs fallback token guardado: {fallbackToken.Substring(0, Math.Min(30, fallbackToken.Length))}...");
        });

        Console.WriteLine("[AppDelegate] Device token registered (APNs fallback mode)");
    }

    // APNs: Manejar fallo de registro
    [Export("application:didFailToRegisterForRemoteNotificationsWithError:")]
    public void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error)
    {
        Console.WriteLine($"[AppDelegate] FailedToRegisterForRemoteNotifications: {error?.Description ?? "Unknown error"}");
    }

    // Recibir notificaciones remotas
    [Export("application:didReceiveRemoteNotification:")]
    public void ReceivedRemoteNotification(UIApplication application, NSDictionary userInfo)
    {
        Console.WriteLine("[AppDelegate] ReceivedRemoteNotification");
    }

    // Manejar notificaciones cuando la app está en background
    [Export("application:didReceiveRemoteNotification:fetchCompletionHandler:")]
    public void DidReceiveRemoteNotification(UIApplication application, NSDictionary userInfo,
        Action<UIBackgroundFetchResult> completionHandler)
    {
        Console.WriteLine("[AppDelegate] DidReceiveRemoteNotification (background)");

        // Guardar alarma_id como respaldo para cuando la navegación esté lista
        var alarmaId = ExtraerAlarmaId(userInfo);
        if (!string.IsNullOrEmpty(alarmaId))
        {
            Console.WriteLine($"[AppDelegate] alarma_id en background: {alarmaId}");
            Preferences.Set("alarma_id", alarmaId);
        }

        completionHandler(UIBackgroundFetchResult.NewData);
    }

    // OAUTH: Manejar Universal Links (Apple Sign In + SOSpect Deep Links)
    public override bool ContinueUserActivity(UIApplication application, NSUserActivity userActivity,
        UIApplicationRestorationHandler completionHandler)
    {
        Console.WriteLine($"[AppDelegate] ContinueUserActivity called - ActivityType: {userActivity?.ActivityType}");
        Console.WriteLine($"[AppDelegate] WebPageUrl: {userActivity?.WebPageUrl}");

        // DEEP LINKS (2026-02-24): Manejar Universal Links https://www.sospect.com/a/{id}
        // (2026-02-25): También acepta dev.sospect.com para ambiente de desarrollo
        if (userActivity?.ActivityType == "NSUserActivityTypeBrowsingWeb")
        {
            var url = userActivity.WebPageUrl;
            if (url != null && (url.Host == "www.sospect.com" || url.Host == "dev.sospect.com"))
            {
                var path = url.Path ?? string.Empty; // "/a/1084"
                var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2 && parts[0] == "a")
                {
                    string alarmaId = parts[1];
                    Console.WriteLine($"[AppDelegate] Universal Link detectado → alarmaId={alarmaId}");
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        (Microsoft.Maui.Controls.Application.Current as sospect.App)?.NavigateToAlarma(alarmaId);
                    });
                    return true;
                }
            }
        }

        // OAUTH: Delegar al MAUI Platform para Apple Sign In / Google / Facebook
        if (Microsoft.Maui.ApplicationModel.Platform.ContinueUserActivity(application, userActivity, completionHandler))
        {
            Console.WriteLine("[AppDelegate] ContinueUserActivity handled by MAUI Platform");
            return true;
        }

        Console.WriteLine("[AppDelegate] ContinueUserActivity not handled by MAUI Platform");
        return base.ContinueUserActivity(application, userActivity, completionHandler);
    }

    // DEEP LINKS + OAUTH: Manejar URL scheme sospect:// y OAuth callbacks (Apple, Google, Facebook)
    public override bool OpenUrl(UIApplication app, NSUrl url, NSDictionary options)
    {
        Console.WriteLine($"[AppDelegate] OpenUrl called with: {url}");

        // DEEP LINK via URL scheme: sospect://alarma/{id} (desde botón "Abrir en SOSpect" de la web)
        if (url.Scheme == "sospect" && url.Host == "alarma")
        {
            var alarmaId = url.Path?.TrimStart('/');
            if (!string.IsNullOrEmpty(alarmaId))
            {
                Console.WriteLine($"[AppDelegate] URL scheme deep link → alarmaId={alarmaId}");
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    (Microsoft.Maui.Controls.Application.Current as sospect.App)?.NavigateToAlarma(alarmaId);
                });
                return true;
            }
        }

        // OAUTH: Delegar al MAUI Platform para Apple Sign In / Google / Facebook
        if (Microsoft.Maui.ApplicationModel.Platform.OpenUrl(app, url, options))
        {
            Console.WriteLine("[AppDelegate] OpenUrl handled by MAUI Platform");
            return true;
        }

        Console.WriteLine("[AppDelegate] OpenUrl not handled by MAUI Platform");
        return base.OpenUrl(app, url, options);
    }

    // Extrae alarma_id del NSDictionary de userInfo de una notificación APNs
    internal static string ExtraerAlarmaId(NSDictionary userInfo)
    {
        if (userInfo == null) return null;
        var key = new NSString("alarma_id");
        if (userInfo.ContainsKey(key))
            return userInfo[key]?.ToString();
        return null;
    }
}

// Delegate para manejar notificaciones en foreground
public class UserNotificationCenterDelegate : UNUserNotificationCenterDelegate
{
    public override void WillPresentNotification(UNUserNotificationCenter center, UNNotification notification,
        Action<UNNotificationPresentationOptions> completionHandler)
    {
        Console.WriteLine("[Notifications] WillPresentNotification");
        // Mostrar notificación incluso cuando la app está en foreground
        completionHandler(UNNotificationPresentationOptions.Alert |
                         UNNotificationPresentationOptions.Badge |
                         UNNotificationPresentationOptions.Sound);
    }

    public override void DidReceiveNotificationResponse(UNUserNotificationCenter center,
        UNNotificationResponse response, Action completionHandler)
    {
        Console.WriteLine("[Notifications] DidReceiveNotificationResponse");

        // Extraer alarma_id y chat_id del payload y navegar (equivalente al NotificationOpened de Android)
        var userInfo = response.Notification.Request.Content.UserInfo;

        // Log de todo el payload para diagnóstico
        Console.WriteLine($"[Notifications] userInfo keys count={userInfo?.Count.ToString() ?? "-1"}");
        if (userInfo != null)
        {
            foreach (var key in userInfo.Keys)
                Console.WriteLine($"[Notifications]   key={key} value={userInfo[key]}");
        }

        var alarmaId = AppDelegate.ExtraerAlarmaId(userInfo);

        // 2026-04-10: Extraer chat_id (para notificaciones de mensajes de chat de promoción)
        string chatIdStr = "0";
        var chatKey = new Foundation.NSString("chat_id");
        if (userInfo != null && userInfo.ContainsKey(chatKey))
            chatIdStr = userInfo[chatKey]?.ToString() ?? "0";

        Console.WriteLine($"[Notifications] alarma_id={alarmaId}, chat_id={chatIdStr}");

        if (!string.IsNullOrEmpty(alarmaId))
        {
            Preferences.Set("alarma_id", alarmaId);
            Preferences.Set("chat_id_notif", chatIdStr);
            Console.WriteLine($"[Notifications] Guardado en Preferences → alarma_id={alarmaId}, chat_id_notif={chatIdStr}");

            if (App.Current is App app)
            {
                app.NavigateToAlarma(alarmaId);
            }
        }

        completionHandler();
    }
}
