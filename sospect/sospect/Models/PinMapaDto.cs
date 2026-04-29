// Models/PinMapaDto.cs
// Creado:    2026-03-01 — Rediseño Viewport-Driven del mapa
// Modificado: 2026-03-02 — Enriquecido con campos sociales para igualar nivel visual de Cache B

using System.Collections.Generic;
using Newtonsoft.Json;

namespace sospect.Models
{
    /// <summary>
    /// Pin individual del mapa (zoom >= 15) o pin sintético que representa un cluster (zoom &lt;= 14).
    /// El ícono se resuelve localmente usando App.TiposAlarmaDisponibles con tipoalarma_id.
    /// Los campos enriquecidos (cols 5-11) solo vienen poblados para zoom >= 15 (pines individuales).
    /// </summary>
    public class PinMapaDto
    {
        public long alarma_id { get; set; }
        public decimal latitud { get; set; }
        public decimal longitud { get; set; }
        public short tipoalarma_id { get; set; }

        /// <summary>
        /// true = alarma activa (pin con color según tipo).
        /// false = alarma cerrada en los últimos 90 min (pin gris ClosedAlarmPin).
        /// </summary>
        public bool estado_alarma { get; set; }

        /// <summary>
        /// Cantidad de alarmas agrupadas en este pin (solo para pines sintéticos de cluster, zoom &lt;= 14).
        /// 0 = pin individual real. > 1 = cluster de varias alarmas.
        /// </summary>
        public int cantidad_cluster { get; set; } = 0;

        // ── Campos enriquecidos (zoom >= 15) ────────────────────────────────────────
        /// <summary>Texto del tipo de alarma para mostrar en InfoWindow (ej. "Advertencia/Peligro").</summary>
        public string? descripciontipoalarma { get; set; }
        /// <summary>Badge icono policía: true si hay agentes atendiendo.</summary>
        public bool flag_alarma_siendo_atendida { get; set; }
        /// <summary>Badge contador rojo: cantidad de interacciones en la alarma.</summary>
        public int cantidad_interacciones { get; set; }
        /// <summary>Badge verificado (✓ verde): true si el creador es de red de confianza.</summary>
        public bool flag_red_confianza { get; set; }
        /// <summary>user_id_thirdparty del creador; se compara con App.persona para flag_propietario.</summary>
        public string? user_id_creador_alarma { get; set; }
        /// <summary>Primera descripción de la alarma (para InfoWindow snippet adicional).</summary>
        public string? descripcionalarma { get; set; }
        /// <summary>Distancia en metros desde el usuario al pin (0 si userLat/userLon no se enviaron).</summary>
        public decimal distancia_en_metros { get; set; }
        /// <summary>
        /// ID de la alarma padre. Null para alarmas normales.
        /// Poblado para pines hijo como "sospechoso huyendo" (tipo 9), que se conectan
        /// con una polyline roja al pin padre en el mapa.
        /// </summary>
        public long? alarma_id_padre { get; set; }
    }

    /// <summary>
    /// Cluster de alarmas agrupadas por celda de grid (zoom &lt;= 14).
    /// </summary>
    public class ClusterMapaDto
    {
        public decimal latitud_centro { get; set; }
        public decimal longitud_centro { get; set; }
        public int cantidad_total { get; set; }
        public short tipoalarma_id { get; set; }
    }

    /// <summary>
    /// Respuesta unificada del endpoint PinesMapa.
    /// tipo = "pines" → items contiene List&lt;PinMapaDto&gt;.
    /// tipo = "clusters" → items contiene List&lt;ClusterMapaDto&gt;.
    /// </summary>
    public class PinesMapaResponse
    {
        public string tipo { get; set; } = "pines";
        public int zoom { get; set; }

        [JsonProperty("items")]
        public object? items_raw { get; set; }

        [JsonIgnore]
        public List<PinMapaDto>? Pines =>
            tipo == "pines"
                ? JsonConvert.DeserializeObject<List<PinMapaDto>>(items_raw?.ToString() ?? "[]")
                : null;

        [JsonIgnore]
        public List<ClusterMapaDto>? Clusters =>
            tipo == "clusters"
                ? JsonConvert.DeserializeObject<List<ClusterMapaDto>>(items_raw?.ToString() ?? "[]")
                : null;
    }
}
