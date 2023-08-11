using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace sospect.Models
{
    public class NuevaZonaVRequest
    {
        [JsonProperty("p_user_id_thirdparty_protector")]
        public string p_user_id_thirdparty_protector { get; set; }

        [JsonProperty("Latitud_zona")]
        public double Latitud_zona { get; set; }

        [JsonProperty("Longitud_zona")]
        public double Longitud_zona { get; set; }

        [JsonProperty("idioma")]
        public string idioma { get; set; }
    }
}
