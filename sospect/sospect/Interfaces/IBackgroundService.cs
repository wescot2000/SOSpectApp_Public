// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using sospect.Models;

namespace sospect.Interfaces
{
    public interface IBackgroundService
    {
        Task RunCodeInBackgroundMode(Func<Ubicaciones, Task<List<AlarmaCercana>>> action, string name = "BackgroundService");
        Task StopBackgroundService();
    }
}


