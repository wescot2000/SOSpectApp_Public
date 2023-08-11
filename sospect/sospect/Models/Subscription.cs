using Newtonsoft.Json;
using System;
namespace sospect.Models
{
    public class Subscription
    {
        [JsonProperty("subscripcion_id")]
        public string Subscripcion_id { get; set; }

        [JsonProperty("user_id_thirdparty")]
        public string User_id_thirdparty { get; set; }
        
        [JsonProperty("descripcion_tipo")] 
        public string Descripcion_tipo { get; set; }

        [JsonProperty("descripcion")]
        public string Descripcion { get; set; }

        [JsonProperty("fecha_finalizacion")]
        public string Fecha_finalizacion { get; set; }

        [JsonProperty("poderes_renovacion")]
        public int Poderes_renovacion { get; set; }

        [JsonProperty("flag_subscr_vencida")]
        public bool Flag_subscr_vencida { get; set; }

        [JsonProperty("observ_subscripcion")]
        public string Observ_subscripcion { get; set; }

        [JsonProperty("flag_renovable")]
        public bool Flag_renovable { get; set; }

        [JsonProperty("texto_renovable")]
        public string Texto_renovable { get; set; }
    }
}

