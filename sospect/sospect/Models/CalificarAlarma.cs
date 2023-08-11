using System;
using Newtonsoft.Json;

namespace sospect.Models
{
    public class CalificarAlarma
    {
        [JsonProperty("p_user_id_thirdparty")]
        public string PUserIdThirdparty { get; set; }

        [JsonProperty("alarma_id")]
        public long AlarmaId { get; set; }

        [JsonProperty("VeracidadAlarma")]
        public bool VeracidadAlarma { get; set; }
    }
}

