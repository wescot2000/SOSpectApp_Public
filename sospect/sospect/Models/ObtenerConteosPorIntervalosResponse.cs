// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

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


