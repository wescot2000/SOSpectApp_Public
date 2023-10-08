using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;
using sospect.Models;
using sospect.Services;
using SkiaSharp;
using Microcharts;
using System.Collections.Generic;
using Xamarin.Forms;
using sospect.Views;


namespace sospect.ViewModels
{
    public class BasicReportPageViewModel : BaseViewModel
    {
        public ICommand TipoAlarmasCommand { get; }
        public ICommand EfectividadAlarmasCommand { get; }
        public ICommand MetricasBasicasCommand { get; }

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
        }

        private async Task TipoAlarmasClicked(INavigation navigation)
        {
            var BasicReportTiposAlarmaPage = new BasicReportTiposAlarmaPage();
            await navigation.PushAsync(BasicReportTiposAlarmaPage);
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
                var properties = new Dictionary<string, string> {
                        { "Object", "BasicReportPageViewModel" },
                        { "Method", "ObtenerReportBasMetricasSueltas" }
                    };
                Microsoft.AppCenter.Crashes.Crashes.TrackError(ex, properties);
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
                var properties = new Dictionary<string, string> {
                        { "Object", "BasicReportPageViewModel" },
                        { "Method", "ObtenerPromedioEfectivoAlarmas" }
                    };
                Microsoft.AppCenter.Crashes.Crashes.TrackError(ex, properties);
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
                var tiposAlarmaReporte = await ApiService.ObtenerReportBasParticipacionTipoAlarma();
                int index = 0;
                TiposAlarmaReporte = new ObservableCollection<TipoAlarmaReporteConColor>(
                    tiposAlarmaReporte.Select(tipo => new TipoAlarmaReporteConColor
                    {
                        TipoAlarma = tipo,
                        Color = GenerateColor(index++, tiposAlarmaReporte.Count)
                    }));

                // Ahora que los datos están cargados, creamos la gráfica
                CreateChart();
            }
            catch (Exception ex)
            {
                var properties = new Dictionary<string, string> {
                        { "Object", "BasicReportPageViewModel" },
                        { "Method", "ObtenerReportBasParticipacionTipoAlarma" }
                    };
                Microsoft.AppCenter.Crashes.Crashes.TrackError(ex, properties);
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
            //return SKColor.FromHsl(hue, 100, 50); // 50 will give you mid-toned colors.
            var lightness = 35 + ((index * 30 / total) % 65); // produces lightness between 35 and 65

            return SKColor.FromHsl(hue, 100, lightness);
        }


        public class TipoAlarmaReporteConColor
        {
            public TipoAlarmaReporte TipoAlarma { get; set; }
            public SKColor Color { get; set; }
        }

        private void CreateChart()
        {
            var entries = new List<ChartEntry>();
            foreach (var tipo in TiposAlarmaReporte)
            {
                entries.Add(new ChartEntry(Convert.ToSingle(tipo.TipoAlarma.Participacion) * 100)
                {
                    ValueLabel = tipo.TipoAlarma.Participacion.ToString(),
                    Color = tipo.Color
                });
            }

            Chart = new PieChart() { Entries = entries };
        }


    }
}
