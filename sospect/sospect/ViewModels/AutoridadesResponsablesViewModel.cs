// ViewModels/AutoridadesResponsablesViewModel.cs
// Creado: 2026-03-05
// Módulo: Autoridades Políticas Responsables
// Propósito: Carga y expone la lista de autoridades responsables para el territorio de una alarma.

using Microsoft.Maui.Controls;
using sospect.DTOs;
using sospect.Helpers;
using sospect.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace sospect.ViewModels
{
    public class AutoridadesResponsablesViewModel : BaseViewModel
    {
        private readonly long _alarmaId;

        private ObservableCollection<AutoridadResponsableDto> _autoridades;
        public ObservableCollection<AutoridadResponsableDto> Autoridades
        {
            get => _autoridades;
            set => SetProperty(ref _autoridades, value);
        }

        private string _tituloAlarma;
        public string TituloAlarma
        {
            get => _tituloAlarma;
            set => SetProperty(ref _tituloAlarma, value);
        }

        // ── Comandos de contacto ──────────────────────────────────────────────

        /// <summary>
        /// Navega a la página de reporte de desempeño del político seleccionado.
        /// </summary>
        public ICommand VerReporteCommand => new Command<AutoridadResponsableDto>(async (autoridad) =>
        {
            if (autoridad == null) return;
            INavigation navigation = GetCurrentNavigation();
            if (navigation != null)
                await navigation.PushAsync(new sospect.Views.ReporteAutoridadPage(autoridad));
        });

        public ICommand AbrirEmailCommand => new Command<AutoridadResponsableDto>(async (autoridad) =>
        {
            if (autoridad?.TieneEmail == true)
            {
                try { await Launcher.OpenAsync($"mailto:{autoridad.Email}"); }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"AbrirEmailCommand: {ex.Message}");
                    CrashlyticsHelper.LogError(ex, "AutoridadesResponsablesViewModel", "AbrirEmailCommand");
                }
            }
        });

        public ICommand AbrirTelefonoCommand => new Command<AutoridadResponsableDto>(async (autoridad) =>
        {
            if (autoridad?.TieneTelefono == true)
            {
                try { await Launcher.OpenAsync($"tel:{autoridad.Telefono}"); }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"AbrirTelefonoCommand: {ex.Message}");
                    CrashlyticsHelper.LogError(ex, "AutoridadesResponsablesViewModel", "AbrirTelefonoCommand");
                }
            }
        });

        public ICommand AbrirSitioWebCommand => new Command<AutoridadResponsableDto>(async (autoridad) =>
        {
            if (autoridad?.TieneSitioWeb == true)
            {
                try { await Launcher.OpenAsync(autoridad.SitioWeb!); }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"AbrirSitioWebCommand: {ex.Message}");
                    CrashlyticsHelper.LogError(ex, "AutoridadesResponsablesViewModel", "AbrirSitioWebCommand");
                }
            }
        });

        public ICommand AbrirTwitterCommand => new Command<AutoridadResponsableDto>(async (autoridad) =>
        {
            if (autoridad?.TieneTwitter == true)
            {
                try
                {
                    var handle = autoridad.Twitter!.TrimStart('@');
                    await Launcher.OpenAsync($"https://twitter.com/{handle}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"AbrirTwitterCommand: {ex.Message}");
                    CrashlyticsHelper.LogError(ex, "AutoridadesResponsablesViewModel", "AbrirTwitterCommand");
                }
            }
        });

        // ── Constructor ───────────────────────────────────────────────────────

        public AutoridadesResponsablesViewModel(long alarmaId, string? tituloAlarma = null)
        {
            _alarmaId = alarmaId;
            TituloAlarma = tituloAlarma ?? string.Empty;
            Autoridades = new ObservableCollection<AutoridadResponsableDto>();
            _ = CargarAutoridadesAsync();
        }

        // ── Helpers de navegación ─────────────────────────────────────────────

        private INavigation GetCurrentNavigation()
        {
            try
            {
                var currentPage = Application.Current?.MainPage;
                if (currentPage is NavigationPage navPage)
                    return navPage.Navigation;
                if (currentPage is TabbedPage tabbedPage)
                {
                    if (tabbedPage.CurrentPage is NavigationPage tabNavPage)
                        return tabNavPage.Navigation;
                    return tabbedPage.Navigation;
                }
                return currentPage?.Navigation;
            }
            catch
            {
                return null;
            }
        }

        // ── Carga de datos ────────────────────────────────────────────────────

        private async Task CargarAutoridadesAsync()
        {
            IsRunning = true;
            EmptyState = false;

            try
            {
                var resultado = await ApiService.ObtenerAutoridadesPorAlarma(_alarmaId);

                if (resultado != null && resultado.Count > 0)
                {
                    Autoridades = new ObservableCollection<AutoridadResponsableDto>(resultado);
                    EmptyState = false;
                }
                else
                {
                    EmptyState = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"AutoridadesResponsablesViewModel.CargarAutoridadesAsync: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "AutoridadesResponsablesViewModel", "CargarAutoridadesAsync");
                EmptyState = true;
            }
            finally
            {
                IsRunning = false;
            }
        }
    }
}
