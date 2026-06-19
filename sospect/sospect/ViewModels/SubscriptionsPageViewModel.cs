// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using sospect.Models;
using Microsoft.Maui.Controls;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Windows.Input;
using sospect.Services;
using sospect.Utils;
using sospect.Helpers;
using System.Collections.Specialized;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using sospect.Views;
using sospect.Interfaces;

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
        public ICommand VerDetalleSubscripcionCommand { get; }

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
            VerDetalleSubscripcionCommand = new Command<MisSubscripciones>(VerDetalleSubscripcion);
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
                    
                }

                IsListEmpty = Subscriptions.Count == 0;
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "SubscriptionsPageViewModel", "LoadSubscriptions");
            }
            finally
            {
                IsRunning = false;
            }
        }

        private async void ShowCancelConfirmation(MisSubscripciones subscription)
        {
            var LblCancelarSubscripcion = await TranslateExtension.TranslateAsync("LblCancelarSubscripcion");
            var LblSeguroDeCancelar = await TranslateExtension.TranslateAsync("LblSeguroDeCancelar");
            var LblAceptar = await TranslateExtension.TranslateAsync("LblAceptar");
            var LabelCancelar = await TranslateExtension.TranslateAsync("LabelCancelar");
            var answer = await ModernAlerts.ShowConfirmation(LblCancelarSubscripcion, LblSeguroDeCancelar, LblAceptar, LabelCancelar, false);
            if (answer)
            {
                CancelSubscription(subscription);
            }
        }
        private async void RenewSubscription(MisSubscripciones subscription)
        {
            var saldoPoderesInsuficiente = await TranslateExtension.TranslateAsync("LblSaldoPoderesInsuficiente");
            var comprarPoderes = await TranslateExtension.TranslateAsync("LblComprarPoderes");
            var cancelar = await TranslateExtension.TranslateAsync("LabelCancelar");
            var LabelError = await TranslateExtension.TranslateAsync("LabelError");
            var LblErrorRenovando = await TranslateExtension.TranslateAsync("LblErrorRenovando");
            var LabelOK = await TranslateExtension.TranslateAsync("LabelOK");
            var LblRenovacionAplicada = await TranslateExtension.TranslateAsync("LblRenovacionAplicada");
            var LabelInformacion = await TranslateExtension.TranslateAsync("LabelInformacion");
            var MensajeError = await TranslateExtension.TranslateAsync("MensajeError");

            if (_parametros.SaldoPoderes < subscription.poderes_renovacion)
            {
                var answer = await ModernAlerts.ShowConfirmation(saldoPoderesInsuficiente, "", comprarPoderes, cancelar, false);
                if (answer)
                {
                    var navigation = GetCurrentNavigation();
                    if (navigation != null)
                    {
                        await navigation.PushAsync(new PurchaseSuperPowersPage());
                    }
                }
                return;
            }

            var seguroRenovar = TranslateExtension.Translate("LblSeguroRenovar");
            var si = await TranslateExtension.TranslateAsync("LabelSi");
            var no = await TranslateExtension.TranslateAsync("LabelNo");

            var answerRenovar = await ModernAlerts.ShowConfirmation(seguroRenovar, "", si, no, false);
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
                    await ModernAlerts.ShowSuccess(LabelOK, LblRenovacionAplicada);
                    var navigation = GetCurrentNavigation();
                    if (navigation != null)
                    {
                        await navigation.PushAsync(new MenuPage());
                    }
                }
                else
                {
                    await ModernAlerts.ShowError(LabelError, LblErrorRenovando);
                }
            }
            catch (Exception ex)
            {
                await ModernAlerts.ShowWarning(LabelInformacion, MensajeError);
                CrashlyticsHelper.LogError(ex, "SubscriptionsPageViewModel", "RenewSubscription");
            }
            finally
            {
                IsRunning = false;
            }
            
        }

        public async void CancelSubscription(MisSubscripciones subscription)
        {
            var LblCancelarSubscripcion = await TranslateExtension.TranslateAsync("LblCancelarSubscripcion");
            var LblSeguroDeCancelar = await TranslateExtension.TranslateAsync("LblSeguroDeCancelar");
            var LblAceptar = await TranslateExtension.TranslateAsync("LblAceptar");
            var LabelCancelar = await TranslateExtension.TranslateAsync("LabelCancelar");
            var LabelError = await TranslateExtension.TranslateAsync("LabelError");
            var LblErrorCancelando = await TranslateExtension.TranslateAsync("LblErrorCancelando");
            var LabelOK = await TranslateExtension.TranslateAsync("LabelOK");
            var LblCancelacionAplicada = await TranslateExtension.TranslateAsync("LblCancelacionAplicada");
            var LabelInformacion = await TranslateExtension.TranslateAsync("LabelInformacion");
            var MensajeError = await TranslateExtension.TranslateAsync("MensajeError");

            var answer = await ModernAlerts.ShowConfirmation(LblCancelarSubscripcion, LblSeguroDeCancelar, LblAceptar, LabelCancelar, false);

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
                    await ModernAlerts.ShowSuccess(LabelOK, LblCancelacionAplicada);
                    //await Application.Current.MainPage.Navigation.PushAsync(new HomePage());
                    Subscriptions.Remove(subscription);
                    _ = HomeViewModel.RefrescarParametrosAsync();
                    MessagingCenter.Send(this, "DatosActualizados");
                }
                else
                {
                    await ModernAlerts.ShowError(LabelError, LblErrorCancelando);
                }
            }
            catch (Exception ex)
            {
                await ModernAlerts.ShowWarning(LabelInformacion, MensajeError);
                CrashlyticsHelper.LogError(ex, "SubscriptionsPageViewModel", "CancelSubscription");
            }
            finally
            {
                IsRunning = false;
            }

        }

        /// <summary>
        /// Navega al detalle de la suscripción si es una promoción
        /// </summary>
        private async void VerDetalleSubscripcion(MisSubscripciones subscription)
        {
            // Solo navegar si es una promoción
            if (subscription.es_promocion)
            {
                // Obtener la navegación correcta del TabbedPage
                // MainPage es un TabbedPage, y cada tab tiene su propio NavigationPage
                var navigation = GetCurrentNavigation();
                if (navigation != null)
                {
                    await navigation.PushAsync(new DetallePromocionPage(subscription.subscripcion_id));
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("VerDetalleSubscripcion: No se pudo obtener la navegación");
                }
            }
        }

        /// <summary>
        /// Obtiene la navegación del tab actual en el TabbedPage
        /// </summary>
        private INavigation GetCurrentNavigation()
        {
            try
            {
                var mainPage = Application.Current?.MainPage;

                // Si es un TabbedPage, obtener la navegación del tab actual
                if (mainPage is TabbedPage tabbedPage)
                {
                    var currentPage = tabbedPage.CurrentPage;
                    if (currentPage is NavigationPage navigationPage)
                    {
                        return navigationPage.Navigation;
                    }
                    return currentPage?.Navigation;
                }

                // Si es un NavigationPage directo
                if (mainPage is NavigationPage navPage)
                {
                    return navPage.Navigation;
                }

                // Fallback: usar la navegación del MainPage
                return mainPage?.Navigation;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetCurrentNavigation error: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "SubscriptionsPageViewModel", "GetCurrentNavigation");
                return null;
            }
        }

    }
}


