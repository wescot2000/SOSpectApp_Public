// Views/Popups/MetricaRankingPickerPopup.xaml.cs
// Popup para seleccionar la métrica del ranking de políticos.
// Estilo consistente con TipoAlarmaPickerPopup.
// Creado: 2026-04-07

using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;
using sospect.Helpers;
using sospect.Resources.Fonts;
using sospect.ViewModels;
using System;
using System.Collections.Generic;

namespace sospect.Views.Popups
{
    public partial class MetricaRankingPickerPopup : Popup
    {
        public event EventHandler<RankingPoliticosViewModel.MetricaOpcion> MetricaSelected;

        private bool _isClosing = false;
        private readonly RankingPoliticosViewModel.MetricaOpcion _seleccionActual;

        public MetricaRankingPickerPopup(
            List<RankingPoliticosViewModel.MetricaOpcion> metricas,
            RankingPoliticosViewModel.MetricaOpcion seleccionActual)
        {
            InitializeComponent();
            _seleccionActual = seleccionActual;
            PoblarMetricas(metricas);
        }

        private void PoblarMetricas(List<RankingPoliticosViewModel.MetricaOpcion> metricas)
        {
            try
            {
                foreach (var metrica in metricas)
                {
                    bool esSeleccionada = metrica.Key == _seleccionActual?.Key;

                    var fila = new Border
                    {
                        BackgroundColor = esSeleccionada
                            ? Color.FromArgb("#E3F2FD")
                            : Colors.White,
                        Stroke = esSeleccionada
                            ? Color.FromArgb("#1465bb")
                            : Color.FromArgb("#E8E8E8"),
                        StrokeThickness = esSeleccionada ? 1.5 : 1,
                        Padding = new Thickness(16, 12),
                        Margin = new Thickness(0, 0, 0, 8)
                    };
                    fila.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
                    {
                        CornerRadius = new CornerRadius(8)
                    };

                    var grid = new Grid
                    {
                        ColumnDefinitions = new ColumnDefinitionCollection
                        {
                            new ColumnDefinition { Width = GridLength.Star },
                            new ColumnDefinition { Width = GridLength.Auto }
                        }
                    };

                    var lblNombre = new Label
                    {
                        Text = metrica.Nombre,
                        FontSize = 15,
                        TextColor = esSeleccionada
                            ? Color.FromArgb("#1465bb")
                            : Color.FromArgb("#2C3E50"),
                        FontAttributes = esSeleccionada ? FontAttributes.Bold : FontAttributes.None,
                        VerticalOptions = LayoutOptions.Center
                    };

                    grid.Add(lblNombre, 0, 0);

                    if (esSeleccionada)
                    {
                        var lblCheck = new Label
                        {
                            Text = IconFont.Check,
                            FontFamily = "IcoMoonFamily",
                            FontSize = 16,
                            TextColor = Color.FromArgb("#1465bb"),
                            VerticalOptions = LayoutOptions.Center,
                            HorizontalOptions = LayoutOptions.End
                        };
                        grid.Add(lblCheck, 1, 0);
                    }

                    fila.Content = grid;

                    var tapGesture = new TapGestureRecognizer();
                    var metricaCapturada = metrica; // captura para closure
                    tapGesture.Tapped += (s, e) => OnMetricaSeleccionada(metricaCapturada);
                    fila.GestureRecognizers.Add(tapGesture);

                    StackMetricas.Children.Add(fila);
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "MetricaRankingPickerPopup", "PoblarMetricas");
            }
        }

        private async void OnMetricaSeleccionada(RankingPoliticosViewModel.MetricaOpcion metrica)
        {
            if (_isClosing) return;
            _isClosing = true;
            try
            {
                MetricaSelected?.Invoke(this, metrica);
                await CloseAsync();
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "MetricaRankingPickerPopup", "OnMetricaSeleccionada");
            }
        }

        private async void OnCancelTapped(object sender, EventArgs e)
        {
            if (_isClosing) return;
            _isClosing = true;
            try
            {
                await CloseAsync();
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "MetricaRankingPickerPopup", "OnCancelTapped");
            }
        }
    }
}
