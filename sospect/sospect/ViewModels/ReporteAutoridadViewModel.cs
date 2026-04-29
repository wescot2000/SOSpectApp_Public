// ViewModels/ReporteAutoridadViewModel.cs
// Módulo político: ViewModel para la página de reporte de desempeño de un político.
// Creado: 2026-03-09
// MODIFICADO: 2026-03-28 - Agregada calificación ciudadana (1-5 estrellas) + propiedades de aprobación
// MODIFICADO: 2026-04-07 - Agregado VerRankingCommand para navegar al ranking de políticos

using Microcharts;
using Microsoft.Maui.Controls;
using SkiaSharp;
using sospect.DTOs;
using sospect.Helpers;
using sospect.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Threading.Tasks;
using System.Windows.Input;

namespace sospect.ViewModels
{
    public class ReporteAutoridadViewModel : BaseViewModel
    {
        private readonly AutoridadResponsableDto _autoridad;

        // ── Datos del político (para el header) ───────────────────────────────

        public string NombreCompleto => _autoridad.NombreCompleto ?? string.Empty;
        public string CargoNombre => _autoridad.CargoNombre ?? string.Empty;
        public string TerritorioNombre => _autoridad.TerritorioNombre ?? string.Empty;
        public string? FotoUrl => _autoridad.FotoUrl;
        public bool TieneFoto => !string.IsNullOrEmpty(_autoridad.FotoUrl);

        // ── KPIs ──────────────────────────────────────────────────────────────

        private int _cntTotal;
        public int CntTotal
        {
            get => _cntTotal;
            set => SetProperty(ref _cntTotal, value);
        }

        private int _cntAbiertas;
        public int CntAbiertas
        {
            get => _cntAbiertas;
            set => SetProperty(ref _cntAbiertas, value);
        }

        private int _cntCerradas;
        public int CntCerradas
        {
            get => _cntCerradas;
            set => SetProperty(ref _cntCerradas, value);
        }

        private decimal? _pctResolucion;
        public decimal? PctResolucion
        {
            get => _pctResolucion;
            set
            {
                SetProperty(ref _pctResolucion, value);
                OnPropertyChanged(nameof(PctResolucionDecimal));
                OnPropertyChanged(nameof(PctResolucionTexto));
            }
        }

        public double PctResolucionDecimal =>
            PctResolucion.HasValue ? (double)(PctResolucion.Value / 100m) : 0;

        public string PctResolucionTexto =>
            PctResolucion.HasValue ? $"{PctResolucion:F0}%" : "–";

        private int _cntLikes;
        public int CntLikes
        {
            get => _cntLikes;
            set => SetProperty(ref _cntLikes, value);
        }

        private int _cntReenvios;
        public int CntReenvios
        {
            get => _cntReenvios;
            set => SetProperty(ref _cntReenvios, value);
        }

        private string _avgDiasResolucion = "–";
        public string AvgDiasResolucion
        {
            get => _avgDiasResolucion;
            set => SetProperty(ref _avgDiasResolucion, value);
        }

        private string _fechaCalculo = string.Empty;
        public string FechaCalculo
        {
            get => _fechaCalculo;
            set => SetProperty(ref _fechaCalculo, value);
        }

        // ── Aprobación ciudadana ──────────────────────────────────────────────

        /// <summary>
        /// Calificación que el usuario actual ya registró para este político (1-5).
        /// 0 significa que aún no ha calificado.
        /// Cuando cambia, se notifica a todas las propiedades de estrella individuales
        /// para que el converter en XAML actualice los colores.
        /// </summary>
        private int _calificacionActual;
        public int CalificacionActual
        {
            get => _calificacionActual;
            set
            {
                SetProperty(ref _calificacionActual, value);
                // Notificar a las 5 propiedades de estrella para actualizar el color vía converter
                for (int i = 1; i <= 5; i++)
                    OnPropertyChanged($"Estrella{i}Color");
            }
        }

        /// <summary>
        /// Porcentaje de aprobación ciudadana (0-100%). NULL si nadie ha calificado.
        /// Actualizado por el cron diario de refrescar_metricas_politico.
        /// </summary>
        private decimal? _pctAprobacion;
        public decimal? PctAprobacion
        {
            get => _pctAprobacion;
            set
            {
                SetProperty(ref _pctAprobacion, value);
                OnPropertyChanged(nameof(PctAprobacionTexto));
            }
        }

        public string PctAprobacionTexto =>
            PctAprobacion.HasValue ? $"{PctAprobacion:F0}%" : "–";

        private int _cntVotantesAprobacion;
        public int CntVotantesAprobacion
        {
            get => _cntVotantesAprobacion;
            set
            {
                SetProperty(ref _cntVotantesAprobacion, value);
                OnPropertyChanged(nameof(CntVotantesTexto));
            }
        }

        public string CntVotantesTexto => _cntVotantesAprobacion.ToString();

        /// <summary>
        /// Comando que recibe el número de estrella tocada (1-5 como string en CommandParameter)
        /// y registra la calificación en la API.
        /// </summary>
        public ICommand CalificarCommand { get; }

        /// <summary>
        /// Navega al ranking de políticos locales pre-filtrado en aprobación ciudadana.
        /// </summary>
        public ICommand VerRankingCommand { get; }

        // ── Gráfico ───────────────────────────────────────────────────────────

        private Chart _chart;
        public Chart Chart
        {
            get => _chart;
            set => SetValue(ref _chart, value);
        }

        // ── Leyenda del gráfico ───────────────────────────────────────────────

        private ObservableCollection<TipoAlarmaItemConColor> _tiposAlarma;
        public ObservableCollection<TipoAlarmaItemConColor> TiposAlarma
        {
            get => _tiposAlarma;
            set => SetProperty(ref _tiposAlarma, value);
        }

        // ── Constructor ───────────────────────────────────────────────────────

        public ReporteAutoridadViewModel(AutoridadResponsableDto autoridad, INavigation navigation)
        {
            _autoridad = autoridad;
            TiposAlarma = new ObservableCollection<TipoAlarmaItemConColor>();

            CalificarCommand = new Command<string>(async (param) =>
            {
                if (!int.TryParse(param, out int estrella) || estrella < 1 || estrella > 5)
                    return;

                // Actualizar UI inmediatamente (feedback visual rápido)
                CalificacionActual = estrella;

                int resultado = await ApiService.RegistrarAprobacion((int)_autoridad.PoliticoId, estrella);
                if (resultado < 0)
                {
                    // Si falló, revertir
                    System.Diagnostics.Debug.WriteLine($"[ReporteAutoridadViewModel] CalificarCommand: falló para estrella={estrella}");
                }
            });

            VerRankingCommand = new Command(async () =>
            {
                var rankingPage = new Views.RankingPoliticosPage();
                await navigation.PushAsync(rankingPage);
            });

            _ = CargarReporteAsync();
        }

        // ── Carga de datos ────────────────────────────────────────────────────

        private async Task CargarReporteAsync()
        {
            IsRunning = true;
            EmptyState = false;

            try
            {
                // Cargar en paralelo: métricas del político + calificación previa del usuario
                var tareaReporte       = ApiService.ObtenerReportePolitico((int)_autoridad.PoliticoId);
                var tareaCalificacion  = ApiService.ObtenerAprobacionPersona((int)_autoridad.PoliticoId);

                await Task.WhenAll(tareaReporte, tareaCalificacion);

                var reporte      = tareaReporte.Result;
                int calificacion = tareaCalificacion.Result;  // 0 = no calificado, -1 = error

                if (reporte == null)
                {
                    EmptyState = true;
                    // Cargar calificación previa aunque no haya métricas
                    if (calificacion > 0)
                        CalificacionActual = calificacion;
                    return;
                }

                CntTotal = reporte.CntTotal;
                CntAbiertas = reporte.CntAbiertas;
                CntCerradas = reporte.CntCerradas;
                PctResolucion = reporte.PctResolucion;
                CntLikes = reporte.CntLikes;
                CntReenvios = reporte.CntReenvios;
                AvgDiasResolucion = reporte.AvgDiasResolucionTexto;
                FechaCalculo = reporte.FechaCalculo.ToString("dd MMM yyyy HH:mm");
                PctAprobacion = reporte.PctAprobacion;
                CntVotantesAprobacion = reporte.CntVotantesAprobacion;

                // Calificación previa del usuario para este político
                if (calificacion > 0)
                    CalificacionActual = calificacion;

                // Construir leyenda con colores
                var total = reporte.TiposAlarma.Count;
                int index = 0;
                TiposAlarma = new ObservableCollection<TipoAlarmaItemConColor>(
                    reporte.TiposAlarma.Select(t => new TipoAlarmaItemConColor
                    {
                        TipoalarmaId = t.TipoalarmaId,
                        Descripcion = t.Descripcion ?? string.Empty,
                        Cnt = t.Cnt,
                        Pct = t.Pct.HasValue ? $"{t.Pct:F0}%" : "–",
                        Color = GenerateColor(index++, total == 0 ? 1 : total)
                    }));

                CreateChart(reporte.TiposAlarma);

                EmptyState = reporte.CntTotal == 0 && !reporte.TiposAlarma.Any();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"ReporteAutoridadViewModel.CargarReporteAsync: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "ReporteAutoridadViewModel", "CargarReporteAsync");
                EmptyState = true;
            }
            finally
            {
                IsRunning = false;
            }
        }

        // ── Chart helpers (mismo patrón que BasicReportPageViewModel) ─────────

        private void CreateChart(List<TipoAlarmaItemDto> tipos)
        {
            try
            {
                var entries = new List<ChartEntry>();
                int total = tipos.Count;
                int i = 0;

                foreach (var tipo in tipos)
                {
                    entries.Add(new ChartEntry(Convert.ToSingle(tipo.Pct ?? 0))
                    {
                        ValueLabel = tipo.Pct.HasValue ? $"{tipo.Pct:F0}%" : string.Empty,
                        Color = GenerateColor(i++, total == 0 ? 1 : total)
                    });
                }

                Chart = new PieChart { Entries = entries };
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ReporteAutoridadViewModel", "CreateChart");
                Chart = null;
            }
        }

        private SKColor GenerateColor(int index, int total)
        {
            var hue = (index * 360 / total) % 360;
            var lightness = 35 + ((index * 30 / total) % 65);
            return SKColor.FromHsl(hue, 100, lightness);
        }

        // ── Clase auxiliar ────────────────────────────────────────────────────

        public class TipoAlarmaItemConColor
        {
            public long TipoalarmaId { get; set; }
            public string Descripcion { get; set; } = string.Empty;
            public int Cnt { get; set; }
            public string Pct { get; set; } = "–";
            public SKColor Color { get; set; }

            /// <summary>
            /// Color en formato hexadecimal para usar en XAML con Label.TextColor.
            /// </summary>
            public string ColorHex => $"#{Color.Red:X2}{Color.Green:X2}{Color.Blue:X2}";

            /// <summary>
            /// Descripción traducida al idioma actual del dispositivo usando AppResources.
            /// Usa la misma clave Alarm_{id} que el modelo TipoAlarmaReporte.
            /// 2026-04-12: Cambiado a ResourceManager estático (Lazy) para evitar fallo
            ///             en iOS donde crear una nueva instancia por getter causa null silencioso.
            ///             Usa misma instancia compartida que TranslateExtension.
            /// </summary>
            private static readonly Lazy<ResourceManager> _resMgr =
                new Lazy<ResourceManager>(() => new ResourceManager(
                    "sospect.Resources.AppResources",
                    typeof(TipoAlarmaItemConColor).GetTypeInfo().Assembly));

            public string DescripcionTraducida
            {
                get
                {
                    try
                    {
                        string key = $"Alarm_{TipoalarmaId}";
                        string? traducida = _resMgr.Value.GetString(key, CultureInfo.CurrentCulture);
                        if (!string.IsNullOrEmpty(traducida))
                            return traducida;
                        // Fallback: intentar con cultura neutral (en) — cubre iOS con es-CO, es-US, etc.
                        string? neutral = _resMgr.Value.GetString(key, CultureInfo.InvariantCulture);
                        if (!string.IsNullOrEmpty(neutral))
                            return neutral;
                        // Fallback final: descripción del servidor
                        return !string.IsNullOrEmpty(Descripcion) ? Descripcion : key;
                    }
                    catch
                    {
                        return !string.IsNullOrEmpty(Descripcion) ? Descripcion : $"Alarm_{TipoalarmaId}";
                    }
                }
            }
        }
    }
}
