using System;
using Android.App;
using Android.Gms.Common;
using Firebase.Messaging;
using sospect.Models;
using sospect.Services;
using static Android.Provider.Settings;
using System.Threading.Tasks;
using Android.Gms.Extensions;

namespace sospect.Droid.Services
{
    public class DeviceInstallationService : IDeviceInstallationService
    {
        private string token;

        public string Token
        {
            get
            {
                // Si el token no ha sido obtenido, lo solicitamos.
                if (string.IsNullOrEmpty(token))
                    GetToken();

                return token;
            }
            set => token = value;
        }

        private async void GetToken()
        {
            try
            {
                token = (await FirebaseMessaging.Instance.GetToken()).ToString();
            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert("Error",ex.Message,"OK");
            }
        }

        public bool NotificationsSupported
            => GoogleApiAvailability.Instance
                .IsGooglePlayServicesAvailable(Application.Context) == ConnectionResult.Success;

        public string GetDeviceId()
            => Secure.GetString(Application.Context.ContentResolver, Secure.AndroidId);

        public DeviceInstallation GetDeviceInstallation(params string[] tags)
        {
            if (!NotificationsSupported)
                throw new Exception(GetPlayServicesError());

            if (string.IsNullOrWhiteSpace(Token))
                throw new Exception("Unable to resolve token for FCM");

            var installation = new DeviceInstallation
            {
                InstallationId = GetDeviceId(),
                Platform = "fcm",
                PushChannel = Token
            };

            App.TokenHubNotification = Token;
            installation.Tags.AddRange(tags);

            return installation;
        }

        string GetPlayServicesError()
        {
            int resultCode = GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(Application.Context);

            if (resultCode != ConnectionResult.Success)
                return GoogleApiAvailability.Instance.IsUserResolvableError(resultCode) ?
                           GoogleApiAvailability.Instance.GetErrorString(resultCode) :
                           "This device is not supported";

            return "An error occurred preventing the use of push notifications";
        }
    }
}

