using System;
using Newtonsoft.Json;

namespace sospect.Models
{
    public class RespuestaCalificacionDescripcion
    {
        [JsonProperty("iddescripcion")]
        public long Iddescripcion { get; set; }

        [JsonProperty("calificaciondescripcion")]
        public long Calificaciondescripcion { get; set; }
    }
}

