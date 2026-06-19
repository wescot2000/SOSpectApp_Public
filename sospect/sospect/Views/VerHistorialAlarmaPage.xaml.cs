// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
using System.Linq;
using System.Threading.Tasks;
using sospect.Models;
using sospect.ViewModels;
using sospect.Views.Popups;
using sospect.Helpers;
using Microsoft.Maui.Controls;

namespace sospect.Views
{
    public partial class VerHistorialAlarmaPage : ContentPage
    {
        private readonly VerHistorialAlarmaViewModel _viewModel;

        public VerHistorialAlarmaPage(AlarmaCercana alarma)
        {
            InitializeComponent();
            _viewModel = new VerHistorialAlarmaViewModel(alarma, Navigation);
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CargarYDesplazar();
        }

        private async Task CargarYDesplazar()
        {
            try
            {
                await _viewModel.CargarHistorial();

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await Task.Delay(300);
                    if (_viewModel.ListadoDescripcionesAlarmas?.Any() == true)
                    {
                        var lastItem = _viewModel.ListadoDescripcionesAlarmas.LastOrDefault();
                        if (lastItem != null)
                            HistorialListView.ScrollTo(lastItem, ScrollToPosition.End, animated: true);
                    }
                });
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "VerHistorialAlarmaPage", "CargarYDesplazar");
                System.Diagnostics.Debug.WriteLine($"VerHistorialAlarmaPage CargarYDesplazar ERROR: {ex.Message}");
            }
        }

        // VerFotoCommand necesario para los TapGestureRecognizer de miniaturas
        // (se delega al ViewModel que expone el comando)
    }
}


