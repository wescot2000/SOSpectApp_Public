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
    public class CerrarAlarmaRequest
    {
        [JsonProperty("p_alarma_id")]
        public long p_alarma_id { get; set; }

        [JsonProperty("p_user_id_thirdparty")]
        public string p_user_id_thirdparty { get; set; }

        [JsonProperty("p_descripcion_cierre")]
        public string p_descripcion_cierre { get; set; }

        [JsonProperty("p_flag_es_falsaalarma")]
        public bool p_flag_es_falsaalarma { get; set; }

        [JsonProperty("p_flag_hubo_captura")]
        public bool p_flag_hubo_captura { get; set; }

        [JsonProperty("p_idioma")]
        public string p_idioma { get; set; }

        [JsonProperty("p_tipo_cierre")]
        public string p_tipo_cierre { get; set; }

        [JsonProperty("p_flag_persona_encontrada")]
        public bool? p_flag_persona_encontrada { get; set; }

        [JsonProperty("p_flag_mascota_recuperada")]
        public bool? p_flag_mascota_recuperada { get; set; }

        [JsonProperty("Fotos")]
        public List<FotoAlarmaDto> Fotos { get; set; }
    }

}


