// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using sospect.Models;
using sospect.Services;
using SkiaSharp;
using Microcharts; //  DESCOMENTADO: Microcharts está disponible en .csproj
using System.Collections.Generic;
using Microsoft.Maui.Controls;
using sospect.Views;
using sospect.Interfaces;
using sospect.Helpers;

namespace sospect.ViewModels
{
    public class BasicReportPageViewModel : BaseViewModel
    {
        public ICommand TipoAlarmasCommand { get; }
        public ICommand EfectividadAlarmasCommand { get; }
        public ICommand MetricasBasicasCommand { get; }
        public ICommand HistorialCommand { get; }

        //  RESTAURADO: Chart property descomentada
        private Chart _chart;
        public Chart Chart
        {
            get => _chart;
            set => SetValue(ref _chart, value);
        }

        private ObservableCollection<TipoAlarmaReporteConColor> _TiposAlarmaReporte;
        public ObservableCollection<TipoAlarmaReporteConColor> TiposAlarmaReporte
        {
            get => this._TiposAlarmaReporte;
            set
            {
                if (_TiposAlarmaReporte != value)
                {
                    _TiposAlarmaReporte = value;
                    OnPropertyChanged(nameof(TiposAlarmaReporte));
                }
            }
        }

        private ObservableCollection<MetricasBasicasReporte> _MetricasBasicasReporte;
        public ObservableCollection<MetricasBasicasReporte> MetricasBasicasReporte
        {
            get => this._MetricasBasicasReporte;
            set => this.SetValue(ref this._MetricasBasicasReporte, value);
        }

        private PromEfectivoAlarmasReporteBasResponse _EfectividadAlarmas;
        public PromEfectivoAlarmasReporteBasResponse EfectividadAlarmas
        {
            get => this._EfectividadAlarmas;
            set => this.SetValue(ref this._EfectividadAlarmas, value);
        }

        private string _descripcionCompleta = "{helpers:TranslateExtension Text='LblReporteBasicoDescripcion'}";
        public string DescripcionCompleta
        {
            get => _descripcionCompleta;
            set
            {
                if (value == _descripcionCompleta) return;
                _descripcionCompleta = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DescripcionCorta));
            }
        }

        public string DescripcionCorta
        {
            get
            {
                if (DescripcionCompleta == null)
                    return null;

                var palabras = DescripcionCompleta.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (palabras.Length <= 10)
                    return DescripcionCompleta;

                return string.Join(" ", palabras.Take(10)) + "...";
            }
        }

        private bool _expandir;
        public bool Expandir
        {
            get => _expandir;
            set
            {
                if (_expandir != value)
                {
                    _expandir = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DescripcionCorta));
                }
            }
        }

        public ICommand ExpandirCommand => new RelayCommand(() => Expandir = !Expandir);

        public BasicReportPageViewModel(INavigation navigation)
        {
            Init();
            TipoAlarmasCommand = new Command(async () => await TipoAlarmasClicked(navigation));
            EfectividadAlarmasCommand = new Command(async () => await EfectividadAlarmasClicked(navigation));
            MetricasBasicasCommand = new Command(async () => await MetricasBasicasClicked(navigation));
            HistorialCommand = new Command(() => HistorialClicked(navigation));
        }

        private async Task TipoAlarmasClicked(INavigation navigation)
        {
            var BasicReportTiposAlarmaPage = new BasicReportTiposAlarmaPage();
            await navigation.PushAsync(BasicReportTiposAlarmaPage);
        }

        private async Task HistorialClicked(INavigation navigation)
        {
            var HistorialPage = new HistorialPage();
            await navigation.PushAsync(HistorialPage);
        }

        private async Task EfectividadAlarmasClicked(INavigation navigation)
        {
            var BasicReportEfectividadAlarmasPage = new BasicReportEfectividadAlarmasPage();
            await navigation.PushAsync(BasicReportEfectividadAlarmasPage);
        }

        private async Task MetricasBasicasClicked(INavigation navigation)
        {
            var BasicReportMetricasBasicasPage = new BasicReportMetricasBasicasPage();
            await navigation.PushAsync(BasicReportMetricasBasicasPage);
        }

        private async void Init()
        {
            await CargarTiposAlarmaReporte();
            await CargarMetricasBasicasReporte();
            await CargarPromedioEfectivoAlarmas();
        }

        int apiCallsInProgress = 0;

        private bool IsBusy
        {
            get
            {
                return apiCallsInProgress > 0;
            }
        }

        private async Task CargarMetricasBasicasReporte()
        {
            if (IsBusy)
            {
                return;
            }

            apiCallsInProgress++;

            IsRunning = true;
            try
            {
                var metricasBasicasReporte = await ApiService.ObtenerReportBasMetricasSueltas();
                MetricasBasicasReporte = new ObservableCollection<MetricasBasicasReporte>(metricasBasicasReporte);
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "BasicReportPageViewModel", "CargarMetricasBasicasReporte");
            }
            finally
            {
                apiCallsInProgress--;
                IsRunning = false;
            }
        }

        private async Task CargarPromedioEfectivoAlarmas()
        {
            if (IsBusy)
            {
                return;
            }

            apiCallsInProgress++;

            IsRunning = true;
            try
            {
                var efectividadAlarmas = await ApiService.ObtenerPromedioEfectivoAlarmas();
                EfectividadAlarmas = efectividadAlarmas;
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "BasicReportPageViewModel", "CargarPromedioEfectivoAlarmas");
            }
            finally
            {
                IsRunning = false;
                apiCallsInProgress--;
            }
        }

        private async Task CargarTiposAlarmaReporte()
        {
            if (IsBusy)
            {
                return;
            }

            apiCallsInProgress++;

            IsRunning = true;
            try
            {
                Console.WriteLine($"[BasicReport-DIAG] CargarTiposAlarmaReporte INICIO - IsMainThread={Microsoft.Maui.ApplicationModel.MainThread.IsMainThread} ThreadId={System.Threading.Thread.CurrentThread.ManagedThreadId}");

                var tiposAlarmaReporte = await ApiService.ObtenerReportBasParticipacionTipoAlarma();

                Console.WriteLine($"[BasicReport-DIAG] API retornó {tiposAlarmaReporte?.Count ?? 0} items - IsMainThread={Microsoft.Maui.ApplicationModel.MainThread.IsMainThread} ThreadId={System.Threading.Thread.CurrentThread.ManagedThreadId}");

                int index = 0;
                var coleccion = new ObservableCollection<TipoAlarmaReporteConColor>(
                    tiposAlarmaReporte.Select(tipo => new TipoAlarmaReporteConColor
                    {
                        TipoAlarma = tipo,
                        Color = GenerateColor(index++, tiposAlarmaReporte.Count)
                    }));

                Console.WriteLine($"[BasicReport-DIAG] Colección creada con {coleccion.Count} items. Primer DescripcionTraducida='{coleccion.FirstOrDefault()?.DescripcionTraducida}'");

                // 2026-04-12: Asegurar que la asignación al BindableLayout ocurra en el hilo principal.
                //             En iOS, actualizaciones de UI desde hilos de background se ignoran silenciosamente.
                await Microsoft.Maui.ApplicationModel.MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Console.WriteLine($"[BasicReport-DIAG] Asignando TiposAlarmaReporte en main thread - IsMainThread={Microsoft.Maui.ApplicationModel.MainThread.IsMainThread}");
                    TiposAlarmaReporte = coleccion;
                    Console.WriteLine($"[BasicReport-DIAG] TiposAlarmaReporte asignado. Count={TiposAlarmaReporte?.Count}");
                    CreateChart();
                });
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "BasicReportPageViewModel", "CargarTiposAlarmaReporte");
            }
            finally
            {
                IsRunning = false;
                apiCallsInProgress--;
            }
        }

        private SKColor GenerateColor(int index, int total)
        {
            var hue = (index * 360 / total) % 360;
            var lightness = 35 + ((index * 30 / total) % 65); // produces lightness between 35 and 65

            return SKColor.FromHsl(hue, 100, lightness);
        }

        public class TipoAlarmaReporteConColor
        {
            public TipoAlarmaReporte TipoAlarma { get; set; }
            public SKColor Color { get; set; }

            // 2026-04-12: Propiedades directas para evitar binding anidado (TipoAlarma.XXX)
            //             en iOS con BindableLayout. Los bindings anidados a propiedades
            //             computadas sin INotifyPropertyChanged se evalúan pero el resultado
            //             no se aplica al Label en iOS. Usar binding directo es el patrón
            //             que funciona (mismo enfoque de TipoAlarmaItemConColor en ReporteAutoridadViewModel).
            public string DescripcionTraducida
            {
                get
                {
                    var valor = TipoAlarma?.DescripcionTraducida ?? string.Empty;
                    Console.WriteLine($"[BasicReport-DIAG] TipoAlarmaReporteConColor.DescripcionTraducida getter - valor='{valor}' IsMainThread={Microsoft.Maui.ApplicationModel.MainThread.IsMainThread} ThreadId={System.Threading.Thread.CurrentThread.ManagedThreadId}");
                    return valor;
                }
            }
            public string ParticipacionTexto => TipoAlarma != null ? $"{TipoAlarma.Participacion:N1}%" : string.Empty;
        }

        //  RESTAURADO: Método CreateChart() completo del código Xamarin original
        private void CreateChart()
        {
            try
            {
                var entries = new List<ChartEntry>();

                if (TiposAlarmaReporte != null && TiposAlarmaReporte.Any())
                {
                    foreach (var tipo in TiposAlarmaReporte)
                    {
                        if (tipo?.TipoAlarma != null)
                        {
                            entries.Add(new ChartEntry(Convert.ToSingle(tipo.TipoAlarma.Participacion) * 100)
                            {
                                ValueLabel = tipo.TipoAlarma.Participacion.ToString(),
                                Color = tipo.Color
                            });
                        }
                    }
                }

                // Crear el gráfico de pastel
                Chart = new PieChart() { Entries = entries };
            }
            catch (Exception ex)
            {
                // Log del error pero no fallar la carga del reporte
                CrashlyticsHelper.LogError(ex, "BasicReportPageViewModel", "CreateChart");

                // En caso de error, dejar Chart como null o crear uno vacío
                Chart = null;
            }
        }
    }
}


