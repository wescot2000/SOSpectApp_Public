using System;

namespace sospect.Interfaces
{
    public interface INotification
    {
        void SendNotification(string message, string alarma_id);
    }
}