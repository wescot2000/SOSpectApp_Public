using System;
using System.Threading.Tasks;
using sospect.Interfaces;
using sospect.iOS.Implementaciones;
using UserNotifications;
using Xamarin.Forms;

[assembly: Dependency(typeof(PermissionManageriOS))]
namespace sospect.iOS.Implementaciones
{
    public class PermissionManageriOS : IPermissionManager
    {
        public Task<bool> CheckNotificationPermission()
        {
            var tcs = new TaskCompletionSource<bool>();

            UNUserNotificationCenter.Current.GetNotificationSettings((settings) =>
            {
                var hasPermission = settings.AlertSetting == UNNotificationSetting.Enabled;
                tcs.SetResult(hasPermission);
            });

            return tcs.Task;
        }
    }
}

