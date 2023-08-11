using System;
using Android.Content;
using sospect.Droid.Implementaciones;
using sospect.Interfaces;
using Xamarin.Forms;

[assembly: Dependency(typeof(LocationSettings))]
namespace sospect.Droid.Implementaciones
{
    public class LocationSettings : ILocationSettings
    {
        public bool IsGpsAvailable()
        {
            bool value = false;
            Android.Locations.LocationManager manager = (Android.Locations.LocationManager)Android.App.Application.Context.GetSystemService(Android.Content.Context.LocationService);
            if (!manager.IsProviderEnabled(Android.Locations.LocationManager.GpsProvider))
            {
                //gps disable
                value = false;
            }
            else
            {
                //Gps enable
                value = true;
            }
            return value;
        }

        public void OpenSettings()
        {
            Intent intent = new Android.Content.Intent(Android.Provider.Settings.ActionLocat‌​ionSourceSettings);
            intent.AddFlags(ActivityFlags.NewTask);
            Android.App.Application.Context.StartActivity(intent);
        }
    }
}

