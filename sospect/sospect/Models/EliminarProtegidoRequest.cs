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
