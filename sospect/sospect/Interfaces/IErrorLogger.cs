using System;
using System.Collections.Generic;
using System.Text;

namespace sospect.Interfaces
{
    public interface IErrorLogger
    {
        void LogError(Exception ex, Dictionary<string, string> properties = null);
    }
}
