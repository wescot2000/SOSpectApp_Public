using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Devices.Sensors;
using sospect.Extensions;
using sospect.Interfaces;
using sospect.Models;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;  // Importante para obtener servicios

namespace sospect.Utils
{
    public class LocationUtils
    {
        public static async Task<Ubicaciones?> ObtenerUbicacionActual()
        {
            var request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(3));

            // Obtener el servicio de GPS usando MAUI DI a través de Application.Current
            var gpsService = MauiProgram.CreateMauiApp().Services.GetService<ILocationSettings>();
            if (gpsService != null && gpsService.IsGpsAvailable())
            {
                var location = await Geolocation.GetLocationAsync(request);
                return new Ubicaciones()
                {
                    latitud = location.Latitude.Trim(6),
                    longitud = location.Longitude.Trim(6),
                    Idioma = IdiomUtil.ObtenerCodigoDeIdioma()
                };
            }
            else
            {
                return null;
            }
        }

        public static async Task<Ubicaciones?> ObtenerUbicacionActualEnSegundoPlano()
        {
            var request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(3));

            var gpsService = MauiProgram.CreateMauiApp().Services.GetService<ILocationSettings>();
            if (gpsService != null && gpsService.IsGpsAvailable())
            {
                var location = await Geolocation.GetLocationAsync(request);
                return new Ubicaciones()
                {
                    latitud = location.Latitude.Trim(6),
                    longitud = location.Longitude.Trim(6),
                    Idioma = IdiomUtil.ObtenerCodigoDeIdioma()
                };
            }
            else
            {
                return null;
            }
        }
    }
}