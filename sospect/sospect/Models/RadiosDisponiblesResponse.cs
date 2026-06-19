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
    public class RadiosDisponiblesResponse
    {
        [JsonProperty("radio_alarmas_id")]
        public int radio_alarmas_id { get; set; }

        [JsonProperty("radio_mts")]
        public int radio_mts { get; set; }

    }
}


