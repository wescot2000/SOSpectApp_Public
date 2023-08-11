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
using sospect.Models;
using sospect.Services;
using sospect.Utils;
using sospect.Views;
using Xamarin.Essentials;
using Xamarin.Forms;

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
                await Application.Current.MainPage.DisplayAlert(LabelError, ex.Message, LabelOK);
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

                if (_parametros.SaldoPoderes < _poderesRequeridos)
                {
                    var answer = await Application.Current.MainPage.DisplayAlert(saldoPoderesInsuficiente, "", comprarPoderes, cancelar);
                    if (answer)
                    {
                        await Application.Current.MainPage.Navigation.PushAsync(new PurchaseSuperPowersPage());
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
                var response = await ApiService.CompletarSubscripcion(request);
                IsRunning = false;

                var LabelError = TranslateExtension.Translate("LabelError");
                var LabelExito = TranslateExtension.Translate("LabelExito");
                var LabelOK = TranslateExtension.Translate("LabelOK");
                var LblSubscripcionFallida = TranslateExtension.Translate("LblSubscripcionFallida");
                var LblSubscripcionCompletada = TranslateExtension.Translate("LblSubscripcionCompletada");
                if (response.IsSuccess)
                {
                    await Application.Current.MainPage.DisplayAlert(LabelExito, LblSubscripcionCompletada, LabelOK);
                    MessagingCenter.Send(this, "DatosActualizados");
                    await Application.Current.MainPage.Navigation.PopToRootAsync();
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert(LabelError, LblSubscripcionFallida, LabelOK);
                }
            }
        }

        public async Task LoadApprovedSubscriptions()
        {
            IsRunning = true;
            var response = await ApiService.ObtenerSolicitudesAprobadas();
            IsRunning = false;

            if (response.IsSuccess)
            {
                ApprovedSubscriptions = new ObservableCollection<ApprovedSubscription>(response.Data);
                IsListEmpty = ApprovedSubscriptions.Count == 0;
            }
            
        }
    }
}
