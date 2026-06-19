// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

namespace sospect.Models
{
    public class VerificarAceptacionTerminosResponse
    {
        public bool IsSuccess { get; set; }
        public long? ChatId { get; set; }
        public bool YaAceptoTerminos { get; set; }
        public string EstadoChat { get; set; }
        public bool EsProveedor { get; set; }
        public string Message { get; set; }
    }
}


