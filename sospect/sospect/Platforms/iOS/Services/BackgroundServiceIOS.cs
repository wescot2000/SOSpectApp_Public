using CoreLocation;
using Foundation;
using sospect.Interfaces;
using sospect.Models;
using sospect.Services;
using sospect.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace sospect.Platforms.iOS.Services
{
    public class BackgroundServiceIOS : IBackgroundService
    {
        private CLLocationManager _locationManager;
        private bool _isTracking = false;

        public Task RunCodeInBackgroundMode(
            Func<Ubicaciones, Task<List<AlarmaCercana>>> action,
            string name = "BackgroundService")
        {
            if (_isTracking)
            {
                System.Diagnostics.Debug.WriteLine("[BackgroundServiceIOS] Ya está activo");
                return Task.CompletedTask;
            }

            System.Diagnostics.Debug.WriteLine("[BackgroundServiceIOS] Iniciando seguimiento");

            // NOTA: iOS usa DistanceFilter (40 metros) en lugar de intervalo de tiempo.
            // El intervalo configurable por el usuario (intervalo_background_minutos)
            // se usa solo en Android. En iOS, el API se llama cuando el usuario se
            // mueve 40+ metros, lo cual es más eficiente para la batería.
            _locationManager = new CLLocationManager
            {
                AllowsBackgroundLocationUpdates = true,
                PausesLocationUpdatesAutomatically = false,
                DesiredAccuracy = CLLocation.AccuracyBest,
                DistanceFilter = 40  // 40 metros de movimiento
            };

            _locationManager.LocationsUpdated += async (sender, e) =>
            {
                var location = e.Locations[^1];  // Última ubicación
                if (location != null)
                {
                    await UpdateLocationInBackground(location);
                }
            };

            // Solicitar permiso "Always" si no está concedido
            if (CLLocationManager.Status != CLAuthorizationStatus.AuthorizedAlways)
            {
                _locationManager.RequestAlwaysAuthorization();
            }

            _locationManager.StartUpdatingLocation();
            _locationManager.StartMonitoringSignificantLocationChanges(); // Sobrevive force-close
            _isTracking = true;
            Preferences.Set("ios_tracking_active", true); // Persiste estado para re-lanzamiento tras force-close

            System.Diagnostics.Debug.WriteLine("[BackgroundServiceIOS] Seguimiento iniciado (GPS 40m + cambios significativos)");
            return Task.CompletedTask;
        }

        private async Task UpdateLocationInBackground(CLLocation location)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[BackgroundServiceIOS] Nueva ubicación: {location.Coordinate.Latitude}, {location.Coordinate.Longitude}");

                var userId = await SecureStorage.GetAsync("user_id_thirdparty");
                if (string.IsNullOrEmpty(userId))
                {
                    System.Diagnostics.Debug.WriteLine("[BackgroundServiceIOS] No hay usuario logueado");
                    return;
                }

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
                                System.Diagnostics.Debug.WriteLine($"[BackgroundServiceIOS] País obtenido de usuario: {paisId}");
                            }
                            if (!string.IsNullOrEmpty(persona.Idioma))
                            {
                                idiomaId = persona.Idioma;
                                System.Diagnostics.Debug.WriteLine($"[BackgroundServiceIOS] Idioma obtenido de usuario: {idiomaId}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[BackgroundServiceIOS] Error obteniendo datos del usuario: {ex.Message}, usando defaults");
                }

                // Llamar al API ligero para background
                var resultado = await ApiService.ActualizarUbicacionBackground(
                    (decimal)location.Coordinate.Latitude,
                    (decimal)location.Coordinate.Longitude,
                    paisId,
                    idiomaId
                );

                if (resultado.success && resultado.response != null)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[BackgroundServiceIOS] Ubicación actualizada. Alarmas: {resultado.response.CantidadAlarmas}, " +
                        $"Notificación enviada: {resultado.response.NotificacionEnviada}, Mensaje: {resultado.response.Mensaje}"
                    );
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[BackgroundServiceIOS] Error actualizando ubicación: {resultado.error}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BackgroundServiceIOS] Error: {ex.Message}");
            }
        }

        public Task StopBackgroundService()
        {
            if (_locationManager != null && _isTracking)
            {
                _locationManager.StopUpdatingLocation();
                _locationManager.StopMonitoringSignificantLocationChanges();
                _isTracking = false;
                Preferences.Set("ios_tracking_active", false); // Limpia estado: no relanzar si el usuario desactivó
                System.Diagnostics.Debug.WriteLine("[BackgroundServiceIOS] Seguimiento detenido");
            }

            return Task.CompletedTask;
        }
    }
}
