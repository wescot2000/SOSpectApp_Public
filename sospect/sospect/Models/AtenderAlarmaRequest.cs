using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace sospect.Models
{
    public class AtenderAlarmaRequest
    {
        [JsonProperty("p_alarma_id")]
        public long p_alarma_id { get; set; }

        [JsonProperty("p_user_id_thirdparty")]
        public string p_user_id_thirdparty { get; set; }

        [JsonProperty("p_idioma")]
        public string p_idioma { get; set; }
    }

}
