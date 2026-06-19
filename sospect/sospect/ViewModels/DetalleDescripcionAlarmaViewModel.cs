// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Newtonsoft.Json;
using sospect.Helpers;
using sospect.Interfaces;
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
    public class DetalleDescripcionAlarmaViewModel : BaseViewModel
    {
        private bool _cargandoDescripciones = false;
        public ICommand EditarDetalleAlarmaCommand => new Command<DetalleDescripcionAlarma>(OnEditarDetalleAlarmaCommand);
        public ICommand DescribirAlarmaCommand => new Command<AlarmaCercana>(OnDescribirAlarmaCommand);
        public ICommand CalificarPositivoAlarmaCommand => new Command<DetalleDescripcionAlarma>(OnDescribirPositivoAlarmaCommand);
        public ICommand CalificarNegativoAlarmaCommand => new Command<DetalleDescripcionAlarma>(OnDescribirNegativoAlarmaCommand);
        public ICommand VerAlarmaEnMapaCommand => new Command<AlarmaCercana>(OnVerAlarmaEnMapaCommand);
        public ICommand VerFotoCommand => new Command<FotoDescripcionAlarma>(OnVerFotoCommand);
        public ICommand NavegearAChatPublicidadCommand => new Command<AlarmaCercana>(OnNavegarAChatPublicidadCommand);

        // ─── AUTORIDADES RESPONSABLES (2026-03-05) ────────────────────────────────
        public ICommand VerAutoridadesResponsablesCommand => new Command<AlarmaCercana>(
            async (alarmaCercana) =>
            {
                INavigation navigation = GetCurrentNavigation();
                if (navigation != null)
                    await navigation.PushAsync(
                        new AutoridadesResponsablesPage(
                            alarmaCercana.alarma_id,
                            alarmaCercana.descripciontipoalarma));
            });

        public ICommand CerrarAlarmaCommand => new Command<AlarmaCercana>(
    async (alarmaCercana) =>
    {
        if (!PuedeCerrarAlarma)
        {
            var AccionNoPermitida = await TranslateExtension.TranslateAsync("AccionNoPermitida");
            var SoloCreadorCierraAlarma = await TranslateExtension.TranslateAsync("SoloCreadorCierraAlarma");

            await ModernAlerts.ShowWarning(AccionNoPermitida, SoloCreadorCierraAlarma);
            return;
        }
        OnCerrarAlarmaCommand(alarmaCercana);
    });

        public ICommand ConfirmarAlarmaCommand => new Command<AlarmaCercana>(
            async (alarmaCercana) =>
            {
                if (!PuedeConfirmarAlarma)
                {
                    var AccionNoPermitida = await TranslateExtension.TranslateAsync("AccionNoPermitida");
                    var SoloCreadorConfirmaAlarma = await TranslateExtension.TranslateAsync("SoloCreadorConfirmaAlarma");
                    var LabelOK = await TranslateExtension.TranslateAsync("LabelOK");

                    await ModernAlerts.ShowWarning(AccionNoPermitida, SoloCreadorConfirmaAlarma);
                    return;
                }
                OnConfirmarAlarmaCommand(alarmaCercana);
            });

        public ICommand AtenderAlarmaCommand => new Command<AlarmaCercana>(
            async (alarmaCercana) =>
            {
                var AccionNoPermitida = await TranslateExtension.TranslateAsync("AccionNoPermitida");
                var SoloAutoridadCompetente = await TranslateExtension.TranslateAsync("SoloAutoridadCompetente");
                var LabelOK = await TranslateExtension.TranslateAsync("LabelOK");

                if (!PuedeAtenderAlarma)
                {
                    await ModernAlerts.ShowWarning(AccionNoPermitida, SoloAutoridadCompetente);
                    return;
                }
                OnAtenderAlarmaCommand(alarmaCercana);
            });

        private bool _flagRedConfianza;
        public bool FlagRedConfianza
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

        private bool _isPopupOpen = false;
        public bool IsPopupOpen
        {
            get => _isPopupOpen;
            set => SetProperty(ref _isPopupOpen, value);
        }

        private AlarmaCercana _alarmaSeleccionada;
        public AlarmaCercana AlarmaSeleccionada
        {
            get => _alarmaSeleccionada;
            set => SetValue(ref _alarmaSeleccionada, value);
        }

        // ─── SEGUIR USUARIO (M9 - 23-02-2026) ───────────────────────────────────
        private bool _estaSiguiendo;
        public bool EstaSiguiendo
        {
            get => _estaSiguiendo;
            set
            {
                if (SetProperty(ref _estaSiguiendo, value))
                {
                    OnPropertyChanged(nameof(TextoBotonSeguir));
                    OnPropertyChanged(nameof(ColorBotonSeguir));
                }
            }
        }

        public string TextoBotonSeguir => EstaSiguiendo
            ? TranslateExtension.Translate("DejarDeSeguirUsuario")
            : TranslateExtension.Translate("SeguirUsuario");

        public Color ColorBotonSeguir => EstaSiguiendo ? Colors.Gray : Color.FromArgb("#2980B9");

        private ObservableCollection<DetalleDescripcionAlarma> _ListadoDescripcionesAlarmas;
        public ObservableCollection<DetalleDescripcionAlarma> ListadoDescripcionesAlarmas
        {
            get => this._ListadoDescripcionesAlarmas;
            set => this.SetValue(ref this._ListadoDescripcionesAlarmas, value);
        }

        private bool _isNavigating;
        public bool IsNavigating
        {
            get => _isNavigating;
            set => SetValue(ref _isNavigating, value);
        }

        private AlarmaCercana alarmaCercana;

        // CORRECCIÓN: Método para obtener Navigation del contexto actual
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
                System.Diagnostics.Debug.WriteLine($"DetalleDescripcionAlarmaViewModel: Error obteniendo Navigation: {ex.Message}");
                return null;
            }
        }

        public bool PuedeCerrarAlarma
        {
            get
            {
                if (!AlarmaSeleccionada.EsAlarmaActiva)
                    return false;

                var tipoCierre = AlarmaSeleccionada.tipo_cierre;

                if (tipoCierre == "sin_cierre")
                    return false;

                if (tipoCierre == "cierre_encuesta")
                    return true;

                return AlarmaSeleccionada.flag_propietario_alarma || AlarmaSeleccionada.flag_es_policia;
            }
        }

        public bool PuedeConfirmarAlarma =>
            AlarmaSeleccionada.EsAlarmaActiva && !AlarmaSeleccionada.usuariocalificoalarma && !AlarmaSeleccionada.flag_propietario_alarma;

        public bool PuedeAtenderAlarma =>
            AlarmaSeleccionada.EsAlarmaActiva && AlarmaSeleccionada.flag_es_policia;

        public DetalleDescripcionAlarmaViewModel(AlarmaCercana alarmaCercana)
        {
            this.alarmaCercana = alarmaCercana;
            AlarmaSeleccionada = alarmaCercana;
            ListadoDescripcionesAlarmas = new ObservableCollection<DetalleDescripcionAlarma>();
            _ = CargarListadoDetalleDescripcionesAlarma(alarmaCercana);

            _ = InitializeAsync();
            _ = CargarEstadoSeguimientoAsync(alarmaCercana);

            MessagingCenter.Subscribe<object>("valor", "RegistroActualizado", async (sender) =>
            {
                await CargarListadoDetalleDescripcionesAlarma(this.alarmaCercana);
            });
        }

        private async Task InitializeAsync()
        {
            var LabelEscritoRedConfianza = await TranslateExtension.TranslateAsync("LabelEscritoRedConfianza");

            FlagRedConfianza = alarmaCercana.flag_red_confianza;
            if (FlagRedConfianza)
            {
                TextoFlagRedConfianza = LabelEscritoRedConfianza;
            }
            else
            {
                TextoFlagRedConfianza = string.Empty;
            }
        }

        private async void OnEditarDetalleAlarmaCommand(DetalleDescripcionAlarma obj)
        {
            if (IsPopupOpen) return;
            IsPopupOpen = true;

            IsRunning = true;
            try
            {
                DescribirAlarma describirAlarmaActualizacion = new DescribirAlarma()
                {
                    alarma_id = obj.AlarmaId,
                    p_user_id_thirdparty = App.persona.user_id_thirdparty,
                    DescripcionAlarma = obj.Descripcionalarma,
                    DescripcionArmas = obj.Descripcionarmas,
                    DescripcionSospechoso = obj.Descripcionsospechoso,
                    DescripcionVehiculo = obj.Descripcionvehiculo
                };

                // CORRECCIÓN: Usar GetCurrentNavigation para obtener el contexto de navegación correcto
                INavigation navigation = GetCurrentNavigation();
                if (navigation != null)
                {
                    await navigation.PushAsync(new DescribirAlarmaPage(describirAlarmaActualizacion));
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("DetalleDescripcionAlarmaViewModel: Navigation no disponible");
                }
            }
            catch (Exception ex)
            {
                var LabelInformacion = await TranslateExtension.TranslateAsync("LabelInformacion");
                var MensajeError = await TranslateExtension.TranslateAsync("MensajeError");
                await ModernAlerts.ShowWarning(LabelInformacion, MensajeError);
                CrashlyticsHelper.LogError(ex, "DetalleDescripcionAlarmaViewModel", "OnEditarDetalleAlarmaCommand");
            }
            finally
            {
                IsPopupOpen = false;
                IsRunning = false;
            }
        }

        private async void OnDescribirAlarmaCommand(AlarmaCercana alarmaCercana)
        {
            if (IsPopupOpen) return;
            IsPopupOpen = true;

            IsRunning = true;
            try
            {
                // CRÍTICO: Verificar si es alarma promocional con chat habilitado
                // Si lo es, debe navegar a ChatPublicidadPage, NO a DescribirAlarmaPage
                if (alarmaCercana != null && alarmaCercana.is_advertising && alarmaCercana.publicidad_contacto_habilitado)
                {
                    System.Diagnostics.Debug.WriteLine($"[DetalleDescripcion] DescribirAlarmaCommand detectó alarma promocional {alarmaCercana.alarma_id} - delegando a NavegearAChatPublicidadCommand");

                    // Usar el comando especializado para chat de publicidad
                    if (NavegearAChatPublicidadCommand.CanExecute(alarmaCercana))
                    {
                        NavegearAChatPublicidadCommand.Execute(alarmaCercana);
                    }
                    return;
                }

                // Flujo normal para alarmas NO promocionales: ir a DescribirAlarmaPage (chat comunitario)
                INavigation navigation = GetCurrentNavigation();
                if (navigation != null)
                {
                    await navigation.PushAsync(new DescribirAlarmaPage(alarmaCercana));
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("DetalleDescripcionAlarmaViewModel: Navigation no disponible");
                }
            }
            catch (Exception ex)
            {
                var LabelInformacion = await TranslateExtension.TranslateAsync("LabelInformacion");
                var MensajeError = await TranslateExtension.TranslateAsync("MensajeError");
                await ModernAlerts.ShowWarning(LabelInformacion, MensajeError);
                CrashlyticsHelper.LogError(ex, "DetalleDescripcionAlarmaViewModel", "OnDescribirAlarmaCommand");
            }
            finally
            {
                IsPopupOpen = false;
                IsRunning = false;
            }
        }

        private async void OnCerrarAlarmaCommand(AlarmaCercana alarmaCercana)
        {
            var LabelInformacion = await TranslateExtension.TranslateAsync("LabelInformacion");
            var MensajeError = await TranslateExtension.TranslateAsync("MensajeError");

            IsRunning = true;
            try
            {
                var navigation = GetCurrentNavigation();
                if (navigation == null)
                {
                    System.Diagnostics.Debug.WriteLine("DetalleDescripcionAlarmaViewModel: Navigation no disponible para abrir pantalla de cierre");
                    return;
                }

                // Si ya hay una votación de cierre activa, informar al usuario y no navegar.
                if (alarmaCercana.TieneVotacionActiva)
                {
                    var msgVotacion = await TranslateExtension.TranslateAsync("LblAlarmaEnVotacionCierre");
                    await ModernAlerts.ShowWarning(LabelInformacion, msgVotacion);
                    return;
                }

                var tipoCierre = alarmaCercana.tipo_cierre ?? "cierre_captura";

                Page paginaCierre;
                switch (tipoCierre)
                {
                    case "cierre_encuesta":
                        paginaCierre = new CierreEncuestaPage(alarmaCercana);
                        break;
                    case "cierre_persona":
                        paginaCierre = new CierrePersonaPage(alarmaCercana);
                        break;
                    case "cierre_mascota":
                        paginaCierre = new CierreMascotaPage(alarmaCercana);
                        break;
                    case "cierre_basico":
                        paginaCierre = new CierreBasicoPage(alarmaCercana);
                        break;
                    case "cierre_captura":
                    default:
                        paginaCierre = new CierreCapturaPage(alarmaCercana);
                        break;
                }

                await navigation.PushAsync(paginaCierre);
            }
            catch (Exception ex)
            {
                await ModernAlerts.ShowWarning(LabelInformacion, MensajeError);
                CrashlyticsHelper.LogError(ex, "DetalleDescripcionAlarmaViewModel", "OnCerrarAlarmaCommand");
            }
            finally
            {
                IsRunning = false;
            }
        }

        private async void OnAtenderAlarmaCommand(AlarmaCercana alarmaCercana)
        {
            var LabelOK = await TranslateExtension.TranslateAsync("LabelOK");
            var LabelInformacion = await TranslateExtension.TranslateAsync("LabelInformacion");
            var MensajeError = await TranslateExtension.TranslateAsync("MensajeError");

            IsRunning = true;
            try
            {
                var currentPage = GetCurrentPage();
                if (currentPage != null)
                {
                    var popup = new AtenderAlarmaPopup(alarmaCercana);
                    await currentPage.ShowPopupAsync(popup);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("DetalleDescripcionAlarmaViewModel: página activa no disponible para mostrar popup");
                }
            }
            catch (Exception ex)
            {
                await ModernAlerts.ShowWarning(LabelInformacion, MensajeError);
                CrashlyticsHelper.LogError(ex, "DetalleDescripcionAlarmaViewModel", "OnAtenderAlarmaCommand");
            }
            finally
            {
                IsRunning = false;
            }
        }

        private async void OnVerAlarmaEnMapaCommand(AlarmaCercana alarma)
        {
            if (IsNavigating || alarma == null)
                return;

            IsNavigating = true;

            try
            {
                var currentPage = GetCurrentPage();
                if (currentPage != null)
                {
                    var popup = new VerMapaPopup(alarma);
                    await currentPage.ShowPopupAsync(popup);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("DetalleDescripcionAlarmaViewModel: página activa no disponible para mostrar popup");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DetalleDescripcionAlarmaViewModel: Error en OnVerAlarmaEnMapaCommand: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "DetalleDescripcionAlarmaViewModel", "OnVerAlarmaEnMapaCommand");

                // Mostrar mensaje de error al usuario en lugar de intentar navegar
                var LabelInformacion = await TranslateExtension.TranslateAsync("LabelInformacion");
                var MensajeError = await TranslateExtension.TranslateAsync("MensajeError");
                await ModernAlerts.ShowWarning(LabelInformacion, $"{MensajeError}: {ex.Message}");
            }
            finally
            {
                IsNavigating = false;
            }
        }

        private async void OnVerFotoCommand(FotoDescripcionAlarma foto)
        {
            if (foto == null)
                return;

            try
            {
                var currentPage = GetCurrentPage();
                if (currentPage != null)
                {
                    // Buscar la descripción que contiene esta foto para obtener todas las fotos y el texto de descripción
                    var descripcionConFoto = ListadoDescripcionesAlarmas?.FirstOrDefault(d => d.Fotos?.Any(f => f.FotoId == foto.FotoId) == true);

                    if (descripcionConFoto != null && descripcionConFoto.Fotos != null && descripcionConFoto.Fotos.Any())
                    {
                        // Encontrar el índice de la foto seleccionada
                        int indiceInicial = descripcionConFoto.Fotos.FindIndex(f => f.FotoId == foto.FotoId);
                        if (indiceInicial < 0) indiceInicial = 0;

                        // Crear texto de descripción concatenado
                        string textoDescripcion = await ConstruirTextoDescripcionAsync(descripcionConFoto);

                        // Abrir popup con todas las fotos de esta descripción
                        var popup = new VerFotoPopup(descripcionConFoto.Fotos, indiceInicial, textoDescripcion, descripcionConFoto.FechadescripcionLocal);
                        await currentPage.ShowPopupAsync(popup);
                    }
                    else
                    {
                        // Fallback: abrir solo con la foto individual (modo antiguo)
                        var popup = new VerFotoPopup(foto);
                        await currentPage.ShowPopupAsync(popup);
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("DetalleDescripcionAlarmaViewModel: página activa no disponible para mostrar popup");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DetalleDescripcionAlarmaViewModel: Error en OnVerFotoCommand: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "DetalleDescripcionAlarmaViewModel", "OnVerFotoCommand");

                var LabelInformacion = await TranslateExtension.TranslateAsync("LabelInformacion");
                var MensajeError = await TranslateExtension.TranslateAsync("MensajeError");
                await ModernAlerts.ShowWarning(LabelInformacion, MensajeError);
            }
        }

        private async Task<string> ConstruirTextoDescripcionAsync(DetalleDescripcionAlarma descripcion)
        {
            var partes = new List<string>();

            if (!string.IsNullOrWhiteSpace(descripcion.Descripcionalarma))
                partes.Add(descripcion.Descripcionalarma);

            if (!string.IsNullOrWhiteSpace(descripcion.Descripcionsospechoso))
            {
                var lblSospechoso = await TranslateExtension.TranslateAsync("LblSospechosoCorto");
                partes.Add($"{lblSospechoso} {descripcion.Descripcionsospechoso}");
            }

            if (!string.IsNullOrWhiteSpace(descripcion.Descripcionvehiculo))
            {
                var lblVehiculo = await TranslateExtension.TranslateAsync("LblVehiculoCorto");
                partes.Add($"{lblVehiculo} {descripcion.Descripcionvehiculo}");
            }

            if (!string.IsNullOrWhiteSpace(descripcion.Descripcionarmas))
            {
                var lblArmas = await TranslateExtension.TranslateAsync("LblArmasCorto");
                partes.Add($"{lblArmas} {descripcion.Descripcionarmas}");
            }

            return partes.Any() ? string.Join(" • ", partes) : "";
        }

        private async void OnNavegarAChatPublicidadCommand(AlarmaCercana alarma)
        {
            if (IsNavigating || alarma == null)
                return;

            // Verificar que sea alarma promocional con chat habilitado
            if (!alarma.is_advertising || !alarma.publicidad_contacto_habilitado)
            {
                System.Diagnostics.Debug.WriteLine("DetalleDescripcionAlarmaViewModel: Alarma no es promocional o no tiene chat habilitado");
                return;
            }

            IsNavigating = true;
            IsRunning = true;

            try
            {
                System.Diagnostics.Debug.WriteLine($"[DetalleDescripcionAlarmaViewModel] Verificando términos para alarma {alarma.alarma_id}");

                // Verificar si el usuario ya aceptó términos
                var response = await ApiService.VerificarAceptacionTerminos(alarma.alarma_id, App.persona.user_id_thirdparty);

                if (!response.IsSuccess)
                {
                    var LabelInformacion = await TranslateExtension.TranslateAsync("LabelInformacion");
                    await ModernAlerts.ShowWarning(LabelInformacion, response.Message ?? "Error verificando términos");
                    return;
                }

                bool yaAceptoTerminos = response.YaAceptoTerminos;
                System.Diagnostics.Debug.WriteLine($"[DetalleDescripcionAlarmaViewModel] Ya aceptó términos: {yaAceptoTerminos}");

                // Si no ha aceptado, mostrar términos
                if (!yaAceptoTerminos)
                {
                    bool acepto = await MostrarTerminosYCondicionesAsync();

                    if (!acepto)
                    {
                        // Usuario rechazó términos
                        var chatNoDisponible = await TranslateExtension.TranslateAsync("ChatNoDisponible");
                        var debesAceptarTerminos = await TranslateExtension.TranslateAsync("DebesAceptarTerminos");
                        await ModernAlerts.ShowWarning(chatNoDisponible, debesAceptarTerminos);
                        return;
                    }
                }

                // Navegar a ChatPublicidadPage
                INavigation navigation = GetCurrentNavigation();
                if (navigation != null)
                {
                    await navigation.PushAsync(new ChatPublicidadPage(alarma.alarma_id));
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("DetalleDescripcionAlarmaViewModel: Navigation no disponible");
                    var LabelInformacion = await TranslateExtension.TranslateAsync("LabelInformacion");
                    var MensajeError = await TranslateExtension.TranslateAsync("MensajeError");
                    await ModernAlerts.ShowWarning(LabelInformacion, MensajeError);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DetalleDescripcionAlarmaViewModel: Error en OnNavegarAChatPublicidadCommand: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "DetalleDescripcionAlarmaViewModel", "OnNavegarAChatPublicidadCommand");

                var LabelInformacion = await TranslateExtension.TranslateAsync("LabelInformacion");
                var MensajeError = await TranslateExtension.TranslateAsync("MensajeError");
                await ModernAlerts.ShowWarning(LabelInformacion, $"{MensajeError}: {ex.Message}");
            }
            finally
            {
                IsNavigating = false;
                IsRunning = false;
            }
        }

        private async Task<bool> MostrarTerminosYCondicionesAsync()
        {
            try
            {
                // PATRÓN UI: Mostrar popup con los 4 términos y switch de aceptación
                // Similar a PromocionLocalPage pero para el interesado
                var currentPage = GetCurrentPage();
                if (currentPage != null)
                {
                    var popup = new AceptarTerminosChatPopup();
                    await currentPage.ShowPopupAsync(popup);

                    // Leer el resultado de la propiedad pública del popup
                    // (CommunityToolkit.Maui 12.x no retorna valores, usa propiedades del popup)
                    return popup.UsuarioAceptoTerminos;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("DetalleDescripcionAlarmaViewModel: página activa no disponible para mostrar popup");
                    return false;
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "DetalleDescripcionAlarmaViewModel", "MostrarTerminosYCondicionesAsync");
                return false;
            }
        }

        private async void OnConfirmarAlarmaCommand(AlarmaCercana obj)
        {
            var RecibeAlarma = await TranslateExtension.TranslateAsync("RecibeAlarma");
            var LabelConfirmo = await TranslateExtension.TranslateAsync("LabelConfirmo");
            var LabelNoSeguro = await TranslateExtension.TranslateAsync("LabelNoSeguro");
            var LabelEsFalsa = await TranslateExtension.TranslateAsync("LabelEsFalsa");
            var LabelConfirmar = await TranslateExtension.TranslateAsync("LabelConfirmar");
            var LabelInformacion = await TranslateExtension.TranslateAsync("LabelInformacion");
            var AdvertenciaCalificacion = await TranslateExtension.TranslateAsync("AdvertenciaCalificacion");
            var MensajeError = await TranslateExtension.TranslateAsync("MensajeError");

            IsRunning = true;
            try
            {
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
                }
                catch (Exception ex)
                {
                    await ModernAlerts.ShowWarning(LabelInformacion, MensajeError);
                    CrashlyticsHelper.LogError(ex, "DetalleDescripcionAlarmaViewModel", "OnConfirmarAlarmaCommand-CalificarAlarma");
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
            }
            catch (Exception ex)
            {
                await ModernAlerts.ShowWarning(LabelInformacion, MensajeError);
                CrashlyticsHelper.LogError(ex, "DetalleDescripcionAlarmaViewModel", "OnConfirmarAlarmaCommand");
            }
            finally
            {
                IsRunning = false;
            }
        }

        public string AlertaReportadaPorMi(bool flag_propietario_alarma)
        {
            var LblAlertaReportadaPorMi = TranslateExtension.Translate("LblAlertaReportadaPorMi");
            return flag_propietario_alarma ? LblAlertaReportadaPorMi : "";
        }

        private async void OnDescribirPositivoAlarmaCommand(DetalleDescripcionAlarma obj)
        {
            string calificacionRealizar = obj.CalificacionOtrasDescripciones == "Apagado" ? "Positivo" : obj.CalificacionOtrasDescripciones == "Positivo" ? "Apagado" : "Positivo";
            var LabelOK = await TranslateExtension.TranslateAsync("LabelOK");
            var LabelInformacion = await TranslateExtension.TranslateAsync("LabelInformacion");
            var MensajeError = await TranslateExtension.TranslateAsync("MensajeError");

            CalificacionDescripcionAlarma calificacion = new CalificacionDescripcionAlarma()
            {
                CalificacionDescripcion = calificacionRealizar,
                Iddescripcion = obj.Iddescripcion,
                PUserIdThirdparty = App.persona.user_id_thirdparty
            };

            IsRunning = true;
            try
            {
                RespuestaCalificacionDescripcion response = await ApiService.CalificarDescripcionAlarma(calificacion);
                if (response != null)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        obj.CalificacionOtrasDescripciones = calificacionRealizar;
                        obj.CalificacionDescripcion = response.Calificaciondescripcion;
                    });
                }
            }
            catch (Exception ex)
            {
                await ModernAlerts.ShowWarning(LabelInformacion, MensajeError);
                CrashlyticsHelper.LogError(ex, "DetalleDescripcionAlarmaViewModel", "OnDescribirPositivoAlarmaCommand");
            }
            finally
            {
                IsRunning = false;
            }
        }

        private async void OnDescribirNegativoAlarmaCommand(DetalleDescripcionAlarma obj)
        {
            string calificacionRealizar = obj.CalificacionOtrasDescripciones == "Apagado" ? "Negativo" : obj.CalificacionOtrasDescripciones == "Negativo" ? "Apagado" : "Negativo";
            var LabelOK = await TranslateExtension.TranslateAsync("LabelOK");
            var LabelInformacion = await TranslateExtension.TranslateAsync("LabelInformacion");
            var MensajeError = await TranslateExtension.TranslateAsync("MensajeError");

            CalificacionDescripcionAlarma calificacion = new CalificacionDescripcionAlarma()
            {
                CalificacionDescripcion = calificacionRealizar,
                Iddescripcion = obj.Iddescripcion,
                PUserIdThirdparty = App.persona.user_id_thirdparty
            };

            IsRunning = true;
            try
            {
                RespuestaCalificacionDescripcion response = await ApiService.CalificarDescripcionAlarma(calificacion);
                if (response != null)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        obj.CalificacionOtrasDescripciones = calificacionRealizar;
                        obj.CalificacionDescripcion = response.Calificaciondescripcion;
                    });
                }
            }
            catch (Exception ex)
            {
                await ModernAlerts.ShowWarning(LabelInformacion, MensajeError);
                CrashlyticsHelper.LogError(ex, "DetalleDescripcionAlarmaViewModel", "OnDescribirNegativoAlarmaCommand");
            }
            finally
            {
                IsRunning = false;
            }
        }

        public async Task RecargarListadoDescripciones()
        {
            await CargarListadoDetalleDescripcionesAlarma(this.alarmaCercana);
        }

        /// <summary>
        /// Obtiene la página actualmente visible navegando por NavigationPage/TabbedPage/FlyoutPage.
        /// Necesario en iOS donde App.Current.MainPage es la raíz, no la página activa.
        /// </summary>
        private static Page GetCurrentPage()
        {
            var mainPage = Application.Current?.MainPage;
            Page currentPage = mainPage;
            while (currentPage != null)
            {
                if (currentPage is NavigationPage navPage && navPage.CurrentPage != null)
                    currentPage = navPage.CurrentPage;
                else if (currentPage is TabbedPage tabbedPage && tabbedPage.CurrentPage != null)
                    currentPage = tabbedPage.CurrentPage;
                else if (currentPage is FlyoutPage flyoutPage && flyoutPage.Detail != null)
                    currentPage = flyoutPage.Detail;
                else
                    break;
            }
            return currentPage;
        }

        private async Task CargarListadoDetalleDescripcionesAlarma(AlarmaCercana alarmaCercana)
        {
            if (_cargandoDescripciones) return;
            _cargandoDescripciones = true;
            var LabelOK = await TranslateExtension.TranslateAsync("LabelOK");
            var LabelInformacion = await TranslateExtension.TranslateAsync("LabelInformacion");
            var MensajeError = await TranslateExtension.TranslateAsync("MensajeError");
            var LabelEscritoRedConfianza = await TranslateExtension.TranslateAsync("LabelEscritoRedConfianza");
            IsRunning = true;
            try
            {
                Console.WriteLine($"[AUTORIDADES] CargarListado - AlarmaId={alarmaCercana?.alarma_id}, CatId_ANTES={AlarmaSeleccionada?.CategoriaAlarmaId}");
                List<DetalleDescripcionAlarma> response = await ApiService.ListarDescripcionesAlarma(alarmaCercana);
                Console.WriteLine($"[AUTORIDADES] Response count={response?.Count ?? -1}");

                // CORRECCIÓN: Verificar que response no sea null antes de llamar .Any()
                if (response != null && response.Any())
                {
                    // Tomar categoria_alarma_id de la primera fila (real o sintética).
                    // La función SQL garantiza siempre al menos 1 fila con este campo,
                    // incluso cuando la alarma no tiene descripciones (fila sintética).
                    // Al asignar CategoriaAlarmaId, AlarmaCercana dispara internamente
                    // OnPropertyChanged(nameof(MostrarAutoridadesResponsables)), lo que
                    // actualiza el binding IsVisible del botón directamente.
                    var primeraFila = response.FirstOrDefault();
                    Console.WriteLine($"[AUTORIDADES] PrimeraFila - Iddescripcion={primeraFila?.Iddescripcion}, CategoriaAlarmaId={primeraFila?.CategoriaAlarmaId?.ToString() ?? "NULL"}");
                    if (primeraFila?.CategoriaAlarmaId.HasValue == true)
                    {
                        AlarmaSeleccionada.CategoriaAlarmaId = primeraFila.CategoriaAlarmaId;
                        Console.WriteLine($"[AUTORIDADES] AlarmaSeleccionada.CategoriaAlarmaId={AlarmaSeleccionada.CategoriaAlarmaId}, MostrarAutoridadesResponsables={AlarmaSeleccionada.MostrarAutoridadesResponsables}");
                    }
                    else
                    {
                        Console.WriteLine($"[AUTORIDADES] ADVERTENCIA: CategoriaAlarmaId es null en la primera fila — el botón NO se mostrará");
                    }

                    // Filtrar la fila sintética (iddescripcion == 0) — solo lleva metadata, no se muestra
                    var descripcionesReales = response.Where(d => d.Iddescripcion > 0).ToList();
                    Console.WriteLine($"[AUTORIDADES] DescripcionesReales count={descripcionesReales.Count}");

                    foreach (var descripcion in descripcionesReales)
                    {
                        descripcion.FlagRedConfianza = descripcion.flag_red_confianza;
                        descripcion.TextoFlagRedConfianza = descripcion.flag_red_confianza ? LabelEscritoRedConfianza : string.Empty;
                    }
                    ListadoDescripcionesAlarmas = new ObservableCollection<DetalleDescripcionAlarma>(descripcionesReales);
                    EmptyState = !descripcionesReales.Any();
                }
                else
                {
                    Console.WriteLine($"[AUTORIDADES] Response vacío o null — EmptyState=true");
                    EmptyState = true;
                }
            }
            catch (Exception ex)
            {
                await ModernAlerts.ShowWarning(LabelInformacion, MensajeError);
                CrashlyticsHelper.LogError(ex, "DetalleDescripcionAlarmaViewModel", "CargarListadoDetalleDescripcionesAlarma");
            }
            finally
            {
                IsRunning = false;
                _cargandoDescripciones = false;
            }
        }
        private async Task CargarEstadoSeguimientoAsync(AlarmaCercana alarma)
        {
            try
            {
                if (App.persona?.user_id_thirdparty == null || alarma?.user_id_creador_alarma == null) return;
                // No mostrar botón si es la propia alarma del usuario
                if (alarma.flag_propietario_alarma) return;
                EstaSiguiendo = await ApiService.EstasSiguiendoAsync(
                    App.persona.user_id_thirdparty,
                    alarma.user_id_creador_alarma);
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "DetalleDescripcionAlarmaViewModel", "CargarEstadoSeguimientoAsync");
            }
        }

        public ICommand SeguirUsuarioCommand => new Command(async () =>
        {
            try
            {
                if (App.persona?.user_id_thirdparty == null || AlarmaSeleccionada?.user_id_creador_alarma == null) return;

                var lblSeguir = TranslateExtension.Translate("SeguirConfirmacion");
                var lblDejar = TranslateExtension.Translate("DejarDeSeguirConfirmacion");

                bool confirmar = await ModernAlerts.ShowConfirmation(
                    EstaSiguiendo ? TranslateExtension.Translate("DejarDeSeguirUsuario") : TranslateExtension.Translate("SeguirUsuario"),
                    EstaSiguiendo ? lblDejar : lblSeguir);

                if (!confirmar) return;

                ResponseMessage result;
                if (EstaSiguiendo)
                    result = await ApiService.DejarDeSeguirUsuario(App.persona.user_id_thirdparty, AlarmaSeleccionada.user_id_creador_alarma);
                else
                    result = await ApiService.SeguirUsuario(App.persona.user_id_thirdparty, AlarmaSeleccionada.user_id_creador_alarma);

                if (result?.IsSuccess == true)
                {
                    bool acabaDeSeguir = !EstaSiguiendo;
                    EstaSiguiendo = !EstaSiguiendo;
                    if (acabaDeSeguir)
                        await ModernAlerts.ShowInfo(
                            TranslateExtension.Translate("SeguirUsuario"),
                            TranslateExtension.Translate("MensajeSeguirUsuario"));
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "DetalleDescripcionAlarmaViewModel", "SeguirUsuarioCommand");
            }
        });

    }
}

