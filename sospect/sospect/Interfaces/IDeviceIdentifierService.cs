using System;

namespace sospect.Interfaces
{
    public interface IDeviceIdentifierService
    {
        string GenerateCombinedIdentifier(string sid);
    }
}