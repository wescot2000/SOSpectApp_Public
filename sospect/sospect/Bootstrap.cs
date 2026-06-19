// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
using sospect.Services;

namespace sospect
{
    public static class Bootstrap
    {
        public static void Begin(Func<IDeviceInstallationService> deviceInstallationService)
        {
            ServiceContainer.Register(deviceInstallationService);

            ServiceContainer.Register<IPushDemoNotificationActionService>(()
                => new PushDemoNotificationActionService());

            ServiceContainer.Register<INotificationRegistrationService>(()
                => new NotificationRegistrationService(
                    AppConfiguration.ApiHost,
                    ""));
        }
    }
}



