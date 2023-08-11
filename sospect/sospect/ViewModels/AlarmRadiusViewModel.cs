using System;
using System.ComponentModel;
using System.Net.Http;
using System.Text;
using System.Windows.Input;
using Newtonsoft.Json;
using Xamarin.Forms;
using sospect.Helpers;
using sospect.Models;
using System.Collections.ObjectModel;
using sospect.Services;
using System.Linq;
using sospect.Utils;
using System.Threading.Tasks;
using Xamarin.Essentials;
using sospect.Views;

namespace sospect.ViewModels
{

    public class AlarmRadiusViewModel : BaseViewModel
    {
        private int _radioActual;
        public int RadioActual
        {
            get => this._radioActual;
            set => this.SetValue(ref this._radioActual, value);
        }

        private int _poderesActualesUsuario;
        public int PoderesActualesUsuario
        {
            get => this._poderesActualesUsuario;
            set => this.SetValue(ref this._poderesActualesUsuario, value);
        }
        private string _saldoPoderes;
        public string SaldoPoderes
        {
            get => this._saldoPoderes;
            set => this.SetValue(ref this._saldoPoderes, value);
        }

        private RadiosDisponiblesResponse _newRadius;
        private ObservableCollection<RadiosDisponiblesResponse> _availableRadios;

        public AlarmRadiusViewModel(INavigation navigation)
        {
            var LblSaldoPoderesEs = TranslateExtension.Translate("LblSaldoPoderesEs");
            ParametrosUsuario parametros = JsonConvert.DeserializeObject<ParametrosUsuario>(Preferences.Get("ParametrosUsuario", ""));

            RadioActual = parametros.radio_alarmas_mts_actual;
            SaldoPoderes = $"{LblSaldoPoderesEs} {parametros.SaldoPoderes}";
            PoderesActualesUsuario = parametros.SaldoPoderes;

            _ = Task.Run(() => LoadAvailableRadios());
            CreateSubscriptionCommand = new Command(CreateSubscription);
        }

        public RadiosDisponiblesResponse NewRadius
        {
            get => _newRadius;
            set
            {
                _newRadius = value;
                OnPropertyChanged(nameof(NewRadius));
                OnPropertyChanged(nameof(RequiredPowersLabel));
            }
        }

        public ObservableCollection<RadiosDisponiblesResponse> AvailableRadios
        {
            get => _availableRadios;
            set
            {
                _availableRadios = value;
                OnPropertyChanged(nameof(AvailableRadios));
            }
        }

        private string _requiredPowersLabel;
        public string RequiredPowersLabel
        {
            get => _requiredPowersLabel;
            set
            {
                _requiredPowersLabel = value;
                OnPropertyChanged(nameof(RequiredPowersLabel));
            }
        }

        public ICommand CreateSubscriptionCommand { get; }

        private async void LoadAvailableRadios()
        {
            IsRunning = true;
            var radios = await ApiService.ObtenerRadiosDisponibles();
            IsRunning = false;

            AvailableRadios = new ObservableCollection<RadiosDisponiblesResponse>(radios);

            if (radios.Count > 0)
            {
                NewRadius = radios.FirstOrDefault();
            }
        }

        private async void CreateSubscription()
        {
            int requiredPowers = CalculateRequiredPowers(NewRadius);
            int roundedRadius = (int)Math.Ceiling(NewRadius.radio_mts / 100.0) * 100;
            var saldoPoderesInsuficiente = TranslateExtension.Translate("LblSaldoPoderesInsuficiente");
            var comprarPoderes = TranslateExtension.Translate("LblComprarPoderes");
            var cancelar = TranslateExtension.Translate("LabelCancelar");

            if (PoderesActualesUsuario < requiredPowers)
            {
                var answer = await Application.Current.MainPage.DisplayAlert(saldoPoderesInsuficiente, "", comprarPoderes, cancelar);
                if (answer)
                {
                    await Application.Current.MainPage.Navigation.PushAsync(new PurchaseSuperPowersPage());
                }
                return;
            }

            var requestData = new NuevoRadioRequest
            {
                p_user_id_thirdparty_protector = App.persona.user_id_thirdparty,
                cantidad_subscripcion = roundedRadius,
                idioma = IdiomUtil.ObtenerCodigoDeIdioma()
            };

            IsRunning = true;
            var response = await ApiService.SubscribirNuevoRadio(requestData);
            IsRunning = false;

            var exito = TranslateExtension.Translate("Exito");
            var subscreada = TranslateExtension.Translate("SubscripcionCreada");
            var LabelOk = TranslateExtension.Translate("LabelOK");
            var labelerror = TranslateExtension.Translate("LabelError");
            var MensajeError = TranslateExtension.Translate("MensajeError");


            if (response.IsSuccess)
            {
                await Application.Current.MainPage.DisplayAlert(exito, subscreada, LabelOk);
                MessagingCenter.Send(this, "DatosActualizados");
                await Application.Current.MainPage.Navigation.PopToRootAsync();
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert(labelerror, MensajeError, LabelOk);
            }
        }

        public int CalculateRequiredPowers(RadiosDisponiblesResponse newRadius)
        {
            return (int)Math.Ceiling((newRadius.radio_mts - RadioActual) / 100.0) * 40;
        }
    }
}
