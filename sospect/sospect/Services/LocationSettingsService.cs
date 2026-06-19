// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

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

