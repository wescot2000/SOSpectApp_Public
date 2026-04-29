using System;
namespace sospect.Models
{
    public class AgregarUsuarioRedConfianzaRequest
    {
        public string UserIdThirdpartyLider { get; set; }
        public string UserIdThirdpartyNuevo { get; set; }
        public string Nickname { get; set; }
    }
}
