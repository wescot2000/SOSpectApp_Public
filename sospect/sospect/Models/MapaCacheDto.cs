// Models/MapaCacheDto.cs
// Creado: 2026-03-01 — Rediseño Viewport-Driven del mapa
// Wrapper del caché del mapa en disco. Incluye los pines y el bounding box del viewport
// con el que se hizo la última petición, para validar si el caché es relevante al reiniciar.

using System;
using System.Collections.Generic;

namespace sospect.Models
{
    public class MapaCacheDto
    {
        public List<PinMapaDto> Pines { get; set; } = new();

        // Viewport con el que se guardó este caché
        public decimal ViewportMinLat { get; set; }
        public decimal ViewportMaxLat { get; set; }
        public decimal ViewportMinLon { get; set; }
        public decimal ViewportMaxLon { get; set; }

        /// <summary>Timestamp del último guardado. Solo para logging/debug.</summary>
        public DateTime GuardadoEn { get; set; }

        /// <summary>
        /// Retorna true si el viewport actual intersecta el viewport guardado.
        /// Si no intersecta, el caché pertenece a otra zona/ciudad y no debe pintarse.
        /// Lógica: dos rectángulos se intersectan si ninguno queda completamente fuera del otro.
        /// </summary>
        public bool IntersectaViewport(decimal currentMinLat, decimal currentMaxLat,
                                       decimal currentMinLon, decimal currentMaxLon)
        {
            bool latSolapada = currentMinLat <= ViewportMaxLat && currentMaxLat >= ViewportMinLat;
            bool lonSolapada = currentMinLon <= ViewportMaxLon && currentMaxLon >= ViewportMinLon;
            return latSolapada && lonSolapada;
        }
    }
}
