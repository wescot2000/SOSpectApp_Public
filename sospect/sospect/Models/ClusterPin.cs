using sospect.CustomRenderers;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Location = Microsoft.Maui.Devices.Sensors.Location;

namespace sospect.Models
{
    /// <summary>
    /// Representa un pin de cluster que agrupa múltiples alarmas cercanas.
    /// Sigue las reglas semánticas definidas en el Manual General SOSpect, sección 6:
    /// - Icono del tipo dominante
    /// - Badge con conteo total
    /// - Posición en centroide geográfico
    /// - Misma familia visual que pines individuales
    /// </summary>
    public class ClusterPin : CustomPin
    {
        /// <summary>
        /// Lista de alarmas agrupadas en este cluster
        /// </summary>
        public List<AlarmaCercana> AlarmasAgrupadas { get; set; }

        /// <summary>
        /// Número total de alarmas en el cluster (usado para badge).
        /// Puede ser sobrescrito directamente para clusters del viewport (sin AlarmasAgrupadas).
        /// </summary>
        private int? _totalAlarmasOverride;
        public int TotalAlarmas => _totalAlarmasOverride ?? (AlarmasAgrupadas?.Count ?? 0);

        /// <summary>
        /// Tipo de alarma dominante en el cluster
        /// Determinado por: mayor conteo, o si hay empate, mayor ranking_relevancia promedio
        /// </summary>
        public long TipoDominante { get; set; }

        /// <summary>
        /// Ranking de relevancia promedio de las alarmas en el cluster
        /// Usado para ordenar clusters y resolver empates de tipo dominante
        /// </summary>
        public decimal RankingRelevanciaPromedio { get; set; }

        /// <summary>
        /// Indica si este pin es un cluster (para distinguirlo de pines individuales)
        /// </summary>
        public bool EsCluster { get; set; }

        /// <summary>
        /// Clave de la celda grid a la que pertenece este cluster
        /// Formato: "lat_lng" (ej: "4.6_-74.1")
        /// </summary>
        public string GridCellKey { get; set; }

        /// <summary>
        /// Bounds geográficos del cluster (para zoom al hacer tap)
        /// </summary>
        public MapSpan ClusterBounds { get; set; }

        /// <summary>
        /// Constructor por defecto
        /// </summary>
        public ClusterPin() : base()
        {
            AlarmasAgrupadas = new List<AlarmaCercana>();
            EsCluster = true;
        }

        /// <summary>
        /// Constructor con lista de alarmas
        /// </summary>
        /// <param name="alarmas">Lista de alarmas a agrupar</param>
        public ClusterPin(List<AlarmaCercana> alarmas) : this()
        {
            AlarmasAgrupadas = alarmas ?? new List<AlarmaCercana>();
            CalcularPropiedades();
        }

        /// <summary>
        /// Constructor liviano para clusters del viewport (desde PinesMapa endpoint).
        /// No requiere AlarmasAgrupadas individuales — solo centroide, tipo y cantidad.
        /// </summary>
        public ClusterPin(decimal latitud, decimal longitud, int tipoalarma_id, int cantidadTotal) : this()
        {
            Location = new Location((double)latitud, (double)longitud);
            TipoAlarma = tipoalarma_id;
            TipoDominante = tipoalarma_id;
            _totalAlarmasOverride = cantidadTotal;
            MarkerId = $"vp_cluster_{latitud:F4}_{longitud:F4}";
            IdAlarma = MarkerId;
            Label = cantidadTotal.ToString();
            Address = cantidadTotal.ToString();
        }

        /// <summary>
        /// Calcula las propiedades del cluster según las reglas semánticas:
        /// - Tipo dominante
        /// - Centroide geográfico
        /// - Ranking promedio
        /// - Bounds geográficos
        /// </summary>
        private void CalcularPropiedades()
        {
            if (AlarmasAgrupadas == null || !AlarmasAgrupadas.Any())
                return;

            // 1. Calcular tipo dominante
            var gruposPorTipo = AlarmasAgrupadas
                .GroupBy(a => a.tipoalarma_id)
                .Select(g => new
                {
                    TipoId = g.Key,
                    Conteo = g.Count(),
                    RankingPromedio = g.Average(a => (decimal?)a.ranking_relevancia) ?? 0
                })
                .OrderByDescending(g => g.Conteo)
                .ThenByDescending(g => g.RankingPromedio)
                .ToList();

            if (gruposPorTipo.Any())
            {
                var dominante = gruposPorTipo.First();
                TipoDominante = dominante.TipoId;
                TipoAlarma = dominante.TipoId;
            }

            // 2. Calcular centroide geográfico
            var latitudPromedio = AlarmasAgrupadas.Average(a => (double)a.latitud_alarma);
            var longitudPromedio = AlarmasAgrupadas.Average(a => (double)a.longitud_alarma);
            Location = new Location(latitudPromedio, longitudPromedio);

            // 3. Calcular ranking promedio
            RankingRelevanciaPromedio = AlarmasAgrupadas
                .Average(a => (decimal?)a.ranking_relevancia) ?? 0;

            // 4. Calcular bounds para zoom
            if (AlarmasAgrupadas.Count > 0)
            {
                var minLat = AlarmasAgrupadas.Min(a => (double)a.latitud_alarma);
                var maxLat = AlarmasAgrupadas.Max(a => (double)a.latitud_alarma);
                var minLng = AlarmasAgrupadas.Min(a => (double)a.longitud_alarma);
                var maxLng = AlarmasAgrupadas.Max(a => (double)a.longitud_alarma);

                // Calcular centro y radio del bounds
                var centerLat = (minLat + maxLat) / 2;
                var centerLng = (minLng + maxLng) / 2;
                var centerLocation = new Location(centerLat, centerLng);

                // Calcular radio máximo desde el centro hasta cualquier alarma
                var maxDistance = AlarmasAgrupadas
                    .Max(a => Utils.HaversineUtils.DistanceInMeters(
                        centerLat, centerLng,
                        (double)a.latitud_alarma, (double)a.longitud_alarma));

                // Agregar 20% de padding para mejor visualización
                var radiusWithPadding = maxDistance * 1.2;

                ClusterBounds = MapSpan.FromCenterAndRadius(
                    centerLocation,
                    new Distance(radiusWithPadding));
            }

            // 5. Configurar propiedades del pin base
            MarkerId = $"cluster_{GridCellKey ?? Guid.NewGuid().ToString()}";
            IdAlarma = MarkerId;

            // NOTA: Label y Address se deben establecer en HomePage.xaml.cs con TranslateExtension
            // para soportar internacionalización. Aquí solo ponemos valores por defecto.
            Label = TotalAlarmas.ToString();
            Address = TotalAlarmas.ToString();

            // Asociar la primera alarma como referencia (para compatibilidad)
            AlarmaCercana = AlarmasAgrupadas.FirstOrDefault();
        }

        /// <summary>
        /// Recalcula todas las propiedades del cluster
        /// Útil después de agregar/quitar alarmas dinámicamente
        /// </summary>
        public void Recalcular()
        {
            CalcularPropiedades();
        }
    }
}
