// DTOs/ReportePoliticoDto.cs
// Módulo político: DTO para el reporte de desempeño de un político.
// Espejo de ReportePoliticoResponse de la API.
// Creado: 2026-03-09
// MODIFICADO: 2026-03-28 - Agregados PctAprobacion y CntVotantesAprobacion

using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace sospect.DTOs
{
    /// <summary>
    /// Métricas de desempeño de un político.
    /// Retornado por GET /Politicos/ObtenerReportePolitico/{politicoId}
    /// </summary>
    public class ReportePoliticoDto
    {
        [JsonProperty("cntTotal")]
        public int CntTotal { get; set; }

        [JsonProperty("cntAbiertas")]
        public int CntAbiertas { get; set; }

        [JsonProperty("cntCerradas")]
        public int CntCerradas { get; set; }

        /// <summary>
        /// Porcentaje de resolución. NULL si no hay alarmas (protección división por cero en SQL).
        /// </summary>
        [JsonProperty("pctResolucion")]
        public decimal? PctResolucion { get; set; }

        [JsonProperty("cntLikes")]
        public int CntLikes { get; set; }

        [JsonProperty("cntReenvios")]
        public int CntReenvios { get; set; }

        /// <summary>
        /// Tiempo promedio de resolución en días. NULL si no hay alarmas cerradas con fecha conocida.
        /// </summary>
        [JsonProperty("avgDiasResolucion")]
        public decimal? AvgDiasResolucion { get; set; }

        /// <summary>
        /// Timestamp de la última actualización de la vista materializada.
        /// </summary>
        [JsonProperty("fechaCalculo")]
        public DateTime FechaCalculo { get; set; }

        /// <summary>
        /// Porcentaje de aprobación ciudadana (0-100%). NULL si nadie ha calificado aún.
        /// Calculado como AVG(calificacion)*20 (escala 1-5 → 0-100%).
        /// Se actualiza diariamente con el cron de refrescar_metricas_politico.
        /// </summary>
        [JsonProperty("pctAprobacion")]
        public decimal? PctAprobacion { get; set; }

        /// <summary>
        /// Cantidad de usuarios que han calificado a este político.
        /// </summary>
        [JsonProperty("cntVotantesAprobacion")]
        public int CntVotantesAprobacion { get; set; }

        /// <summary>
        /// Distribución de alarmas por tipo (para el gráfico pastel).
        /// </summary>
        [JsonProperty("tiposAlarma")]
        public List<TipoAlarmaItemDto> TiposAlarma { get; set; } = new();

        // ── Propiedades calculadas para la UI ─────────────────────────────────

        /// <summary>
        /// Valor entre 0 y 1 para usar con ProgressBar.Value.
        /// </summary>
        public double PctResolucionDecimal =>
            PctResolucion.HasValue ? (double)(PctResolucion.Value / 100m) : 0;

        /// <summary>
        /// Texto del tiempo promedio de resolución para mostrar en la UI.
        /// Ejemplo: "4.2" o "–" si no hay datos.
        /// </summary>
        public string AvgDiasResolucionTexto =>
            AvgDiasResolucion.HasValue ? $"{AvgDiasResolucion:F1}" : "–";
    }

    /// <summary>
    /// Tipo de alarma con conteo y porcentaje para el gráfico pastel del reporte.
    /// </summary>
    public class TipoAlarmaItemDto
    {
        [JsonProperty("tipoalarmaId")]
        public long TipoalarmaId { get; set; }

        [JsonProperty("descripcion")]
        public string? Descripcion { get; set; }

        [JsonProperty("cnt")]
        public int Cnt { get; set; }

        /// <summary>
        /// Porcentaje del total. NULL si cnt_total = 0.
        /// </summary>
        [JsonProperty("pct")]
        public decimal? Pct { get; set; }
    }
}
