// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using sospect.Interfaces;
using sospect.Models;
using sospect.Services;
using sospect.Utils;
using sospect.Helpers;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Devices.Sensors;

namespace sospect.Platforms.Android.Services
{
    [Service(
        ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeLocation,
        Exported = false
    )]
    public class LocationForegroundService : Service
    {
        private const int NOTIFICATION_ID = 9999;
        private const string CHANNEL_ID = "LocationTrackingChannel";
        private Timer _locationTimer;
        private TimeSpan _updateInterval; // Ahora es dinámico desde configuración del usuario

        public override IBinder OnBind(Intent intent) => null;

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            System.Diagnostics.Debug.WriteLine("========================================");
            System.Diagnostics.Debug.WriteLine("[LocationForegroundService] OnStartCommand LLAMADO");
            System.Diagnostics.Debug.WriteLine($"[LocationForegroundService] Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            System.Diagnostics.Debug.WriteLine($"[LocationForegroundService] StartId: {startId}");
            System.Diagnostics.Debug.WriteLine("========================================");

            // Leer intervalo configurado por el usuario desde Preferences
            var intervaloPref = Preferences.Get("intervalo_background_minutos", 5); // Default 5 minutos
            _updateInterval = TimeSpan.FromMinutes(intervaloPref);

            System.Diagnostics.Debug.WriteLine($"[LocationForegroundService] Intervalo configurado: {intervaloPref} minutos");

            // Crear y mostrar notificación persistente (requerido para foreground service)
            var notification = CreateNotification();
            StartForeground(NOTIFICATION_ID, notification);

            System.Diagnostics.Debug.WriteLine("[LocationForegroundService] Notificación foreground creada");

            // Iniciar timer para actualizar ubicación según intervalo configurado
            _locationTimer = new Timer(
                async _ => await UpdateLocationInBackground(),
                null,
                TimeSpan.Zero,  // Ejecutar inmediatamente
                _updateInterval
            );

            System.Diagnostics.Debug.WriteLine($"[LocationForegroundService] Timer iniciado - primera ejecución inmediata, luego cada {intervaloPref} minutos");
            System.Diagnostics.Debug.WriteLine($"[LocationForegroundService] Próxima actualización programada para: {DateTime.Now.Add(_updateInterval):yyyy-MM-dd HH:mm:ss}");

            return StartCommandResult.Sticky; // Reiniciar si el OS lo mata
        }

        private async Task UpdateLocationInBackground()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("========================================");
                System.Diagnostics.Debug.WriteLine($"[LocationForegroundService] TICK DEL TIMER: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                System.Diagnostics.Debug.WriteLine("[LocationForegroundService] Iniciando actualización de ubicación...");

                // Obtener user_id desde SecureStorage
                var userId = await SecureStorage.GetAsync("user_id_thirdparty");
                System.Diagnostics.Debug.WriteLine($"[LocationForegroundService] UserId obtenido: {(string.IsNullOrEmpty(userId) ? "VACIO" : "OK")}");

                if (string.IsNullOrEmpty(userId))
                {
                    System.Diagnostics.Debug.WriteLine("[LocationForegroundService] No hay usuario logueado, omitiendo actualización");
                    System.Diagnostics.Debug.WriteLine("========================================");
                    return;
                }

                // Obtener ubicación actual directamente (sin depender de ILocationSettings)
                System.Diagnostics.Debug.WriteLine("[LocationForegroundService] Solicitando ubicación GPS...");

                Location location = null;
                try
                {
                    var request = new Microsoft.Maui.Devices.Sensors.GeolocationRequest(
                        Microsoft.Maui.Devices.Sensors.GeolocationAccuracy.Best,
                        TimeSpan.FromSeconds(10) // Timeout más largo para background
                    );

                    location = await Microsoft.Maui.Devices.Sensors.Geolocation.GetLocationAsync(request);
                }
                catch (Exception gpsEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[LocationForegroundService] Error obteniendo GPS: {gpsEx.Message}");
                }

                if (location == null)
                {
                    System.Diagnostics.Debug.WriteLine("[LocationForegroundService] ERROR: No se pudo obtener ubicación GPS");
                    System.Diagnostics.Debug.WriteLine("========================================");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[LocationForegroundService] Ubicación GPS obtenida: Lat={location.Latitude}, Lon={location.Longitude}");

                // Obtener país e idioma del usuario desde Preferences (guardado en login)
                string paisId = "CO"; // Default fallback
                string idiomaId = "es"; // Default fallback
                try
                {
                    var userJson = Preferences.Get("User", "");
                    if (!string.IsNullOrEmpty(userJson))
                    {
                        var persona = Newtonsoft.Json.JsonConvert.DeserializeObject<Persona>(userJson);
                        if (persona != null)
                        {
                            if (!string.IsNullOrEmpty(persona.Pais))
                            {
                                paisId = persona.Pais;
                                System.Diagnostics.Debug.WriteLine($"[LocationForegroundService] País obtenido de usuario: {paisId}");
                            }
                            if (!string.IsNullOrEmpty(persona.Idioma))
                            {
                                idiomaId = persona.Idioma;
                                System.Diagnostics.Debug.WriteLine($"[LocationForegroundService] Idioma obtenido de usuario: {idiomaId}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[LocationForegroundService] Error obteniendo datos del usuario: {ex.Message}, usando defaults");
                }

                // Llamar al API ligero para background
                System.Diagnostics.Debug.WriteLine("[LocationForegroundService] Llamando API ActualizarUbicacionBackground...");
                System.Diagnostics.Debug.WriteLine($"[LocationForegroundService] Params: Lat={location.Latitude}, Lon={location.Longitude}, Pais={paisId}, Idioma={idiomaId}");

                var resultado = await ApiService.ActualizarUbicacionBackground(
                    (decimal)location.Latitude,
                    (decimal)location.Longitude,
                    paisId,
                    idiomaId
                );

                if (resultado.success && resultado.response != null)
                {
                    System.Diagnostics.Debug.WriteLine("========================================");
                    System.Diagnostics.Debug.WriteLine("[LocationForegroundService] ✓ API CALL SUCCESS");
                    System.Diagnostics.Debug.WriteLine(
                        $"[LocationForegroundService] Alarmas: {resultado.response.CantidadAlarmas}, " +
                        $"Notificación enviada: {resultado.response.NotificacionEnviada}, Mensaje: {resultado.response.Mensaje}"
                    );
                    System.Diagnostics.Debug.WriteLine($"[LocationForegroundService] Próxima actualización en {_updateInterval.TotalMinutes} minutos");
                    System.Diagnostics.Debug.WriteLine("========================================");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("========================================");
                    System.Diagnostics.Debug.WriteLine("[LocationForegroundService] ✗ API CALL FAILED");
                    System.Diagnostics.Debug.WriteLine($"[LocationForegroundService] Error: {resultado.error}");
                    System.Diagnostics.Debug.WriteLine("========================================");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("========================================");
                System.Diagnostics.Debug.WriteLine("[LocationForegroundService] ✗ EXCEPTION");
                System.Diagnostics.Debug.WriteLine($"[LocationForegroundService] Message: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[LocationForegroundService] StackTrace: {ex.StackTrace}");
                System.Diagnostics.Debug.WriteLine("========================================");
            }
        }

        private Notification CreateNotification()
        {
            // Obtener textos traducidos desde AppResources
            string canalNombre = TranslateExtension.Translate("NotificacionCanalNombre");
            string canalDescripcion = TranslateExtension.Translate("NotificacionCanalDescripcion");
            string notifTitulo = TranslateExtension.Translate("NotificacionServicioTitulo");
            string notifTexto = TranslateExtension.Translate("NotificacionServicioTexto");

            // Crear canal de notificación para Android 8.0+
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(
                    CHANNEL_ID,
                    canalNombre,
                    NotificationImportance.Low
                )
                {
                    Description = canalDescripcion,
                    LockscreenVisibility = NotificationVisibility.Public
                };

                var notificationManager = GetSystemService(NotificationService) as NotificationManager;
                notificationManager?.CreateNotificationChannel(channel);
            }

            // Construir notificación persistente
            var notificationBuilder = new NotificationCompat.Builder(this, CHANNEL_ID)
                .SetContentTitle(notifTitulo)
                .SetContentText(notifTexto)
                .SetSmallIcon(Resource.Drawable.ic_notificacionespush)
                .SetOngoing(true)  // No puede ser descartada por el usuario
                .SetPriority(NotificationCompat.PriorityLow)
                .SetCategory(NotificationCompat.CategoryService);

            return notificationBuilder.Build();
        }

        public override void OnDestroy()
        {
            System.Diagnostics.Debug.WriteLine("[LocationForegroundService] Deteniendo servicio");

            _locationTimer?.Dispose();
            StopForeground(true);

            base.OnDestroy();
        }
    }
}


