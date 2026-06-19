// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace sospect.Models
{
    public class CancelarSubscripcionRequest
    {
        [JsonProperty("subscripcion_id")]
        public long subscripcion_id { get; set; }

        [JsonProperty("user_id_thirdparty")]
        public string user_id_thirdparty { get; set; }

        [JsonProperty("idioma")]
        public string idioma { get; set; }
    }
}


