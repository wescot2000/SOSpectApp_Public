using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace sospect.Models
{
    public class MisSubscripciones
    {
        [JsonProperty("subscripcion_id")]
        public long subscripcion_id { get; set; }

        [JsonProperty("user_id_thirdparty")]
        public string user_id_thirdparty { get; set; }

        [JsonProperty("descripcion_tipo")]
        public string descripcion_tipo { get; set; }

        [JsonProperty("descripcion")]
        public string descripcion { get; set; }

        [JsonProperty("fecha_finalizacion")]
        public DateTime fecha_finalizacion { get; set; }

        [JsonProperty("poderes_renovacion")]
        public int poderes_renovacion { get; set; }

        [JsonProperty("flag_subscr_vencida")]
        public bool flag_subscr_vencida { get; set; }

        [JsonProperty("observ_subscripcion")]
        public string observ_subscripcion { get; set; }

        [JsonProperty("flag_renovable")]
        public bool flag_renovable { get; set; }

        [JsonProperty("texto_renovable")]
        public string texto_renovable { get; set; }

        /// <summary>
        /// Tipo de suscripción (FK a tiposubscripcion)
        /// </summary>
        [JsonProperty("tipo_subscr_id")]
        public int tipo_subscr_id { get; set; }

        /// <summary>
        /// Indica si es una suscripción de tipo promoción local
        /// Se usa para determinar comportamiento de la tarjeta (sin botones renovar/cancelar, clicable)
        /// </summary>
        [JsonProperty("es_promocion")]
        public bool es_promocion { get; set; }

        public string CombinedFlags
        {
            get
            {
                return $"{flag_subscr_vencida},{flag_renovable},{es_promocion}";
            }
        }
    }
}
