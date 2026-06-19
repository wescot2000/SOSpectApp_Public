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
    public class SuspenderPermisoRequest
    {
        [JsonProperty("p_user_id_thirdparty_protegido")]
        public string UserIdThirdPartyProtegido { get; set; }

        [JsonProperty("p_tiempo_suspension")]
        public int TiempoSuspension { get; set; }

        [JsonProperty("idioma")]
        public string Idioma { get; set; }
    }

}


