using System;
using System.Threading.Tasks;

namespace sospect.Interfaces
{
	public interface IPermissionManager
	{
		Task<bool> CheckNotificationPermission();
	}
}

