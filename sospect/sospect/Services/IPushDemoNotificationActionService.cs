using System;
using sospect.Models;

namespace sospect.Services
{
    public interface IPushDemoNotificationActionService : INotificationActionService
    {
        event EventHandler<string> ActionTriggered;
    }
}

