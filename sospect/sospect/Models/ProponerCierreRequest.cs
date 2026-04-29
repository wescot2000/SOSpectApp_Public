using Newtonsoft.Json;
using System.Collections.Generic;

namespace sospect.Models
{
    public class ProponerCierreRequest
    {
        [JsonProperty("p_alarma_id")]
        public long p_alarma_id { get; set; }

        [JsonProperty("p_user_id_thirdparty")]
        public string p_user_id_thirdparty { get; set; }

        [JsonProperty("p_descripcion")]
        public string p_descripcion { get; set; }

        [JsonProperty("p_idioma")]
        public string p_idioma { get; set; }

        [JsonProperty("Fotos")]
        public List<FotoAlarmaDto> Fotos { get; set; }
    }
}
