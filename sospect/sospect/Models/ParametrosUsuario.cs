using System;
using Newtonsoft.Json;

namespace sospect.Models
{
    public class ParametrosUsuario
    {
        [JsonProperty("p_user_id_thirdparty")]
        public string PUserIdThirdparty { get; set; }

        [JsonProperty("tiempoRefrescoUbicacion")]
        public int TiempoRefrescoUbicacion { get; set; }

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
    }
}
