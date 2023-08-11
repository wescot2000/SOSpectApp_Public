using System;
using sospect.Models;
using System.Collections.Generic;
using System.Linq;

namespace sospect.Services
{
    public class PushDemoNotificationActionService : IPushDemoNotificationActionService
    {
        readonly Dictionary<string, string> _actionMappings = new Dictionary<string, string>
        {
            { "action_a", "" },
            { "action_b", "" }
        };

        public event EventHandler<string> ActionTriggered = delegate { };

        public void TriggerAction(string action)
        {
            if (!_actionMappings.TryGetValue(action, out var pushDemoAction))
                return;

            List<Exception> exceptions = new List<Exception>();

            foreach (var handler in ActionTriggered?.GetInvocationList())
            {
                try
                {
                    handler.DynamicInvoke(this, pushDemoAction);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            if (exceptions.Any())
                throw new AggregateException(exceptions);
        }
    }
}

