using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;
using sospect.Helpers;
using sospect.Models;
using sospect.Services;
using sospect.Views;
using sospect.Views.Popups;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace sospect.ViewModels
{
    /// <summary>
    /// ViewModel de solo lectura del historial de una alarma.
    /// Se usa cuando la alarma está en proceso de votación de cierre:
    /// el usuario puede ver las descripciones pero no puede interactuar con la alarma.
    /// </summary>
    public class VerHistorialAlarmaViewModel : BaseViewModel
    {
        private readonly INavigation _navigation;

        // --- Alarma ---
        private AlarmaCercana _alarmaSeleccionada;
        public AlarmaCercana AlarmaSeleccionada
        {
            get => _alarmaSeleccionada;
            set => SetValue(ref _alarmaSeleccionada, value);
        }

        // --- Listado de descripciones ---
        private ObservableCollection<DetalleDescripcionAlarma> _listadoDescripcionesAlarmas;
        public ObservableCollection<DetalleDescripcionAlarma> ListadoDescripcionesAlarmas
        {
            get => _listadoDescripcionesAlarmas;
            set => SetValue(ref _listadoDescripcionesAlarmas, value);
        }

        private bool _emptyState;
        public bool EmptyState
        {
            get => _emptyState;
            set => SetValue(ref _emptyState, value);
        }

        // --- Commands (solo lectura) ---
        public ICommand VerAlarmaEnMapaCommand { get; }
        public ICommand VerAutoridadesResponsablesCommand { get; }
        public ICommand GoBackCommand { get; }
        public ICommand VerFotoCommand { get; }

        public VerHistorialAlarmaViewModel(AlarmaCercana alarma, INavigation navigation)
        {
            AlarmaSeleccionada = alarma;
            _navigation = navigation;

            VerAlarmaEnMapaCommand = new Command<AlarmaCercana>(async (a) =>
            {
                try
                {
                    var currentPage = GetCurrentPage();
                    if (currentPage != null)
                        await currentPage.ShowPopupAsync(new VerMapaPopup(a));
                }
                catch (Exception ex)
                {
                    CrashlyticsHelper.LogError(ex, "VerHistorialAlarmaViewModel", "VerAlarmaEnMapaCommand");
                }
            });

            VerAutoridadesResponsablesCommand = new Command<AlarmaCercana>(async (a) =>
            {
                try
                {
                    await _navigation.PushAsync(new AutoridadesResponsablesPage(a.alarma_id, a.descripciontipoalarma));
                }
                catch (Exception ex)
                {
                    CrashlyticsHelper.LogError(ex, "VerHistorialAlarmaViewModel", "VerAutoridadesResponsablesCommand");
                }
            });

            GoBackCommand = new Command(async () => await _navigation.PopAsync());

            VerFotoCommand = new Command<FotoDescripcionAlarma>(async (foto) =>
            {
                if (foto == null) return;
                try
                {
                    var currentPage = GetCurrentPage();
                    if (currentPage == null) return;

                    var descripcionConFoto = ListadoDescripcionesAlarmas?
                        .FirstOrDefault(d => d.Fotos?.Any(f => f.FotoId == foto.FotoId) == true);

                    if (descripcionConFoto?.Fotos?.Any() == true)
                    {
                        int idx = descripcionConFoto.Fotos.FindIndex(f => f.FotoId == foto.FotoId);
                        if (idx < 0) idx = 0;
                        string texto = await ConstruirTextoDescripcionAsync(descripcionConFoto);
                        var popup = new VerFotoPopup(descripcionConFoto.Fotos, idx, texto, descripcionConFoto.FechadescripcionLocal);
                        await currentPage.ShowPopupAsync(popup);
                    }
                    else
                    {
                        await currentPage.ShowPopupAsync(new VerFotoPopup(foto));
                    }
                }
                catch (Exception ex)
                {
                    CrashlyticsHelper.LogError(ex, "VerHistorialAlarmaViewModel", "VerFotoCommand");
                }
            });
        }

        public async Task CargarHistorial()
        {
            var LabelInformacion = await TranslateExtension.TranslateAsync("LabelInformacion");
            var MensajeError = await TranslateExtension.TranslateAsync("MensajeError");
            var LabelEscritoRedConfianza = await TranslateExtension.TranslateAsync("LabelEscritoRedConfianza");
            IsRunning = true;
            try
            {
                var response = await ApiService.ListarDescripcionesAlarma(AlarmaSeleccionada);

                if (response != null && response.Any())
                {
                    // Actualizar CategoriaAlarmaId para el botón Ver autoridades
                    var primeraFila = response.FirstOrDefault();
                    if (primeraFila?.CategoriaAlarmaId.HasValue == true)
                        AlarmaSeleccionada.CategoriaAlarmaId = primeraFila.CategoriaAlarmaId;

                    // Filtrar fila sintética (iddescripcion == 0)
                    var descripcionesReales = response.Where(d => d.Iddescripcion > 0).ToList();
                    foreach (var d in descripcionesReales)
                    {
                        d.FlagRedConfianza = d.flag_red_confianza;
                        d.TextoFlagRedConfianza = d.flag_red_confianza ? LabelEscritoRedConfianza : string.Empty;
                    }
                    ListadoDescripcionesAlarmas = new ObservableCollection<DetalleDescripcionAlarma>(descripcionesReales);
                    EmptyState = !descripcionesReales.Any();
                }
                else
                {
                    EmptyState = true;
                }
            }
            catch (Exception ex)
            {
                await ModernAlerts.ShowWarning(LabelInformacion, MensajeError);
                CrashlyticsHelper.LogError(ex, "VerHistorialAlarmaViewModel", "CargarHistorial");
            }
            finally
            {
                IsRunning = false;
            }
        }

        // ─── Helpers ──────────────────────────────────────────────────────────────

        private async Task<string> ConstruirTextoDescripcionAsync(DetalleDescripcionAlarma descripcion)
        {
            var partes = new List<string>();
            if (!string.IsNullOrWhiteSpace(descripcion.Descripcionalarma))
                partes.Add(descripcion.Descripcionalarma);
            if (!string.IsNullOrWhiteSpace(descripcion.Descripcionsospechoso))
            {
                var lbl = await TranslateExtension.TranslateAsync("LblSospechosoCorto");
                partes.Add($"{lbl} {descripcion.Descripcionsospechoso}");
            }
            if (!string.IsNullOrWhiteSpace(descripcion.Descripcionvehiculo))
            {
                var lbl = await TranslateExtension.TranslateAsync("LblVehiculoCorto");
                partes.Add($"{lbl} {descripcion.Descripcionvehiculo}");
            }
            if (!string.IsNullOrWhiteSpace(descripcion.Descripcionarmas))
            {
                var lbl = await TranslateExtension.TranslateAsync("LblArmasCorto");
                partes.Add($"{lbl} {descripcion.Descripcionarmas}");
            }
            return partes.Any() ? string.Join(" • ", partes) : "";
        }

        /// <summary>
        /// Obtiene la página actualmente visible (igual que en DetalleDescripcionAlarmaViewModel).
        /// </summary>
        private static Page GetCurrentPage()
        {
            var mainPage = Application.Current?.MainPage;
            Page currentPage = mainPage;
            while (currentPage != null)
            {
                if (currentPage is NavigationPage navPage && navPage.CurrentPage != null)
                {
                    currentPage = navPage.CurrentPage;
                }
                else if (currentPage is TabbedPage tabbedPage && tabbedPage.CurrentPage != null)
                {
                    currentPage = tabbedPage.CurrentPage;
                }
                else if (currentPage is FlyoutPage flyoutPage && flyoutPage.Detail != null)
                {
                    currentPage = flyoutPage.Detail;
                }
                else
                {
                    break;
                }
            }
            return currentPage;
        }
    }
}
