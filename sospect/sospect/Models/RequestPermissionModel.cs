// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace sospect.Models
{
    public class RequestPermissionModel
    {
        [JsonProperty("p_user_id_thirdparty_protector")]
        public string PUserIdThirdpartyProtector { get; set; }

        [JsonProperty("p_user_id_thirdparty_protegido")]
        public string PUserIdThirdpartyProtegido { get; set; }

        [JsonProperty("tiempo_subscripcion_dias")]
        public int TiempoSubscripcionDias { get; set; }

        [JsonProperty("idioma")]
        public string Idioma { get; set; }

        [JsonProperty("TiporelacionId")]
        public int TiporelacionId { get; set; }
    }


}


