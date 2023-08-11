using Android.App;
using Android.Content;
using Android.Locations;
using Android.OS;
using Android.Runtime;
using Newtonsoft.Json;
using sospect;
using sospect.Interfaces;
using sospect.Models;
using sospect.Services;
using sospect.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using static Java.Util.Jar.Attributes;

[assembly: Dependency(typeof(IBackgroundService))]
namespace sospect.Droid.Services
{
    [Service]
    public class BackgroundService : Service, IBackgroundService, ILocationListener
    {
        private static Timer _timer;
        private static bool _timerStarted;
        private static double anterior_latitud = 0;
        private static double anterior_longitud = 0;
        private static double distanciaEnMetros = 0;
        private static Ubicaciones ubicacionActual;


        public override IBinder? OnBind(Intent? intent)
        {
            return null;
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            // Aquí se debe iniciar el seguimiento de ubicación en segundo plano
            ubicacionActual = new Ubicaciones();
            RunCodeInBackgroundMode(ApiService.ActualizarUbicacion);
            return StartCommandResult.Sticky;
        }

        public override void OnDestroy()
        {
            //base.OnDestroy();
            //_timerStarted = false;
            //_timer?.Dispose();
            //StopSelf();
        }

        public async Task RunCodeInBackgroundMode(Func<Ubicaciones, Task<List<AlarmaCercana>>> action, string name = "BackgroundService")
        {
            _timerStarted = true;
            _timer = new Timer(RunCode, action, 0, 60000);
        }

        private async void RunCode(object state)
        {
            Func<Ubicaciones, Task<List<AlarmaCercana>>> action = (Func<Ubicaciones, Task<List<AlarmaCercana>>>)state;
            var powerManager = (PowerManager)Android.App.Application.Context.GetSystemService(Context.PowerService);
            var wakeLock = powerManager.NewWakeLock(WakeLockFlags.Partial, "BackgroundService");
            //acquire a partial wakelock. This prevents the phone from going to sleep as long as it is not released.
            wakeLock.Acquire();
            var taskEnded = false;

            await Task.Factory.StartNew(async () =>
            {
                try
                {
                    //here we run the actual code
                    System.Diagnostics.Debug.WriteLine($"Background task '{"BackgroundService"}' started");
                    //Ubicaciones? ubicacionActual = await LocationUtils.ObtenerUbicacionActual();

                    LocationManager locationManager = GetSystemService(LocationService) as LocationManager;
                    locationManager.RequestLocationUpdates(LocationManager.GpsProvider, 20000, 30, this);

                    App.ubicacionActual = ubicacionActual;

                    if (anterior_latitud != 0 && anterior_longitud != 0)
                    {
                        distanciaEnMetros = HaversineUtils.DistanceInMeters(anterior_latitud, anterior_longitud, ubicacionActual.latitud, ubicacionActual.longitud);
                        System.Diagnostics.Debug.WriteLine("Distancia en metros: " + distanciaEnMetros);
                    }

                    anterior_latitud = ubicacionActual.latitud;
                    anterior_longitud = ubicacionActual.longitud;
                    ubicacionActual.p_user_id_thirdparty = App.persona.user_id_thirdparty;

                    //Solo se podra actualizar la ubicacion si existe una ubicacion actual, una sesion de usuario y el usuario se ha movido al menos 35 metros de su anterior posición
                    if (ubicacionActual != null && !string.IsNullOrEmpty(App.persona.user_id_thirdparty) && distanciaEnMetros >= 70)
                    {

                        App.AlarmasCercanasAMostrar = await action(arg: ubicacionActual);

                        MessagingCenter.Send<IBackgroundService, List<AlarmaCercana>>(this, "", App.AlarmasCercanasAMostrar);
                    }

                    System.Diagnostics.Debug.WriteLine($"Background task '{"BackgroundService"}' finished");
                    wakeLock.Release();
                    taskEnded = true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error presentado '{ex.StackTrace}'");
                }
            });
        }

        public void OnLocationChanged(Android.Locations.Location location)
        {
            ubicacionActual.latitud = location.Latitude;
            ubicacionActual.latitud = location.Longitude;
            ubicacionActual.p_user_id_thirdparty = App.persona?.user_id_thirdparty;
            ubicacionActual.Idioma = IdiomUtil.ObtenerCodigoDeIdioma();
        }

        public void OnProviderDisabled(string provider)
        {

        }

        public void OnProviderEnabled(string provider)
        {

        }

        public void OnStatusChanged(string? provider, [GeneratedEnum] Availability status, Bundle? extras)
        {

        }
    }
}

