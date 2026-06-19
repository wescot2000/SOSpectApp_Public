// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Storage;
using Newtonsoft.Json;
#if ANDROID
using Plugin.FirebasePushNotifications;
#endif
using sospect.AuthHelpers;
using sospect.Helpers;
using sospect.Interfaces;
using sospect.Models;
using sospect.Services;
using sospect.Views;
using sospect.Views.Popups;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;

namespace sospect.ViewModels
{

    public class MenuPageViewModel : BaseViewModel
    {
        public static event EventHandler DatosActualizados;

        private string _userId;
        public string userId
        {
            get => this._userId;
            set => this.SetValue(ref this._userId, value);
        }

        private string _loginUsuario;
        public string LoginUsuario
        {
            get => this._loginUsuario;
            set => this.SetValue(ref this._loginUsuario, value);
        }

        private string _saldoPoderes;
        public string SaldoPoderes
        {
            get => this._saldoPoderes;
            set => this.SetValue(ref this._saldoPoderes, value);
        }

        private string _credibilidadActual;
        public string CredibilidadActual
        {
            get => this._credibilidadActual;
            set => this.SetValue(ref this._credibilidadActual, value);
        }

        private string _textoFlagRedConfianza;
        public string TextoFlagRedConfianza
        {
            get => this._textoFlagRedConfianza;
            set => this.SetValue(ref this._textoFlagRedConfianza, value);
        }

        private string _MarcaBloqueo;
        public string MarcaBloqueo
        {
            get => this._MarcaBloqueo;
            set => this.SetValue(ref this._MarcaBloqueo, value);
        }

        private string _radioActual;
        public string RadioActual
        {
            get => this._radioActual;
            set => this.SetValue(ref this._radioActual, value);
        }

        private bool _flagRedConfianza;
        public bool FlagRedConfianza
        {
            get => this._flagRedConfianza;
            set => this.SetValue(ref this._flagRedConfianza, value);
        }

        public ICommand SuperpoderesCommand { get; }
        public ICommand RadioAlertasCommand { get; }
        public ICommand CrearZonasCommand { get; }
        public ICommand ProtegidosCommand { get; }
        public ICommand RenovarSubscrCommand { get; }
        public ICommand TerminosCondicionesCommand { get; }
        public ICommand AcercaDeCommand { get; }
        public ICommand ManualSospectCommand { get; }
        public ICommand BorraYCierraSesionCommand { get; }
        public ICommand CerrarSesionCommand { get; }
        public ICommand ReportesCommand { get; }
        public ICommand NotifCommand { get; }
        public ICommand CopyUserIdCommand { get; }
        public ICommand HistorialCommand { get; }
        public ICommand TrustedNetworkCommand { get; }

        public MenuPageViewModel(INavigation navigation)
        {
            InitializeAsync(navigation);
            ActualizarDatos(this, EventArgs.Empty);
            MessagingCenter.Subscribe<AlarmRadiusViewModel>(this, "DatosActualizados", (sender) =>
            {
                ActualizarDatos(this, EventArgs.Empty);
            });
            MessagingCenter.Subscribe<SubscriptionsPageViewModel>(this, "DatosActualizados", (sender) =>
            {
                ActualizarDatos(this, EventArgs.Empty);
            });

            SuperpoderesCommand = new Command(() => SuperpoderesClicked());
            ProtegidosCommand = new Command(() => ProtegidosClicked());
            CrearZonasCommand = new Command(async () => await CrearZonasClicked(navigation));
            RadioAlertasCommand = new Command(async () => await RadioAlertasClicked(navigation));
            RenovarSubscrCommand = new Command(async () => await RenovarSubscrOptions(navigation));
            TerminosCondicionesCommand = new Command(() => TerminosyCondicionesClicked(navigation));
            AcercaDeCommand = new Command(async () => await AcercaDeClicked(navigation));
            ManualSospectCommand = new Command(async () => await ManualSospectClicked(navigation));
            BorraYCierraSesionCommand = new Command(() => BorrayCierraSesionClicked());
            CerrarSesionCommand = new Command(async () => await CerrarSesionClicked(navigation));
            ReportesCommand = new Command(async () => await ReportesClicked(navigation));
            NotifCommand = new Command(async () => await NotifClicked(navigation));
            CopyUserIdCommand = new Command(CopyUserIdToClipboard);
            HistorialCommand = new Command(() => HistorialClicked(navigation));
            TrustedNetworkCommand = new Command(() => TrustedNetworkClicked(navigation));
        }

        public void ActualizarDatos(object sender, EventArgs e)
        {
            try
            {
                ParametrosUsuario parametros = JsonConvert.DeserializeObject<ParametrosUsuario>(Preferences.Get("ParametrosUsuario", ""));
                var LblUsuarioID = TranslateExtension.Translate("LblUsuarioID");
                var LblMiLogin = TranslateExtension.Translate("LblMiLogin");
                var LblSaldoPoderesEs = TranslateExtension.Translate("LblSaldoPoderesEs");
                var LblMarcasNegativas = TranslateExtension.Translate("LblMarcasNegativas");
                var LblRadioActual = TranslateExtension.Translate("LblRadioActual");
                var LblCredibilidadActual = TranslateExtension.Translate("LblCredibilidadActual");
                var LblUsuarioRedConfianza = TranslateExtension.Translate("LabelPertenecesRedConfianza");

                userId = App.persona.user_id_thirdparty;
                LoginUsuario = $"{LblMiLogin} " + App.persona.login;
                SaldoPoderes = $"{LblSaldoPoderesEs} {parametros.SaldoPoderes}";
                MarcaBloqueo = $"{LblMarcasNegativas} {parametros.MarcaBloqueo}";
                RadioActual = $"{LblRadioActual} {parametros.radio_alarmas_mts_actual}";
                CredibilidadActual = $"{LblCredibilidadActual} {parametros.credibilidad_persona}%";
                FlagRedConfianza = parametros.flag_red_confianza;

                if (FlagRedConfianza)
                {
                    TextoFlagRedConfianza = LblUsuarioRedConfianza;
                }
                else
                {
                    TextoFlagRedConfianza = string.Empty;
                }
            }
            catch (Exception ex)
            {
                ParametrosUsuario parametros = JsonConvert.DeserializeObject<ParametrosUsuario>(Preferences.Get("ParametrosUsuario", ""));
                CrashlyticsHelper.LogError(ex, "MenuPageViewModel", "ActualizarDatos");
            }
        }

        private async Task InitializeAsync(INavigation navigation)
        {
            try
            {
                await HomeViewModel.InicializarParametrosUsuarioAsync();
                DatosActualizados?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                ParametrosUsuario parametros = JsonConvert.DeserializeObject<ParametrosUsuario>(Preferences.Get("ParametrosUsuario", ""));
                CrashlyticsHelper.LogError(ex, "MenuPageViewModel", "InitializeAsync");
            }
        }

        private async Task TrustedNetworkClicked(INavigation navigation)
        {
            var TrustedNetwork = new TrustedNetwork();
            await navigation.PushAsync(TrustedNetwork);
        }

        private async Task HistorialClicked(INavigation navigation)
        {
            var HistorialPage = new HistorialPage();
            await navigation.PushAsync(HistorialPage);
        }

        private async void SuperpoderesClicked()
        {
            try
            {
                var superPowersPopupPage = new SuperPowersPopupPage();
                await Application.Current.MainPage.ShowPopupAsync(superPowersPopupPage);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en SuperpoderesClicked: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "MenuPageViewModel", "SuperpoderesClicked");

                var errorTitle = await TranslateExtension.TranslateAsync("LabelError") ?? "Error";
                var errorMessage = await TranslateExtension.TranslateAsync("LabelErrorReiniciar") ?? "Surgió un error inesperado. Por favor reinicia SOSpect. Si persiste el error contacta a soporte@wescot.com.co";

                await ModernAlerts.ShowError(errorTitle, errorMessage);

            }
        }

        private async void CopyUserIdToClipboard()
        {
            var LabelOK = await TranslateExtension.TranslateAsync("LabelOK");
            var LblCopiado = await TranslateExtension.TranslateAsync("LblCopiado");
            var LblUsuarioCopiado = await TranslateExtension.TranslateAsync("LblUsuarioCopiado");

            if (!string.IsNullOrEmpty(userId))
            {
                await Clipboard.SetTextAsync(userId);
                await ModernAlerts.ShowSuccess(LblCopiado, LblUsuarioCopiado);
            }
        }

        private async Task RadioAlertasClicked(INavigation navigation)
        {
            var AlarmRadiusPage = new AlarmRadiusPage();
            await navigation.PushAsync(AlarmRadiusPage);
        }

        private async Task CrearZonasClicked(INavigation navigation)
        {
#if ANDROID
            await navigation.PushAsync(new ZoneSubscriptionPageAndroid());
#else
            await navigation.PushAsync(new ZoneSubscriptionPage());
#endif
        }

        private async Task RenovarSubscrOptions(INavigation navigation)
        {
            var SubscriptionsPage = new SubscriptionsPage();
            await navigation.PushAsync(SubscriptionsPage);
        }

        private async void ProtegidosClicked()
        {
            try
            {
                var protegidosPopup = new ProtegidosPopupPage();
                await Application.Current.MainPage.ShowPopupAsync(protegidosPopup);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en ProtegidosClicked: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "MenuPageViewModel", "ProtegidosClicked");

                var errorTitle = await TranslateExtension.TranslateAsync("LabelError") ?? "Error";
                var errorMessage = await TranslateExtension.TranslateAsync("LabelErrorReiniciar") ?? "Surgió un error inesperado. Por favor reinicia SOSpect. Si persiste el error contacta a soporte@wescot.com.co";

                await ModernAlerts.ShowError(errorTitle, errorMessage);
            }
        }

        private async Task TerminosyCondicionesClicked(INavigation navigation)
        {
            var TermsAndConditionsPage = new TermsAndConditionsPage();
            await navigation.PushAsync(TermsAndConditionsPage);
        }

        private async Task ManualSospectClicked(INavigation navigation)
        {
            var ManualDeUsuarioPage = new ManualDeUsuarioPage();
            await navigation.PushAsync(ManualDeUsuarioPage);
        }

        private async Task AcercaDeClicked(INavigation navigation)
        {
            var AboutPage = new AboutPage();
            await navigation.PushAsync(AboutPage);
        }

        private async Task ReportesClicked(INavigation navigation)
        {
            var ReportsPage = new ReportsPage();
            await navigation.PushAsync(ReportsPage);
        }

        private async Task NotifClicked(INavigation navigation)
        {
            var ConfiguracionNotificacionesPage = new ConfiguracionNotificacionesPage();
            await navigation.PushAsync(ConfiguracionNotificacionesPage);
        }

        private async void BorrayCierraSesionClicked()
        {
            try
            {
                var confirmarWipeData = new Views.Popups.WipeDataPopup();
                await Application.Current.MainPage.ShowPopupAsync(confirmarWipeData);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en BorrayCierraSesionClicked: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "MenuPageViewModel", "BorrayCierraSesionClicked");

                var errorTitle = await TranslateExtension.TranslateAsync("LabelError") ?? "Error";
                var errorMessage = await TranslateExtension.TranslateAsync("LabelErrorReiniciar") ?? "Surgió un error inesperado. Por favor reinicia SOSpect. Si persiste el error contacta a soporte@wescot.com.co";

                await ModernAlerts.ShowError(errorTitle, errorMessage);
            }
        }

        private async Task CerrarSesionClicked(INavigation navigation)
        {
            var SeguroCerrarSesion = await TranslateExtension.TranslateAsync("SeguroCerrarSesion");
            var LabelSi = await TranslateExtension.TranslateAsync("LabelSi");
            var LabelNo = await TranslateExtension.TranslateAsync("LabelNo");
            var OptCerrarSesion = await TranslateExtension.TranslateAsync("OptCerrarSesion") ?? "Cerrar Sesión";

            // Con título personalizado
            var confirmarCierre = await ModernAlerts.ShowConfirmation(OptCerrarSesion, SeguroCerrarSesion, LabelSi, LabelNo, false);

            if (confirmarCierre)
            {
                await SecureStorage.SetAsync("access_token", "");
                await SecureStorage.SetAsync("access_token", string.Empty);
                Preferences.Set("alarma_id", "");
                Preferences.Set("HasSeenTutorial", false);
                Preferences.Set("ParametrosUsuario", "");
                Preferences.Set("User", "");
                #if ANDROID
                await IFirebasePushNotification.Current.UnregisterForPushNotificationsAsync();
                #endif
                var MainPage = new NavigationPage(new LoginPage()) { BarBackgroundColor = Colors.Black };
                App.Current.MainPage = MainPage;
            }
        }

        ~MenuPageViewModel()
        {
            MessagingCenter.Unsubscribe<AlarmRadiusViewModel>(this, "DatosActualizados");
            MessagingCenter.Unsubscribe<SubscriptionsPageViewModel>(this, "DatosActualizados");
        }
    }
}

