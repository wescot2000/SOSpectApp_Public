// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.Messaging;
using Newtonsoft.Json;
using sospect.Helpers;
using sospect.Interfaces;
using sospect.Messages;
using sospect.Models;
using sospect.Services;
using sospect.Views;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace sospect.ViewModels
{
    public class HistorialAlarmaViewModel : BaseViewModel
    {
        private readonly IPopupService _popupService;

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

        private string _tituloPagina;
        public string TituloPagina
        {
            get => _tituloPagina;
            set => SetValue(ref _tituloPagina, value);
        }

        private bool _isRefreshing;
        public bool IsRefreshing
        {
            get => _isRefreshing;
            set => SetValue(ref _isRefreshing, value);
        }

        public long? AlarmaId { get; set; }
        public HistorialAlarmaViewModel(long? alarmaId = null)
        {
            // CORRECCIÓN: Inicializar _popupService usando DependencyService
            _popupService = DependencyService.Get<IPopupService>();

            this.AlarmaId = alarmaId;

            // Establecer el título según si es alarma específica o historial general
            Task.Run(async () =>
            {
                if (alarmaId != null)
                {
                    // Alarma específica desde mensaje
                    TituloPagina = await TranslateExtension.TranslateAsync("LblAlarmaRecibida");
                    await ObtenerAlarma(alarmaId);
                }
                else
                {
                    // Historial general
                    TituloPagina = await TranslateExtension.TranslateAsync("LblTituloHistorialAlertas");
                    await ObtenerAlarmas();
                }
            });
        }

        public ICommand RefreshCommand => new Command(async () =>
        {
            IsRefreshing = true;
            try
            {
                if (AlarmaId != null)
                {
                    await ObtenerAlarma(AlarmaId);
                }
                else
                {
                    await ObtenerAlarmas();
                }
            }
            finally
            {
                IsRefreshing = false;
            }
        });

        bool IsNavigating = false;

        // FIX 2026-04-17: internal para que HistorialPage pueda hacer await explícito desde OnAppearing (fix iOS vacío)
        internal async Task ObtenerAlarma(long? alarmaId)
        {
            var LabelAlarmaDeRedConfianza = await TranslateExtension.TranslateAsync("LabelAlarmaDeRedConfianza");
            Console.WriteLine($"[HistorialVM] LabelAlarmaDeRedConfianza='{LabelAlarmaDeRedConfianza}', CultureInfo.CurrentUICulture={System.Globalization.CultureInfo.CurrentUICulture.Name}");
            var LabelOK = await TranslateExtension.TranslateAsync("LabelOK");
            var LabelError = await TranslateExtension.TranslateAsync("LabelError");
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
                        alarma.CalcularCredibilidad();

                        Console.WriteLine($"[HistorialVM] alarma_id={alarma.alarma_id}, flag_red_confianza={alarma.flag_red_confianza}, CategoriaId={alarma.CategoriaAlarmaId}");
                        if (alarma.flag_red_confianza)
                        {
                            TextoFlagRedConfianza = LabelAlarmaDeRedConfianza;
                            Console.WriteLine($"[HistorialVM] TextoFlagRedConfianza asignado='{TextoFlagRedConfianza}'");
                        }
                        else
                        {
                            TextoFlagRedConfianza = string.Empty;
                        }

                        // Si la alarma tiene votación activa, propagarlo al cache local para que
                        // el feed (Siguiendo/Para Ti) también redirija correctamente sin esperar refresh.
                        if (alarma.TieneVotacionActiva)
                        {
                            ActualizarVotacionEnCache(alarma.alarma_id, alarma.UsuarioYaVoto);
                        }
                    }

                    ListadoAlarmas = new ObservableCollection<AlarmaCercana>(response);
                    Preferences.Set("alarma_id", null);
                    EmptyState = false;

                    // AUTO-NAVEGACIÓN: Si HistorialPage fue abierta desde notificación (AlarmaId != null)
                    // y la alarma tiene votación activa, navegar automáticamente a la pantalla correcta.
                    if (AlarmaId.HasValue && response.Count == 1 && response[0].TieneVotacionActiva)
                    {
                        var alarmaVotacion = response[0];
                        bool esProponente = App.AlarmasProponenteCierre.Contains(alarmaVotacion.alarma_id);

                        // AlarmasProponenteCierre es in-memory: se pierde al reiniciar la app.
                        if (!alarmaVotacion.flag_propietario_alarma && !esProponente)
                        {
                            try
                            {
                                var resp = await ApiService.ObtenerSolicitudCierreActiva(
                                    alarmaVotacion.alarma_id, App.persona.user_id_thirdparty);
                                if (resp?.IsSuccess == true && resp.Data != null)
                                {
                                    var solicitud = JsonConvert.DeserializeObject<SolicitudCierreResponse>(resp.Data.ToString());
                                    Console.WriteLine($"[HistorialVM] Auto-nav API es_proponente={solicitud?.es_proponente}");
                                    if (solicitud?.es_proponente == true)
                                    {
                                        esProponente = true;
                                        App.AlarmasProponenteCierre.Add(alarmaVotacion.alarma_id);
                                        Console.WriteLine($"[HistorialVM] Auto-nav proponente confirmado por API");
                                    }
                                }
                            }
                            catch (Exception exApi)
                            {
                                Console.WriteLine($"[HistorialVM] Auto-nav: Error verificando proponente via API: {exApi.Message}");
                            }
                        }

                        bool vaAHistorial = esProponente;
                        Console.WriteLine($"[HistorialVM] Auto-nav: alarma_id={alarmaVotacion.alarma_id}, esProponente={esProponente}, vaAHistorial={vaAHistorial}");
                        MainThread.BeginInvokeOnMainThread(async () =>
                        {
                            try
                            {
                                INavigation navigation = GetCurrentNavigation();
                                if (navigation != null)
                                {
                                    if (vaAHistorial)
                                        await navigation.PushAsync(new VerHistorialAlarmaPage(alarmaVotacion));
                                    else
                                        await navigation.PushAsync(new CierreEncuestaPage(alarmaVotacion));
                                }
                            }
                            catch (Exception navEx)
                            {
                                Console.WriteLine($"[HistorialVM] Auto-nav ERROR: {navEx.Message}");
                            }
                        });
                    }
                }
                else
                {
                    EmptyState = true;
                }

            }
            catch (Exception ex)
            {
                await ModernAlerts.ShowError(LabelError, ex.Message);
                CrashlyticsHelper.LogError(ex, "HistorialAlarmaViewModel", "ObtenerAlarma");
            }
            
        }

        public ICommand DescribirAlarmaCommand => new Command<AlarmaCercana>(OnDescribirAlarmaCommand);
        public ICommand VerDetallesAlarmaCommand => new Command<AlarmaCercana>(OnVerDetallesAlarmaCommand);
        public ICommand ConfirmarAlarmaCommand => new Command<AlarmaCercana>(OnConfirmarAlarmaCommand);
        public ICommand VerAlarmaEnMapaCommand => new Command<AlarmaCercana>(OnVerAlarmaEnMapaCommand);
        public ICommand CerrarAlarmaCommand => new Command<AlarmaCercana>(OnCerrarAlarmaCommand);
        public ICommand AtenderAlarmaCommand => new Command<AlarmaCercana>(OnAtenderAlarmaCommand);
        public ICommand GoBackCommand => new Command(async () => await GoBack());

        private async Task GoBack()
        {
            var LabelOK = await TranslateExtension.TranslateAsync("LabelOK");
            var ErrorText = await TranslateExtension.TranslateAsync("LabelError");

            if (IsNavigating)
                return;

            IsNavigating = true;
            try
            {
                await App.Current.MainPage.Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await ModernAlerts.ShowError(ErrorText, ex.Message);
            }
            finally
            {
                IsNavigating = false;
            }
        }


        private async void OnVerAlarmaEnMapaCommand(AlarmaCercana alarma)
        {
            if (IsNavigating)
            {
                return;
            }
            IsNavigating = true;

            await _popupService.PushAsync(new Views.Popups.VerMapaPopup(alarma));

            IsNavigating = false;
        }

        private async void OnConfirmarAlarmaCommand(AlarmaCercana obj)
        {
            if (IsNavigating)
            {
                return;
            }
            IsNavigating = true;

            var RecibeAlarma = await TranslateExtension.TranslateAsync("RecibeAlarma");
            var LabelConfirmo = await TranslateExtension.TranslateAsync("LabelConfirmo");
            var LabelNoSeguro = await TranslateExtension.TranslateAsync("LabelNoSeguro");
            var LabelEsFalsa = await TranslateExtension.TranslateAsync("LabelEsFalsa");
            var LabelConfirmar = await TranslateExtension.TranslateAsync("LabelConfirmar");
            var LabelInformacion = await TranslateExtension.TranslateAsync("LabelInformacion");
            var AdvertenciaCalificacion = await TranslateExtension.TranslateAsync("AdvertenciaCalificacion");
            var MensajeError = await TranslateExtension.TranslateAsync("MensajeError");

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
                PUserIdThirdparty = App.persona.user_id_thirdparty
            };

            ResponseMessage? responseMessage = null;
            bool respuesta;

            try
            {
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
                                                    // No hacer nada, solo se cierra
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
                        // Usuario cerró sin elegir
                        break;
                }
            }
            catch (Exception ex)
            {
                await ModernAlerts.ShowWarning(LabelInformacion, MensajeError);
                CrashlyticsHelper.LogError(ex, "HistorialAlarmaViewModel", "OnConfirmarAlarmaCommand");
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

                await ModernAlerts.ShowInfo(LabelInformacion, mensajeSalida);
            }

            IsNavigating = false;
        }

        public string AlertaReportadaPorMi(bool flag_propietario_alarma)
        {
            var LblAlertaReportadaPorMi = TranslateExtension.Translate("LblAlertaReportadaPorMi");
            return flag_propietario_alarma ? LblAlertaReportadaPorMi : "";
        }


        private async void OnVerDetallesAlarmaCommand(AlarmaCercana alarmaCercana)
        {
            if (IsNavigating)
            {
                return;
            }
            IsNavigating = true;

            try
            {
                // CORRECCIÓN: Verificar que alarmaCercana no sea null
                if (alarmaCercana == null)
                {
                    Console.WriteLine("[HistorialVM-DIAG] alarmaCercana es null");
                    return;
                }

                Console.WriteLine($"[HistorialVM-DIAG] alarma_id={alarmaCercana.alarma_id}, CategoriaAlarmaId={alarmaCercana.CategoriaAlarmaId}, TieneVotacionActiva={alarmaCercana.TieneVotacionActiva}, is_advertising={alarmaCercana.is_advertising}");

                // CORRECCIÓN: Obtener Navigation del tab actual en TabbedPage
                INavigation navigation = GetCurrentNavigation();
                if (navigation == null)
                {
                    Console.WriteLine("[HistorialVM-DIAG] Navigation no disponible");
                    return;
                }

                // VOTACIÓN DE CIERRE COMUNITARIO:
                // - Propietario de la alarma → VerHistorialAlarmaPage (solo lectura, no puede votar)
                // - Proponente del cierre    → VerHistorialAlarmaPage (ya solicitó el cierre)
                // - Otros usuarios           → CierreEncuestaPage (para votar; ya bloquea si ya votó)
                if (alarmaCercana.TieneVotacionActiva)
                {
                    bool esProponente = App.AlarmasProponenteCierre.Contains(alarmaCercana.alarma_id);

                    // AlarmasProponenteCierre es in-memory: se pierde al reiniciar la app.
                    // Si no está en cache local y no es propietario, consultar la API.
                    if (!alarmaCercana.flag_propietario_alarma && !esProponente)
                    {
                        try
                        {
                            var resp = await ApiService.ObtenerSolicitudCierreActiva(
                                alarmaCercana.alarma_id, App.persona.user_id_thirdparty);
                            if (resp?.IsSuccess == true && resp.Data != null)
                            {
                                var solicitud = JsonConvert.DeserializeObject<SolicitudCierreResponse>(resp.Data.ToString());
                                Console.WriteLine($"[HistorialVM-DIAG] API es_proponente={solicitud?.es_proponente}");
                                if (solicitud?.es_proponente == true)
                                {
                                    esProponente = true;
                                    App.AlarmasProponenteCierre.Add(alarmaCercana.alarma_id);
                                    Console.WriteLine($"[HistorialVM-DIAG] Proponente confirmado por API");
                                }
                            }
                        }
                        catch (Exception exApi)
                        {
                            Console.WriteLine($"[HistorialVM-DIAG] Error verificando proponente via API: {exApi.Message}");
                        }
                    }

                    Console.WriteLine($"[HistorialVM-DIAG] TieneVotacionActiva=true → esProponente={esProponente}");
                    if (esProponente)
                    {
                        await navigation.PushAsync(new VerHistorialAlarmaPage(alarmaCercana));
                    }
                    else
                    {
                        await navigation.PushAsync(new CierreEncuestaPage(alarmaCercana));
                    }
                    return;
                }

                // 2026-04-11: Alarmas de tipoalarma_id=13 (Promoción local) → DetallePromocionVistaPage
                // Nota: CategoriaAlarmaId=6 (Publicidad) pero preferimos comparar tipoalarma_id=13 que es más específico.
                if (alarmaCercana.tipoalarma_id == 13)
                {
                    Console.WriteLine($"[HistorialVM-DIAG] tipoalarma_id=13 → DetallePromocionVistaPage");
                    await navigation.PushAsync(new DetallePromocionVistaPage(alarmaCercana));
                    return;
                }

                Console.WriteLine($"[HistorialVM-DIAG] → DetalleDescripcionAlarmaPage (cat={alarmaCercana.CategoriaAlarmaId})");
                await navigation.PushAsync(new DetalleDescripcionAlarmaPage(alarmaCercana));
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "HistorialAlarmaViewModel", "OnVerDetallesAlarmaCommand");
                System.Diagnostics.Debug.WriteLine($"Error navegando a DetalleDescripcionAlarmaPage: {ex.Message}");
            }
            finally
            {
                IsNavigating = false;
            }
        }

        // CORRECCIÓN: Helper method para obtener Navigation desde TabbedPage
        private INavigation GetCurrentNavigation()
        {
            try
            {
                if (App.Current?.MainPage is Microsoft.Maui.Controls.TabbedPage tabbedPage)
                {
                    // Obtener la página actual del TabbedPage
                    var currentPage = tabbedPage.CurrentPage;

                    // Si es NavigationPage, devolver su Navigation
                    if (currentPage is NavigationPage navPage)
                    {
                        return navPage.Navigation;
                    }

                    // Si la página actual tiene Navigation, devolverlo
                    if (currentPage?.Navigation != null)
                    {
                        return currentPage.Navigation;
                    }
                }

                // Fallback: usar App.Current.MainPage.Navigation si está disponible
                if (App.Current?.MainPage?.Navigation != null)
                {
                    return App.Current.MainPage.Navigation;
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HistorialAlarmaViewModel: Error obteniendo Navigation: {ex.Message}");
                return null;
            }
        }

        private async void OnDescribirAlarmaCommand(AlarmaCercana alarmaCercana)
        {
            if (IsNavigating)
            {
                return;
            }
            IsNavigating = true;

            try
            {
                // CORRECCIÓN: Verificar que alarmaCercana y Navigation no sean null
                if (alarmaCercana == null)
                {
                    System.Diagnostics.Debug.WriteLine("OnDescribirAlarmaCommand: alarmaCercana es null");
                    return;
                }

                if (App.Current?.MainPage?.Navigation != null)
                {
                    await App.Current.MainPage.Navigation.PushAsync(new DescribirAlarmaPage(alarmaCercana));
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("OnDescribirAlarmaCommand: Navigation es null");
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "HistorialAlarmaViewModel", "OnDescribirAlarmaCommand");
                System.Diagnostics.Debug.WriteLine($"Error navegando a DescribirAlarmaPage: {ex.Message}");
            }
            finally
            {
                IsNavigating = false;
            }
        }

        private async void OnCerrarAlarmaCommand(AlarmaCercana alarmaCercana)
        {
            if (IsNavigating)
            {
                return;
            }
            IsNavigating = true;

            await _popupService.PushAsync(new Views.Popups.CerrarAlarmaPopUp(alarmaCercana));

            IsNavigating = false;
        }

        private async void OnAtenderAlarmaCommand(AlarmaCercana alarmaCercana)
        {
            if (IsNavigating)
            {
                return;
            }
            IsNavigating = true;

            await _popupService.PushAsync(new Views.Popups.AtenderAlarmaPopup(alarmaCercana));
            IsNavigating = false;
        }

        // Propagar el estado de votación activa al cache local para que el feed
        // redirija correctamente sin necesidad de un refresh completo.
        private static void ActualizarVotacionEnCache(long alarmaId, bool usuarioYaVoto)
        {
            try
            {
                var alarmaA = App.AlarmasCacheadas?.FirstOrDefault(a => a.alarma_id == alarmaId);
                if (alarmaA != null)
                {
                    alarmaA.TieneVotacionActiva = true;
                    alarmaA.UsuarioYaVoto = usuarioYaVoto;
                    Console.WriteLine($"[HistorialVM] Cache A: alarma {alarmaId} TieneVotacionActiva=true, UsuarioYaVoto={usuarioYaVoto}");
                }

                var alarmaB = App.AlarmasCacheadasParaTi?.FirstOrDefault(a => a.alarma_id == alarmaId);
                if (alarmaB != null)
                {
                    alarmaB.TieneVotacionActiva = true;
                    alarmaB.UsuarioYaVoto = usuarioYaVoto;
                    Console.WriteLine($"[HistorialVM] Cache B: alarma {alarmaId} TieneVotacionActiva=true, UsuarioYaVoto={usuarioYaVoto}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HistorialVM] ActualizarVotacionEnCache ERROR: {ex.Message}");
            }
        }

        internal async Task ObtenerAlarmas()
        {
            var LabelOK = await TranslateExtension.TranslateAsync("LabelOK");
            var LabelInformacion = await TranslateExtension.TranslateAsync("LabelInformacion");
            var LblHabilitaGPSReintenta = await TranslateExtension.TranslateAsync("LblHabilitaGPSReintenta");
            var MensajeError = await TranslateExtension.TranslateAsync("MensajeError");

            IsRunning = true;

            if (App.ubicacionActual != null)
            {
                try
                {
                    App.ubicacionActual.PantallaOrigen = "DescribirAlarma";
                    ListadoAlarmas = new ObservableCollection<AlarmaCercana>(await ApiService.ActualizarHistorial(App.ubicacionActual));

                    foreach (var alarma in ListadoAlarmas)
                    {
                        alarma.CalcularCredibilidad();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error: {ex.Message}");
                    await ModernAlerts.ShowWarning(LabelInformacion, MensajeError);
                    CrashlyticsHelper.LogError(ex, "HistorialAlarmaViewModel", "ObtenerAlarmas");
                }
            }
            else
            {
                await ModernAlerts.ShowWarning(LabelInformacion, LblHabilitaGPSReintenta);
            }

            EmptyState = ListadoAlarmas == null || !ListadoAlarmas.Any();

            IsRunning = false;
        }


        private ObservableCollection<AlarmaCercana> _ListadoAlarmas;
        public ObservableCollection<AlarmaCercana> ListadoAlarmas
        {
            get => this._ListadoAlarmas;
            set => this.SetValue(ref this._ListadoAlarmas, value);
        }

        // ─── COMANDOS BARRA DE ACCIONES (mismos que DescribirAlarmaViewModel) ──────

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
                CrashlyticsHelper.LogError(ex, "HistorialAlarmaViewModel", "DarLikeCommand");
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
                CrashlyticsHelper.LogError(ex, "HistorialAlarmaViewModel", "ReenviarCommand");
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
                CrashlyticsHelper.LogError(ex, "HistorialAlarmaViewModel", "CompartirCommand");
            }
        });

        public ICommand MarcarVerdaderaCommand => new Command<AlarmaCercana>(async (alarma) =>
        {
            if (alarma == null || App.persona?.user_id_thirdparty == null) return;
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
                        WeakReferenceMessenger.Default.Send(new VotoAlarmaMessage(alarma.alarma_id, true, alarma.cantidad_verdaderos, alarma.cantidad_falsos));
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "HistorialAlarmaViewModel", "MarcarVerdaderaCommand");
            }
        });

        public ICommand MarcarFalsaCommand => new Command<AlarmaCercana>(async (alarma) =>
        {
            if (alarma == null || App.persona?.user_id_thirdparty == null) return;
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
                        WeakReferenceMessenger.Default.Send(new VotoAlarmaMessage(alarma.alarma_id, false, alarma.cantidad_verdaderos, alarma.cantidad_falsos));
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "HistorialAlarmaViewModel", "MarcarFalsaCommand");
            }
        });
    }
}



