using System;
using Newtonsoft.Json;

namespace sospect.Models
{
    public class ParametrosUsuario
    {
        [JsonProperty("p_user_id_thirdparty")]
        public string PUserIdThirdparty { get; set; }

        [JsonProperty("tiempoRefrescoUbicacion")]
        public int? TiempoRefrescoUbicacion { get; set; }

        [JsonProperty("marca_bloqueo")]
        public int MarcaBloqueo { get; set; }

        [JsonProperty("radio_mts")]
        public int RadioMts { get; set; }

        [JsonProperty("mensajesParaUsuario")]
        public int MensajesParaUsuario { get; set; }

        [JsonProperty("flag_bloqueo_usuario")]
        public bool FlagBloqueoUsuario { get; set; }

        [JsonProperty("flag_usuario_debe_firmar_cto")]
        public bool FlagUsuarioDebeFirmarCto { get; set; }

        [JsonProperty("saldo_poderes")]
        public int SaldoPoderes { get; set; }

        [JsonProperty("latitud")]
        public double Latitud { get; set; }

        [JsonProperty("longitud")]
        public double Longitud { get; set; }

        [JsonProperty("fechafin_bloqueo_usuario")]
        public DateTime fechafin_bloqueo_usuario { get; set; }

        [JsonProperty("fecha_parametro")]
        public DateTime FechaParametro { get; set; }

        [JsonProperty("radio_alarmas_mts_actual")]
        public int radio_alarmas_mts_actual { get; set; }

        [JsonProperty("credibilidad_persona")]
        public double credibilidad_persona { get; set; }

        [JsonProperty("flag_red_confianza")]
        public bool flag_red_confianza { get; set; }

        [JsonProperty("flag_convenio")]
        public bool flag_convenio { get; set; }

        // Costos de promociones locales
        [JsonProperty("costo_base_promocion")]
        public int? CostoBasePromocion { get; set; }

        [JsonProperty("costo_logo")]
        public int? CostoLogo { get; set; }

        [JsonProperty("costo_contacto")]
        public int? CostoContacto { get; set; }

        [JsonProperty("costo_domicilio")]
        public int? CostoDomicilio { get; set; }

        [JsonProperty("costo_por_500m_extra")]
        public int? CostoPor500mExtra { get; set; }

        [JsonProperty("costo_por_dia_extra")]
        public int? CostoPorDiaExtra { get; set; }

        [JsonProperty("costo_por_media_extra")]
        public int? CostoPorMediaExtra { get; set; }

        [JsonProperty("costo_por_50_usuarios_push")]
        public int? CostoPor50UsuariosPush { get; set; }

        // Configuracion de feed
        [JsonProperty("limite_alarmas_feed")]
        public int? LimiteAlarmasFeed { get; set; }
    }
}
