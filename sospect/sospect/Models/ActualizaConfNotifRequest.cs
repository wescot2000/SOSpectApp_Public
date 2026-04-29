using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace sospect.Models
{
    public class ActualizaConfNotifRequest
    {

        [JsonProperty("p_user_id_thirdparty")]
        public string p_user_id_thirdparty { get; set; }

        [JsonProperty("p_notif_alarma_cercana_habilitada")]
        public bool p_notif_alarma_cercana_habilitada { get; set; }

        [JsonProperty("p_notif_alarma_protegido_habilitada")]
        public bool p_notif_alarma_protegido_habilitada { get; set; }

        [JsonProperty("p_notif_alarma_zona_vigilancia_habilitada")]
        public bool p_notif_alarma_zona_vigilancia_habilitada { get; set; }

        [JsonProperty("p_notif_alarma_policia_habilitada")]
        public bool p_notif_alarma_policia_habilitada { get; set; }

        [JsonProperty("p_dias_notif_policia_apagada")]
        public int? p_dias_notif_policia_apagada { get; set; }

        [JsonProperty("p_limite_alarmas_feed")]
        public int p_limite_alarmas_feed { get; set; }

        [JsonProperty("p_intervalo_background_minutos")]
        public int p_intervalo_background_minutos { get; set; }

        [JsonProperty("p_paises_feed_filtro")]
        public List<string>? p_paises_feed_filtro { get; set; }

    }

}
