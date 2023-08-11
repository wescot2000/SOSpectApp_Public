using System;
using CoreLocation;
using Foundation;
using sospect.Interfaces;
using sospect.iOS.Implementaciones;
using UIKit;
using Xamarin.Forms;

[assembly: Dependency(typeof(LocationSettings))]
namespace sospect.iOS.Implementaciones
{
    public class LocationSettings : ILocationSettings
    {
        public bool IsGpsAvailable()
        {
            bool value = false;

            if (CLLocationManager.LocationServicesEnabled)
            {
                if (CLLocationManager.Status == CLAuthorizationStatus.Authorized || CLLocationManager.Status == CLAuthorizationStatus.AuthorizedAlways || CLLocationManager.Status == CLAuthorizationStatus.AuthorizedWhenInUse)
                {//enable
                    value = true;
                }
                else if (CLLocationManager.Status == CLAuthorizationStatus.Denied)
                {
                    value = false;
                    OpenSettings();
                }
                else
                {
                    value = false;
                    RequestRuntime();
                }
            }
            else
            {
                //location service false
                value = false;
                //ask user to open system setting page to turn on it manually.
            }
            return value;
        }

        public void RequestRuntime()
        {
            CLLocationManager cLLocationManager = new CLLocationManager();
            cLLocationManager.RequestWhenInUseAuthorization();
        }

        public void OpenSettings()
        {

            UIApplication.SharedApplication.OpenUrl(new NSUrl(UIApplication.OpenSettingsUrlString));
        }
    }
}

