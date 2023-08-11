using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace sospect.Models
{
    public class RequestPermissionModel
    {
        [JsonProperty("p_user_id_thirdparty_protector")]
        public string PUserIdThirdpartyProtector { get; set; }

        [JsonProperty("p_user_id_thirdparty_protegido")]
        public string PUserIdThirdpartyProtegido { get; set; }

        [JsonProperty("tiempo_subscripcion_dias")]
        public int TiempoSubscripcionDias { get; set; }

        [JsonProperty("idioma")]
        public string Idioma { get; set; }

        [JsonProperty("TiporelacionId")]
        public int TiporelacionId { get; set; }
    }


}
