// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Newtonsoft.Json;
using sospect.Helpers;
using sospect.Interfaces;
using sospect.Models;
using sospect.Services;
using sospect.Utils;
using sospect.Views;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Controls;

namespace sospect.ViewModels
{
    public class CompleteSubscriptionPageViewModel : BaseViewModel
    {
        private ParametrosUsuario _parametros;
        private ObservableCollection<ApprovedSubscription> _approvedSubscriptions;
        private ApprovedSubscription _selectedSubscription;
        private int _poderesRequeridos;
        private bool _isBusy;
        private bool _isListEmpty;

        public Command CompleteSubscriptionCommand { get; set; }

        public ObservableCollection<ApprovedSubscription> ApprovedSubscriptions
        {
            get { return _approvedSubscriptions; }
            set { SetProperty(ref _approvedSubscriptions, value); }
        }

        public ApprovedSubscription SelectedSubscription
        {
            get { return _selectedSubscription; }
            set { SetProperty(ref _selectedSubscription, value); }
        }

        public bool IsBusy
        {
            get { return _isBusy; }
            set { SetProperty(ref _isBusy, value); }
        }

        public bool IsListEmpty
        {
            get { return _isListEmpty; }
            set { SetProperty(ref _isListEmpty, value); }
        }

        public CompleteSubscriptionPageViewModel()
        {
            _parametros = JsonConvert.DeserializeObject<ParametrosUsuario>(Preferences.Get("ParametrosUsuario", ""));
            ApprovedSubscriptions = new ObservableCollection<ApprovedSubscription>();
            CompleteSubscriptionCommand = new Command<ApprovedSubscription>(CompleteSubscription);
            ApprovedSubscriptions.CollectionChanged += OnApprovedSubscriptionsChanged;
            _ = Task.Run(() => LoadSubscriptionValues());
            _ = Task.Run(() => LoadApprovedSubscriptions());
        }

        private async void LoadSubscriptionValues()
        {
            IsRunning = true;
            try
            {
                List<SubscriptionValue> subscriptionValues = await ApiService.ObtenerValoresDeSubscripcion();

                foreach (var value in subscriptionValues)
                {
                    if (value.TipoSubscrId == 3)
                    {
                        _poderesRequeridos = value.CantidadPoderesRequeridos;
                        break;  // No necesitamos seguir buscando
                    }
                }
            }
            catch (Exception ex)
            {
                var LabelError = TranslateExtension.Translate("LabelError");
                var LabelOK = TranslateExtension.Translate("LabelOK");
                await ModernAlerts.ShowError(LabelError, ex.Message);
                CrashlyticsHelper.LogError(ex, "CompleteSubscriptionPageViewModel", "LoadSubscriptionValues");
            }
            finally
            {
                IsRunning = false;
            }
        }

        private void OnApprovedSubscriptionsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            IsListEmpty = ApprovedSubscriptions.Count == 0;
        }

        public async void CompleteSubscription(ApprovedSubscription SelectedSubscription)
        {
            if (SelectedSubscription != null)
            {
                var saldoPoderesInsuficiente = TranslateExtension.Translate("LblSaldoPoderesInsuficiente");
                var comprarPoderes = TranslateExtension.Translate("LblComprarPoderes");
                var cancelar = TranslateExtension.Translate("LabelCancelar");
                var LabelError = TranslateExtension.Translate("LabelError");
                var LabelExito = TranslateExtension.Translate("LabelExito");
                var LabelOK = TranslateExtension.Translate("LabelOK");
                //var LblSubscripcionFallida = TranslateExtension.Translate("LblSubscripcionFallida");
                var LblSubscripcionCompletada = TranslateExtension.Translate("LblSubscripcionCompletada");

                if (_parametros.SaldoPoderes < _poderesRequeridos)
                {
                    var answer = await ModernAlerts.ShowConfirmation(saldoPoderesInsuficiente, "", comprarPoderes, cancelar, false);
                    if (answer)
                    {
                        await GetCurrentTabNavigation().PushAsync(new PurchaseSuperPowersPage());
                    }
                    return;
                }
                var request = new CompleteSubscriptionRequest
                {
                    P_user_id_thirdparty_protector = App.persona.user_id_thirdparty,
                    P_user_id_thirdparty_protegido = SelectedSubscription.UserIdProtegido,
                    idioma = IdiomUtil.ObtenerCodigoDeIdioma()
                };

                IsRunning = true;
                bool navigatedAway = false;
                try
                {
                    var response = await ApiService.CompletarSubscripcion(request);
                    if (response.IsSuccess)
                    {
                        await ModernAlerts.ShowSuccess(LabelExito, LblSubscripcionCompletada);
                        navigatedAway = true;
                        await GetCurrentTabNavigation().PopAsync();
                        _ = HomeViewModel.RefrescarParametrosAsync();
                        MessagingCenter.Send(this, "DatosActualizados");
                    }
                    else
                    {
                        await ModernAlerts.ShowError(LabelError, response.Message);
                        //await Application.Current.MainPage.DisplayAlert(LabelError, LblSubscripcionFallida, LabelOK);
                    }
                }
                catch (Exception ex)
                {
                    if (!navigatedAway)
                    {
                        var MensajeError = TranslateExtension.Translate("MensajeError");
                        var LabelInformacion = TranslateExtension.Translate("LabelInformacion");
                        await ModernAlerts.ShowWarning(LabelInformacion, MensajeError);
                    }
                    CrashlyticsHelper.LogError(ex, "CompleteSubscriptionPageViewModel", "CompleteSubscription");
                }
                finally
                {
                    IsRunning = false;
                }               
            }
        }

        private INavigation GetCurrentTabNavigation()
        {
            if (Application.Current.MainPage is TabbedPage tabbedPage)
            {
                if (tabbedPage.CurrentPage is NavigationPage navPage)
                    return navPage.Navigation;
                return tabbedPage.CurrentPage?.Navigation ?? tabbedPage.Navigation;
            }
            return Application.Current.MainPage.Navigation;
        }

        public async Task LoadApprovedSubscriptions()
        {
            IsRunning = true;
            try
            {
                var response = await ApiService.ObtenerSolicitudesAprobadas();
                if (response.IsSuccess)
                {
                    ApprovedSubscriptions = new ObservableCollection<ApprovedSubscription>(response.Data);
                    IsListEmpty = ApprovedSubscriptions.Count == 0;
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "CompleteSubscriptionPageViewModel", "LoadApprovedSubscriptions");
            }
            finally
            {
                IsRunning = false;
            }
        }
    }
}


