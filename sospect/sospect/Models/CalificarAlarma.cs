// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
using Newtonsoft.Json;

namespace sospect.Models
{
    public class CalificarAlarma
    {
        [JsonProperty("p_user_id_thirdparty")]
        public string PUserIdThirdparty { get; set; }

        [JsonProperty("alarma_id")]
        public long AlarmaId { get; set; }

        [JsonProperty("VeracidadAlarma")]
        public bool VeracidadAlarma { get; set; }
    }
}



