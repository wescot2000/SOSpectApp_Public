using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;
using Newtonsoft.Json;
using sospect.AppConstants;
using sospect.AuthHelpers;
using sospect.Helpers;
using sospect.Interfaces;
using sospect.Models;
using sospect.Services;
using sospect.Utils;
using sospect.Views;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace sospect.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        internal string authenticationUrl = AppConfiguration.ApiHost + "/mobileauth/";
        //const string authenticationUrl = "https://www.wescot.com.co:81/mobileauth/";
        //const string authenticationUrl = "https://localhost:7079/mobileauth/";

        public ICommand GoogleCommand { get; }

        public ICommand FacebookCommand { get; }

        public ICommand AppleCommand { get; }

        private INotificationRegistrationService _notificationRegistrationService;

        public LoginViewModel()
        {
            _notificationRegistrationService = ServiceContainer.Resolve<INotificationRegistrationService>();
            GoogleCommand = new RelayCommand(async () => await OnAuthenticate("Google"));
            FacebookCommand = new RelayCommand(async () => await OnAuthenticate("Facebook"));
            AppleCommand = new RelayCommand(async () => await OnAuthenticate("Apple"));
        }

        void ShowAlert(string message)
        {
            var okText = TranslateExtension.Translate("LabelOK");

            MainThread.BeginInvokeOnMainThread(() =>
            {
                App.Current.MainPage.DisplayAlert("SOSpect", message, okText).ContinueWith((task) =>
                {
                    if (task.IsFaulted) throw task.Exception;
                });
            });
        }

        async Task OnAuthenticate(string scheme)
        {
            IsRunning = true;
            try
            {
                WebAuthenticatorResult r = null;

                var authUrl = new Uri(authenticationUrl + scheme);
                var callbackUrl = new Uri("sospect://");

                r = await WebAuthenticator.AuthenticateAsync(authUrl, callbackUrl);

                var accessToken = r?.AccessToken;

                r.Properties.TryGetValue("email", out var email);
                r.Properties.TryGetValue("NameIdentifier", out var sid);
                r.Properties.TryGetValue("access_token", out var access_token);

                await _notificationRegistrationService.RegisterDeviceAsync(new string[1] { sid });

                Persona persona = new Persona()
                {
                    login = email,
                    marca_bloqueo = 0,
                    user_id_thirdparty = sid,
                    Plataforma = Device.RuntimePlatform,
                    //RegistrationId = "dshajdhasjkldh",
                    RegistrationId = App.TokenHubNotification,
                    Idioma = IdiomUtil.ObtenerCodigoDeIdioma()
                };

                App.persona = persona;

                ResponseMessage response = await ApiService.RegisterUser(persona);

                if (response.IsSuccess)
                {
                    Preferences.Set("User", JsonConvert.SerializeObject(persona));
                    Preferences.Set("access_token", access_token);
                    Application.Current.MainPage = new NavigationPage(new SospectTabs()) { BarBackgroundColor = Color.Black };
                }
                else
                {
                    var okText = TranslateExtension.Translate("LabelOK");
                    var ErrorText = TranslateExtension.Translate("LabelError");
                    var mensajeSalida = "";
                    try
                    {
                        mensajeSalida = response.Message == null ? null : TranslateExtension.Translate(response.Message.Replace(" ", ""));
                    }
                    catch (Exception)
                    {
                        mensajeSalida = response.Message;
                    }
                    await Application.Current.MainPage.DisplayAlert(ErrorText, mensajeSalida, okText);
                }
                IsRunning = false;
            }

            catch (OperationCanceledException exOCE)
            {
                var okText = TranslateExtension.Translate("LabelOK");
                var CanceladoText = TranslateExtension.Translate("LabelCancelado");
                var LoginCanceladoText = TranslateExtension.Translate("LabelLoginCancelado");
                IsRunning = false;
                Console.WriteLine("Login canceled.");
                await App.Current.MainPage.DisplayAlert(CanceladoText, LoginCanceladoText, okText);
                var properties = new Dictionary<string, string> {
                        { "Object", "LoginViewModel" },
                        { "Method", "OnAuthenticate-OperationCanceledException" }
                    };
                Microsoft.AppCenter.Crashes.Crashes.TrackError(exOCE, properties);
            }
            catch (Exception ex)
            {
                var FalloText = TranslateExtension.Translate("LabelFallo");
                var okText = TranslateExtension.Translate("LabelOK");
                IsRunning = false;
                Console.WriteLine($"{ex.Message}");
                await App.Current.MainPage.DisplayAlert(FalloText, $"{ex.Message}", okText);
                var properties = new Dictionary<string, string> {
                        { "Object", "LoginViewModel" },
                        { "Method", "OnAuthenticate" }
                    };
                Microsoft.AppCenter.Crashes.Crashes.TrackError(ex, properties);

                if (Device.RuntimePlatform == Device.iOS)
                {
                    var settingsService = Xamarin.Forms.DependencyService.Get<ISettingsService>();
                    settingsService?.OpenSettings();
                    App.justCheckedNotificationPermissions = true;
                }

            }
            finally
            {
                IsRunning = false;
            }
        }
    }
}

