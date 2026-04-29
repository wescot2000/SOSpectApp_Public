using sospect.Models;
using sospect.CustomRenderers;
using sospect.Utils;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Location = Microsoft.Maui.Devices.Sensors.Location;

namespace sospect.Helpers
{
    /// <summary>
    /// Helper para clustering grid-based de alarmas en el mapa.
    /// Implementa las especificaciones MANDATORY del Manual General SOSpect, secciones 5-7:
    ///
    /// - Grid-based clustering (NO hierarchical)
    /// - Recalcula en cada cambio de zoom/viewport
    /// - Flat clustering (NO nested clusters)
    /// - Activación automática según zoom level y cantidad de pines
    ///
    /// Tabla de tamaños de grid:
    /// | Zoom Level | Grid Cell Size |
    /// |------------|----------------|
    /// | ≤ 10       | 2 km           |
    /// | 11-12      | 1 km           |
    /// | 13-14      | 500 m          |
    /// | ≥ 15       | Disabled       |
    /// </summary>
    public static class GridClusteringHelper
    {
        // Constantes de configuración según manual
        private const int ZOOM_THRESHOLD_CLUSTERING = 14;  // Clustering activo cuando zoom ≤ 14
        private const int MAX_PINS_WITHOUT_CLUSTERING = 40; // Forzar clustering si pines > 40

        // Tamaños de grid según zoom level (en metros)
        private const double GRID_SIZE_ZOOM_LOW = 2000;    // ≤ 10: 2 km
        private const double GRID_SIZE_ZOOM_MEDIUM = 1000; // 11-12: 1 km
        private const double GRID_SIZE_ZOOM_HIGH = 500;    // 13-14: 500 m

        // Constantes para cálculo de zoom level
        private const double EARTH_EQUATOR_CIRCUMFERENCE = 40075017; // metros
        private const int TILE_SIZE = 256; // pixels

        /// <summary>
        /// Determina si el clustering debe estar activo según el zoom level y cantidad de pines.
        ///
        /// NUEVA ESTRATEGIA (adaptativa al radio del usuario):
        /// - Calcular zoom base del usuario desde radio_alarmas_mts_actual
        /// - Activar clustering solo cuando el usuario está ALEJADO (zoom mucho menor que su radio)
        /// - Esto funciona automáticamente para 100m, 200m, 3km, etc.
        /// </summary>
        /// <param name="zoomLevel">Nivel de zoom actual</param>
        /// <param name="cantidadPines">Cantidad de pines visibles en el viewport</param>
        /// <param name="radioUsuarioMetros">Radio del usuario en metros (de radio_alarmas_mts_actual)</param>
        /// <returns>true si clustering debe activarse, false en caso contrario</returns>
        public static bool DebeActivarClustering(int zoomLevel, int cantidadPines, int radioUsuarioMetros = 100)
        {
            // Forzar clustering si hay demasiados pines (performance)
            if (cantidadPines > MAX_PINS_WITHOUT_CLUSTERING)
            {
                System.Diagnostics.Debug.WriteLine($"GridClustering: Clustering FORZADO por cantidad de pines ({cantidadPines} > {MAX_PINS_WITHOUT_CLUSTERING})");
                return true;
            }

            // Calcular el zoom que corresponde al radio del usuario
            // Fórmula: zoom = log2(EARTH_CIRCUMFERENCE / (radio * 2 * TILE_SIZE))
            // El "* 2" es porque queremos el diámetro completo visible
            var zoomUsuario = CalcularZoomDesdeRadio(radioUsuarioMetros);

            // Activar clustering solo si el usuario está ALEJADO (zoom menor = más alejado)
            // Threshold: 3 niveles de zoom por debajo del zoom del usuario
            // Ejemplo: radio=100m → zoom≈15 → clustering activo en zoom ≤12
            // Ejemplo: radio=3000m → zoom≈12 → clustering activo en zoom ≤9
            var zoomThreshold = zoomUsuario - 3;

            var shouldCluster = zoomLevel <= zoomThreshold;

            System.Diagnostics.Debug.WriteLine($"GridClustering: Radio usuario={radioUsuarioMetros}m → ZoomUsuario={zoomUsuario} → Threshold={zoomThreshold} → ZoomActual={zoomLevel} → Clustering={shouldCluster}");

            return shouldCluster;
        }

        /// <summary>
        /// Calcula el nivel de zoom que corresponde a un radio en metros.
        /// Útil para determinar cuándo activar clustering basado en el radio del usuario.
        /// </summary>
        /// <param name="radioMetros">Radio en metros</param>
        /// <returns>Nivel de zoom aproximado</returns>
        private static int CalcularZoomDesdeRadio(int radioMetros)
        {
            try
            {
                // Fórmula inversa: zoom = log2(EARTH_CIRCUMFERENCE / (diametro * TILE_SIZE))
                var diametroMetros = radioMetros * 2.0;
                var ratio = EARTH_EQUATOR_CIRCUMFERENCE / (diametroMetros * TILE_SIZE);
                var zoom = Math.Log(ratio, 2);
                return Math.Max(0, Math.Min(21, (int)Math.Round(zoom)));
            }
            catch
            {
                return 15; // Default seguro
            }
        }

        /// <summary>
        /// Calcula el nivel de zoom aproximado desde un MapSpan.
        /// Basado en la fórmula de Google Maps para calcular zoom desde distancia visible.
        ///
        /// Fórmula:
        /// zoom = log2(EARTH_CIRCUMFERENCE / (latitudeDelta * TILE_SIZE))
        /// </summary>
        /// <param name="mapSpan">MapSpan actual del mapa</param>
        /// <returns>Nivel de zoom aproximado (0-21, escala Google Maps)</returns>
        public static int CalcularZoomLevel(MapSpan mapSpan)
        {
            if (mapSpan == null)
                return 15; // Default: zoom alto (sin clustering)



            try

            {

                // Obtener delta de latitud en grados

                var latitudeDelta = mapSpan.LatitudeDegrees;



                if (latitudeDelta <= 0)

                    return 15;



                // Convertir delta a metros (aproximado)

                var metersPerDegree = EARTH_EQUATOR_CIRCUMFERENCE / 360.0;

                var latitudeSpanMeters = latitudeDelta * metersPerDegree;



                // Calcular zoom level

                // zoom = log2(EARTH_CIRCUMFERENCE / (span * TILE_SIZE))

                var ratio = EARTH_EQUATOR_CIRCUMFERENCE / (latitudeSpanMeters * TILE_SIZE);

                var zoomLevel = Math.Log(ratio, 2);



                // Redondear y limitar entre 0 y 21

                var roundedZoom = (int)Math.Round(zoomLevel);

                return Math.Max(0, Math.Min(21, roundedZoom));

            }

            catch (Exception ex)

            {

                System.Diagnostics.Debug.WriteLine($"GridClusteringHelper: Error calculando zoom level: {ex.Message}");
                return 15; // Default: zoom alto (sin clustering)
            }
        }

        /// <summary>
        /// Obtiene el tamaño de celda del grid según el nivel de zoom.
        /// Implementa la tabla OFICIAL del manual:
        /// - ≤ 10: 2 km
        /// - 11-12: 1 km
        /// - 13-14: 500 m
        /// - ≥ 15: N/A (clustering desactivado)
        /// </summary>
        /// <param name="zoomLevel">Nivel de zoom actual</param>
        /// <returns>Tamaño de celda en metros</returns>
        public static double ObtenerTamañoGrid(int zoomLevel)
        {
            if (zoomLevel <= 10)
                return GRID_SIZE_ZOOM_LOW;    // 2 km
            else if (zoomLevel <= 12)
                return GRID_SIZE_ZOOM_MEDIUM; // 1 km
            else if (zoomLevel <= 14)
                return GRID_SIZE_ZOOM_HIGH;   // 500 m
            else
                return 0; // Clustering desactivado
        }

        /// <summary>
        /// Calcula la clave de celda del grid para una coordenada geográfica.
        /// Divide el espacio en celdas cuadradas del tamaño especificado.
        ///
        /// La clave es un string "gridLat_gridLng" que identifica de forma única cada celda.
        /// </summary>
        /// <param name="latitud">Latitud en grados</param>
        /// <param name="longitud">Longitud en grados</param>
        /// <param name="tamañoGridMetros">Tamaño de la celda en metros</param>
        /// <returns>Clave única de la celda del grid</returns>
        public static string CalcularCeldaGrid(double latitud, double longitud, double tamañoGridMetros)
        {
            // Convertir tamaño de grid de metros a grados (aproximado)
            // 1 grado de latitud ≈ 111,320 metros
            var metrosPorGradoLat = 111320.0;

            // 1 grado de longitud varía según latitud: cos(lat) * 111,320
            var metrosPorGradoLng = Math.Cos(latitud * Math.PI / 180.0) * 111320.0;

            var gridSizeLat = tamañoGridMetros / metrosPorGradoLat;
            var gridSizeLng = tamañoGridMetros / metrosPorGradoLng;

            // Calcular índices de celda
            var gridLat = Math.Floor(latitud / gridSizeLat);
            var gridLng = Math.Floor(longitud / gridSizeLng);

            return $"{gridLat}_{gridLng}";
        }

        /// <summary>
        /// Clusteriza una lista de alarmas usando algoritmo grid-based.
        ///
        /// Algoritmo:
        /// 1. Divide el viewport en celdas grid según zoom level
        /// 2. Agrupa alarmas por celda
        /// 3. Para celdas con >1 alarma: crea ClusterPin
        /// 4. Para celdas con =1 alarma: mantiene pin individual
        ///
        /// Sigue reglas semánticas (sección 6 del manual):
        /// - Icono del tipo dominante
        /// - Badge con total de alarmas
        /// - Posición en centroide geográfico
        /// </summary>
        /// <param name="alarmas">Lista completa de alarmas</param>
        /// <param name="zoomLevel">Nivel de zoom actual</param>
        /// <param name="radioUsuarioMetros">Radio del usuario en metros (de radio_alarmas_mts_actual)</param>
        /// <returns>Lista de CustomPins (mix de individuales y clusters)</returns>
        public static List<CustomPin> ClusterizarAlarmas(List<AlarmaCercana> alarmas, int zoomLevel, int radioUsuarioMetros = 100)
        {
            var resultado = new List<CustomPin>();

            try
            {
                System.Diagnostics.Debug.WriteLine($"GridClustering: Iniciando clustering con {alarmas?.Count ?? 0} alarmas, zoom {zoomLevel}, radio usuario {radioUsuarioMetros}m");

                if (alarmas == null || !alarmas.Any())
                {
                    System.Diagnostics.Debug.WriteLine("GridClustering: Lista de alarmas vacía");
                    return resultado;
                }

                // Verificar si clustering debe activarse (con radio del usuario)
                if (!DebeActivarClustering(zoomLevel, alarmas.Count, radioUsuarioMetros))
                {
                    System.Diagnostics.Debug.WriteLine($"GridClustering: Clustering NO activado (zoom {zoomLevel}, {alarmas.Count} alarmas, radio {radioUsuarioMetros}m)");
                    // Retornar pines individuales
                    return ConvertirAlarmasAPines(alarmas);
                }

                // Obtener tamaño de grid
                var tamañoGrid = ObtenerTamañoGrid(zoomLevel);
                System.Diagnostics.Debug.WriteLine($"GridClustering: Clustering ACTIVADO - Tamaño grid: {tamañoGrid}m");

                // Agrupar alarmas por celda del grid Y tipo de alarma
                // CRÍTICO: Solo agrupar alarmas del MISMO tipo (evita mezclar tipos diferentes)
                var cells = new Dictionary<string, List<AlarmaCercana>>();

                foreach (var alarma in alarmas)
                {
                    var cellKey = CalcularCeldaGrid(
                        (double)alarma.latitud_alarma,
                        (double)alarma.longitud_alarma,
                        tamañoGrid);

                    // Crear clave compuesta: "celda_tipo" para separar alarmas de distinto tipo
                    var cellTypeKey = $"{cellKey}_tipo{alarma.tipoalarma_id}";

                    if (!cells.ContainsKey(cellTypeKey))
                        cells[cellTypeKey] = new List<AlarmaCercana>();

                    cells[cellTypeKey].Add(alarma);
                }

                System.Diagnostics.Debug.WriteLine($"GridClustering: {alarmas.Count} alarmas agrupadas en {cells.Count} grupos (celda + tipo)");

                // Procesar cada celda
                int clusterCount = 0;
                int individualCount = 0;

                foreach (var cell in cells)
                {
                    if (cell.Value.Count > 1)
                    {
                        // Crear cluster
                        var cluster = new ClusterPin(cell.Value)
                        {
                            GridCellKey = cell.Key
                        };

                        resultado.Add(cluster);
                        clusterCount++;

                        // Log detallado: tipo de alarma y IDs de las alarmas agrupadas
                        var alarmaIds = string.Join(", ", cell.Value.Select(a => a.alarma_id));
                        System.Diagnostics.Debug.WriteLine($"GridClustering: Cluster creado en '{cell.Key}' con {cell.Value.Count} alarmas tipo {cluster.TipoDominante} (IDs: {alarmaIds})");
                    }
                    else
                    {
                        // Pin individual
                        var alarma = cell.Value.First();
                        var pin = ConvertirAlarmaAPin(alarma);
                        resultado.Add(pin);
                        individualCount++;
                        System.Diagnostics.Debug.WriteLine($"GridClustering: Pin individual creado para alarma {alarma.alarma_id} tipo {alarma.tipoalarma_id} en celda {cell.Key}");
                    }
                }

                System.Diagnostics.Debug.WriteLine($"GridClustering: Resultado - {clusterCount} clusters + {individualCount} pines individuales = {resultado.Count} total");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GridClustering: ERROR - {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"GridClustering: StackTrace - {ex.StackTrace}");
                CrashlyticsHelper.LogError(ex, "GridClusteringHelper", "ClusterizarAlarmas");

                // En caso de error, retornar pines individuales (fallback seguro)
                return ConvertirAlarmasAPines(alarmas);
            }

            return resultado;
        }

        /// <summary>
        /// Convierte una AlarmaCercana individual a CustomPin.
        /// Mantiene compatibilidad con el flujo existente de HomePage.
        /// </summary>
        private static CustomPin ConvertirAlarmaAPin(AlarmaCercana alarma)
        {
            var pin = new CustomPin()
            {
                MarkerId = alarma.alarma_id.ToString(),
                Id = alarma.alarma_id.ToString(),
                Label = $"Alarma {alarma.alarma_id}",
                TipoAlarma = alarma.tipoalarma_id,
                Type = PinType.Generic,
                Address = alarma.descripciontipoalarma,
                Location = new Location((double)alarma.latitud_alarma, (double)alarma.longitud_alarma),
                FlagPropietarioAlarma = alarma.flag_propietario_alarma,
                AlarmaCercana = alarma
            };

            return pin;
        }

        /// <summary>
        /// Convierte una lista de AlarmaCercana a CustomPins individuales (sin clustering).
        /// </summary>
        private static List<CustomPin> ConvertirAlarmasAPines(List<AlarmaCercana> alarmas)
        {
            var resultado = new List<CustomPin>();

            foreach (var alarma in alarmas)
            {
                resultado.Add(ConvertirAlarmaAPin(alarma));
            }

            return resultado;
        }

        /// <summary>
        /// Filtra alarmas que están dentro del viewport visible del mapa.
        /// Optimización: reduce cantidad de alarmas a clusterizar.
        /// </summary>
        /// <param name="alarmas">Lista completa de alarmas</param>
        /// <param name="visibleRegion">Región visible del mapa</param>
        /// <returns>Lista de alarmas dentro del viewport</returns>
        public static List<AlarmaCercana> FiltrarPorViewport(List<AlarmaCercana> alarmas, MapSpan visibleRegion)
        {
            if (alarmas == null || !alarmas.Any() || visibleRegion == null)
                return alarmas ?? new List<AlarmaCercana>();

            try
            {
                var centerLat = visibleRegion.Center.Latitude;
                var centerLng = visibleRegion.Center.Longitude;
                var latDelta = visibleRegion.LatitudeDegrees;
                var lngDelta = visibleRegion.LongitudeDegrees;

                var minLat = centerLat - latDelta / 2;
                var maxLat = centerLat + latDelta / 2;
                var minLng = centerLng - lngDelta / 2;
                var maxLng = centerLng + lngDelta / 2;

                var alarmasEnViewport = alarmas
                    .Where(a =>
                        (double)a.latitud_alarma >= minLat &&
                        (double)a.latitud_alarma <= maxLat &&
                        (double)a.longitud_alarma >= minLng &&
                        (double)a.longitud_alarma <= maxLng)
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"GridClustering: Viewport filter - {alarmasEnViewport.Count}/{alarmas.Count} alarmas en viewport");

                return alarmasEnViewport;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GridClustering: Error filtrando por viewport: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "GridClusteringHelper", "FiltrarPorViewport");
                return alarmas; // Fallback: retornar todas
            }
        }
    }
}
