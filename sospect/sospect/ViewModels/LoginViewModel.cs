// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
#if ANDROID
using Plugin.FirebasePushNotifications;
#endif
using sospect.AppConstants;
using sospect.Helpers;
using sospect.Interfaces;
using sospect.Models;
using sospect.Services;
using sospect.Utils;
using sospect.Views;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;

namespace sospect.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private bool _isAuthenticating = false;
        internal string authenticationUrl = AppConfiguration.ApiHost + "/mobileauth/";

        public ICommand GoogleCommand { get; }
        public ICommand FacebookCommand { get; }
        public ICommand AppleCommand { get; }

        public LoginViewModel()
        {
            // SOLUCIÓN: Usar RelayCommand en lugar de AsyncRelayCommand para mejor control de errores
            GoogleCommand = new RelayCommand(async () => await OnAuthenticateSafe("Google"));
            FacebookCommand = new RelayCommand(async () => await OnAuthenticateSafe("Facebook"));
            AppleCommand = new RelayCommand(async () => await OnAuthenticateSafe("Apple"));

            // Firebase Push Notifications v4.x se auto-registra via UseFirebasePushNotifications()
            // El registro manual ya no es necesario en el constructor

            CheckAuthentication();
        }

        // NUEVO: Wrapper seguro para manejo de errores
        private async Task OnAuthenticateSafe(string scheme)
        {
            try
            {
                await OnAuthenticate(scheme);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error en OnAuthenticateSafe: {ex.Message}");

                CrashlyticsHelper.LogError(ex, "LoginViewModel", "OnAuthenticateSafe", new Dictionary<string, string> {
                    { "Scheme", scheme }
                });

                // Mostrar error al usuario
                var okText = await TranslateExtension.TranslateAsync("LabelOK") ?? "OK";
                var errorText = await TranslateExtension.TranslateAsync("LabelError") ?? "Error";

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    if (Application.Current?.MainPage != null)
                    {
                        await ModernAlerts.ShowError(errorText, ex.Message, okText);
                    }
                });
            }
            finally
            {
                _isAuthenticating = false;
                IsRunning = false;
            }
        }

        private async void CheckAuthentication()
        {
            try
            {
                if (_isAuthenticating) return;

                var accessToken = await SecureStorage.GetAsync("access_token");
                if (!string.IsNullOrEmpty(accessToken))
                {
                    var userJson = Preferences.Get("User", null);
                    if (!string.IsNullOrEmpty(userJson))
                    {
                        var persona = JsonConvert.DeserializeObject<Persona>(userJson);
                        App.persona = persona;

                        // Evitar recrear SospectTabs si ya está activo
                        if (Application.Current.MainPage is SospectTabs)
                        {
                            return;
                        }

                        // NOTA: SospectTabs (TabbedPage) ya tiene NavigationPages internos.
                        // NO envolver en otro NavigationPage para evitar doble barra de navegación.
                        Application.Current.MainPage = new SospectTabs();
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "LoginViewModel", "CheckAuthentication");
            }
        }

        void ShowAlert(string message)
        {
            var okText = TranslateExtension.Translate("LabelOK");

            MainThread.BeginInvokeOnMainThread(() =>
            {
                App.Current?.MainPage?.DisplayAlert("SOSpect", message, okText).ContinueWith((task) =>
                {
                    if (task.IsFaulted) throw task.Exception;
                });
            });
        }
        public async Task OnAuthenticate(string scheme, bool isTokenRenewal = false)
        {
            var okText = await TranslateExtension.TranslateAsync("LabelOK") ?? "OK";
            var ErrorText = await TranslateExtension.TranslateAsync("LabelError") ?? "Error";
            var mensajeSalida = "Register user API failed to execute. Verify internet connectivity.";
            var mensajeSalida2 = "Could not get a claims token. Please check your connection or try again later.";

            // Limpiar el token previo si estamos renovando
            if (!isTokenRenewal)
                await SecureStorage.SetAsync("access_token", string.Empty);

            if (_isAuthenticating)
                return;

            _isAuthenticating = true;
            IsRunning = true;

            try
            {
                WebAuthenticatorResult r = null;

                var authUrl = new Uri(authenticationUrl + scheme);
                var callbackUrl = new Uri("sospect://");

                // DEBUGGING: Log de URLs
                Console.WriteLine($"[LoginViewModel] OnAuthenticate - Scheme: {scheme}");
                Console.WriteLine($"[LoginViewModel] authUrl: {authUrl}");
                Console.WriteLine($"[LoginViewModel] callbackUrl: {callbackUrl}");

                // VALIDACIÓN ADICIONAL para prevenir NullReferenceException
                if (authUrl == null || callbackUrl == null)
                {
                    throw new InvalidOperationException("URLs de autenticación no válidas");
                }

                Console.WriteLine($"[LoginViewModel] Llamando WebAuthenticator.AuthenticateAsync...");
                r = await WebAuthenticator.AuthenticateAsync(authUrl, callbackUrl);
                Console.WriteLine($"[LoginViewModel] WebAuthenticator completado - Result: {(r != null ? "OK" : "NULL")}");

                // VALIDACIÓN: Verificar que r no sea null
                if (r == null)
                {
                    throw new InvalidOperationException("Resultado de autenticación nulo");
                }

                var accessToken = r?.AccessToken;

                r.Properties.TryGetValue("email", out var email);
                r.Properties.TryGetValue("NameIdentifier", out var sid);
                r.Properties.TryGetValue("access_token", out var access_token);

                Console.WriteLine($"[LoginViewModel] Datos extraídos del callback:");
                Console.WriteLine($"  - email: {email ?? "NULL"}");
                Console.WriteLine($"  - NameIdentifier: {sid ?? "NULL"}");
                Console.WriteLine($"  - access_token presente: {!string.IsNullOrEmpty(access_token)}");

                // VALIDACIÓN: Verificar datos críticos
                if (string.IsNullOrEmpty(access_token))
                {
                    throw new InvalidOperationException("Token de acceso no recibido");
                }

                // Almacenar el nuevo token
                await SecureStorage.SetAsync("access_token", access_token);
                Console.WriteLine($"[LoginViewModel] access_token guardado en SecureStorage");

                // Si es una renovación, no necesitamos continuar con el registro ni redirigir
                if (isTokenRenewal)
                {
                    return;
                }

                Preferences.Set("userMail", email);

                string firebaseToken = App.TokenHubNotification;

                // Verificar si está vacío o nulo
                if (string.IsNullOrEmpty(firebaseToken))
                {
                    #if ANDROID
                    try
                    {
                        // Paso 1: Forzar la creación de un nuevo token desregistrando primero
                        await IFirebasePushNotification.Current .UnregisterForPushNotificationsAsync();
                        await Task.Delay(1000); // Espera breve para asegurar des-registro
                        await IFirebasePushNotification.Current .RegisterForPushNotificationsAsync();

                        // Paso 2: Esperar al nuevo token
                        firebaseToken = await ((App)App.Current).GetFirebaseTokenAsync();
                        if (string.IsNullOrEmpty(firebaseToken))
                        {
                            await ModernAlerts.ShowError(ErrorText,
                                                        mensajeSalida2,
                                                        okText);

                            CrashlyticsHelper.LogError(new Exception("Token FCM no disponible. Usando GUID."), "LoginViewModel", "OnAuthenticate-TokenFallback", new Dictionary<string, string> {
                        { "GeneratedGUID", firebaseToken ?? "null" }
                    });

                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        CrashlyticsHelper.LogError(ex, "LoginViewModel", "OnAuthenticate-TokenGeneration", new Dictionary<string, string> {
                    { "ErrorMessage", ex.Message }
                });
                    }
                    #else
                    // iOS: Obtener token de Firebase (ahora soportado con Plugin.FirebasePushNotifications)
                    firebaseToken = App.TokenHubNotification;
                    Console.WriteLine($"[LoginViewModel] iOS: Token desde App.TokenHubNotification: {firebaseToken ?? "NULL"}");

                    if (string.IsNullOrEmpty(firebaseToken))
                    {
                        // Si aún no hay token, esperar a que Firebase lo genere
                        Console.WriteLine("[LoginViewModel] Esperando token de Firebase...");
                        firebaseToken = await ((App)App.Current).GetFirebaseTokenAsync();

                        if (string.IsNullOrEmpty(firebaseToken))
                        {
                            await ModernAlerts.ShowError(ErrorText,
                                "No se pudo obtener el token de notificaciones. Por favor, verifica los permisos.",
                                okText);
                            return;
                        }
                        Console.WriteLine($"[LoginViewModel] Token de Firebase obtenido: {firebaseToken}");
                    }
                    #endif
                }

                // Obtener el código del país
                string countryCode = await ObtenerCodigoPais();

                Persona persona = new Persona()
                {
                    login = email,
                    marca_bloqueo = 0,
                    user_id_thirdparty = sid,
                    Plataforma = DeviceInfo.Platform.ToString(),
                    RegistrationId = firebaseToken, // <- Aquí usamos el token procesado
                    Idioma = IdiomUtil.ObtenerCodigoDeIdioma(),
                    Pais = string.IsNullOrEmpty(countryCode) ? "CO" : countryCode
                };

                Console.WriteLine($"[LoginViewModel] Persona creada:");
                Console.WriteLine($"  - login: {persona.login}");
                Console.WriteLine($"  - user_id_thirdparty: {persona.user_id_thirdparty}");
                Console.WriteLine($"  - RegistrationId: {persona.RegistrationId}");
                Console.WriteLine($"  - Plataforma: {persona.Plataforma}");

                App.persona = persona;

                // NUEVO: Guardar user_id_thirdparty en SecureStorage para el servicio de ubicación en segundo plano
                await SecureStorage.SetAsync("user_id_thirdparty", sid);
                System.Diagnostics.Debug.WriteLine($"[LoginViewModel] user_id_thirdparty guardado en SecureStorage: {sid}");

                var LblErrorInicializandoParametros = TranslateExtension.Translate("LblErrorInicializandoParametros");

                Console.WriteLine($"[LoginViewModel] App.FMCTokenChanged: {App.FMCTokenChanged}");

                // CORRECCIÓN iOS: SIEMPRE intentar registrar al usuario
                // En iOS, FMCTokenChanged es siempre false porque Firebase no está disponible
                // El backend es idempotente - actualiza si existe, crea si no existe
                Console.WriteLine($"[LoginViewModel] Llamando ApiService.RegisterUser...");
                ResponseMessage response = await ApiService.RegisterUser(persona);
                Console.WriteLine($"[LoginViewModel] RegisterUser completado - IsSuccess: {response.IsSuccess}");

                if (response.IsSuccess)
                {
                    Preferences.Set("User", JsonConvert.SerializeObject(persona));
                    App.FMCTokenChanged = false;

                    // Marcar que estamos en proceso de primer login para evitar cierre de sesión por race condition
                    App.IsFirstLoginInProgress = true;

                    // CORRECCIÓN: NO llamar InicializarParametrosUsuarioAsync aquí (causa race condition)
                    // En su lugar, navegar directamente a VerificarYNavegar → HomePage
                    // HomePage.OnAppearing llamará InicializarParametrosUsuarioAsync con el delay natural
                    // de la navegación, dando tiempo al COMMIT de la transacción en el backend
                    Console.WriteLine($"[LoginViewModel] RegisterUser exitoso - navegando a VerificarYNavegar...");
                    await VerificarYNavegar();
                }
                else
                {
                    // Error al registrar usuario
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
                            CrashlyticsHelper.LogError(e, "LoginViewModel", "OnAuthenticate-RegisterUser");
                        }
                    }

                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await ModernAlerts.ShowError(ErrorText, mensajeSalida, okText);
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LoginViewModel] ERROR en OnAuthenticate:");
                Console.WriteLine($"  - Tipo: {ex.GetType().Name}");
                Console.WriteLine($"  - Mensaje: {ex.Message}");
                Console.WriteLine($"  - StackTrace: {ex.StackTrace}");

                await SecureStorage.SetAsync("access_token", string.Empty);

                #if ANDROID
                await IFirebasePushNotification.Current .UnregisterForPushNotificationsAsync();
                #endif

                var FalloText = await TranslateExtension.TranslateAsync("LabelFallo") ?? "Fallo";

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await ModernAlerts.ShowError(FalloText, ex.Message, okText);
                    Application.Current.MainPage = new NavigationPage(new LoginPage());
                });

                CrashlyticsHelper.LogError(ex, "LoginViewModel", "OnAuthenticate");

                if (DeviceInfo.Platform == DevicePlatform.iOS)
                {
                    var settingsService = DependencyService.Get<ISettingsService>();
                    settingsService?.OpenSettings();
                    App.justCheckedNotificationPermissions = true;
                }
            }
            finally
            {
                _isAuthenticating = false;
                IsRunning = false;
            }
        }

        private async Task VerificarYNavegar()
        {
            try
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        // Obtener los parámetros del usuario
                        var parametrosGuardados = Preferences.Get("ParametrosUsuario", "");
                        if (!string.IsNullOrEmpty(parametrosGuardados))
                        {
                            ParametrosUsuario parametros = JsonConvert.DeserializeObject<ParametrosUsuario>(parametrosGuardados);

                            // Verificar si el usuario debe firmar el contrato
                            if (parametros.FlagUsuarioDebeFirmarCto)
                            {
                                System.Diagnostics.Debug.WriteLine("LoginViewModel: Usuario debe firmar contrato, navegando a TermsAndConditionsPage");
                                #if ANDROID
                                await IFirebasePushNotification.Current .RegisterForPushNotificationsAsync();
                                #endif
                                Application.Current.MainPage = new NavigationPage(new TermsAndConditionsPage()) { BarBackgroundColor = Colors.Black };
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine("LoginViewModel: Usuario no necesita firmar contrato, navegando a SospectTabs");
                                #if ANDROID
                                await IFirebasePushNotification.Current .RegisterForPushNotificationsAsync();
                                #endif

                                // Iniciar servicio de ubicación en segundo plano después del login exitoso
                                await IniciarServicioUbicacionDespuesDelLogin();

                                Application.Current.MainPage = new SospectTabs();
                            }
                        }
                        else
                        {
                            // Si no hay parámetros (primer login), esperar 1 segundo antes de navegar
                            // para dar tiempo al COMMIT de la transacción en el backend
                            System.Diagnostics.Debug.WriteLine("LoginViewModel: No hay parámetros guardados (primer login)");
                            Console.WriteLine("[LoginViewModel] Esperando 1000ms para COMMIT de transacción antes de navegar...");
                            await Task.Delay(1000);

                            #if ANDROID
                            await IFirebasePushNotification.Current .RegisterForPushNotificationsAsync();
                            #endif

                            // Iniciar servicio de ubicación en segundo plano después del login exitoso
                            await IniciarServicioUbicacionDespuesDelLogin();

                            Console.WriteLine("[LoginViewModel] Navegando a SospectTabs...");
                            Application.Current.MainPage = new SospectTabs();
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error en VerificarYNavegar: {ex.Message}");
                        CrashlyticsHelper.LogError(ex, "LoginViewModel", "VerificarYNavegar");

                        // Fallback: ir a SospectTabs
                        #if ANDROID
                        await IFirebasePushNotification.Current .RegisterForPushNotificationsAsync();
                        #endif

                        // Iniciar servicio de ubicación en segundo plano después del login exitoso
                        await IniciarServicioUbicacionDespuesDelLogin();

                        Application.Current.MainPage = new SospectTabs();
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en VerificarYNavegar (outer): {ex.Message}");
                CrashlyticsHelper.LogError(ex, "LoginViewModel", "VerificarYNavegar-Outer");
            }
        }

        private async Task<string> ObtenerCodigoPais()
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                    if (status != PermissionStatus.Granted)
                    {
                        Debug.WriteLine("Permiso de ubicación denegado.");
                        return Preferences.Get("LastCountryCode", "US"); // Valor por defecto
                    }
                }

                var location = await Geolocation.GetLastKnownLocationAsync();

                if (location == null)
                {
                    location = await Geolocation.GetLocationAsync(new GeolocationRequest
                    {
                        DesiredAccuracy = GeolocationAccuracy.Medium,
                        Timeout = TimeSpan.FromSeconds(30)
                    });
                }

                if (location != null)
                {
                    var placemarks = await Geocoding.GetPlacemarksAsync(location.Latitude, location.Longitude);
                    var placemark = placemarks?.FirstOrDefault();

                    if (placemark != null)
                    {
                        Preferences.Set("LastCountryCode", placemark.CountryCode); // Cache
                        return placemark.CountryCode;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error obteniendo el código de país: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "LoginViewModel", "ObtenerCodigoPais");
            }

            return Preferences.Get("LastCountryCode", "US"); // Valor por defecto desde el cache
        }

        private async Task IniciarServicioUbicacionDespuesDelLogin()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("========================================");
                System.Diagnostics.Debug.WriteLine($"[LoginViewModel] IniciarServicioUbicacionDespuesDelLogin LLAMADO: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

                // Verificar que hay usuario logueado
                var userId = await SecureStorage.GetAsync("user_id_thirdparty");
                System.Diagnostics.Debug.WriteLine($"[LoginViewModel] UserId desde SecureStorage: {(string.IsNullOrEmpty(userId) ? "VACIO" : userId)}");

                if (string.IsNullOrEmpty(userId))
                {
                    System.Diagnostics.Debug.WriteLine("[LoginViewModel] ERROR: No hay user_id_thirdparty en SecureStorage");
                    System.Diagnostics.Debug.WriteLine("========================================");
                    return;
                }

                // Dar tiempo a que la app se inicialice completamente
                await Task.Delay(1000);

                // Obtener el servicio de background del DI
                if (Application.Current?.Handler?.MauiContext?.Services == null)
                {
                    System.Diagnostics.Debug.WriteLine("[LoginViewModel] ERROR: No se pudo obtener el contexto de la app");
                    System.Diagnostics.Debug.WriteLine("========================================");
                    return;
                }

                var backgroundService = Application.Current.Handler.MauiContext.Services.GetService<sospect.Interfaces.IBackgroundService>();
                if (backgroundService == null)
                {
                    System.Diagnostics.Debug.WriteLine("[LoginViewModel] ERROR: IBackgroundService no está registrado en DI");
                    System.Diagnostics.Debug.WriteLine("========================================");
                    return;
                }

                System.Diagnostics.Debug.WriteLine("[LoginViewModel] IBackgroundService obtenido, iniciando servicio...");

                // Iniciar servicio de ubicación en segundo plano
                await backgroundService.RunCodeInBackgroundMode(
                    sospect.Services.ApiService.ActualizarUbicacion,
                    "LocationTracking"
                );

                System.Diagnostics.Debug.WriteLine("[LoginViewModel] Servicio de ubicación iniciado correctamente después del login");

                // NUEVO: Esperar un momento y luego solicitar permisos directamente (sin popup intermedio)
                await Task.Delay(1500);

                // NUEVO: Solicitar permisos de ubicación (popups del sistema directamente)
#if ANDROID
                if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Q)
                {
                    System.Diagnostics.Debug.WriteLine("[LoginViewModel] === INICIO SOLICITUD DE PERMISOS ===");

                    // CAMBIO CRÍTICO: Pedir DIRECTAMENTE LocationAlways
                    // Android automáticamente pedirá LocationWhenInUse primero si es necesario
                    // Esto evita el problema de tener "While using app" preseleccionado
                    var backgroundLocationStatus = await Permissions.CheckStatusAsync<Permissions.LocationAlways>();
                    System.Diagnostics.Debug.WriteLine($"[LoginViewModel] Estado actual LocationAlways: {backgroundLocationStatus}");

                    if (backgroundLocationStatus != PermissionStatus.Granted)
                    {
                        // POPUP EDUCATIVO: Explicar al usuario por qué necesitamos este permiso
                        System.Diagnostics.Debug.WriteLine("[LoginViewModel] Mostrando popup educativo antes de solicitar permiso...");

                        // Obtener textos traducidos (usando método síncrono para evitar race conditions)
                        var tituloPopup = TranslateExtension.Translate("PermisoUbicacionFondoTitulo");
                        var mensajePopup = TranslateExtension.Translate("PermisoUbicacionFondoMensaje");
                        var btnPermitir = TranslateExtension.Translate("LabelPermitir");
                        var btnAhoraNo = TranslateExtension.Translate("LabelAhoraNo");

                        bool shouldRequest = await sospect.Helpers.ModernAlerts.ShowConfirmation(
                            tituloPopup,
                            mensajePopup,
                            btnPermitir,
                            btnAhoraNo
                        );

                        if (!shouldRequest)
                        {
                            System.Diagnostics.Debug.WriteLine("[LoginViewModel] Usuario seleccionó 'Ahora no' en el popup educativo");
                            System.Diagnostics.Debug.WriteLine("[LoginViewModel] El servicio funcionará solo con la app abierta");
                            System.Diagnostics.Debug.WriteLine("[LoginViewModel] === FIN SOLICITUD DE PERMISOS (CANCELADO) ===");
                            System.Diagnostics.Debug.WriteLine("========================================");
                            return;
                        }

                        System.Diagnostics.Debug.WriteLine("[LoginViewModel] Usuario aceptó el popup educativo, solicitando permiso LocationAlways...");
                        backgroundLocationStatus = await Permissions.RequestAsync<Permissions.LocationAlways>();
                        System.Diagnostics.Debug.WriteLine($"[LoginViewModel] Resultado LocationAlways: {backgroundLocationStatus}");

                        if (backgroundLocationStatus == PermissionStatus.Granted)
                        {
                            System.Diagnostics.Debug.WriteLine("[LoginViewModel] Permiso de ubicación en segundo plano CONCEDIDO");

                            // IMPORTANTE: Reiniciar servicio para que tenga los permisos
                            await Task.Delay(500);
                            System.Diagnostics.Debug.WriteLine("[LoginViewModel] Reiniciando servicio con permisos...");

                            try
                            {
                                await backgroundService.StopBackgroundService();
                                await Task.Delay(300);
                                await backgroundService.RunCodeInBackgroundMode(
                                    sospect.Services.ApiService.ActualizarUbicacion,
                                    "LocationTracking"
                                );
                                System.Diagnostics.Debug.WriteLine("[LoginViewModel] Servicio REINICIADO con permisos completos");
                            }
                            catch (Exception restartEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"[LoginViewModel] Error reiniciando servicio: {restartEx.Message}");
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[LoginViewModel] Usuario DENEGÓ permiso LocationAlways: {backgroundLocationStatus}");
                            System.Diagnostics.Debug.WriteLine("[LoginViewModel] El servicio funcionará solo con la app abierta");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[LoginViewModel] Permiso LocationAlways ya estaba concedido previamente");
                    }

                    System.Diagnostics.Debug.WriteLine("[LoginViewModel] === FIN SOLICITUD DE PERMISOS ===");
                }
#endif

                System.Diagnostics.Debug.WriteLine("========================================");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("========================================");
                System.Diagnostics.Debug.WriteLine("[LoginViewModel] ERROR iniciando servicio de ubicación");
                System.Diagnostics.Debug.WriteLine($"[LoginViewModel] Message: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[LoginViewModel] StackTrace: {ex.StackTrace}");
                System.Diagnostics.Debug.WriteLine("========================================");
                CrashlyticsHelper.LogError(ex, "LoginViewModel", "IniciarServicioUbicacionDespuesDelLogin");
            }
        }
    }
}


