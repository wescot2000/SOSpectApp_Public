using Microsoft.Maui.ApplicationModel;
using sospect.Interfaces;
using Microsoft.Maui.Devices.Sensors;

namespace sospect.Services
{
    public class LocationSettingsService : ILocationSettings
    {
        public bool IsGpsAvailable()
        {
            // Verifica si el dispositivo tiene acceso a la ubicación
            var current = Connectivity.Current.NetworkAccess;
            
            // Devuelve true si la conectividad es igual a Internet (esto implica que el GPS está disponible)
            return current == NetworkAccess.Internet && Geolocation.Default != null;
        }

        public void OpenSettings()
        {
            // Abre la configuración del sistema para permitir al usuario habilitar el GPS
            AppInfo.ShowSettingsUI();
        }
    }
}