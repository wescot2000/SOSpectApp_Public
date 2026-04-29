using System;
using Newtonsoft.Json;

namespace sospect.Models
{
    /// <summary>
    /// DTO para mensajes de chat privado de promociones
    /// </summary>
    public class MensajeChatDto
    {
        [JsonProperty("mensaje_id")]
        public long MensajeId { get; set; }

        [JsonProperty("chat_id")]
        public long ChatId { get; set; }

        [JsonProperty("remitente_persona_id")]
        public long RemitentePersonaId { get; set; }

        [JsonProperty("contenido")]
        public string Contenido { get; set; }

        [JsonProperty("fecha_envio")]
        public DateTime FechaEnvio { get; set; }

        [JsonProperty("leido")]
        public bool Leido { get; set; }

        [JsonProperty("fecha_lectura")]
        public DateTime? FechaLectura { get; set; }

        /// <summary>
        /// Calculado por el servidor: true si remitente_persona_id == el usuario autenticado.
        /// Evita depender de App.persona.persona_id que puede ser 0 en caché fría.
        /// </summary>
        [JsonProperty("es_propio")]
        public bool EsPropio { get; set; }

        [JsonProperty("url_media")]
        public string? UrlMedia { get; set; }

        [JsonProperty("tipo_media")]
        public string? TipoMedia { get; set; }

        // Propiedades calculadas para binding en CollectionView DataTemplate
        public bool EsAjeno => !EsPropio;
        public bool TieneTexto => !string.IsNullOrEmpty(Contenido);
        public bool TieneImagenAdjunta => !string.IsNullOrEmpty(UrlMedia) && TipoMedia != "video";
        public bool TieneVideoAdjunto => !string.IsNullOrEmpty(UrlMedia) && TipoMedia == "video";
    }
}
