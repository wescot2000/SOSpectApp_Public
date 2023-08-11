using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace sospect.Models
{
    public class EliminarProtectorRequest
    {
        [JsonProperty("p_user_id_thirdparty_protector")]
        public string UserIdThirdPartyProtector { get; set; }

        [JsonProperty("p_user_id_thirdparty_protegido")]
        public string UserIdThirdPartyProtegido { get; set; }

        [JsonProperty("Idioma")]
        public string Idioma { get; set; }
    }

}
