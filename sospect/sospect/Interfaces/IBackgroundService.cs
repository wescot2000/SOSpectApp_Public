using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using sospect.Models;

namespace sospect.Interfaces
{
    public interface IBackgroundService
    {
        Task RunCodeInBackgroundMode(Func<Ubicaciones, Task<List<AlarmaCercana>>> action, string name = "BackgroundService");
    }
}

