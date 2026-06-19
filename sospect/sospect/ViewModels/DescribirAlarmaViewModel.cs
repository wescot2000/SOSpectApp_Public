// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.Messaging;
using Newtonsoft.Json;
using sospect.Messages;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Storage;
using sospect.Helpers;
using sospect.Interfaces;
using sospect.Models;
using sospect.Services;
using sospect.Views;
using sospect.Views.Popups;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace sospect.ViewModels
{
    public class DescribirAlarmaViewModel : BaseViewModel
    {
        private IPopupService _popupService;
        private bool _isInitialized = false;

        private bool _flagRedConfianza;
        public bool flag_red_confianza
        {
            get => _flagRedConfianza;
            set => SetValue(ref _flagRedConfianza, value);
        }

        private string _textoFlagRedConfianza;
        public string TextoFlagRedConfianza
        {
            get => _textoFlagRedConfianza;
            set => SetValue(ref _textoFlagRedConfianza, value);
        }

        public long? AlarmaId { get; set; }

        // SOLUCI�N: Constructor seguro sin llamadas que puedan interferir con la navegaci�n
        public DescribirAlarmaViewModel(long? alarmaId = null)
        {
            try
            {
                this.AlarmaId = alarmaId;

                // Restaurar comportamiento de Xamarin: cargar alarma si viene un ID
                if (alarmaId != null)
                {
                    Task.Run(async () =>
                    {
                        await ObtenerAlarma(alarmaId);
                    });
                }

                System.Diagnostics.Debug.WriteLine($"DescribirAlarmaViewModel: Constructor completado con alarmaId={alarmaId}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DescribirAlarmaViewModel: Error en constructor: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "DescribirAlarmaViewModel", "Constructor");
            }
        }

        // SOLUCI�N: Inicializaci�n lazy para evitar problemas durante startup
        private async Task EnsureInitializedAsync()
        {
            if (_isInitialized)
                return;

            try
            {
                // Ya no necesitamos obtener IPopupService - usamos ShowPopupAsync directamente

                // Cargar datos si hay un AlarmaId espec�fico
                if (AlarmaId != null)
                {
                    await ObtenerAlarma(AlarmaId);
                }

                _isInitialized = true;
                System.Diagnostics.Debug.WriteLine("DescribirAlarmaViewModel: Inicializaci�n diferida completada");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DescribirAlarmaViewModel: Error en inicializaci�n diferida: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "DescribirAlarmaViewModel", "InicializarDatosAsync");
            }
        }

        public ICommand RefreshCommand => new Command(async () =>
        {
            await EnsureInitializedAsync();

            if (AlarmaId != null)
            {
                await ObtenerAlarma(AlarmaId);
            }
            else
            {
                await ObtenerAlarmas();
            }
        });

        bool IsNavigating = false;

        private async Task ObtenerAlarma(long? alarmaId)
        {
            var LabelOK = TranslateExtension.Translate("LabelOK");
            var MensajeError = TranslateExtension.Translate("MensajeError");
            try
            {
                IsRunning = true;
                List<AlarmaCercana> response = await ApiService.ObtenerAlarma(alarmaId);
                IsRunning = false;

                // CORRECCIÓN: Verificar que response no sea null antes de llamar .Any()
                if (response != null && response.Any())
                {
                    foreach (var alarma in response)
                    {
                        alarma.ActualizarUbicacionTerritorial();
                        alarma.CalcularCredibilidad();
                    }

                    ListadoAlarmas = new ObservableCollection<AlarmaCercana>(response);
                    Preferences.Set("alarma_id", null);
                    EmptyState = false;
                }
                else
                {
                    EmptyState = true;
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "DescribirAlarmaViewModel", "OnFinalizePublicacionAlarmaCommand");
                // SOLUCI�N: Validar que MainPage est� disponible antes de usarlo
                if (App.Current?.MainPage != null)
                {
                    await ModernAlerts.ShowError(MensajeError, ex.Message);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"DescribirAlarmaViewModel: Error sin MainPage disponible: {ex.Message}");
                }
            }
        }

        // SOLUCIÓN: Variables separadas para diferentes tipos de operaciones
        private bool _isNavigatingToDetails = false;
        private bool _isNavigatingToHistorial = false;
        private bool _isShowingPopup = false;
        private bool _isConfirmingAlarm = false;

        // CORRECCIÓN: Método para obtener Navigation del tab actual en TabbedPage
        private INavigation GetCurrentNavigation()
        {
            try
            {
                if (App.Current?.MainPage is Microsoft.Maui.Controls.TabbedPage tabbedPage)
                {
                    // Obtener la página actual del TabbedPage
                    var currentPage = tabbedPage.CurrentPage;
                    System.Diagnostics.Debug.WriteLine($"DescribirAlarmaViewModel: CurrentPage tipo: {currentPage?.GetType().Name}");

                    // Si es NavigationPage, devolver su Navigation
                    if (currentPage is NavigationPage navPage)
                    {
                        System.Diagnostics.Debug.WriteLine($"DescribirAlarmaViewModel: Navigation encontrado desde NavigationPage");
                        return navPage.Navigation;
                    }

                    // Si la página actual tiene Navigation, devolverlo
                    if (currentPage?.Navigation != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"DescribirAlarmaViewModel: Navigation encontrado desde CurrentPage");
                        return currentPage.Navigation;
                    }
                }

                // Fallback: usar App.Current.MainPage.Navigation si está disponible
                if (App.Current?.MainPage?.Navigation != null)
                {
                    System.Diagnostics.Debug.WriteLine($"DescribirAlarmaViewModel: Navigation encontrado desde MainPage (fallback)");
                    return App.Current.MainPage.Navigation;
                }

                System.Diagnostics.Debug.WriteLine($"DescribirAlarmaViewModel: Navigation NO encontrado");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DescribirAlarmaViewModel: Error obteniendo Navigation: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "DescribirAlarmaViewModel", "GetSafeNavigation");
                return null;
            }
        }

        public ICommand DescribirAlarmaCommand => new Command<AlarmaCercana>(OnDescribirAlarmaCommand);
        public ICommand VerDetallesAlarmaCommand => new Command<AlarmaCercana>(OnVerDetallesAlarmaCommand);
        public ICommand ConfirmarAlarmaCommand => new Command<AlarmaCercana>(OnConfirmarAlarmaCommand);
        public ICommand VerAlarmaEnMapaCommand => new Command<AlarmaCercana>(OnVerAlarmaEnMapaCommand);
        public ICommand CerrarAlarmaCommand => new Command<AlarmaCercana>(OnCerrarAlarmaCommand);
        public ICommand AtenderAlarmaCommand => new Command<AlarmaCercana>(OnAtenderAlarmaCommand);
        public ICommand HistorialCommand => new Command(async () => await GoToHistorialPage());

        private async Task GoToHistorialPage()
        {
            await EnsureInitializedAsync();

            var LabelOK = await TranslateExtension.TranslateAsync("LabelOK");
            var ErrorText = await TranslateExtension.TranslateAsync("LabelError");

            if (_isNavigatingToHistorial)
            {
                System.Diagnostics.Debug.WriteLine("DescribirAlarmaViewModel: GoToHistorialPage bloqueado - navegación en progreso");
                return;
            }

            _isNavigatingToHistorial = true;
            System.Diagnostics.Debug.WriteLine("DescribirAlarmaViewModel: GoToHistorialPage iniciado");

            try
            {
                // CORRECCIÓN: Usar GetCurrentNavigation
                INavigation navigation = GetCurrentNavigation();

                if (navigation != null)
                {
                    await navigation.PushAsync(new HistorialPage());
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("DescribirAlarmaViewModel: Navigation no disponible para Historial");
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "DescribirAlarmaViewModel", "GoToHistorialPage");

                if (App.Current?.MainPage != null)
                {
                    await ModernAlerts.ShowError(ErrorText, ex.Message);
                }
            }
            finally
            {
                _isNavigatingToHistorial = false;
                System.Diagnostics.Debug.WriteLine("DescribirAlarmaViewModel: GoToHistorialPage completado - flag reseteado");
            }
        }

        private async void OnVerAlarmaEnMapaCommand(AlarmaCercana alarma)
        {
            if (_isShowingPopup || alarma == null)
            {
                System.Diagnostics.Debug.WriteLine("DescribirAlarmaViewModel: OnVerAlarmaEnMapaCommand bloqueado");
                return;
            }

            _isShowingPopup = true;
            System.Diagnostics.Debug.WriteLine($"DescribirAlarmaViewModel: OnVerAlarmaEnMapaCommand iniciado para alarma {alarma.alarma_id}");

            try
            {
                await EnsureInitializedAsync();

                // SOLUCI�N: Usar CommunityToolkit.Maui directamente como en Xamarin
                if (App.Current?.MainPage != null)
                {
                    var popup = new Views.Popups.VerMapaPopup(alarma);
                    await App.Current.MainPage.ShowPopupAsync(popup);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("DescribirAlarmaViewModel: MainPage no disponible para mostrar popup");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DescribirAlarmaViewModel: Error en OnVerAlarmaEnMapaCommand: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "DescribirAlarmaViewModel", "OnVerAlarmaEnMapaCommand");

                // Fallback: navegar a p�gina normal si el popup falla
                INavigation navigation = GetCurrentNavigation();
                if (navigation != null)
                {
                    await navigation.PushAsync(new DetalleDescripcionAlarmaPage(alarma));
                }
            }
            finally
            {
                _isShowingPopup = false;
                System.Diagnostics.Debug.WriteLine("DescribirAlarmaViewModel: OnVerAlarmaEnMapaCommand completado - flag reseteado");
            }
        }

        private async void OnConfirmarAlarmaCommand(AlarmaCercana obj)
        {
            if (_isConfirmingAlarm || obj == null)
            {
                System.Diagnostics.Debug.WriteLine("DescribirAlarmaViewModel: OnConfirmarAlarmaCommand bloqueado");
                return;
            }

            _isConfirmingAlarm = true;
            System.Diagnostics.Debug.WriteLine($"DescribirAlarmaViewModel: OnConfirmarAlarmaCommand iniciado para alarma {obj.alarma_id}");

            try
            {
                await EnsureInitializedAsync();

                var RecibeAlarma = await TranslateExtension.TranslateAsync("RecibeAlarma");
                var LabelConfirmo = await TranslateExtension.TranslateAsync("LabelConfirmo");
                var LabelNoSeguro = await TranslateExtension.TranslateAsync("LabelNoSeguro");
                var LabelEsFalsa = await TranslateExtension.TranslateAsync("LabelEsFalsa");
                var LabelConfirmar = await TranslateExtension.TranslateAsync("LabelConfirmar");
                var LabelInformacion = await TranslateExtension.TranslateAsync("LabelInformacion");
                var AdvertenciaCalificacion = await TranslateExtension.TranslateAsync("AdvertenciaCalificacion");
                var MensajeError = await TranslateExtension.TranslateAsync("MensajeError");

                if (App.Current?.MainPage == null)
                {
                    System.Diagnostics.Debug.WriteLine("DescribirAlarmaViewModel: MainPage no disponible");
                    return;
                }

                var resultado = await ModernAlerts.ShowThreeOptions(
                    RecibeAlarma,
                    "",
                    LabelConfirmo,
                    LabelNoSeguro,
                    LabelEsFalsa
                );

                CalificarAlarma calificacionAlarma = new CalificarAlarma()
                {
                    AlarmaId = obj.alarma_id,
                    PUserIdThirdparty = App.persona?.user_id_thirdparty
                };

                ResponseMessage responseMessage = null;
                bool respuesta;

                switch (resultado)
                {
                    case ThreeOptionResult.Option1: // Confirmo
                        calificacionAlarma.VeracidadAlarma = true;
                        respuesta = await ModernAlerts.ShowConfirmation(LabelConfirmar, AdvertenciaCalificacion);
                        if (respuesta)
                        {
                            responseMessage = await ApiService.CalificarAlarma(calificacionAlarma);
                        }
                        break;

                    case ThreeOptionResult.Option2: // No estoy seguro
                                                    // No hacer nada
                        break;

                    case ThreeOptionResult.Option3: // Es falsa
                        calificacionAlarma.VeracidadAlarma = false;
                        respuesta = await ModernAlerts.ShowConfirmation(LabelConfirmar, AdvertenciaCalificacion);
                        if (respuesta)
                        {
                            IsRunning = true;
                            responseMessage = await ApiService.CalificarAlarma(calificacionAlarma);
                            IsRunning = false;
                        }
                        break;

                    case ThreeOptionResult.Cancelled:
                        break;
                }

                if (responseMessage != null)
                {
                    var mensajeSalida = "";
                    try
                    {
                        mensajeSalida = responseMessage.Message == null ? null :
                            await TranslateExtension.TranslateAsync(responseMessage.Message.Replace(" ", ""));
                    }
                    catch (Exception)
                    {
                        mensajeSalida = responseMessage.Message;
                    }

                    if (App.Current?.MainPage != null)
                    {
                        await ModernAlerts.ShowInfo(LabelInformacion, mensajeSalida);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DescribirAlarmaViewModel: Error en OnConfirmarAlarmaCommand: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "DescribirAlarmaViewModel", "OnConfirmarAlarmaCommand");
            }
            finally
            {
                _isConfirmingAlarm = false;
                System.Diagnostics.Debug.WriteLine("DescribirAlarmaViewModel: OnConfirmarAlarmaCommand completado - flag reseteado");
            }
        }

        public string AlertaReportadaPorMi(bool flag_propietario_alarma)
        {
            var LblAlertaReportadaPorMi = TranslateExtension.Translate("LblAlertaReportadaPorMi");
            return flag_propietario_alarma ? LblAlertaReportadaPorMi : "";
        }

        private async void OnVerDetallesAlarmaCommand(AlarmaCercana alarmaCercana)
        {
            if (_isNavigatingToDetails || alarmaCercana == null)
            {
                Console.WriteLine($"[DescribirVM-NAV] Bloqueado: isNavigating={_isNavigatingToDetails}, alarma={alarmaCercana?.alarma_id}");
                return;
            }

            _isNavigatingToDetails = true;

            // LOG DIAGNÓSTICO: valores que determinan la navegación
            Console.WriteLine($"[DescribirVM-NAV] ===== INICIO tap alarma =====");
            Console.WriteLine($"[DescribirVM-NAV] alarma_id={alarmaCercana.alarma_id}");
            Console.WriteLine($"[DescribirVM-NAV] TieneVotacionActiva={alarmaCercana.TieneVotacionActiva}");
            Console.WriteLine($"[DescribirVM-NAV] flag_propietario_alarma={alarmaCercana.flag_propietario_alarma}");
            Console.WriteLine($"[DescribirVM-NAV] UsuarioYaVoto={alarmaCercana.UsuarioYaVoto}");
            Console.WriteLine($"[DescribirVM-NAV] is_advertising={alarmaCercana.is_advertising}");
            Console.WriteLine($"[DescribirVM-NAV] AlarmasProponenteCierre.Count={App.AlarmasProponenteCierre.Count}");
            Console.WriteLine($"[DescribirVM-NAV] esProponente={App.AlarmasProponenteCierre.Contains(alarmaCercana.alarma_id)}");

            try
            {
                await EnsureInitializedAsync();

                INavigation navigation = GetCurrentNavigation();
                Console.WriteLine($"[DescribirVM-NAV] navigation={navigation?.GetType().Name ?? "NULL"}");

                if (navigation != null)
                {
                    if (alarmaCercana.is_advertising)
                    {
                        Console.WriteLine($"[DescribirVM-NAV] → DetallePromocionVistaPage");
                        await navigation.PushAsync(new DetallePromocionVistaPage(alarmaCercana));
                    }
                    else if (alarmaCercana.TieneVotacionActiva)
                    {
                        // VOTACIÓN DE CIERRE COMUNITARIO:
                        // - Propietario de la alarma → VerHistorialAlarmaPage (solo lectura, no puede votar)
                        // - Proponente del cierre    → VerHistorialAlarmaPage (ya solicitó el cierre)
                        // - Otros usuarios           → CierreEncuestaPage (para votar; ya bloquea si ya votó)
                        bool esProponente = App.AlarmasProponenteCierre.Contains(alarmaCercana.alarma_id);

                        // AlarmasProponenteCierre es in-memory: se pierde al reiniciar la app.
                        // Si no está en cache local, consultar la API para confirmar si este usuario
                        // fue quien propuso el cierre (evita enviarlo a CierreEncuestaPage).
                        if (!esProponente)
                        {
                            try
                            {
                                Console.WriteLine($"[DescribirVM-NAV] Verificando proponente via API: alarma_id={alarmaCercana.alarma_id}, App.persona.persona_id={App.persona?.persona_id}");
                                var resp = await ApiService.ObtenerSolicitudCierreActiva(
                                    alarmaCercana.alarma_id, App.persona.user_id_thirdparty);
                                Console.WriteLine($"[DescribirVM-NAV] API resp: IsSuccess={resp?.IsSuccess}, DataNull={resp?.Data == null}");
                                if (resp?.IsSuccess == true && resp.Data != null)
                                {
                                    var solicitud = JsonConvert.DeserializeObject<SolicitudCierreResponse>(resp.Data.ToString());
                                    Console.WriteLine($"[DescribirVM-NAV] solicitud: es_proponente={solicitud?.es_proponente}");
                                    if (solicitud?.es_proponente == true)
                                    {
                                        esProponente = true;
                                        App.AlarmasProponenteCierre.Add(alarmaCercana.alarma_id);
                                        Console.WriteLine($"[DescribirVM-NAV] Proponente confirmado por API");
                                    }
                                }
                            }
                            catch (Exception exApi)
                            {
                                Console.WriteLine($"[DescribirVM-NAV] Error verificando proponente via API: {exApi.Message}");
                            }
                        }

                        bool vaAHistorial = esProponente;
                        Console.WriteLine($"[DescribirVM-NAV] TieneVotacionActiva=true → vaAHistorial={vaAHistorial} (esProponente={esProponente})");
                        if (vaAHistorial)
                        {
                            Console.WriteLine($"[DescribirVM-NAV] → VerHistorialAlarmaPage");
                            await navigation.PushAsync(new VerHistorialAlarmaPage(alarmaCercana));
                        }
                        else
                        {
                            Console.WriteLine($"[DescribirVM-NAV] → CierreEncuestaPage");
                            await navigation.PushAsync(new CierreEncuestaPage(alarmaCercana));
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[DescribirVM-NAV] → DetalleDescripcionAlarmaPage (flujo normal)");
                        await navigation.PushAsync(new DetalleDescripcionAlarmaPage(alarmaCercana));
                    }
                }
                else
                {
                    Console.WriteLine($"[DescribirVM-NAV] ERROR: Navigation es null");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DescribirVM-NAV] EXCEPCION: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "DescribirAlarmaViewModel", "OnVerDetallesAlarmaCommand");
            }
            finally
            {
                _isNavigatingToDetails = false;
                Console.WriteLine($"[DescribirVM-NAV] ===== FIN tap alarma =====");
            }
        }

        private async void OnDescribirAlarmaCommand(AlarmaCercana alarmaCercana)
        {
            if (_isNavigatingToDetails || alarmaCercana == null)
            {
                System.Diagnostics.Debug.WriteLine("DescribirAlarmaViewModel: OnDescribirAlarmaCommand bloqueado");
                return;
            }

            _isNavigatingToDetails = true;
            System.Diagnostics.Debug.WriteLine($"DescribirAlarmaViewModel: OnDescribirAlarmaCommand iniciado para alarma {alarmaCercana.alarma_id}");

            try
            {
                await EnsureInitializedAsync();

                // CORRECCIÓN: Usar GetCurrentNavigation
                INavigation navigation = GetCurrentNavigation();

                if (navigation != null)
                {
                    await navigation.PushAsync(new DescribirAlarmaPage(alarmaCercana));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DescribirAlarmaViewModel: Error en OnDescribirAlarmaCommand: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "DescribirAlarmaViewModel", "OnDescribirAlarmaCommand");

                // Fallback: usar navegaci�n normal
                INavigation navigation = GetCurrentNavigation();
                if (navigation != null)
                {
                    await navigation.PushAsync(new DetalleDescripcionAlarmaPage(alarmaCercana));
                }
            }
            finally
            {
                _isNavigatingToDetails = false;
                System.Diagnostics.Debug.WriteLine("DescribirAlarmaViewModel: OnDescribirAlarmaCommand completado - flag reseteado");
            }
        }

        private async void OnCerrarAlarmaCommand(AlarmaCercana alarmaCercana)
        {
            if (_isShowingPopup || alarmaCercana == null)
            {
                System.Diagnostics.Debug.WriteLine("DescribirAlarmaViewModel: OnCerrarAlarmaCommand bloqueado");
                return;
            }

            _isShowingPopup = true;
            System.Diagnostics.Debug.WriteLine($"DescribirAlarmaViewModel: OnCerrarAlarmaCommand iniciado para alarma {alarmaCercana.alarma_id}");

            try
            {
                await EnsureInitializedAsync();

                if (App.Current?.MainPage != null)
                {
                    var popup = new Views.Popups.CerrarAlarmaPopUp(alarmaCercana);
                    await App.Current.MainPage.ShowPopupAsync(popup);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DescribirAlarmaViewModel: Error en OnCerrarAlarmaCommand: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "DescribirAlarmaViewModel", "OnCerrarAlarmaCommand");
            }
            finally
            {
                _isShowingPopup = false;
                System.Diagnostics.Debug.WriteLine("DescribirAlarmaViewModel: OnCerrarAlarmaCommand completado - flag reseteado");
            }
        }

        private async void OnAtenderAlarmaCommand(AlarmaCercana alarmaCercana)
        {
            if (_isShowingPopup || alarmaCercana == null)
            {
                System.Diagnostics.Debug.WriteLine("DescribirAlarmaViewModel: OnAtenderAlarmaCommand bloqueado");
                return;
            }

            _isShowingPopup = true;
            System.Diagnostics.Debug.WriteLine($"DescribirAlarmaViewModel: OnAtenderAlarmaCommand iniciado para alarma {alarmaCercana.alarma_id}");

            try
            {
                await EnsureInitializedAsync();

                if (App.Current?.MainPage != null)
                {
                    var popup = new Views.Popups.AtenderAlarmaPopup(alarmaCercana);
                    await App.Current.MainPage.ShowPopupAsync(popup);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DescribirAlarmaViewModel: Error en OnAtenderAlarmaCommand: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "DescribirAlarmaViewModel", "OnAtenderAlarmaCommand");
            }
            finally
            {
                _isShowingPopup = false;
                System.Diagnostics.Debug.WriteLine("DescribirAlarmaViewModel: OnAtenderAlarmaCommand completado - flag reseteado");
            }
        }

        internal async Task ObtenerAlarmas()
        {
            try
            {
                await EnsureInitializedAsync();

                var LabelOK = await TranslateExtension.TranslateAsync("LabelOK");
                var LabelInformacion = await TranslateExtension.TranslateAsync("LabelInformacion");
                var LblHabilitaGPSReintenta = await TranslateExtension.TranslateAsync("LblHabilitaGPSReintenta");
                var MensajeError = await TranslateExtension.TranslateAsync("MensajeError");
                var LabelAlarmaDeRedConfianza = await TranslateExtension.TranslateAsync("LabelAlarmaDeRedConfianza");

                IsRunning = true;

                if (App.ubicacionActual != null)
                {
                    try
                    {
                        // NUEVO: Filtrado client-side desde caché (diseño Twitter/X)
                        System.Diagnostics.Debug.WriteLine($"[DescribirAlarma] ===== INICIANDO FILTRADO CLIENT-SIDE =====");
                        System.Diagnostics.Debug.WriteLine($"[DescribirAlarma] Tab activa: {TabActiva}");

                        // Cache diferenciado por pestaña (04-02-2026):
                        // "ParaTi"    → Cache B (AlarmasCacheadasParaTi), lazy load si está vacío
                        // "Siguiendo" → Cache A (AlarmasCacheadas)
                        List<AlarmaCercana>? todasLasAlarmas;

                        if (TabActiva == "ParaTi")
                        {
                            // 21022026: Eliminado el lazy load de Feed B. Ya no se llama RefrescarFeedParaTi()
                            // directamente desde aquí porque viola la regla de secuenciación (Feed A primero).
                            // Feed B se carga en el arranque desde cerrada (App.EsPrimerArranque),
                            // al tocar el botón Refresh, o al hacer pull-to-refresh en esta pestaña.
                            // Si el cache está vacío (primera instalación), se muestra lista vacía hasta
                            // que RefrescarAmbosFeeds del arranque complete en background.
                            todasLasAlarmas = App.AlarmasCacheadasParaTi;
                            if (todasLasAlarmas == null || todasLasAlarmas.Count == 0)
                            {
                                System.Diagnostics.Debug.WriteLine("[DescribirAlarma] Cache 'Para ti' vacío — esperando RefrescarAmbosFeeds del arranque o pull-to-refresh del usuario.");
                            }
                        }
                        else
                        {
                            todasLasAlarmas = App.AlarmasCacheadas;
                        }

                        System.Diagnostics.Debug.WriteLine($"[DescribirAlarma] Alarmas disponibles en caché ({TabActiva}): {todasLasAlarmas?.Count ?? 0}");

                        if (todasLasAlarmas == null || todasLasAlarmas.Count == 0)
                        {
                            System.Diagnostics.Debug.WriteLine("[DescribirAlarma] No hay alarmas en caché, mostrando lista vacía");
                            ListadoAlarmas = new ObservableCollection<AlarmaCercana>();
                            IsRunning = false;
                            return;
                        }

                        // DEBUG: Mostrar muestra de alarmas antes de filtrar
                        if (todasLasAlarmas.Count > 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"[DescribirAlarma] Muestra de alarmas (primeras 3):");
                            for (int i = 0; i < Math.Min(3, todasLasAlarmas.Count); i++)
                            {
                                var a = todasLasAlarmas[i];
                                System.Diagnostics.Debug.WriteLine($"  [{i}] ID={a.alarma_id}, Distancia={a.distancia_en_metros}m, Estado={a.estado_alarma}, Radio={a.radio_alarmas_mts_actual}m");
                            }
                        }

                        // SIMPLIFICADO: Filtrar según pestaña activa usando FLAGS de la API
                        // Los flags vienen pre-calculados desde las vistas SQL (vw_alarmas_para_ti o vw_notificacion_alarmas)
                        List<AlarmaCercana> alarmasFiltradas;

                        if (TabActiva == "ParaTi")
                        {
                            // "Para ti": Mostrar TODAS las alarmas + Filtro de tipos
                            // La vista vw_alarmas_para_ti ya retorna el conjunto correcto:
                            // 1. Alarmas de seguridad personal (flag_seguridad_personal = true)
                            // 2. Alarmas virales/relevantes ordenadas por ranking_relevancia
                            System.Diagnostics.Debug.WriteLine($"[DescribirAlarma] Aplicando filtro 'Para ti' - Mostrando TODAS las alarmas desde API");

                            alarmasFiltradas = todasLasAlarmas.ToList();

                            // NUEVO: Aplicar filtro de tipos de alarma (diseño Twitter/X)
                            var alarmasAntesDelFiltro = alarmasFiltradas.Count;
                            alarmasFiltradas = Helpers.FiltroAlarmasHelper.FiltrarPorTipo(alarmasFiltradas);
                            System.Diagnostics.Debug.WriteLine($"[DescribirAlarma] Filtro de tipos aplicado: {alarmasAntesDelFiltro} → {alarmasFiltradas.Count} alarmas");

                            // Reglas 25 y 26: diversidad de tipos y variación por sesión en virales
                            alarmasFiltradas = Helpers.RankingDiversidadHelper.AplicarDiversidad(alarmasFiltradas);

                            var seguridadPersonal = alarmasFiltradas.Count(a => a.flag_seguridad_personal);
                            var virales = alarmasFiltradas.Count(a => !a.flag_seguridad_personal);

                            System.Diagnostics.Debug.WriteLine($"[DescribirAlarma] ===== RESULTADO FILTRADO 'PARA TI' =====");
                            System.Diagnostics.Debug.WriteLine($"[DescribirAlarma] Total: {alarmasFiltradas.Count} alarmas (Seguridad personal: {seguridadPersonal}, Virales: {virales})");
                        }
                        else
                        {
                            // "Siguiendo/En tu radio": Filtrar por flag_visible_siguiendo
                            // Este flag viene desde vw_notificacion_alarmas e indica:
                            // - Alarmas de seguridad personal (cercanas, protegidos, propias, zonas vigilancia)
                            // - Y (activas O cerradas en últimos 90 minutos)
                            System.Diagnostics.Debug.WriteLine($"[DescribirAlarma] Aplicando filtro 'Siguiendo' - flag_visible_siguiendo=true");

                            alarmasFiltradas = todasLasAlarmas
                                .Where(a => a.flag_visible_siguiendo)
                                .OrderByDescending(a => a.fecha_alarma ?? DateTime.MinValue)
                                .ToList();

                            System.Diagnostics.Debug.WriteLine($"[DescribirAlarma] ===== RESULTADO FILTRADO 'SIGUIENDO' =====");
                            System.Diagnostics.Debug.WriteLine($"[DescribirAlarma] Total alarmas: {alarmasFiltradas.Count} (flag_visible_siguiendo=true)");
                        }

                        // OPTIMIZACIÓN: Procesar alarmas EN BACKGROUND antes de asignar a UI
                        System.Diagnostics.Debug.WriteLine($"[DescribirAlarma] Procesando {alarmasFiltradas.Count} alarmas en background...");

                        await Task.Run(() =>
                        {
                            // OPTIMIZADO: Calcular credibilidad y ubicación territorial en background
                            // Logs de TERRITORIO eliminados para mejorar rendimiento (reducción ~1,000 líneas de log)
                            bool hayAlarmaRedConfianza = false;

                            foreach (var alarma in alarmasFiltradas)
                            {
                                alarma.ActualizarUbicacionTerritorial();
                                alarma.CalcularCredibilidad();

                                if (alarma.flag_red_confianza && !hayAlarmaRedConfianza)
                                {
                                    hayAlarmaRedConfianza = true;
                                }
                            }

                            // OPTIMIZADO: Solo una llamada a MainThread en lugar de una por cada alarma con flag
                            if (hayAlarmaRedConfianza)
                            {
                                MainThread.BeginInvokeOnMainThread(() =>
                                {
                                    TextoFlagRedConfianza = LabelAlarmaDeRedConfianza;
                                });
                            }
                        });

                        // 15-04-2026: Diff/merge para preservar scroll position.
                        // En vez de recrear ObservableCollection (que resetea scroll al tope),
                        // sincronizamos: remover los que ya no están, actualizar existentes, insertar nuevos.
                        System.Diagnostics.Debug.WriteLine($"[DescribirAlarma] Sincronizando ObservableCollection (diff/merge) en Main Thread...");
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            SincronizarListadoAlarmas(alarmasFiltradas);
                        });
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error: {ex.Message}");
                        CrashlyticsHelper.LogError(ex, "DescribirAlarmaViewModel", "ObtenerAlarmas-Inner");

                        if (App.Current?.MainPage != null)
                        {
                            await ModernAlerts.ShowWarning(LabelInformacion, MensajeError);
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("DescribirAlarmaViewModel: App.ubicacionActual es null");
                    IsRunning = false;
                }

                EmptyState = ListadoAlarmas == null || !ListadoAlarmas.Any();
                IsRunning = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DescribirAlarmaViewModel: Error en ObtenerAlarmas: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "DescribirAlarmaViewModel", "ObtenerAlarmas");
                IsRunning = false;
            }
        }

        private ObservableCollection<AlarmaCercana> _ListadoAlarmas;
        public ObservableCollection<AlarmaCercana> ListadoAlarmas
        {
            get => this._ListadoAlarmas;
            set
            {
                this.SetValue(ref this._ListadoAlarmas, value);
                // Actualizar EmptyState automáticamente cuando cambia ListadoAlarmas
                EmptyState = _ListadoAlarmas == null || !_ListadoAlarmas.Any();
            }
        }

        /// <summary>
        /// 15-04-2026: Sincroniza ListadoAlarmas con la nueva lista sin recrear la colección.
        /// Preserva la instancia de ObservableCollection (y por tanto la posición de scroll del CollectionView).
        /// Algoritmo: (1) remover items que ya no están, (2) actualizar propiedades de existentes,
        /// (3) insertar nuevos en posición correcta, (4) reordenar si es necesario.
        /// DEBE ejecutarse en Main Thread.
        /// </summary>
        private void SincronizarListadoAlarmas(List<AlarmaCercana> alarmasFiltradas)
        {
            // Caso 1: Primera carga o lista anterior vacía — crear nueva colección (no hay scroll que preservar)
            if (_ListadoAlarmas == null || _ListadoAlarmas.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine($"[DiffMerge] Primera carga — creando nueva ObservableCollection con {alarmasFiltradas.Count} items");
                ListadoAlarmas = new ObservableCollection<AlarmaCercana>(alarmasFiltradas);
                return;
            }

            // Caso 2: Nueva lista vacía — limpiar
            if (alarmasFiltradas == null || alarmasFiltradas.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[DiffMerge] Nueva lista vacía — limpiando colección");
                _ListadoAlarmas.Clear();
                EmptyState = true;
                return;
            }

            // Caso 3: Diff/merge por alarma_id
            var nuevosIds = new HashSet<long>(alarmasFiltradas.Select(a => a.alarma_id));
            int removidos = 0, actualizados = 0, insertados = 0, movidos = 0;

            // Paso 1: Remover items que ya no están en la nueva lista (recorrer de atrás hacia adelante)
            for (int i = _ListadoAlarmas.Count - 1; i >= 0; i--)
            {
                if (!nuevosIds.Contains(_ListadoAlarmas[i].alarma_id))
                {
                    _ListadoAlarmas.RemoveAt(i);
                    removidos++;
                }
            }

            // Paso 2: Crear índice de posiciones actuales
            var posicionActual = new Dictionary<long, int>();
            for (int i = 0; i < _ListadoAlarmas.Count; i++)
                posicionActual[_ListadoAlarmas[i].alarma_id] = i;

            // Paso 3: Recorrer nueva lista en orden — actualizar existentes o insertar nuevos
            for (int i = 0; i < alarmasFiltradas.Count; i++)
            {
                var nueva = alarmasFiltradas[i];

                if (posicionActual.TryGetValue(nueva.alarma_id, out int idxActual))
                {
                    // Existe: actualizar propiedades volátiles (las que cambian entre refreshes)
                    var existente = _ListadoAlarmas[idxActual];
                    ActualizarPropiedadesVolatiles(existente, nueva);
                    actualizados++;

                    // Verificar si necesita moverse a otra posición
                    if (idxActual != i)
                    {
                        // Solo mover si el item está fuera de posición respecto al orden deseado
                        if (idxActual < _ListadoAlarmas.Count)
                        {
                            _ListadoAlarmas.Move(idxActual, Math.Min(i, _ListadoAlarmas.Count - 1));
                            movidos++;
                            // Recalcular índice después del move
                            posicionActual.Clear();
                            for (int j = 0; j < _ListadoAlarmas.Count; j++)
                                posicionActual[_ListadoAlarmas[j].alarma_id] = j;
                        }
                    }
                }
                else
                {
                    // No existe: insertar en la posición correcta
                    int insertIdx = Math.Min(i, _ListadoAlarmas.Count);
                    _ListadoAlarmas.Insert(insertIdx, nueva);
                    insertados++;
                    // Recalcular índice después del insert
                    posicionActual.Clear();
                    for (int j = 0; j < _ListadoAlarmas.Count; j++)
                        posicionActual[_ListadoAlarmas[j].alarma_id] = j;
                }
            }

            EmptyState = _ListadoAlarmas.Count == 0;
            System.Diagnostics.Debug.WriteLine($"[DiffMerge] Resultado: {removidos} removidos, {actualizados} actualizados, {insertados} insertados, {movidos} movidos. Total: {_ListadoAlarmas.Count}");
        }

        /// <summary>
        /// Actualiza las propiedades que pueden cambiar entre refreshes de API.
        /// Solo modifica propiedades con INPC (SetProperty) para que la UI refresque automáticamente,
        /// y propiedades simples que no requieren notificación de cambio inmediata.
        /// </summary>
        private static void ActualizarPropiedadesVolatiles(AlarmaCercana existente, AlarmaCercana nueva)
        {
            // Propiedades con INPC (SetProperty) — la UI se actualiza automáticamente
            existente.cantidad_likes = nueva.cantidad_likes;
            existente.usuario_dio_like = nueva.usuario_dio_like;
            existente.cantidad_reenvios = nueva.cantidad_reenvios;
            existente.usuario_reenvio = nueva.usuario_reenvio;
            existente.cantidad_verdaderos = nueva.cantidad_verdaderos;
            existente.cantidad_falsos = nueva.cantidad_falsos;
            existente.usuario_voto_verdadero = nueva.usuario_voto_verdadero;
            existente.usuario_voto_falso = nueva.usuario_voto_falso;
            existente.usuario_anonimizado = nueva.usuario_anonimizado;
            existente.CategoriaAlarmaId = nueva.CategoriaAlarmaId;

            // Propiedades simples (sin INPC individual, pero cambian entre refreshes)
            existente.estado_alarma = nueva.estado_alarma;
            existente.EsAlarmaActiva = nueva.EsAlarmaActiva;
            existente.cantidad_interacciones = nueva.cantidad_interacciones;
            existente.cantidad_videos = nueva.cantidad_videos;
            existente.cantidad_fotos = nueva.cantidad_fotos;
            existente.flag_alarma_siendo_atendida = nueva.flag_alarma_siendo_atendida;
            existente.cantidad_agentes_atendiendo = nueva.cantidad_agentes_atendiendo;
            existente.flag_red_confianza = nueva.flag_red_confianza;
            existente.calificacion_alarma = nueva.calificacion_alarma;
            existente.usuariocalificoalarma = nueva.usuariocalificoalarma;
            existente.calificacionalarmausuario = nueva.calificacionalarmausuario;
            existente.Flag_hubo_captura = nueva.Flag_hubo_captura;
            existente.distancia_en_metros = nueva.distancia_en_metros;
            existente.flag_visible_mapa = nueva.flag_visible_mapa;
            existente.flag_visible_siguiendo = nueva.flag_visible_siguiendo;
            existente.flag_seguridad_personal = nueva.flag_seguridad_personal;
            existente.ranking_relevancia = nueva.ranking_relevancia;
            existente.TieneVotacionActiva = nueva.TieneVotacionActiva;
            existente.UsuarioYaVoto = nueva.UsuarioYaVoto;

            // Fotos: actualizar solo si cambiaron (INPC en el setter notifica TieneFotos/CantidadFotos)
            if (nueva.Fotos?.Count != existente.Fotos?.Count)
                existente.Fotos = nueva.Fotos;
        }

        // ============================================
        // NUEVO: PROPIEDADES PARA DISEÑO TWITTER/X
        // ============================================

        // Pestaña activa: "ParaTi" o "Siguiendo"
        private string _tabActiva = "ParaTi"; // Default: ParaTi (carga en ~1s vs 15-42s de Siguiendo)
        public string TabActiva
        {
            get => _tabActiva;
            set
            {
                SetValue(ref _tabActiva, value);
                // Actualizar colores de pestañas cuando cambia la activa
                OnPropertyChanged(nameof(TabParaTiBackgroundColor));
                OnPropertyChanged(nameof(TabParaTiTextColor));
                OnPropertyChanged(nameof(TabSiguiendoBackgroundColor));
                OnPropertyChanged(nameof(TabSiguiendoTextColor));
                OnPropertyChanged(nameof(EsSiguiendoTab));
                OnPropertyChanged(nameof(EsParaTiTab));
            }
        }

        // Colores para pestaña "Para ti"
        public Color TabParaTiBackgroundColor =>
            TabActiva == "ParaTi" ? Colors.White : Colors.LightGray;

        public Color TabParaTiTextColor =>
            TabActiva == "ParaTi" ? Colors.Blue : Colors.Gray;

        // Colores para pestaña "Siguiendo"
        public Color TabSiguiendoBackgroundColor =>
            TabActiva == "Siguiendo" ? Colors.White : Colors.LightGray;

        public Color TabSiguiendoTextColor =>
            TabActiva == "Siguiendo" ? Colors.Blue : Colors.Gray;

        // IsRefreshing para RefreshView (pull-to-refresh)
        private bool _isRefreshing;
        public bool IsRefreshing
        {
            get => _isRefreshing;
            set => SetValue(ref _isRefreshing, value);
        }

        // ============================================
        // NUEVO: COMANDOS PARA PESTAÑAS Y REFRESH
        // ============================================

        public ICommand CambiarAParaTiCommand => new Command(async () =>
        {
            if (TabActiva != "ParaTi")
            {
                TabActiva = "ParaTi";
                await ObtenerAlarmas(); // Recargar con filtro "Para ti"
            }
        });

        public ICommand CambiarASiguiendoCommand => new Command(async () =>
        {
            if (TabActiva != "Siguiendo")
            {
                TabActiva = "Siguiendo";
                await ObtenerAlarmas(); // Recargar con filtro "Siguiendo"
            }
        });

        public bool EsSiguiendoTab => TabActiva == "Siguiendo";
        public bool EsParaTiTab => TabActiva == "ParaTi";

        // ============================================
        // VISIBILIDAD DE BARRAS (scroll estilo X/Twitter)
        // ============================================

        private bool _tabHeaderVisible = true;
        public bool TabHeaderVisible
        {
            get => _tabHeaderVisible;
            set => SetValue(ref _tabHeaderVisible, value);
        }

        private bool _barsVisible = true;
        public bool BarsVisible
        {
            get => _barsVisible;
            set
            {
                if (_barsVisible == value) return;
                SetValue(ref _barsVisible, value);
                MessagingCenter.Send<object, bool>(this, "TabBarVisible", value);
            }
        }

        private double _lastScrollOffset = 0;

        public void OnFeedScrolled(double verticalOffset)
        {
            double delta = verticalOffset - _lastScrollOffset;
            _lastScrollOffset = verticalOffset;

            // Umbral de 10dp para evitar micro-movimientos
            if (delta > 10 && BarsVisible)
            {
                BarsVisible = false;
                TabHeaderVisible = false;
            }
            else if (delta < -10 && !BarsVisible)
            {
                BarsVisible = true;
                TabHeaderVisible = true;
            }
        }

        public ICommand AbrirConfiguracionParaTiCommand => new Command(async () =>
        {
            var nav = GetCurrentNavigation();
            if (nav != null)
                await nav.PushAsync(new sospect.Views.ConfiguracionParaTiPage());
        });

        public ICommand RefreshPullToRefreshCommand => new Command(async () =>
        {
            IsRefreshing = true;
            try
            {
                bool success;

                // Pull-to-refresh: acción explícita del usuario — se desactiva temporalmente el guard
                // para que los métodos de refresco puedan ejecutarse aunque DescribirPageActiva sea true.
                App.DescribirPageActiva = false;
                try
                {
                    if (TabActiva == "ParaTi")
                    {
                        // 21022026: REGLA — Pull-to-refresh "Para ti" ejecuta Feed A primero (completo), luego Feed B.
                        // Garantiza que "Siguiendo" y el mapa se refrescan antes del feed de contenido.
                        // RegenerarSeed ya ocurre dentro de App.RefrescarFeedParaTi() al asignar el caché exitosamente.
                        System.Diagnostics.Debug.WriteLine("[DescribirAlarma] Pull-to-refresh 'Para ti': RefrescarAmbosFeeds (A primero, luego B)...");
                        var (feedAOk, feedBOk) = await App.RefrescarAmbosFeeds(ejecutarFeedBSiFeedAFalla: false);
                        success = feedBOk;
                        System.Diagnostics.Debug.WriteLine($"[DescribirAlarma] Pull-to-refresh 'Para ti' completado: A={feedAOk}, B={feedBOk}");
                    }
                    else
                    {
                        // "Siguiendo": solo Feed A — sin cambios (correcto según diseño)
                        success = await App.RefrescarAlarmasDesdeAPI();
                        if (success)
                            System.Diagnostics.Debug.WriteLine("[DescribirAlarma] Pull-to-refresh 'Siguiendo': Cache A reemplazado");
                    }
                }
                finally
                {
                    // Restablecer el guard: volvemos a suspender refrescos automáticos
                    App.DescribirPageActiva = true;
                }

                if (success)
                {
                    await ObtenerAlarmas();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[DescribirAlarma] Pull-to-refresh: No se pudo refrescar desde API");
                }
            }
            finally
            {
                IsRefreshing = false;
            }
        });

        // ─── COMANDOS BARRA DE ACCIONES (M8 - 23-02-2026) ─────────────────────────

        public ICommand DarLikeCommand => new Command<AlarmaCercana>(async (alarma) =>
        {
            if (alarma == null || App.persona?.user_id_thirdparty == null) return;
            try
            {
                if (alarma.usuario_dio_like)
                {
                    var result = await ApiService.QuitarLikeAlarma(App.persona.user_id_thirdparty, alarma.alarma_id);
                    if (result?.IsSuccess == true)
                    {
                        alarma.usuario_dio_like = false;
                        alarma.cantidad_likes = Math.Max(0, alarma.cantidad_likes - 1);
                    }
                }
                else
                {
                    var result = await ApiService.DarLikeAlarma(App.persona.user_id_thirdparty, alarma.alarma_id);
                    if (result?.IsSuccess == true)
                    {
                        alarma.usuario_dio_like = true;
                        alarma.cantidad_likes++;
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "DescribirAlarmaViewModel", "DarLikeCommand");
            }
        });

        public ICommand ReenviarCommand => new Command<AlarmaCercana>(async (alarma) =>
        {
            if (alarma == null || App.persona?.user_id_thirdparty == null) return;
            try
            {
                if (alarma.usuario_reenvio)
                {
                    var result = await ApiService.QuitarReenvioAlarma(App.persona.user_id_thirdparty, alarma.alarma_id);
                    if (result?.IsSuccess == true)
                    {
                        alarma.usuario_reenvio = false;
                        alarma.cantidad_reenvios = Math.Max(0, alarma.cantidad_reenvios - 1);
                    }
                }
                else
                {
                    var label = TranslateExtension.Translate("ReenviarAlarma");
                    var confirm = await ModernAlerts.ShowConfirmation(label, TranslateExtension.Translate("SeguirConfirmacion") ?? label);
                    if (confirm)
                    {
                        var result = await ApiService.ReenviarAlarma(App.persona.user_id_thirdparty, alarma.alarma_id);
                        if (result?.IsSuccess == true)
                        {
                            alarma.usuario_reenvio = true;
                            alarma.cantidad_reenvios++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "DescribirAlarmaViewModel", "ReenviarCommand");
            }
        });

        public ICommand CompartirCommand => new Command<AlarmaCercana>(async (alarma) =>
        {
            if (alarma == null) return;
            try
            {
                string url = $"{AppConfiguration.WebHost}/a/{alarma.alarma_id}";
                string texto = $"{alarma.descripciontipoalarma} - {alarma.Descripcionalarma}\n{url}";
                await Share.RequestAsync(new ShareTextRequest
                {
                    Text = texto,
                    Title = TranslateExtension.Translate("CompartirAlarma") ?? "Compartir"
                });
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "DescribirAlarmaViewModel", "CompartirCommand");
            }
        });

        public ICommand MarcarVerdaderaCommand => new Command<AlarmaCercana>(async (alarma) =>
        {
            if (alarma == null || App.persona?.user_id_thirdparty == null) return;
            // No permitir votar si ya votó en esta sesión
            if (alarma.usuario_voto_verdadero || alarma.usuario_voto_falso) return;
            try
            {
                bool confirmar = await ModernAlerts.ShowConfirmation(
                    TranslateExtension.Translate("MarcarVerdadera") ?? "Verdadera",
                    TranslateExtension.Translate("AdvertenciaCalificacion") ?? "¿Confirmas que esta alarma es verdadera?");
                if (confirmar)
                {
                    var calificacion = new CalificarAlarma { AlarmaId = alarma.alarma_id, PUserIdThirdparty = App.persona.user_id_thirdparty, VeracidadAlarma = true };
                    var response = await ApiService.CalificarAlarma(calificacion);
                    if (response?.IsSuccess == true)
                    {
                        alarma.cantidad_verdaderos++;
                        alarma.usuario_voto_verdadero = true;
                        // Notificar a DetalleDescripcionAlarmaPage para coherencia
                        WeakReferenceMessenger.Default.Send(new VotoAlarmaMessage(alarma.alarma_id, true, alarma.cantidad_verdaderos, alarma.cantidad_falsos));
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "DescribirAlarmaViewModel", "MarcarVerdaderaCommand");
            }
        });

        public ICommand MarcarFalsaCommand => new Command<AlarmaCercana>(async (alarma) =>
        {
            if (alarma == null || App.persona?.user_id_thirdparty == null) return;
            // No permitir votar si ya votó en esta sesión
            if (alarma.usuario_voto_verdadero || alarma.usuario_voto_falso) return;
            try
            {
                bool confirmar = await ModernAlerts.ShowConfirmation(
                    TranslateExtension.Translate("MarcarFalsa") ?? "Falsa",
                    TranslateExtension.Translate("AdvertenciaCalificacion") ?? "¿Confirmas que esta alarma es falsa?");
                if (confirmar)
                {
                    var calificacion = new CalificarAlarma { AlarmaId = alarma.alarma_id, PUserIdThirdparty = App.persona.user_id_thirdparty, VeracidadAlarma = false };
                    var response = await ApiService.CalificarAlarma(calificacion);
                    if (response?.IsSuccess == true)
                    {
                        alarma.cantidad_falsos++;
                        alarma.usuario_voto_falso = true;
                        // Notificar a DetalleDescripcionAlarmaPage para coherencia
                        WeakReferenceMessenger.Default.Send(new VotoAlarmaMessage(alarma.alarma_id, false, alarma.cantidad_verdaderos, alarma.cantidad_falsos));
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "DescribirAlarmaViewModel", "MarcarFalsaCommand");
            }
        });

    }
}

