using System;
using Newtonsoft.Json;

namespace sospect.Models
{
    public class AcceptContractRequest
    {
        [JsonProperty("p_user_id_thirdparty")]
        public string PUserIdThirdparty { get; set; }

        [JsonProperty("p_ip_aceptacion")]
        public string PIpAceptacion { get; set; }
    }
}
