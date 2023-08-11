using System;
using Newtonsoft.Json;

namespace sospect.Models
{
    public class CalificacionDescripcionAlarma
    {
        [JsonProperty("p_user_id_thirdparty")]
        public string PUserIdThirdparty { get; set; }

        [JsonProperty("iddescripcion")]
        public long Iddescripcion { get; set; }

        [JsonProperty("CalificacionDescripcion")]
        public string CalificacionDescripcion { get; set; }
    }
}

