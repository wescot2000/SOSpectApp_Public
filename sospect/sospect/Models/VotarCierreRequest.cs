using Newtonsoft.Json;

namespace sospect.Models
{
    public class VotarCierreRequest
    {
        [JsonProperty("p_solicitud_id")]
        public long p_solicitud_id { get; set; }

        [JsonProperty("p_user_id_thirdparty")]
        public string p_user_id_thirdparty { get; set; }

        [JsonProperty("p_voto")]
        public bool p_voto { get; set; }
    }
}
