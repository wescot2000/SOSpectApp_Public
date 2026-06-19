// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using Newtonsoft.Json;
using System;

namespace sospect.Models
{
    /// <summary>
    /// Modelo para mostrar resumen de chats en la lista del creador de promociones
    /// Agregado: 02-02-2026
    /// </summary>
    public class ChatResumenModel
    {
        [JsonProperty("chatId")]
        public long ChatId { get; set; }

        [JsonProperty("interesadoUserId")]
        public string InteresadoUserId { get; set; }

        [JsonProperty("ultimoMensaje")]
        public string UltimoMensaje { get; set; }

        [JsonProperty("fechaUltimoMensaje")]
        public DateTime FechaUltimoMensaje { get; set; }

        [JsonProperty("mensajesNoLeidos")]
        public int MensajesNoLeidos { get; set; }

        /// <summary>
        /// Formato de fecha para mostrar en la UI (ej: "02/02 14:30")
        /// </summary>
        public string FechaFormateada => FechaUltimoMensaje.ToString("dd/MM HH:mm");

        /// <summary>
        /// Indica si hay mensajes no leídos (para mostrar el punto verde)
        /// </summary>
        public bool TieneNoLeidos => MensajesNoLeidos > 0;

        /// <summary>
        /// Cantidad de no leídos formateada (máximo "99+" para no desbordar)
        /// </summary>
        public string CantidadNoLeidos => MensajesNoLeidos > 99 ? "99+" : MensajesNoLeidos.ToString();

        /// <summary>
        /// Usuario anonimizado para mostrar (primeros 3 + últimos 4 caracteres)
        /// </summary>
        public string UsuarioAnonimizado
        {
            get
            {
                if (string.IsNullOrEmpty(InteresadoUserId) || InteresadoUserId.Length <= 7)
                    return InteresadoUserId ?? "Usuario";

                return $"{InteresadoUserId.Substring(0, 3)}-{InteresadoUserId.Substring(InteresadoUserId.Length - 4)}";
            }
        }
    }
}


