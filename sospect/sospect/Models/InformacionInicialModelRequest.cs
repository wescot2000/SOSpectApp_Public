using System;
using System.Collections.Generic;
using System.Text;

namespace sospect.Models
{
    public class InformacionInicialModelRequest
    {
        public string UserIdThirdparty { get; set; }
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public string NumeroMovil { get; set; }
        public string Email { get; set; }
        public string Pais { get; set; }
        public string NationalId { get; set; }
    }
}