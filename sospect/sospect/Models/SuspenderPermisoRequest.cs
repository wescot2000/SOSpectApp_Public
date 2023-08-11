using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace sospect.Models
{
    public class SuspenderPermisoRequest
    {
        [JsonProperty("p_user_id_thirdparty_protegido")]
        public string UserIdThirdPartyProtegido { get; set; }

        [JsonProperty("p_tiempo_suspension")]
        public int TiempoSuspension { get; set; }

        [JsonProperty("idioma")]
        public string Idioma { get; set; }
    }

}
