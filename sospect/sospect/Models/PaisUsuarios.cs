using Newtonsoft.Json;

namespace sospect.Models
{
    public class PaisUsuarios
    {
        [JsonProperty("pais_id")]
        public string? pais_id { get; set; }            // ISO alpha-2: "CO", "MX"  ← NUEVO 2026-02-26

        [JsonProperty("pais")]
        public string? pais { get; set; }               // Nombre legible: "Colombia", "México"

        [JsonProperty("cantidad_usuarios")]
        public int cantidad_usuarios { get; set; }
    }
}
