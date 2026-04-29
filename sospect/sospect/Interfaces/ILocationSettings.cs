using System;

namespace sospect.Interfaces
{
    public interface ILocationSettings
    {
        void OpenSettings();
        bool IsGpsAvailable();
    }
}