using System;
using sospect.Models;
using sospect.Services;
using UIKit;
using sospect.Helpers;

namespace sospect.iOS.Services
{
    public class DeviceInstallationService : IDeviceInstallationService
    {
        const int SupportedVersionMajor = 13;
        const int SupportedVersionMinor = 0;

        public string Token { get; set; }

        public bool NotificationsSupported
            => UIDevice.CurrentDevice.CheckSystemVersion(SupportedVersionMajor, SupportedVersionMinor);

        public string GetDeviceId()
            => UIDevice.CurrentDevice.IdentifierForVendor.ToString();

        public DeviceInstallation GetDeviceInstallation(params string[] tags)
        {
            if (string.IsNullOrWhiteSpace(Token))
                throw new Exception(GetNotificationsSupportError());

            App.TokenHubNotification = Token;
            var installation = new DeviceInstallation
            {
                InstallationId = GetDeviceId(),
                Platform = "apns",
                PushChannel = Token
            };

            installation.Tags.AddRange(tags);

            return installation;
        }

        string GetNotificationsSupportError()
        {
            //if (!NotificationsSupported)
            //  return $"This app only supports notifications on iOS {SupportedVersionMajor}.{SupportedVersionMinor} and above. You are running {UIDevice.CurrentDevice.SystemVersion}.";

            var LblHabilitarNotifSettings = TranslateExtension.Translate("LblHabilitarNotifSettings");
            var LblErrorHabilitandoUsoNotif = TranslateExtension.Translate("LblErrorHabilitandoUsoNotif");

            if (Token == null)
                return $"{LblHabilitarNotifSettings}";


            return $"{LblErrorHabilitandoUsoNotif}";
        }
    }
}

