using sospect.Helpers;
using sospect.Interfaces;
using sospect.Models;
using sospect.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace sospect.ViewModels
{
    public class ConfiguracionNotificacionesViewModel : BaseViewModel
    {
        private ObservableCollection<int> _diasOptions;
        public ObservableCollection<int> DiasOptions
        {
            get { return _diasOptions; }
            set { SetProperty(ref _diasOptions, value); }
        }

        private ObservableCollection<int> _limitesAlarmasFeedOptions;
        public ObservableCollection<int> LimitesAlarmasFeedOptions
        {
            get { return _limitesAlarmasFeedOptions; }
            set { SetProperty(ref _limitesAlarmasFeedOptions, value); }
        }

        private int _limiteAlarmasFeedSeleccionado;
        public int LimiteAlarmasFeedSeleccionado
        {
            get { return _limiteAlarmasFeedSeleccionado; }
            set { SetProperty(ref _limiteAlarmasFeedSeleccionado, value); }
        }

        private ObservableCollection<int> _intervalosBackgroundOptions;
        public ObservableCollection<int> IntervalosBackgroundOptions
        {
            get { return _intervalosBackgroundOptions; }
            set { SetProperty(ref _intervalosBackgroundOptions, value); }
        }

        private int _intervaloBackgroundSeleccionado;
        public int IntervaloBackgroundSeleccionado
        {
            get { return _intervaloBackgroundSeleccionado; }
            set { SetProperty(ref _intervaloBackgroundSeleccionado, value); }
        }

        //public bool MostrarControlesDeTiempo
        //{
        //    get => RecibirAlarmasAutoridad && Fecha_act_configuracion_notif.HasValue;
        //}


        //private string _fechaApagadoNotifFormatted;
        //public string FechaApagadoNotifFormatted
        //{
        //    get => _fechaApagadoNotifFormatted;
        //    set => SetProperty(ref _fechaApagadoNotifFormatted, value);
        //}

        //private string _tiempoRestanteFormatted;
        //public string TiempoRestanteFormatted
        //{
        //    get => _tiempoRestanteFormatted;
        //    set => SetProperty(ref _tiempoRestanteFormatted, value);
        //}

        private bool _recibirAlarmasCercanas = true;
        public bool RecibirAlarmasCercanas
        {
            get => _recibirAlarmasCercanas;
            set => SetProperty(ref _recibirAlarmasCercanas, value);
        }

        private bool _recibirAlarmasProtegidos = true;
        public bool RecibirAlarmasProtegidos
        {
            get => _recibirAlarmasProtegidos;
            set => SetProperty(ref _recibirAlarmasProtegidos, value);
        }

        private bool _recibirAlarmasZonasVigilancia = true;
        public bool RecibirAlarmasZonasVigilancia
        {
            get => _recibirAlarmasZonasVigilancia;
            set => SetProperty(ref _recibirAlarmasZonasVigilancia, value);
        }

        private bool _flag_es_policia;
        public bool Flag_es_policia
        {
            get => _flag_es_policia;
            set => SetProperty(ref _flag_es_policia, value);
        }

        private DateTime? _fecha_act_configuracion_notif;
        public DateTime? Fecha_act_configuracion_notif
        {
            get => _fecha_act_configuracion_notif;
            set => SetProperty(ref _fecha_act_configuracion_notif, value);
            //set
            //{
            //    SetProperty(ref _fecha_act_configuracion_notif, value);
            //    OnPropertyChanged(nameof(MostrarControlesDeTiempo));
            //}
        }

        //private bool temporizadorActivo = false;

        //public void DetenerTemporizador()
        //{
        //    temporizadorActivo = false;
        //}

        private bool _recibirAlarmasAutoridad = true;
        public bool RecibirAlarmasAutoridad
        {
            get => _recibirAlarmasAutoridad;
            set
            {
                SetProperty(ref _recibirAlarmasAutoridad, value);
                //OnPropertyChanged(nameof(MostrarControlesDeTiempo));
                // Actualizar la propiedad DiasSinNotificaciones en base al nuevo valor
                if (_recibirAlarmasAutoridad)
                {
                    DiasSinNotificaciones = null;
                }
                else if (DiasSinNotificaciones == null)
                {
                    DiasSinNotificaciones = 0; // Valor por defecto cuando se desactiva RecibirAlarmasAutoridad
                }
            }
        }

        private int? _diasSinNotificaciones = 0;
        public int? DiasSinNotificaciones
        {
            get => _diasSinNotificaciones;
            set => SetProperty(ref _diasSinNotificaciones, value);
        }

        public ICommand GuardarConfiguracionCommand { get; }

        public ConfiguracionNotificacionesViewModel()
        {
            GuardarConfiguracionCommand = new Command(GuardarConfiguracion);
            DiasOptions = new ObservableCollection<int>(Enumerable.Range(1, 30));
            LimitesAlarmasFeedOptions = new ObservableCollection<int> { 10, 20, 50, 100, 200, 300, 500 };
            LimiteAlarmasFeedSeleccionado = 100; // Valor por defecto
            // Intervalos optimizados para carga del servidor: 5, 10, 15, 30 minutos
            IntervalosBackgroundOptions = new ObservableCollection<int> { 5, 10, 15, 30 };
            IntervaloBackgroundSeleccionado = 5; // Valor por defecto: 5 minutos

        }

        public async Task CargarConfiguracionesAsync()
        {
            var LabelOK = TranslateExtension.Translate("LabelOK");
            var LabelInformacion = TranslateExtension.Translate("LabelInformacion");
            var MensajeError = TranslateExtension.Translate("MensajeError");

            IsRunning = true;
            try
            {
                var configNotif = await ApiService.ConsultarConfiguracionNotificaciones(App.persona.user_id_thirdparty);

                // Debug: Agregar esto para verificar
                System.Diagnostics.Debug.WriteLine($"=== ConfigNotif recibida ===");
                System.Diagnostics.Debug.WriteLine($"Cercanas: {configNotif?.notif_alarma_cercana_habilitada}");
                System.Diagnostics.Debug.WriteLine($"Protegidos: {configNotif?.notif_alarma_protegido_habilitada}");
                System.Diagnostics.Debug.WriteLine($"Zonas: {configNotif?.notif_alarma_zona_vigilancia_habilitada}");
                System.Diagnostics.Debug.WriteLine($"Policia: {configNotif?.notif_alarma_policia_habilitada}");

                if (configNotif != null)
                {
                    RecibirAlarmasCercanas = configNotif.notif_alarma_cercana_habilitada;
                    RecibirAlarmasProtegidos = configNotif.notif_alarma_protegido_habilitada;
                    RecibirAlarmasZonasVigilancia = configNotif.notif_alarma_zona_vigilancia_habilitada;
                    RecibirAlarmasAutoridad = configNotif.notif_alarma_policia_habilitada;
                    Flag_es_policia = configNotif.flag_es_policia;
                    Fecha_act_configuracion_notif = configNotif.fecha_act_configuracion_notif;

                    // Cargar límite de alarmas feed desde la API
                    LimiteAlarmasFeedSeleccionado = configNotif.limite_alarmas_feed.HasValue && configNotif.limite_alarmas_feed.Value > 0
                        ? configNotif.limite_alarmas_feed.Value
                        : 100; // Default a 100 si viene null o 0

                    // Cargar intervalo de background desde la API
                    IntervaloBackgroundSeleccionado = configNotif.intervalo_background_minutos > 0
                        ? configNotif.intervalo_background_minutos
                        : 5; // Default a 5 si viene 0 o null

                    if (!RecibirAlarmasAutoridad)
                    {
                        DiasSinNotificaciones = (configNotif.dias_notif_policia_apagada ?? 1) - 1;
                    }
                    else
                    {
                        DiasSinNotificaciones = null;
                    }

                    // Debug: Verificar que se asignaron
                    System.Diagnostics.Debug.WriteLine($"=== Después de asignar ===");
                    System.Diagnostics.Debug.WriteLine($"RecibirAlarmasCercanas: {RecibirAlarmasCercanas}");
                    System.Diagnostics.Debug.WriteLine($"RecibirAlarmasProtegidos: {RecibirAlarmasProtegidos}");
                    System.Diagnostics.Debug.WriteLine($"RecibirAlarmasZonasVigilancia: {RecibirAlarmasZonasVigilancia}");
                    System.Diagnostics.Debug.WriteLine($"LimiteAlarmasFeedSeleccionado: {LimiteAlarmasFeedSeleccionado}");
                    System.Diagnostics.Debug.WriteLine($"IntervaloBackgroundSeleccionado: {IntervaloBackgroundSeleccionado}");
                }
            }
            catch (Exception ex)
            {
                await ModernAlerts.ShowWarning(LabelInformacion, MensajeError);
                CrashlyticsHelper.LogError(ex, "ConfiguracionNotificacionesViewModel", "CargarConfiguracionesAsync");
            }
            finally
            {
                IsRunning = false;
            }
        }

        //private void ActualizarTiempoRestante()
        //{
        //    temporizadorActivo = true;
        //    var NoAplicable = TranslateExtension.Translate("LblNoAplicable");
        //    var TiempoExpirado = TranslateExtension.Translate("LblTiempoExpirado");

        //    if (!Fecha_act_configuracion_notif.HasValue || DiasSinNotificaciones == null)
        //    {
        //        TiempoRestanteFormatted = NoAplicable;
        //        return;
        //    }

        //    var fechaFutura = Fecha_act_configuracion_notif.Value.AddDays(DiasSinNotificaciones.Value);
        //    var tiempoRestante = fechaFutura - DateTime.Now;

        //    if (tiempoRestante.TotalSeconds > 0)
        //    {
        //        TiempoRestanteFormatted = tiempoRestante.ToString("dd' días 'hh':'mm':'ss");
        //        // Programa la próxima actualización en 1 segundo
        //        Device.StartTimer(TimeSpan.FromSeconds(1), () =>
        //        {
        //            ActualizarTiempoRestante();
        //            return false; // No repetir automáticamente, se programa manualmente cada vez
        //        });
        //    }
        //    else
        //    {
        //        TiempoRestanteFormatted = TiempoExpirado;
        //    }

        //    Device.StartTimer(TimeSpan.FromSeconds(1), () =>
        //    {
        //        if (temporizadorActivo)
        //        {
        //            ActualizarTiempoRestante();
        //        }
        //        return temporizadorActivo; // Continuará solo si temporizadorActivo es true
        //    });
        //}


        private async void GuardarConfiguracion()
        {
            var LabelOK = TranslateExtension.Translate("LabelOK");
            var LabelInformacion = TranslateExtension.Translate("LabelInformacion");
            var MensajeError = TranslateExtension.Translate("MensajeError");
            var LabelError = TranslateExtension.Translate("LabelError");
            var LabelExito = TranslateExtension.Translate("LabelExito");
            var LblConfNotifGuardadas = TranslateExtension.Translate("LblConfNotifGuardadas");
            var LblConfNotifNoSeGuarda = TranslateExtension.Translate("LblConfNotifNoSeGuarda");
            var LblCamposIncompletos = TranslateExtension.Translate("LblCamposIncompletos");

            if (!ValidarConfiguracion())
            {
                await ModernAlerts.ShowError(LabelError, LblCamposIncompletos);
                return;
            }

            IsRunning = true; // Bloquea la interfaz de usuario

            var actualizaConfNotifRequest = new ActualizaConfNotifRequest
            {
                p_user_id_thirdparty = App.persona.user_id_thirdparty,
                p_notif_alarma_cercana_habilitada = RecibirAlarmasCercanas,
                p_notif_alarma_protegido_habilitada = RecibirAlarmasProtegidos,
                p_notif_alarma_zona_vigilancia_habilitada = RecibirAlarmasZonasVigilancia,
                p_notif_alarma_policia_habilitada = RecibirAlarmasAutoridad,
                p_dias_notif_policia_apagada = RecibirAlarmasAutoridad ? (int?)null : DiasSinNotificaciones+1,
                p_limite_alarmas_feed = LimiteAlarmasFeedSeleccionado,
                p_intervalo_background_minutos = IntervaloBackgroundSeleccionado
            };

            try
            {
                System.Diagnostics.Debug.WriteLine($"=== Guardando configuración ===");
                System.Diagnostics.Debug.WriteLine($"Límite alarmas feed: {LimiteAlarmasFeedSeleccionado}");

                ResponseMessage response = await ApiService.ActualizarConfiguracionNotificaciones(actualizaConfNotifRequest);

                System.Diagnostics.Debug.WriteLine($"=== Respuesta del API ===");
                System.Diagnostics.Debug.WriteLine($"IsSuccess: {response?.IsSuccess}");
                System.Diagnostics.Debug.WriteLine($"Message: {response?.Message}");
                System.Diagnostics.Debug.WriteLine($"Data: {response?.Data}");

                if (response != null && response.IsSuccess)
                {
                    // Actualizar el valor en Preferences para que esté disponible inmediatamente
                    var parametrosGuardados = Preferences.Get("ParametrosUsuario", "");
                    if (!string.IsNullOrEmpty(parametrosGuardados))
                    {
                        var parametros = Newtonsoft.Json.JsonConvert.DeserializeObject<ParametrosUsuario>(parametrosGuardados);
                        if (parametros != null)
                        {
                            parametros.LimiteAlarmasFeed = LimiteAlarmasFeedSeleccionado;
                            Preferences.Set("ParametrosUsuario", Newtonsoft.Json.JsonConvert.SerializeObject(parametros));
                        }
                    }

                    // Guardar intervalo de background en Preferences para que el servicio de fondo lo lea
                    Preferences.Set("intervalo_background_minutos", IntervaloBackgroundSeleccionado);

                    // Reiniciar servicio de ubicación para aplicar nuevo intervalo
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"[ConfigNotif] Reiniciando servicio con nuevo intervalo: {IntervaloBackgroundSeleccionado} minutos");
#if ANDROID
                        // Android: Detener e iniciar el LocationForegroundService
                        var context = Android.App.Application.Context;
                        var serviceIntent = new Android.Content.Intent(context, typeof(sospect.Platforms.Android.Services.LocationForegroundService));

                        // Detener servicio actual
                        context.StopService(serviceIntent);
                        System.Diagnostics.Debug.WriteLine("[ConfigNotif] Servicio Android detenido");

                        await Task.Delay(1000); // Esperar a que se detenga completamente

                        // Iniciar servicio con nuevo intervalo (leerá el nuevo valor de Preferences)
                        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
                        {
                            context.StartForegroundService(serviceIntent);
                        }
                        else
                        {
                            context.StartService(serviceIntent);
                        }
                        System.Diagnostics.Debug.WriteLine("[ConfigNotif] Servicio Android reiniciado con nuevo intervalo");
#elif IOS
                        // iOS: Usar el IBackgroundService
                        var backgroundService = Microsoft.Maui.Controls.Application.Current.Handler?.MauiContext?.Services.GetService<IBackgroundService>();
                        if (backgroundService != null)
                        {
                            await backgroundService.StopBackgroundService();
                            await Task.Delay(500);
                            await backgroundService.RunCodeInBackgroundMode(ApiService.ActualizarUbicacion, "LocationTracking");
                            System.Diagnostics.Debug.WriteLine("[ConfigNotif] Servicio iOS reiniciado");
                        }
#endif
                    }
                    catch (Exception exService)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ConfigNotif] Error reiniciando servicio: {exService.Message}");
                        // No bloquear el guardado si falla el reinicio
                    }

                    await ModernAlerts.ShowSuccess(LabelExito, LblConfNotifGuardadas);
                    MessagingCenter.Send(this, "DatosActualizados");

                    // Intentar navegar de vuelta al MenuPage
                    try
                    {
                        // Obtener el NavigationPage del tab actual (si existe)
                        if (Application.Current.MainPage is TabbedPage tabbedPage &&
                            tabbedPage.CurrentPage is NavigationPage navigationPage)
                        {
                            // Volver una página atrás (al MenuPage)
                            await navigationPage.PopAsync();
                        }
                        else if (Application.Current.MainPage is NavigationPage navPage)
                        {
                            // Si MainPage es directamente un NavigationPage
                            await navPage.PopAsync();
                        }
                        else
                        {
                            // Fallback: intentar con Navigation directamente
                            await Application.Current.MainPage.Navigation.PopAsync();
                        }
                    }
                    catch (Exception navEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al navegar: {navEx.Message}");
                        // Si falla la navegación, no es crítico ya que el guardado fue exitoso
                    }
                }
                else
                {
                    await ModernAlerts.ShowError(LabelError, LblConfNotifNoSeGuarda);
                }
            }
            catch (Exception ex)
            {
                await ModernAlerts.ShowWarning(LabelInformacion, MensajeError);
                CrashlyticsHelper.LogError(ex, "ConfiguracionNotificacionesViewModel", "GuardarConfiguracion");
            }
            finally
            {
                IsRunning = false; // Desbloquea la interfaz de usuario
            }
        }

        private bool ValidarConfiguracion()
        {
            // Si RecibirAlarmasAutoridad es false, entonces DiasSinNotificaciones no debe ser null.
            if (!RecibirAlarmasAutoridad && DiasSinNotificaciones == null)
            {
                return false;
            }
            return true;
        }


    }
}
