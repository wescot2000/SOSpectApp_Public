using System;
using Newtonsoft.Json;

namespace sospect.Models
{
    public class DetalleMensajeRequest
    {
        [JsonProperty("p_user_id_thirdparty")]
        public string PUserIdThirdparty { get; set; }

        [JsonProperty("idioma_dispositivo")]
        public string IdiomaDispositivo { get; set; }

        [JsonProperty("p_mensaje_id")]
        public long PMensajeId { get; set; }
    }
}

