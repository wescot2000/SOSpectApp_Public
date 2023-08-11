using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Input;
using Newtonsoft.Json;
using sospect.Helpers;
using sospect.Models;
using sospect.Services;
using sospect.Utils;
using sospect.Views;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace sospect.ViewModels
{
    public class TermsAndConditionsViewModel : BaseViewModel
    {
        private string _termsAndConditionsText;
        public string TermsAndConditionsText
        {
            get => _termsAndConditionsText;
            set => SetProperty(ref _termsAndConditionsText, value);
        }

        private bool _flagUsuarioDebeFirmar;
        public bool FlagUsuarioDebeFirmar
        {
            get => this._flagUsuarioDebeFirmar;
            set => this.SetValue(ref this._flagUsuarioDebeFirmar, value);
        }

        public ICommand AcceptTermsCommand { get; }
        public ICommand DeclineTermsCommand { get; }

        public TermsAndConditionsViewModel()
        {
            ParametrosUsuario parametros = JsonConvert.DeserializeObject<ParametrosUsuario>(Preferences.Get("ParametrosUsuario", ""));

            FlagUsuarioDebeFirmar = parametros.FlagUsuarioDebeFirmarCto;
            AcceptTermsCommand = new Command(AcceptTerms);
            DeclineTermsCommand = new Command(DeclineTerms);
            Task.Run(() => LoadTermsAndConditionsAsync());
        }

        private async void LoadTermsAndConditionsAsync()
        {
            IsRunning = true;
            TermsAndConditionsText = await ApiService.ObtenerContratoUsuario();
            IsRunning = false;
        }

        private async void AcceptTerms()
        {
            var LblExitoEnAceptacion = TranslateExtension.Translate("LblExitoEnAceptacion");
            var LblFalloEnAceptacion = TranslateExtension.Translate("LblFalloEnAceptacion");
            var LabelInformacion = TranslateExtension.Translate("LabelInformacion");
            var LabelError = TranslateExtension.Translate("LabelError");
            var LabelOK = TranslateExtension.Translate("LabelOK");

            IsRunning = true;

            var response = await ApiService.AceptarContratoDeUsuario(new AcceptContractRequest()
            {
                PIpAceptacion = InternetUtil.GetPublicIpAddress(),
                PUserIdThirdparty = App.persona.user_id_thirdparty
            });

            IsRunning = false;

            if (response.IsSuccess)
            {
                await App.Current.MainPage.DisplayAlert(LabelInformacion, LblExitoEnAceptacion, LabelOK);
                App.Current.MainPage = new NavigationPage(new SospectTabs()) { BarBackgroundColor = Color.Black };
            }
            else
            {
                await App.Current.MainPage.DisplayAlert(LabelError, LblFalloEnAceptacion, LabelOK);
            }
        }

        private async void DeclineTerms()
        {
            var LblDeclinacionAceptacion = TranslateExtension.Translate("LblDeclinacionAceptacion");
            var LabelInformacion = TranslateExtension.Translate("LabelInformacion");
            var LabelOK = TranslateExtension.Translate("LabelOK");
            await App.Current.MainPage.DisplayAlert(LabelInformacion, LblDeclinacionAceptacion, LabelOK);
            System.Diagnostics.Process.GetCurrentProcess().CloseMainWindow();
        }
    }
}
