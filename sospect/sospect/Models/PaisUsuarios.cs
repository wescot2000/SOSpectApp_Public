// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

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


