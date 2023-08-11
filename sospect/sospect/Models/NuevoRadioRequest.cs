using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace sospect.Models
{
    public class NuevoRadioRequest
    {
        [JsonProperty("p_user_id_thirdparty_protector")]
        public string p_user_id_thirdparty_protector { get; set; }

        [JsonProperty("cantidad_subscripcion")]
        public int cantidad_subscripcion { get; set; }

        [JsonProperty("idioma")]
        public string idioma { get; set; }
    }
}
