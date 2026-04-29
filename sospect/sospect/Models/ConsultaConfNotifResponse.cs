using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace sospect.Models
{
    public class ConsultaConfNotifResponse
    {
        [JsonProperty("p_user_id_thirdparty")]
        public string p_user_id_thirdparty { get; set; }

        [JsonProperty("notif_alarma_cercana_habilitada")]
        public bool notif_alarma_cercana_habilitada { get; set; }

        [JsonProperty("notif_alarma_protegido_habilitada")]
        public bool notif_alarma_protegido_habilitada { get; set; }

        [JsonProperty("notif_alarma_zona_vigilancia_habilitada")]
        public bool notif_alarma_zona_vigilancia_habilitada { get; set; }

        [JsonProperty("notif_alarma_policia_habilitada")]
        public bool notif_alarma_policia_habilitada { get; set; }

        [JsonProperty("flag_es_policia")]
        public bool flag_es_policia { get; set; }

        [JsonProperty("fecha_act_configuracion_notif")]
        public DateTime? fecha_act_configuracion_notif { get; set; }

        [JsonProperty("dias_notif_policia_apagada")]
        public int? dias_notif_policia_apagada { get; set; }

        [JsonProperty("limite_alarmas_feed")]
        public int? limite_alarmas_feed { get; set; }

        [JsonProperty("intervalo_background_minutos")]
        public int intervalo_background_minutos { get; set; }

        [JsonProperty("paises_feed_filtro")]
        public List<string>? paises_feed_filtro { get; set; }

        [JsonProperty("pais_usuario")]
        public string? pais_usuario { get; set; }           // ISO alpha-2: "CO", "MX"

        [JsonProperty("pais_usuario_nombre")]
        public string? pais_usuario_nombre { get; set; }   // Nombre legible: "Colombia"  ← NUEVO 2026-02-26
    }
}
