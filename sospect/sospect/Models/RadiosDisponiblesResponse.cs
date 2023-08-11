using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace sospect.Models
{
    public class RadiosDisponiblesResponse
    {
        [JsonProperty("radio_alarmas_id")]
        public int radio_alarmas_id { get; set; }

        [JsonProperty("radio_mts")]
        public int radio_mts { get; set; }

    }
}
