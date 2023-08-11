using System;
using Newtonsoft.Json;

namespace sospect.Models
{
    public class MarcarMensajesLeidosRequest
    {
        [JsonProperty("p_user_id_thirdparty")]
        public string PUserIdThirdparty { get; set; }
    }
}

