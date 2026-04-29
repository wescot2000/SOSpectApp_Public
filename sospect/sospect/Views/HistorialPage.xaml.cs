using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using sospect.Models;
using sospect.ViewModels;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Controls;
using sospect.Helpers;

namespace sospect.Views
{
    public partial class HistorialPage : ContentPage
    {
        private HistorialAlarmaViewModel _viewModel;
        private bool _isInitialized = false;
        long? alarmaIdLocal;

        // Constructor por defecto sin inicialización inmediata del ViewModel
        public HistorialPage()
        {
            InitializeComponent();
            System.Diagnostics.Debug.WriteLine("HistorialPage: Constructor por defecto iniciado sin ViewModel inmediato");
        }

        // Constructor con parámetro de alarma específica
        public HistorialPage(long? alarmaId = null)
        {
            alarmaIdLocal = alarmaId;
            InitializeComponent();
            System.Diagnostics.Debug.WriteLine($"HistorialPage: Constructor con alarmaId={alarmaId} iniciado");

            // FIX 2026-04-17: No crear el ViewModel aquí cuando viene alarmaId.
            // El Task.Run del ViewModel constructor no tiene await, por lo que en iOS
            // la página se renderiza vacía antes de que ObtenerAlarma() termine.
            // La carga se hace en OnAppearing() para garantizar que la UI esté lista.
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (!_isInitialized)
            {
                await InitializeViewModelAsync();
            }
            else
            {
                if (alarmaIdLocal == null)
                {
                    if (string.IsNullOrEmpty(Preferences.Get("alarma_id", "")) || Preferences.Get("alarma_id", "") == "0")
                    {
                        if (BindingContext is HistorialAlarmaViewModel viewmodel)
                        {
                            await viewmodel.ObtenerAlarmas();
                        }
                    }
                    else
                    {
                        return;
                    }
                }
            }
        }

        private async Task InitializeViewModelAsync()
        {
            try
            {
                _isInitialized = true;
                System.Diagnostics.Debug.WriteLine($"HistorialPage: Inicializando ViewModel (alarmaIdLocal={alarmaIdLocal})");

                if (alarmaIdLocal.HasValue)
                {
                    // FIX 2026-04-17 (rev2): Pasar alarmaIdLocal al constructor para que el Task.Run
                    // interno tome el título correcto ("Alarma recibida") y llame ObtenerAlarma.
                    // No llamar ObtenerAlarma adicional desde aquí — el ViewModel ya lo hace internamente.
                    // El race condition de iOS ya no aplica porque el push ocurre antes de OnAppearing.
                    _viewModel = new HistorialAlarmaViewModel(alarmaIdLocal);
                    BindingContext = _viewModel;
                }
                else
                {
                    _viewModel = new HistorialAlarmaViewModel(null);
                    BindingContext = _viewModel;

                    if (string.IsNullOrEmpty(Preferences.Get("alarma_id", "")) || Preferences.Get("alarma_id", "") == "0")
                    {
                        await _viewModel.ObtenerAlarmas();
                    }
                }

                System.Diagnostics.Debug.WriteLine("HistorialPage: ViewModel inicializado exitosamente");
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "HistorialPage", "InitializeViewModelAsync");
                System.Diagnostics.Debug.WriteLine($"HistorialPage: Error inicializando ViewModel: {ex.Message}");
                _viewModel = new HistorialAlarmaViewModel(null);
                BindingContext = _viewModel;
            }
        }

        protected override void OnDisappearing()
        {
            try
            {
                base.OnDisappearing();
                System.Diagnostics.Debug.WriteLine("HistorialPage: OnDisappearing llamado");

                if (BindingContext is HistorialAlarmaViewModel viewModel)
                {
                    viewModel.IsRunning = false;
                }
            }
            catch (ObjectDisposedException ex)
            {
                CrashlyticsHelper.LogError(ex, "HistorialPage", "OnDisappearing");
                System.Diagnostics.Debug.WriteLine($"HistorialPage: ObjectDisposedException manejada: {ex.Message}");
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "HistorialPage", "OnDisappearing");
                System.Diagnostics.Debug.WriteLine($"HistorialPage: Error en OnDisappearing: {ex.Message}");
            }
        }

        public async Task RefreshDataAsync()
        {
            if (BindingContext is HistorialAlarmaViewModel viewModel)
            {
                await viewModel.ObtenerAlarmas();
            }
        }
    }
}