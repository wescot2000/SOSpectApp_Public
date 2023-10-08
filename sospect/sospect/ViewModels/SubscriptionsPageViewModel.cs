using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using sospect.Models;
using Xamarin.Forms;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Windows.Input;
using sospect.Services;
using sospect.Utils;
using sospect.Helpers;
using System.Collections.Specialized;
using Xamarin.Essentials;
using sospect.Views;

namespace sospect.ViewModels
{
    public class SubscriptionsPageViewModel : BaseViewModel
    {
        private bool _isListEmpty;
        private ParametrosUsuario _parametros;

        public ObservableCollection<MisSubscripciones> Subscriptions { get; set; }

        public ICommand RenewSubscriptionCommand { get; }
        public ICommand CancelSubscriptionCommand { get; }
        public ICommand ShowCancelConfirmationCommand { get; }

        public bool IsListEmpty
        {
            get { return _isListEmpty; }
            set { SetProperty(ref _isListEmpty, value); }
        }
        public SubscriptionsPageViewModel()
        {
            _parametros = JsonConvert.DeserializeObject<ParametrosUsuario>(Preferences.Get("ParametrosUsuario", ""));
            Subscriptions = new ObservableCollection<MisSubscripciones>();
            _ = Task.Run(() => LoadSubscriptions());
            RenewSubscriptionCommand = new Command<MisSubscripciones>(RenewSubscription);
            CancelSubscriptionCommand = new Command<MisSubscripciones>(CancelSubscription);
            ShowCancelConfirmationCommand = new Command<MisSubscripciones>(ShowCancelConfirmation);
            Subscriptions.CollectionChanged += OnSubscriptionsChanged;
        }

        private void OnSubscriptionsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            IsListEmpty = Subscriptions.Count == 0;
        }
        private async Task LoadSubscriptions()
        {
            IsRunning = true;
            try
            {
                var misSubscripciones = await ApiService.ObtenerMisSubscripciones();

                foreach (var subscription in misSubscripciones)
                {
                    subscription.descripcion_tipo = TranslateExtension.Translate(subscription.descripcion_tipo?.Replace(" ", ""));
                    subscription.texto_renovable = TranslateExtension.Translate(subscription.texto_renovable?.Replace(" ", ""));
                    subscription.observ_subscripcion = subscription.observ_subscripcion == null ? null : TranslateExtension.Translate(subscription.observ_subscripcion.Replace(" ", ""));

                    Subscriptions.Add(subscription);
                    IsListEmpty = Subscriptions.Count == 0;
                }
            }
            catch (Exception ex)
            {
                var properties = new Dictionary<string, string> {
                        { "Object", "SubscriptionsPageViewModel" },
                        { "Method", "ObtenerMisSubscripciones" }
                    };
                Microsoft.AppCenter.Crashes.Crashes.TrackError(ex, properties);
            }
            finally
            {
                IsRunning = false;
            }
        }

        private async void ShowCancelConfirmation(MisSubscripciones subscription)
        {
            var LblCancelarSubscripcion = TranslateExtension.Translate("LblCancelarSubscripcion");
            var LblSeguroDeCancelar = TranslateExtension.Translate("LblSeguroDeCancelar");
            var LblAceptar = TranslateExtension.Translate("LblAceptar");
            var LabelCancelar = TranslateExtension.Translate("LabelCancelar");
            var answer = await Application.Current.MainPage.DisplayAlert(LblCancelarSubscripcion, LblSeguroDeCancelar, LblAceptar, LabelCancelar);
            if (answer)
            {
                CancelSubscription(subscription);
            }
        }
        private async void RenewSubscription(MisSubscripciones subscription)
        {
            var saldoPoderesInsuficiente = TranslateExtension.Translate("LblSaldoPoderesInsuficiente");
            var comprarPoderes = TranslateExtension.Translate("LblComprarPoderes");
            var cancelar = TranslateExtension.Translate("LabelCancelar");
            var LabelError = TranslateExtension.Translate("LabelError");
            var LblErrorRenovando = TranslateExtension.Translate("LblErrorRenovando");
            var LabelOK = TranslateExtension.Translate("LabelOK");
            var LblRenovacionAplicada = TranslateExtension.Translate("LblRenovacionAplicada");
            var LabelInformacion = TranslateExtension.Translate("LabelInformacion");
            var MensajeError = TranslateExtension.Translate("MensajeError");

            if (_parametros.SaldoPoderes < subscription.poderes_renovacion)
            {
                var answer = await Application.Current.MainPage.DisplayAlert(saldoPoderesInsuficiente, "", comprarPoderes, cancelar);
                if (answer)
                {
                    await Application.Current.MainPage.Navigation.PushAsync(new PurchaseSuperPowersPage());
                }
                return;
            }

            var seguroRenovar = TranslateExtension.Translate("LblSeguroRenovar");
            var si = TranslateExtension.Translate("LabelSi");
            var no = TranslateExtension.Translate("LabelNo");

            var answerRenovar = await Application.Current.MainPage.DisplayAlert(seguroRenovar, "", si, no);
            if (!answerRenovar)
            {
                return;
            }

            var renovarSubscripcionRequest = new RenovarSubscripcionRequest
            {
                subscripcion_id = subscription.subscripcion_id,
                user_id_thirdparty = App.persona.user_id_thirdparty,
                cantidad_poderes = subscription.poderes_renovacion,
                idioma = IdiomUtil.ObtenerCodigoDeIdioma()
            };

            IsRunning = true;
            try
            {
                var response = await ApiService.RenovarSubscripcion(renovarSubscripcionRequest);

                

                if (response.IsSuccess)
                {
                    await Application.Current.MainPage.DisplayAlert(LabelOK, LblRenovacionAplicada, LabelOK);
                    await Application.Current.MainPage.Navigation.PushAsync(new MenuPage());
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert(LabelError, LblErrorRenovando, LabelOK);
                }
            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert(LabelInformacion, MensajeError, LabelOK);
                var properties = new Dictionary<string, string> {
                        { "Object", "SubscriptionsPageViewModel" },
                        { "Method", "RenovarSubscripcion" }
                    };
                Microsoft.AppCenter.Crashes.Crashes.TrackError(ex, properties);
            }
            finally
            {
                IsRunning = false;
            }
            
        }

        public async void CancelSubscription(MisSubscripciones subscription)
        {
            var LblCancelarSubscripcion = TranslateExtension.Translate("LblCancelarSubscripcion");
            var LblSeguroDeCancelar = TranslateExtension.Translate("LblSeguroDeCancelar");
            var LblAceptar = TranslateExtension.Translate("LblAceptar");
            var LabelCancelar = TranslateExtension.Translate("LabelCancelar");
            var LabelError = TranslateExtension.Translate("LabelError");
            var LblErrorCancelando = TranslateExtension.Translate("LblErrorCancelando");
            var LabelOK = TranslateExtension.Translate("LabelOK");
            var LblCancelacionAplicada = TranslateExtension.Translate("LblCancelacionAplicada");
            var LabelInformacion = TranslateExtension.Translate("LabelInformacion");
            var MensajeError = TranslateExtension.Translate("MensajeError");

            var answer = await Application.Current.MainPage.DisplayAlert(LblCancelarSubscripcion, LblSeguroDeCancelar, LblAceptar, LabelCancelar);

            if (!answer)
            {
                return;
            }
            var cancelarSubscripcionRequest = new CancelarSubscripcionRequest
            {
                subscripcion_id = subscription.subscripcion_id,
                user_id_thirdparty = App.persona.user_id_thirdparty,
                idioma = IdiomUtil.ObtenerCodigoDeIdioma()
            };

            IsRunning = true;
            try
            {
                var response = await ApiService.CancelarSubscripcion(cancelarSubscripcionRequest);

                if (response.IsSuccess)
                {
                    await Application.Current.MainPage.DisplayAlert(LabelOK, LblCancelacionAplicada, LabelOK);
                    //await Application.Current.MainPage.Navigation.PushAsync(new HomePage());
                    Subscriptions.Remove(subscription);
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert(LabelError, LblErrorCancelando, LabelOK);
                }
            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert(LabelInformacion, MensajeError, LabelOK);
                var properties = new Dictionary<string, string> {
                        { "Object", "SubscriptionsPageViewModel" },
                        { "Method", "CancelarSubscripcion" }
                    };
                Microsoft.AppCenter.Crashes.Crashes.TrackError(ex, properties);
            }
            finally
            {
                IsRunning = false;
            }
            
        }

    }
}
