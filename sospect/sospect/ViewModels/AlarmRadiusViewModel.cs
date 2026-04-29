using System;
using System.ComponentModel;
using System.Net.Http;
using System.Text;
using System.Windows.Input;
using Newtonsoft.Json;
using Microsoft.Maui.Controls;
using sospect.Helpers;
using sospect.Models;
using System.Collections.ObjectModel;
using sospect.Services;
using System.Linq;
using sospect.Utils;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using sospect.Views;
using System.Collections.Generic;
using sospect.Interfaces;

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
            try
            {
                var radios = await ApiService.ObtenerRadiosDisponibles();
                AvailableRadios = new ObservableCollection<RadiosDisponiblesResponse>(radios);

                if (radios.Count > 0)
                {
                    NewRadius = radios.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "AlarmRadiusViewModel", "LoadAvailableRadios");
            }
            finally
            {
                IsRunning = false;
            }           
        }

        /// <summary>
        /// Obtiene la navegación del tab activo cuando MainPage es TabbedPage.
        /// Necesario porque esta página se pushea en el NavigationPage del tab,
        /// no en App.Current.MainPage.Navigation.
        /// </summary>
        private INavigation GetTabNavigation()
        {
            if (Application.Current.MainPage is TabbedPage tabbedPage &&
                tabbedPage.CurrentPage is NavigationPage navPage)
                return navPage.Navigation;
            return Application.Current.MainPage.Navigation;
        }

        private async void CreateSubscription()
        {
            int requiredPowers = CalculateRequiredPowers(NewRadius);
            int roundedRadius = (int)Math.Ceiling(NewRadius.radio_mts / 100.0) * 100;
            var saldoPoderesInsuficiente = await TranslateExtension.TranslateAsync("LblSaldoPoderesInsuficiente");
            var comprarPoderes = await TranslateExtension.TranslateAsync("LblComprarPoderes");
            var cancelar = await TranslateExtension.TranslateAsync("LabelCancelar");

            if (PoderesActualesUsuario < requiredPowers)
            {
                var answer = await ModernAlerts.ShowConfirmation(saldoPoderesInsuficiente, "", comprarPoderes, cancelar, false);
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

            ParametrosUsuario parametros = JsonConvert.DeserializeObject<ParametrosUsuario>(Preferences.Get("ParametrosUsuario", ""));

            IsRunning = true;
            bool navigatedAway = false;
            try
            {
                var exito = await TranslateExtension.TranslateAsync("Exito");
                var subscreada = await TranslateExtension.TranslateAsync("SubscripcionCreada");
                var LabelError = await TranslateExtension.TranslateAsync("LabelError");

                var response = await ApiService.SubscribirNuevoRadio(requestData);
                if (response.IsSuccess)
                {
                    parametros.radio_alarmas_mts_actual = roundedRadius;
                    Preferences.Set("ParametrosUsuario", JsonConvert.SerializeObject(parametros));
                    await ModernAlerts.ShowSuccess(exito, subscreada);
                    _ = HomeViewModel.RefrescarParametrosAsync();
                    MessagingCenter.Send(this, "DatosActualizados");
                    navigatedAway = true;
                    await GetTabNavigation().PopToRootAsync();
                }
                else
                {
                    await ModernAlerts.ShowError(LabelError, response.Message);
                }
            }
            catch (Exception ex)
            {
                if (!navigatedAway)
                {
                    var LabelInformacion = await TranslateExtension.TranslateAsync("LabelInformacion");
                    var MensajeError = await TranslateExtension.TranslateAsync("MensajeError");
                    await ModernAlerts.ShowWarning(LabelInformacion, MensajeError);
                }
                CrashlyticsHelper.LogError(ex, "AlarmRadiusViewModel", "CreateSubscription");
            }
            finally
            {
                IsRunning = false;
            }
        }

        public int CalculateRequiredPowers(RadiosDisponiblesResponse newRadius)
        {
            return (int)Math.Ceiling((newRadius.radio_mts - RadioActual) / 100.0) * 40;
        }
    }
}
