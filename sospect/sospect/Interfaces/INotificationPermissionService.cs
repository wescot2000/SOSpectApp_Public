using System;
using System.Threading.Tasks;

namespace sospect.Interfaces
{
    public interface INotificationPermissionService
    {
        Task<bool> RequestPermissionAsync();
    }
}