using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using Newtonsoft.Json;
using sospect.AuthHelpers;
using sospect.Helpers;
using sospect.Models;
using sospect.Popups;
using sospect.Views;
using Xamarin.Essentials;
using Xamarin.Forms;

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

        public ICommand SuperpoderesCommand { get; }
        public ICommand RadioAlertasCommand { get; }
        public ICommand CrearZonasCommand { get; }
        public ICommand ProtegidosCommand { get; }
        public ICommand RenovarSubscrCommand { get; }
        public ICommand TerminosCondicionesCommand { get; }
        public ICommand AcercaDeCommand { get; }
        public ICommand ManualSospectCommand { get; }
        public ICommand CerrarSesionCommand { get; }
        public ICommand ReportesCommand { get; }

        public MenuPageViewModel(INavigation navigation)
        {
            InitializeAsync(navigation);
            ActualizarDatos(this, EventArgs.Empty);
            MessagingCenter.Subscribe<AlarmRadiusViewModel>(this, "DatosActualizados", (sender) =>
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
            CerrarSesionCommand = new Command(async () => await CerrarSesionClicked(navigation));
            ReportesCommand = new Command(async () => await ReportesClicked(navigation));
        }

        public void ActualizarDatos(object sender, EventArgs e)
        {
            ParametrosUsuario parametros = JsonConvert.DeserializeObject<ParametrosUsuario>(Preferences.Get("ParametrosUsuario", ""));
            var LblUsuarioID = TranslateExtension.Translate("LblUsuarioID");
            var LblMiLogin = TranslateExtension.Translate("LblMiLogin");
            var LblSaldoPoderesEs = TranslateExtension.Translate("LblSaldoPoderesEs");
            var LblMarcasNegativas = TranslateExtension.Translate("LblMarcasNegativas");
            var LblRadioActual = TranslateExtension.Translate("LblRadioActual");
            var LblCredibilidadActual = TranslateExtension.Translate("LblCredibilidadActual");

            userId = $"{LblUsuarioID}: " + App.persona.user_id_thirdparty;
            LoginUsuario = $"{LblMiLogin} " + App.persona.login;
            SaldoPoderes = $"{LblSaldoPoderesEs} {parametros.SaldoPoderes}";
            MarcaBloqueo = $"{LblMarcasNegativas} {parametros.MarcaBloqueo}";
            RadioActual = $"{LblRadioActual} {parametros.radio_alarmas_mts_actual}";
            CredibilidadActual = $"{LblCredibilidadActual} {parametros.credibilidad_persona}%";
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
                var properties = new Dictionary<string, string> {
                        { "Object", "MenuPageViewModel" },
                        { "Method", "InicializarParametrosUsuarioAsync" }
                    };
                Microsoft.AppCenter.Crashes.Crashes.TrackError(ex, properties);
            }
        }

        private async void SuperpoderesClicked()
        {
            var superPowersPopupPage = new SuperPowersPopupPage();
            await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PushAsync(superPowersPopupPage);
        }

        private async Task RadioAlertasClicked(INavigation navigation)
        {
            var AlarmRadiusPage = new AlarmRadiusPage();
            await navigation.PushAsync(AlarmRadiusPage);
        }

        private async Task CrearZonasClicked(INavigation navigation)
        {
            var zoneSubscriptionPage = new ZoneSubscriptionPage();
            await navigation.PushAsync(zoneSubscriptionPage);
        }

        private async Task RenovarSubscrOptions(INavigation navigation)
        {
            var SubscriptionsPage = new SubscriptionsPage();
            await navigation.PushAsync(SubscriptionsPage);
        }


        private async void ProtegidosClicked()
        {
            var ProtegidosPopupPage = new ProtegidosPopupPage();
            await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PushAsync(ProtegidosPopupPage);
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

        private async Task CerrarSesionClicked(INavigation navigation)
        {
            var SeguroCerrarSesion = TranslateExtension.Translate("SeguroCerrarSesion");
            var LabelSi = TranslateExtension.Translate("LabelSi");
            var LabelNo = TranslateExtension.Translate("LabelNo");
            var action = await App.Current.MainPage.DisplayActionSheet(SeguroCerrarSesion, null, null, LabelSi, LabelNo);

            switch (action)
            {
                case var response when response == LabelSi:
                    Preferences.Set("access_token", "");
                    Preferences.Set("alarma_id", "");
                    Preferences.Set("HasSeenTutorial", false);
                    Preferences.Set("ParametrosUsuario", "");
                    Preferences.Set("User", "");

                    var MainPage = new NavigationPage(new LoginPage()) { BarBackgroundColor = Color.Black };
                    App.Current.MainPage = MainPage;
                    break;
                case var response when response == LabelNo:
                    break;
                default:
                    break;
            }
        }

        ~MenuPageViewModel()
        {
            MessagingCenter.Unsubscribe<AlarmRadiusViewModel>(this, "DatosActualizados");
        }

    }
}

