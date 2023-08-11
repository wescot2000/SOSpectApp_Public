using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace sospect.Models
{
    public class RenovarSubscripcionRequest
    {
        [JsonProperty("subscripcion_id")]
        public long subscripcion_id { get; set; }

        [JsonProperty("user_id_thirdparty")]
        public string user_id_thirdparty { get; set; }

        [JsonProperty("cantidad_poderes")]
        public int cantidad_poderes { get; set; }

        [JsonProperty("idioma")]
        public string idioma { get; set; }
    }
}
