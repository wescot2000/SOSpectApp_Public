using System;
using System.Threading.Tasks;
using sospect.Droid.Implementaciones;
using sospect.Interfaces;
using Xamarin.Forms;

[assembly: Dependency(typeof(PermissionManagerAndroid))]
namespace sospect.Droid.Implementaciones
{
    public class PermissionManagerAndroid : IPermissionManager
    {
        public Task<bool> CheckNotificationPermission()
        {
            return Task.Run(() => true);
        }
    }
}

