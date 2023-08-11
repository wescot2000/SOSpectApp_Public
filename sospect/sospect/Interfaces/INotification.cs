using System;
namespace sospect.Interfaces
{
    public interface INotification
    {
        public void SendNotification(string message, string alarma_id);
    }
}

