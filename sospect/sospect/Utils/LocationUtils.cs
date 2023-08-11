using System;
using System.Threading;
using System.Threading.Tasks;
using sospect.Extensions;
using sospect.Interfaces;
using sospect.Models;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace sospect.Utils
{
    public class LocationUtils
    {
        public static async Task<Ubicaciones?> ObtenerUbicacionActual()
        {
            var request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(30));
            //CancellationTokenSource cts = new CancellationTokenSource();

            bool gpsStatus = DependencyService.Get<ILocationSettings>().IsGpsAvailable();
            if (gpsStatus)
            {
                var location = await Geolocation.GetLocationAsync(request);
                return new Ubicaciones() { latitud = location.Latitude.Trim(6), longitud = location.Longitude.Trim(6), Idioma = IdiomUtil.ObtenerCodigoDeIdioma() };
            }
            else
            {
                return null;
            }
        }

        public static async Task<Ubicaciones?> ObtenerUbicacionActualEnSegundoPlano()
        {
            var request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(30));

            bool gpsStatus = DependencyService.Get<ILocationSettings>().IsGpsAvailable();
            if (gpsStatus)
            {
                var location = await Geolocation.GetLocationAsync(request);
                return new Ubicaciones() { latitud = location.Latitude.Trim(6), longitud = location.Longitude.Trim(6), Idioma = IdiomUtil.ObtenerCodigoDeIdioma() };
            }
            else
            {
                return null;
            }
        }
    }
}

