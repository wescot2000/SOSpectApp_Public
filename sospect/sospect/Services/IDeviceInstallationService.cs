// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
using sospect.Models;

namespace sospect.Services
{
    public interface IDeviceInstallationService
    {
        string Token { get; set; }
        bool NotificationsSupported { get; }
        string GetDeviceId();
        DeviceInstallation GetDeviceInstallation(params string[] tags);
    }
}



