using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace sospect.Models
{
    public class SolicitudCierreResponse
    {
        [JsonProperty("solicitud_id")]
        public long solicitud_id { get; set; }

        [JsonProperty("alarma_id")]
        public long alarma_id { get; set; }

        [JsonProperty("persona_id_solicitante")]
        public long persona_id_solicitante { get; set; }

        [JsonProperty("descripcion")]
        public string descripcion { get; set; }

        [JsonProperty("fecha_solicitud")]
        public DateTime? fecha_solicitud { get; set; }

        [JsonProperty("fecha_limite_votacion")]
        public DateTime? fecha_limite_votacion { get; set; }

        [JsonProperty("estado")]
        public string estado { get; set; }

        [JsonProperty("votos_si")]
        public int votos_si { get; set; }

        [JsonProperty("votos_no")]
        public int votos_no { get; set; }

        [JsonProperty("ya_voto_usuario")]
        public bool ya_voto_usuario { get; set; }

        /// <summary>
        /// True si el usuario que consultó es quien propuso este cierre.
        /// Calculado en el stored procedure: sc.persona_id = v_persona_id.
        /// </summary>
        [JsonProperty("es_proponente")]
        public bool es_proponente { get; set; }

        [JsonProperty("voto_usuario")]
        public bool? voto_usuario { get; set; }

        /// <summary>
        /// JSON de fotos de la propuesta (retornado por ObtenerSolicitudCierreActiva cuando se implemente R6).
        /// </summary>
        [JsonProperty("fotos_json")]
        public string fotos_json { get; set; }

        /// <summary>
        /// Lista de fotos parseada desde fotos_json. Se popula en CierreEncuestaViewModel.CargarSolicitudActiva.
        /// </summary>
        [JsonIgnore]
        public List<FotoDescripcionAlarma> Fotos { get; set; } = new List<FotoDescripcionAlarma>();
    }
}
