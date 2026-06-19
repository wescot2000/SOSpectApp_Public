// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using Newtonsoft.Json;
    using sospect.AuthHelpers;
    using sospect.Helpers;
    using sospect.Models;
    using sospect.Services;
    using sospect.Views.Popups;
    using sospect.Views;
    using Microsoft.Maui.ApplicationModel;
    using Microsoft.Maui.Controls;
    using CommunityToolkit.Maui.Extensions;
    using CommunityToolkit.Maui.Views;

    namespace sospect.ViewModels
    {
        public class ReportsPageViewModel : BaseViewModel
        {
            public ICommand ReporteBasicoCommand { get; }
            public ICommand ReporteCalorCommand { get; }
            public ICommand TimeEvoReportCommand { get; }
            public ICommand PreCogReportsCommand { get; }
            public ICommand AnaliticaDeTextosCommand { get; }
            public ICommand RankingPoliticosCommand { get; }
            public ICommand DashboardEmprendedorCommand { get; }

            // ─── Dashboard de gamificación (2026-04-09) ───────────────────────────────
            private bool _tieneEmprendimiento = false;
            public bool TieneEmprendimiento
            {
                get => _tieneEmprendimiento;
                set
                {
                    SetProperty(ref _tieneEmprendimiento, value);
                    OnPropertyChanged(nameof(TieneDatosDashboard));
                }
            }

            private string _nombreEmprendimiento = "";
            public string NombreEmprendimiento
            {
                get => _nombreEmprendimiento;
                set => SetProperty(ref _nombreEmprendimiento, value);
            }

            private string _puestoEnPaisTexto = "";
            public string PuestoEnPaisTexto
            {
                get => _puestoEnPaisTexto;
                set => SetProperty(ref _puestoEnPaisTexto, value);
            }

            private decimal _reputacionPropia = 0m;
            public decimal ReputacionPropia
            {
                get => _reputacionPropia;
                set => SetProperty(ref _reputacionPropia, value);
            }

            private double _reputacionPropiaProgress = 0.0;
            public double ReputacionPropiaProgress
            {
                get => _reputacionPropiaProgress;
                set => SetProperty(ref _reputacionPropiaProgress, value);
            }

            private double _reputacionMejorProgress = 0.0;
            public double ReputacionMejorProgress
            {
                get => _reputacionMejorProgress;
                set => SetProperty(ref _reputacionMejorProgress, value);
            }

            private double _reputacionPeorProgress = 0.0;
            public double ReputacionPeorProgress
            {
                get => _reputacionPeorProgress;
                set => SetProperty(ref _reputacionPeorProgress, value);
            }

            private string _reputacionMejorTexto = "";
            public string ReputacionMejorTexto
            {
                get => _reputacionMejorTexto;
                set => SetProperty(ref _reputacionMejorTexto, value);
            }

            private string _reputacionPeorTexto = "";
            public string ReputacionPeorTexto
            {
                get => _reputacionPeorTexto;
                set => SetProperty(ref _reputacionPeorTexto, value);
            }

            private string _tiempoRespuesta = "-";
            public string TiempoRespuesta
            {
                get => _tiempoRespuesta;
                set => SetProperty(ref _tiempoRespuesta, value);
            }

            private string _satisfaccion = "-";
            public string Satisfaccion
            {
                get => _satisfaccion;
                set => SetProperty(ref _satisfaccion, value);
            }

            private string _chatsMes = "0";
            public string ChatsMes
            {
                get => _chatsMes;
                set => SetProperty(ref _chatsMes, value);
            }

            private string _transacciones = "0";
            public string Transacciones
            {
                get => _transacciones;
                set => SetProperty(ref _transacciones, value);
            }

            private bool _cargandoDashboard = false;
            public bool CargandoDashboard
            {
                get => _cargandoDashboard;
                set => SetProperty(ref _cargandoDashboard, value);
            }

            private bool _sinDatosEmprendimiento = false;
            public bool SinDatosEmprendimiento
            {
                get => _sinDatosEmprendimiento;
                set
                {
                    SetProperty(ref _sinDatosEmprendimiento, value);
                    OnPropertyChanged(nameof(TieneDatosDashboard));
                }
            }

            /// <summary>
            /// True cuando hay emprendimiento activo Y ya tiene métricas calculadas.
            /// Controla la visibilidad del Frame con datos en DashboardEmprendedorPage.
            /// </summary>
            public bool TieneDatosDashboard => _tieneEmprendimiento && !_sinDatosEmprendimiento;

            public ReportsPageViewModel(INavigation navigation)
            {
                ReporteBasicoCommand = new Command(async () => await ReporteBasicoClicked(navigation));
                ReporteCalorCommand = new Command(async () => await ReporteCalorClicked(navigation));
                TimeEvoReportCommand = new Command(async () => await TimeEvoReportClicked(navigation));
                PreCogReportsCommand = new Command(() => PreCogReportsClicked(navigation));
                AnaliticaDeTextosCommand = new Command(async () => await AnaliticaDeTextosClicked(navigation));
                RankingPoliticosCommand = new Command(async () => await RankingPoliticosClicked(navigation));
                DashboardEmprendedorCommand = new Command(async () => await DashboardEmprendedorClicked(navigation));
            }

            /// <summary>
            /// Carga el dashboard de gamificación del emprendedor.
            /// Se llama desde ReportsPage.OnAppearing (sin parámetro = primer emprendimiento activo)
            /// o desde DashboardEmprendedorClicked (con idEmprendimiento específico).
            /// </summary>
            public async Task CargarDashboardEmprendedorAsync(long idEmprendimiento = 0)
            {
                try
                {
                    Console.WriteLine($"[Dashboard-VM] CargarDashboardEmprendedorAsync START");
                    Console.WriteLine($"[Dashboard-VM] App.persona={App.persona?.user_id_thirdparty ?? "NULL"}");

                    if (string.IsNullOrEmpty(App.persona?.user_id_thirdparty))
                    {
                        Console.WriteLine($"[Dashboard-VM] user_id_thirdparty vacío → abortando");
                        return;
                    }

                    CargandoDashboard = true;

                    var dashboard = await ApiService.ObtenerDashboardEmprendedor(App.persona.user_id_thirdparty, idEmprendimiento);

                    Console.WriteLine($"[Dashboard-VM] Respuesta recibida: dashboard={dashboard != null}, IsSuccess={dashboard?.IsSuccess}, TieneEmprendimiento={dashboard?.TieneEmprendimiento}, Message={dashboard?.Message}");

                    if (dashboard == null || !dashboard.IsSuccess || !dashboard.TieneEmprendimiento)
                    {
                        Console.WriteLine($"[Dashboard-VM] Sin emprendimiento o error → TieneEmprendimiento=false");
                        TieneEmprendimiento = false;
                        SinDatosEmprendimiento = false;
                        return;
                    }

                    // Caso: emprendimiento activo pero sin métricas calculadas aún
                    if (dashboard.SinDatos)
                    {
                        Console.WriteLine($"[Dashboard-VM] sinDatos=true → mostrar estado vacío");
                        TieneEmprendimiento = true;
                        SinDatosEmprendimiento = true;
                        return;
                    }

                    var m = dashboard.MisMetricas;
                    Console.WriteLine($"[Dashboard-VM] MisMetricas={m != null}, Nombre={m?.Nombre}");
                    if (m == null)
                    {
                        Console.WriteLine($"[Dashboard-VM] MisMetricas es null → sinDatos=true");
                        TieneEmprendimiento = true;
                        SinDatosEmprendimiento = true;
                        return;
                    }

                    NombreEmprendimiento = m.Nombre ?? "";
                    ReputacionPropia = m.ReputacionPromedio;
                    ReputacionPropiaProgress = Math.Min(1.0, (double)m.ReputacionPromedio / 5.0);

                    var puestoFmt = await TranslateExtension.TranslateAsync("LblPuestoEnPais");
                    PuestoEnPaisTexto = string.Format(puestoFmt, dashboard.MiPuesto, dashboard.Pais ?? "");

                    TiempoRespuesta = m.PromedioTiempoRespuestaMinutos.HasValue
                        ? $"{m.PromedioTiempoRespuestaMinutos.Value} min"
                        : "-";

                    Satisfaccion = m.PorcentajeSatisfaccion.HasValue
                        ? $"{m.PorcentajeSatisfaccion.Value:F0}%"
                        : "-";

                    ChatsMes = m.TotalChatsMesActual.ToString();
                    Transacciones = m.TotalTransaccionesExitosas.ToString();

                    if (dashboard.Mejor != null)
                    {
                        ReputacionMejorProgress = Math.Min(1.0, (double)dashboard.Mejor.Reputacion / 5.0);
                        ReputacionMejorTexto = $"{dashboard.Mejor.Reputacion:F1} ⭐";
                    }

                    if (dashboard.Peor != null)
                    {
                        ReputacionPeorProgress = Math.Min(1.0, (double)dashboard.Peor.Reputacion / 5.0);
                        ReputacionPeorTexto = $"{dashboard.Peor.Reputacion:F1} ⭐";
                    }

                    TieneEmprendimiento = true;
                    SinDatosEmprendimiento = false;
                    Console.WriteLine($"[Dashboard-VM] TieneEmprendimiento=true → widget visible");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Dashboard-VM] EXCEPCION {ex.GetType().Name}: {ex.Message}");
                    CrashlyticsHelper.LogError(ex, "ReportsPageViewModel", "CargarDashboardEmprendedorAsync");
                    TieneEmprendimiento = false;
                }
                finally
                {
                    CargandoDashboard = false;
                    Console.WriteLine($"[Dashboard-VM] CargarDashboardEmprendedorAsync END - TieneEmprendimiento={TieneEmprendimiento}");
                }
            }

            private async Task ReporteBasicoClicked(INavigation navigation)
            {
                var BasicReportPage = new BasicReportPage();
                await navigation.PushAsync(BasicReportPage);
            }

            private async Task ReporteCalorClicked(INavigation navigation)
            {
                var HeatReportPage = new HeatReportPage();
                await navigation.PushAsync(HeatReportPage);
            }

            private async Task TimeEvoReportClicked(INavigation navigation)
            {
                var TimeEvolutionReportPage = new TimeEvolutionReportPage();
                await navigation.PushAsync(TimeEvolutionReportPage);
            }

            private async Task PreCogReportsClicked(INavigation navigation)
            {
                var PredictiveReportPage = new PredictiveReportPage();
                await navigation.PushAsync(PredictiveReportPage);
            }

            private async Task AnaliticaDeTextosClicked(INavigation navigation)
            {
                var TextAnalyticsPage = new TextAnalyticsPage();
                await navigation.PushAsync(TextAnalyticsPage);
            }

            private async Task RankingPoliticosClicked(INavigation navigation)
            {
                var rankingPage = new RankingPoliticosPage();
                await navigation.PushAsync(rankingPage);
            }

            // 2026-04-12: Guard para evitar apertura concurrente del dashboard (doble-tap o lentitud de red)
            private bool _dashboardNavegando = false;

            private static INavigation GetCurrentNavigation()
            {
                try
                {
                    if (Application.Current?.MainPage is Microsoft.Maui.Controls.TabbedPage tabbedPage)
                    {
                        var currentPage = tabbedPage.CurrentPage;
                        if (currentPage is NavigationPage navPage)
                            return navPage.Navigation;
                        if (currentPage?.Navigation != null)
                            return currentPage.Navigation;
                    }
                    return Application.Current?.MainPage?.Navigation;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Dashboard] GetCurrentNavigation ERROR: {ex.Message}");
                    return null;
                }
            }

            private async Task DashboardEmprendedorClicked(INavigation _unused)
            {
                // Guard: si ya estamos navegando/cargando, ignorar taps adicionales
                if (_dashboardNavegando) return;
                _dashboardNavegando = true;

                try
                {
                    Console.WriteLine($"[Dashboard] DashboardEmprendedorClicked START");

                    // Resolver navegación en tiempo de ejecución (no en constructor) para
                    // compatibilidad con TabbedPage en Android release sin depurador.
                    var navigation = GetCurrentNavigation();
                    if (navigation == null)
                    {
                        Console.WriteLine($"[Dashboard] Navigation no disponible, abortando");
                        return;
                    }

                    // Obtener emprendimientos activos del usuario
                    var resultado = await ApiService.ObtenerEmprendimientosDelUsuario(App.persona.user_id_thirdparty);
                    Console.WriteLine($"[Dashboard] ObtenerEmprendimientosDelUsuario isSuccess={resultado?.IsSuccess} count={resultado?.Emprendimientos?.Count ?? 0}");

                    if (resultado == null || !resultado.IsSuccess || resultado.Emprendimientos == null || resultado.Emprendimientos.Count == 0)
                    {
                        // Sin emprendimientos activos — no debería ocurrir si el botón es visible solo cuando TieneEmprendimiento=true
                        Console.WriteLine($"[Dashboard] Sin emprendimientos activos, abortando");
                        return;
                    }

                    long idEmprendimientoSeleccionado;

                    if (resultado.Emprendimientos.Count == 1)
                    {
                        // Solo uno: ir directo
                        idEmprendimientoSeleccionado = resultado.Emprendimientos[0].IdEmprendimiento;
                        Console.WriteLine($"[Dashboard] Un solo emprendimiento: id={idEmprendimientoSeleccionado}");
                    }
                    else
                    {
                        // Más de uno: mostrar popup personalizado con nombre + NIT
                        Console.WriteLine($"[Dashboard] {resultado.Emprendimientos.Count} emprendimientos, mostrando popup selector");
                        var popup = new SeleccionarEmprendimientoPopup(resultado.Emprendimientos);

                        // 2026-04-12: Usar la página actual de la ventana en lugar de MainPage para
                        // compatibilidad con TabbedPage en release (sin depurador).
                        // Application.Current.MainPage es la TabbedPage raíz y en release puede fallar
                        // con ShowPopupAsync; la ventana activa siempre apunta a la página correcta.
                        var paginaActual = Application.Current?.Windows?.Count > 0
                            ? Application.Current.Windows[0].Page
                            : Application.Current?.MainPage;
                        Console.WriteLine($"[Dashboard] paginaActual tipo={paginaActual?.GetType().Name ?? "null"}");
                        await paginaActual.ShowPopupAsync(popup);

                        if (popup.EmprendimientoSeleccionado == null)
                        {
                            Console.WriteLine($"[Dashboard] Usuario canceló el selector, abortando");
                            return;
                        }

                        idEmprendimientoSeleccionado = popup.EmprendimientoSeleccionado.IdEmprendimiento;
                        Console.WriteLine($"[Dashboard] Emprendimiento seleccionado: id={idEmprendimientoSeleccionado}");
                    }

                    // Cargar el dashboard para el emprendimiento seleccionado
                    await CargarDashboardEmprendedorAsync(idEmprendimientoSeleccionado);

                    var dashboardPage = new DashboardEmprendedorPage(this);
                    await navigation.PushAsync(dashboardPage);
                    Console.WriteLine($"[Dashboard] DashboardEmprendedorClicked END - nav tipo={navigation?.GetType().Name}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Dashboard] DashboardEmprendedorClicked EXCEPCION: {ex.Message}");
                    CrashlyticsHelper.LogError(ex, "ReportsPageViewModel", "DashboardEmprendedorClicked");
                }
                finally
                {
                    // Liberar guard SIEMPRE (incluso en excepción o cancelación)
                    _dashboardNavegando = false;
                    Console.WriteLine($"[Dashboard] DashboardEmprendedorClicked guard liberado");
                }
            }
        }
    }


