using System.Collections.Generic;

namespace sospect.Models
{
    public class ConteoUsuariosPorRadio
    {
        public int RadioMetros { get; set; }
        public int CantidadUsuarios { get; set; }
    }

    public class ObtenerConteosPorIntervalosResponse
    {
        public bool IsSuccess { get; set; }
        public List<ConteoUsuariosPorRadio> Conteos { get; set; }
        public string Message { get; set; }
    }
}
