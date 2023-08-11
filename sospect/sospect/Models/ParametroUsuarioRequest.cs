using System;
using Newtonsoft.Json;

namespace sospect.Models
{
    public class ParametroUsuarioRequest
    {
        [JsonProperty("p_user_id_thirdparty")]
        public string PUserIdThirdparty { get; set; }

        [JsonProperty("RegistrationId")]
        public string RegistrationId { get; set; }

        [JsonProperty("Idioma")]
        public string Idioma { get; set; }
    }
}
