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
    public class EliminarProtegidoRequest
    {
        [JsonProperty("p_user_id_thirdparty_protector")]
        public string p_user_id_thirdparty_protector { get; set; }

        [JsonProperty("p_user_id_thirdparty_protegido")]
        public string p_user_id_thirdparty_protegido { get; set; }

        [JsonProperty("Idioma")]
        public string Idioma { get; set; }
    }

}


