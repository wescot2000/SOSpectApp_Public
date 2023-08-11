using Foundation;
using sospect.Interfaces;
using sospect.iOS.Services;
using sospect.Services;
using UIKit;
using UserNotifications;
using Xamarin.Forms;

[assembly: Dependency(typeof(SettingsService))]
namespace sospect.iOS.Services
{
    public class SettingsService : ISettingsService
    {
        public void OpenSettings()
        {
            UIApplication.SharedApplication.OpenUrl(new NSUrl(UIApplication.OpenSettingsUrlString));
        }

        public void RegisterDeviceAgain()
        {
                UNUserNotificationCenter.Current.RequestAuthorization(
                        UNAuthorizationOptions.Alert |
                        UNAuthorizationOptions.Badge |
                        UNAuthorizationOptions.Sound,
                        (approvalGranted, error) =>
                        {
                            if (approvalGranted && error == null)
                                AppDelegate.RegisterForRemoteNotifications();
                        });
        }
    }
}
