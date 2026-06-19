// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

// ViewModels/RankingPoliticosViewModel.cs
// Módulo político: ViewModel para el ranking top/tail de políticos locales por métrica.
// Creado: 2026-04-07

using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;
using sospect.DTOs;
using sospect.Helpers;
using sospect.Services;
using sospect.Views.Popups;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace sospect.ViewModels
{
    public class RankingPoliticosViewModel : BaseViewModel
    {
        // ── Métricas disponibles ──────────────────────────────────────────────

        public class MetricaOpcion
        {
            public string Key { get; set; } = string.Empty;
            public string Nombre { get; set; } = string.Empty;
            public override string ToString() => Nombre;
        }

        public List<MetricaOpcion> Metricas { get; } = new List<MetricaOpcion>
        {
            new MetricaOpcion { Key = "score_gestion",    Nombre = TranslateExtension.Translate("LblMetricaScoreGestion") },
            new MetricaOpcion { Key = "pct_resolucion",   Nombre = TranslateExtension.Translate("LblMetricaPctResolucion") },
            new MetricaOpcion { Key = "cnt_total",        Nombre = TranslateExtension.Translate("LblMetricaCntAlarmas") },
            new MetricaOpcion { Key = "cnt_likes",        Nombre = TranslateExtension.Translate("LblMetricaCntLikes") },
            new MetricaOpcion { Key = "cnt_reenvios",     Nombre = TranslateExtension.Translate("LblMetricaCntReenvios") },
            new MetricaOpcion { Key = "avg_dias",         Nombre = TranslateExtension.Translate("LblMetricaAvgDias") },
            new MetricaOpcion { Key = "pct_aprobacion",   Nombre = TranslateExtension.Translate("LblMetricaAprobacion") },
        };

        // ── Métrica seleccionada ──────────────────────────────────────────────

        private MetricaOpcion? _metricaSeleccionada;
        public MetricaOpcion? MetricaSeleccionada
        {
            get => _metricaSeleccionada;
            set
            {
                if (SetProperty(ref _metricaSeleccionada, value) && value != null)
                {
                    OnPropertyChanged(nameof(NombreMetricaSeleccionada));
                    OnPropertyChanged(nameof(EsMetricaScoreGestion));
                    _ = CargarRankingAsync(value.Key);
                }
            }
        }

        public string NombreMetricaSeleccionada =>
            _metricaSeleccionada?.Nombre ?? string.Empty;

        public bool EsMetricaScoreGestion =>
            _metricaSeleccionada?.Key == "score_gestion";

        // ── Comandos ──────────────────────────────────────────────────────────

        public ICommand AbrirPickerMetricaCommand { get; }
        public ICommand AbrirCriteriosCommand { get; }

        // ── Listas top/tail ───────────────────────────────────────────────────

        private ObservableCollection<RankingPoliticoItemDto> _top5 = new();
        public ObservableCollection<RankingPoliticoItemDto> Top5
        {
            get => _top5;
            set => SetProperty(ref _top5, value);
        }

        private ObservableCollection<RankingPoliticoItemDto> _tail5 = new();
        public ObservableCollection<RankingPoliticoItemDto> Tail5
        {
            get => _tail5;
            set => SetProperty(ref _tail5, value);
        }

        // ── Estado de vacío ───────────────────────────────────────────────────

        private bool _hayDatos;
        public bool HayDatos
        {
            get => _hayDatos;
            set
            {
                SetProperty(ref _hayDatos, value);
                OnPropertyChanged(nameof(SinDatos));
            }
        }
        public bool SinDatos => !_hayDatos;

        // ── Constructor ───────────────────────────────────────────────────────

        public RankingPoliticosViewModel()
        {
            AbrirPickerMetricaCommand = new Command(async () => await AbrirPickerMetricaAsync());
            AbrirCriteriosCommand     = new Command(async () => await AbrirCriteriosAsync());

            // Seleccionar la primera métrica por defecto para disparar la carga inicial
            if (Metricas.Count > 0)
                MetricaSeleccionada = Metricas[0];
        }

        // ── Apertura del popup de métricas ────────────────────────────────────

        private async Task AbrirPickerMetricaAsync()
        {
            try
            {
                if (App.Current?.MainPage == null) return;

                var popup = new MetricaRankingPickerPopup(Metricas, _metricaSeleccionada);
                popup.MetricaSelected += (s, metrica) =>
                {
                    if (metrica != null)
                        MetricaSeleccionada = metrica;
                };
                await App.Current.MainPage.ShowPopupAsync(popup);
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "RankingPoliticosViewModel", "AbrirPickerMetricaAsync");
            }
        }

        // ── Apertura del popup de criterios ───────────────────────────────────

        private async Task AbrirCriteriosAsync()
        {
            try
            {
                if (App.Current?.MainPage == null) return;
                var popup = new CriteriosGestionPopup();
                await App.Current.MainPage.ShowPopupAsync(popup);
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "RankingPoliticosViewModel", "AbrirCriteriosAsync");
            }
        }

        // ── Carga de datos ────────────────────────────────────────────────────

        private async Task CargarRankingAsync(string metricaKey)
        {
            if (IsRunning) return;

            IsRunning = true;
            HayDatos = false;
            Top5 = new ObservableCollection<RankingPoliticoItemDto>();
            Tail5 = new ObservableCollection<RankingPoliticoItemDto>();

            try
            {
                var resultado = await ApiService.ObtenerRankingPoliticos(metricaKey);

                if (resultado != null)
                {
                    var top5Lista  = resultado.Top5  ?? new List<RankingPoliticoItemDto>();
                    var tail5Lista = resultado.Tail5 ?? new List<RankingPoliticoItemDto>();

                    for (int i = 0; i < top5Lista.Count;  i++) top5Lista[i].Posicion  = i + 1;
                    for (int i = 0; i < tail5Lista.Count; i++) tail5Lista[i].Posicion = i + 1;

                    Top5  = new ObservableCollection<RankingPoliticoItemDto>(top5Lista);
                    Tail5 = new ObservableCollection<RankingPoliticoItemDto>(tail5Lista);
                    HayDatos = Top5.Count > 0 || Tail5.Count > 0;
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "RankingPoliticosViewModel", "CargarRankingAsync");
            }
            finally
            {
                IsRunning = false;
            }
        }
    }
}


