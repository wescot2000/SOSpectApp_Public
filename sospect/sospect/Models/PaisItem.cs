// Models/PaisItem.cs
// Creado: 2026-02-26
// Modelo para la tabla maestra de países (ISO alpha-2)

using Newtonsoft.Json;

namespace sospect.Models
{
    public class PaisItem
    {
        [JsonProperty("pais_id")]
        public string? PaisId { get; set; }     // ISO alpha-2: "CO", "MX", "US"

        [JsonProperty("nombre_es")]
        public string? NombreEs { get; set; }   // Nombre en español: "Colombia", "México"
    }
}
