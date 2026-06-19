// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using Newtonsoft.Json;
using sospect.Models;
using sospect.Services;
using sospect.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using sospect.Helpers;
using sospect.Views;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Linq;
using sospect.Interfaces;

namespace sospect.ViewModels
{
    public class HomeViewModel : BaseViewModel
    {
        // Flag estático para evitar llamadas simultáneas a InicializarParametrosUsuarioAsync
        private static bool _isInitializingParameters = false;
        private static readonly object _initializationLock = new object();
        // Flag en memoria (no en Preferences): se resetea en cada arranque de la app, forzando
        // siempre una llamada al API en cada sesión para obtener parámetros frescos (costos, etc.)
        private static bool _parametrosInicializadosEnSesion = false;

        public HomeViewModel(bool CargarParametros)
        {
            ShowUIButtons = CargarParametros;
            IsRunning = CargarParametros;
            IsNotIOS = Device.RuntimePlatform != Device.iOS;
            Task.Run(async () => await InicializarDatosUsuarioAsync());
            if (App.FMCTokenChanged)
            {
                Task.Run(async () => await RefreshFCMTokenOnDatabase());
            }
        }

        private bool _isNotIOS;
        public bool IsNotIOS
        {
            get => _isNotIOS;
            set
            {
                _isNotIOS = value;
                OnPropertyChanged(nameof(IsNotIOS));
            }
        }

        private bool _mostrarMensajeRestriccion;
        public bool MostrarMensajeRestriccion
        {
            get => _mostrarMensajeRestriccion;
            set
            {
                _mostrarMensajeRestriccion = value;
                OnPropertyChanged(nameof(MostrarMensajeRestriccion));
            }
        }

        public static async Task<bool> InicializarParametrosUsuarioAsync()
        {
            // Evitar llamadas simultáneas - si ya se está inicializando, esperar
            lock (_initializationLock)
            {
                if (_isInitializingParameters)
                {
                    Console.WriteLine($"[HomeViewModel] InicializarParametrosUsuarioAsync YA está en ejecución - esperando...");
                }
            }

            // Esperar activamente hasta que la inicialización previa complete
            while (_isInitializingParameters)
            {
                await Task.Delay(50);
            }

            // Si ya se llamó al API en esta sesión, retornar true inmediatamente.
            // Usamos un flag en MEMORIA (no Preferences) para que en cada nuevo arranque de la app
            // siempre se llame al API y se obtengan parámetros frescos (costos promocionales, etc.)
            if (_parametrosInicializadosEnSesion)
            {
                Console.WriteLine($"[HomeViewModel] Parámetros ya inicializados en esta sesión - retornando true");

                Console.WriteLine($"[HomeViewModel] DIAG-TIPOS: TiposAlarmaDisponibles={App.TiposAlarmaDisponibles?.Count.ToString() ?? "NULL"}");
                if (App.TiposAlarmaDisponibles == null || App.TiposAlarmaDisponibles.Count == 0)
                {
                    Console.WriteLine("[HomeViewModel] Tipos no cargados en shortcut - iniciando carga en background...");
                    _ = App.CargarTiposAlarmaConEstadisticas(); // fire-and-forget, no bloquea el UI
                }

                return true;
            }

            // Marcar que estamos inicializando
            lock (_initializationLock)
            {
                _isInitializingParameters = true;
            }

            try
            {
                // DEBUG: Mostrar de dónde se llama esta función
                var stackTrace = new System.Diagnostics.StackTrace(true);
                Console.WriteLine($"[HomeViewModel] InicializarParametrosUsuarioAsync llamado desde:");
                for (int i = 0; i < Math.Min(10, stackTrace.FrameCount); i++)
                {
                    var frame = stackTrace.GetFrame(i);
                    var method = frame?.GetMethod();
                    if (method != null)
                    {
                        Console.WriteLine($"  [{i}] {method.DeclaringType?.Name}.{method.Name}");
                    }
                }

                ParametroUsuarioRequest request = new ParametroUsuarioRequest()
                {
                    Idioma = IdiomUtil.ObtenerCodigoDeIdioma(),
                    PUserIdThirdparty = App.persona.user_id_thirdparty,
                    RegistrationId = App.persona.RegistrationId
                };

                string parametros = await ApiService.ObtenerParametrosDeUsuario(request);
                if (!string.IsNullOrEmpty(parametros))
                {
                    ParametrosUsuario parametrosUsuario = JsonConvert.DeserializeObject<ParametrosUsuario>(parametros);

                    #if IOS
                    // TEMPORAL iOS: Si PUserIdThirdparty es null, el stored procedure no encontró al usuario
                    // porque el RegistrationId dummy no coincide. Usar parámetros por defecto.
                    if (string.IsNullOrEmpty(parametrosUsuario.PUserIdThirdparty))
                    {
                        Console.WriteLine("[HomeViewModel] iOS: Backend devolvió nulls, usando parámetros por defecto");
                        parametrosUsuario.PUserIdThirdparty = App.persona.user_id_thirdparty;
                        parametrosUsuario.FlagUsuarioDebeFirmarCto = false; // Usuario ya existente
                        parametrosUsuario.FlagBloqueoUsuario = false;
                        parametrosUsuario.RadioMts = 1000; // Radio por defecto 1km
                        parametrosUsuario.SaldoPoderes = 0;
                        parametrosUsuario.credibilidad_persona = 50;
                        // Otros campos quedan en null/default, no son críticos
                    }
                    #endif

                    parametrosUsuario.FechaParametro = DateTime.Now;
                    parametros = JsonConvert.SerializeObject(parametrosUsuario);
                    Preferences.Set("ParametrosUsuario", parametros);

                    // NUEVO: Cargar tipos de alarma con estadísticas para el filtro
                    try
                    {
                        Console.WriteLine("[HomeViewModel] Intentando cargar tipos de alarma...");
                        var tiposCargados = await App.CargarTiposAlarmaConEstadisticas();
                        Console.WriteLine($"[HomeViewModel] Tipos de alarma cargados: {tiposCargados}");
                        if (!tiposCargados)
                        {
                            System.Diagnostics.Debug.WriteLine("[HomeViewModel] Advertencia: No se pudieron cargar tipos de alarma (no crítico)");
                        }
                    }
                    catch (Exception exTipos)
                    {
                        Console.WriteLine($"[HomeViewModel] ERROR cargando tipos de alarma: {exTipos.Message}");
                        System.Diagnostics.Debug.WriteLine($"[HomeViewModel] Error cargando tipos de alarma: {exTipos.Message}");
                        CrashlyticsHelper.LogError(exTipos, "HomeViewModel", "InicializarParametrosUsuarioAsync-CargarTipos");
                        // No fallar el login si no se pueden cargar los tipos (feature no crítico)
                    }

                    Console.WriteLine("[HomeViewModel] InicializarParametrosUsuarioAsync retornando TRUE");

                    // Marcar que ya se inicializaron en esta sesión (flag en memoria, no Preferences)
                    _parametrosInicializadosEnSesion = true;

                    // Desactivar flag de primer login cuando la inicialización es exitosa
                    if (App.IsFirstLoginInProgress)
                    {
                        Console.WriteLine("[HomeViewModel] Primer login completado exitosamente - desactivando flag");
                        App.IsFirstLoginInProgress = false;
                    }

                    return true;
                }
                else
                {
                    Console.WriteLine("[HomeViewModel] ObtenerParametrosDeUsuario devolvió vacío - retornando FALSE");
                    return false;
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "HomeViewModel", "InicializarParametrosUsuarioAsync");
                return false;
            }
            finally
            {
                // Desmarcar el flag de inicialización
                lock (_initializationLock)
                {
                    _isInitializingParameters = false;
                }
            }
        }

        /// <summary>
        /// Fuerza la recarga de parámetros del usuario desde el API (ignora el flag de sesión).
        /// Llamar después de operaciones que consumen poderes para mantener el saldo sincronizado.
        /// </summary>
        public static async Task RefrescarParametrosAsync()
        {
            _parametrosInicializadosEnSesion = false;
            await InicializarParametrosUsuarioAsync();
        }

        private static async Task RefreshFCMTokenOnDatabase()
        {
            var okText = await TranslateExtension.TranslateAsync("LabelOK") ?? "OK";
            var ErrorText = await TranslateExtension.TranslateAsync("LabelError") ?? "Error";
            var mensajeSalida = "Register user API failed to execute. Verify internet connectivity.";
            var mensajeSalida2 = "Could not get a claims token. Please check your connection or try again later.";
            // Intentar registrar al usuario
            App.persona.RegistrationId = await SecureStorage.GetAsync("FMC_token");
            ResponseMessage response = await ApiService.RegisterUser(App.persona);

            if (response.IsSuccess)
            {
                Preferences.Set("User", JsonConvert.SerializeObject(App.persona));
            }
            else
            {
                if (response.Message != null)
                {
                    try
                    {
                        var translatedMessage = await TranslateExtension.TranslateAsync(response.Message.Replace(" ", ""));
                        if (translatedMessage != null)
                        {
                            mensajeSalida = translatedMessage;
                        }
                    }
                    catch (Exception e)
                    {
                        CrashlyticsHelper.LogError(e, "HomeViewModel", "RefreshFCMTokenOnDatabase-RegisterUser");
                    }
                }

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await ModernAlerts.ShowError(ErrorText, mensajeSalida, okText);
                });
            }
        }

        public async Task InicializarDatosUsuarioAsync()
        {
            var LabelError = TranslateExtension.Translate("LabelError");
            var LabelOK = TranslateExtension.Translate("LabelOK");
            try
            {
                if (ShowUIButtons)
                {
                    var parametrosGuardados = Preferences.Get("ParametrosUsuario", "");
                    if (!string.IsNullOrEmpty(parametrosGuardados))
                    {
                        ParametrosUsuario parametros = JsonConvert.DeserializeObject<ParametrosUsuario>(parametrosGuardados);
                        NumeroDeNotificaciones = parametros?.MensajesParaUsuario ?? 0;

                        // En Android, ignorar el flag y mostrar siempre los botones
                        if (Device.RuntimePlatform == Device.Android)
                        {
                            IsNotIOS = true;
                        }
                        else
                        {
                            // En iOS, depender del flag_convenio
                            IsNotIOS = parametros.flag_convenio;
                        }
                    }
                    else
                    {
                        NumeroDeNotificaciones = 0;

                        // Comportamiento por defecto
                        IsNotIOS = Device.RuntimePlatform == Device.Android || false; // Siempre true en Android
                    }

                    // Verificar si algún tipo de alarma requiere mensaje de advertencia según la plataforma
                    VerificarMensajeRestriccion();
                }
            }
            catch (Exception ex)
            {
                await ModernAlerts.ShowError(LabelError, ex.Message);
                CrashlyticsHelper.LogError(ex, "HomeViewModel", "InicializarDatosUsuarioAsync");
            }
        }

        private void VerificarMensajeRestriccion()
        {
            try
            {
                bool esIOS = DeviceInfo.Platform == DevicePlatform.iOS;

                if (esIOS)
                {
                    // En iOS: mostrar el mensaje de restricción siempre que no haya convenio
                    // (IsNotIOS=false significa que el botón de crimen está deshabilitado por falta de convenio).
                    // Los tipos con requiere_mensaje_advertencia_ios=true no son visibles en iOS,
                    // por lo que no aparecen en TiposAlarmaDisponibles — se usa IsNotIOS directamente.
                    MostrarMensajeRestriccion = !IsNotIOS;
                    System.Diagnostics.Debug.WriteLine($"[HomeViewModel] iOS - IsNotIOS={IsNotIOS}, MostrarMensajeRestriccion={MostrarMensajeRestriccion}");
                }
                else if (App.TiposAlarmaDisponibles != null && App.TiposAlarmaDisponibles.Count > 0)
                {
                    // En Android: verificar si al menos un tipo de alarma requiere mensaje de advertencia
                    bool requiereMensaje = App.TiposAlarmaDisponibles.Any(t => t.RequiereMensajeAdvertenciaAndroid);
                    MostrarMensajeRestriccion = requiereMensaje;
                    System.Diagnostics.Debug.WriteLine($"[HomeViewModel] Android - MostrarMensajeRestriccion={requiereMensaje}");
                }
                else
                {
                    MostrarMensajeRestriccion = false;
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "HomeViewModel", "VerificarMensajeRestriccion");
                MostrarMensajeRestriccion = false;
            }
        }


        private int _numeroDeNotificaciones;
        public int NumeroDeNotificaciones
        {
            get { return _numeroDeNotificaciones; }
            set
            {
                _numeroDeNotificaciones = value;
                OnPropertyChanged(nameof(NumeroDeNotificaciones));
                HayNotificaciones = _numeroDeNotificaciones > 0;
            }
        }

        private bool _hayNotificaciones;
        public bool HayNotificaciones
        {
            get { return _hayNotificaciones; }
            set
            {
                _hayNotificaciones = value;
                OnPropertyChanged(nameof(HayNotificaciones));
            }
        }

        private bool _showUIButtons;
        public bool ShowUIButtons
        {
            get { return _showUIButtons; }
            set
            {
                _showUIButtons = value;
                OnPropertyChanged(nameof(ShowUIButtons));
            }
        }

        // Propiedades para banner de refrescado en background (no bloqueante)
        private bool _isRefreshingInBackground;
        public bool IsRefreshingInBackground
        {
            get => _isRefreshingInBackground;
            set
            {
                _isRefreshingInBackground = value;
                OnPropertyChanged(nameof(IsRefreshingInBackground));
            }
        }

        private string _refreshStatusText = "";
        public string RefreshStatusText
        {
            get => _refreshStatusText;
            set
            {
                _refreshStatusText = value;
                OnPropertyChanged(nameof(RefreshStatusText));
            }
        }
    }
}

