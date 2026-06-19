// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using Newtonsoft.Json;
using System;

namespace sospect.DTOs
{
    /// <summary>
    /// DTO para una autoridad política responsable de un territorio.
    /// Mapea la respuesta de GET /Politicos/ObtenerAutoridadesPorAlarma/{alarmaId}
    /// La lista viene ordenada por orden_jerarquico: 4=Edil (más granular), 1=Presidente.
    /// </summary>
    public class AutoridadResponsableDto
    {
        /// <summary>
        /// Posición en la cadena: 4=Edil (más granular), 3=Alcalde, 2=Gobernador, 1=Presidente
        /// </summary>
        [JsonProperty("ordenJerarquico")]
        public int OrdenJerarquico { get; set; }

        /// <summary>
        /// ID del cargo. 1=Presidente, 2=Gobernador, 3=Alcalde, 4=Edil.
        /// </summary>
        [JsonProperty("cargoId")]
        public int CargoId { get; set; }

        [JsonProperty("politicoId")]
        public int PoliticoId { get; set; }

        [JsonProperty("nombreCompleto")]
        public string? NombreCompleto { get; set; }

        [JsonProperty("fotoUrl")]
        public string? FotoUrl { get; set; }

        [JsonProperty("partido")]
        public string? Partido { get; set; }

        [JsonProperty("email")]
        public string? Email { get; set; }

        [JsonProperty("telefono")]
        public string? Telefono { get; set; }

        [JsonProperty("sitioWeb")]
        public string? SitioWeb { get; set; }

        /// <summary>
        /// Handle de Twitter/X (con o sin @). La app navega a https://twitter.com/{handle}
        /// </summary>
        [JsonProperty("twitter")]
        public string? Twitter { get; set; }

        [JsonProperty("fechaInicio")]
        public DateTime FechaInicio { get; set; }

        [JsonProperty("fechaFin")]
        public DateTime? FechaFin { get; set; }

        /// <summary>
        /// Nombre del territorio administrado. Ej: "Cali", "Valle del Cauca", "Colombia"
        /// </summary>
        [JsonProperty("territorioNombre")]
        public string? TerritorioNombre { get; set; }

        /// <summary>
        /// Alarmas de seguridad en los últimos 30 días en su territorio.
        /// Calculado por calcular_metricas_politicos().
        /// </summary>
        [JsonProperty("cntAlarmas30d")]
        public int CntAlarmas30d { get; set; }

        /// <summary>
        /// Porcentaje de alarmas resueltas. NULL si no hay alarmas o si la vista materializada
        /// mv_metricas_politico no se ha refrescado aún.
        /// </summary>
        [JsonProperty("pctResolucion")]
        public decimal? PctResolucion { get; set; }

        /// <summary>
        /// Porcentaje de aprobación ciudadana (calificación 1-5 escalada a 0-100%).
        /// NULL si aún no hay votos o si mv_metricas_politico no se ha refrescado.
        /// </summary>
        [JsonProperty("pctAprobacion")]
        public decimal? PctAprobacion { get; set; }

        // ── Propiedades calculadas para la UI (no vienen de la API) ──────────

        /// <summary>
        /// Nombre del cargo localizado, resuelto desde AppResources con clave "Cargo_{CargoId}".
        /// Ej: CargoId=3 → clave "Cargo_3" → "Alcalde" (es) / "Mayor" (en) / etc.
        /// Fallback al ID si la clave no existe en AppResources.
        /// </summary>
        public string CargoNombre =>
            sospect.Helpers.TranslateExtension.Translate($"Cargo_{CargoId}")
            ?? $"Cargo_{CargoId}";

        /// <summary>
        /// Texto formateado del período de mandato. Ej: "2024 - 2027" o "Desde 2022"
        /// </summary>
        public string MandatoTexto =>
            FechaFin.HasValue
                ? $"{FechaInicio:yyyy} - {FechaFin:yyyy}"
                : $"Desde {FechaInicio:yyyy}";

        public bool TieneFoto => !string.IsNullOrWhiteSpace(FotoUrl);
        public bool TieneEmail => !string.IsNullOrWhiteSpace(Email);
        public bool TieneTelefono => !string.IsNullOrWhiteSpace(Telefono);
        public bool TieneSitioWeb => !string.IsNullOrWhiteSpace(SitioWeb);
        public bool TieneTwitter => !string.IsNullOrWhiteSpace(Twitter);

        // ── Aprobación ciudadana para mostrar en la card ─────────────────────

        /// <summary>
        /// True si hay porcentaje de aprobación disponible para mostrar en la card.
        /// </summary>
        public bool TieneAprobacion => PctAprobacion.HasValue;

        /// <summary>
        /// Texto de aprobación para la card. Ej: "SOSpect: 74%"
        /// </summary>
        public string AprobacionResumen => PctAprobacion.HasValue
            ? $"{sospect.Helpers.TranslateExtension.Translate("LblAprobacionSOSpect")}: {PctAprobacion:F0}%"
            : string.Empty;

        // ── Métrica de resolución para mostrar en la card ────────────────────

        /// <summary>
        /// True si hay un porcentaje de resolución disponible para mostrar en la card.
        /// </summary>
        public bool TieneMetrica => PctResolucion.HasValue;

        /// <summary>
        /// Texto resumen para la card: "Resolución: 34%". Vacío si no hay dato.
        /// </summary>
        public string MetricaResumen => PctResolucion.HasValue
            ? $"{sospect.Helpers.TranslateExtension.Translate("LblResolucion")}: {PctResolucion:F0}%"
            : string.Empty;

        /// <summary>
        /// Color semáforo basado en el porcentaje de resolución:
        /// ≥50% verde, ≥20% naranja, <20% rojo.
        /// </summary>
        public string MetricaColor => PctResolucion switch
        {
            >= 50 => "#27AE60",   // verde
            >= 20 => "#E67E22",   // naranja
            _      => "#E74C3C"   // rojo
        };
    }
}


