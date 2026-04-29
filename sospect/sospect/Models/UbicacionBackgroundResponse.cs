using System.Text.Json.Serialization;

namespace sospect.Models
{
    /// <summary>
    /// Respuesta del endpoint InsertaUbicacionBackground (optimizado para background service)
    /// Creado: 2026-02-10
    /// Propósito: Modelo ultra-liviano para actualización en segundo plano
    /// </summary>
    public class UbicacionBackgroundResponse
    {
        [JsonPropertyName("cantidadAlarmas")]
        public int CantidadAlarmas { get; set; }

        [JsonPropertyName("notificacionEnviada")]
        public bool NotificacionEnviada { get; set; }

        [JsonPropertyName("mensaje")]
        public string Mensaje { get; set; }
    }
}
