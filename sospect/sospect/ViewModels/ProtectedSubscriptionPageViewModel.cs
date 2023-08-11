using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Newtonsoft.Json;
using sospect.Helpers;
using Xamarin.Forms;
using sospect.Models;
using sospect.Services;
using sospect.Utils;
using System.Linq;

namespace sospect.ViewModels
{
    public class ProtectedSubscriptionPageViewModel : BaseViewModel
    {
        public string UserId { get; set; }
        public ObservableCollection<RelationshipType> RelationshipTypes { get; set; }
        public ICommand RequestPermissionCommand { get; set; }

        private RelationshipType _SelectedRelationshipType;
        public RelationshipType SelectedRelationshipType
        {
            get { return _SelectedRelationshipType; }
            set { SetProperty(ref _SelectedRelationshipType, value); }
        }

        private string _resultText;
        public string ResultText
        {
            get { return _resultText; }
            set { SetProperty(ref _resultText, value); }
        }

        private bool _isResultVisible;
        public bool IsResultVisible
        {
            get { return _isResultVisible; }
            set { SetProperty(ref _isResultVisible, value); }
        }

        public ProtectedSubscriptionPageViewModel()
        {
            RelationshipTypes = new ObservableCollection<RelationshipType>();
            RequestPermissionCommand = new Command(async () => await RequestPermissionAsync());
            Device.BeginInvokeOnMainThread(async () => await LoadRelationshipTypes());
        }


        private async Task LoadRelationshipTypes()
        {
            IsRunning = true;
            var relationshipTypes = await ApiService.ObtenerTiposRelaciones();
            IsRunning = false;

            if (relationshipTypes != null)
            {
                foreach (RelationshipType relationshipType in relationshipTypes)
                {
                    RelationshipTypes.Add(relationshipType);
                }

                // Establecer el primer elemento de la lista como el valor predeterminado del Picker
                SelectedRelationshipType = RelationshipTypes.FirstOrDefault(rt => rt.TiporelacionId == 183);
            }
        }



        private async Task RequestPermissionAsync()
        {
            var LabelError = TranslateExtension.Translate("LabelError");
            var LabelExito = TranslateExtension.Translate("LabelExito");
            var LblCompleteCampos = TranslateExtension.Translate("LblCompleteCampos");
            var LabelOK = TranslateExtension.Translate("LabelOK");
            var LblProcesarSolicitud = TranslateExtension.Translate("LblProcesarSolicitud");

            if (string.IsNullOrEmpty(UserId) || SelectedRelationshipType == null)
            {
                await Application.Current.MainPage.DisplayAlert(LabelError, LblCompleteCampos, LabelOK);
                return;
            }

            var requestPermissionModel = new RequestPermissionModel
            {
                PUserIdThirdpartyProtector = App.persona.user_id_thirdparty,
                PUserIdThirdpartyProtegido = UserId,
                TiempoSubscripcionDias = 182,
                Idioma = IdiomUtil.ObtenerCodigoDeIdioma(),
                TiporelacionId = SelectedRelationshipType.TiporelacionId
            };

            IsRunning = true;
            ResponseMessage response = await ApiService.RequestPermissionAsync(requestPermissionModel);
            IsRunning = false;

            if (response.IsSuccess)
            {
                var mensajeSalida = "";
                try
                {
                    mensajeSalida = response.Message == null ? null : TranslateExtension.Translate(response.Message.Replace(" ", ""));
                }
                catch (Exception)
                {
                    mensajeSalida = response.Message;
                }
                IsResultVisible = true;
                await App.Current.MainPage.Navigation.PopAsync();
                await Application.Current.MainPage.DisplayAlert(LabelExito, mensajeSalida, LabelOK);
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert(LabelError, LblProcesarSolicitud, LabelOK);
            }
        }
    }
}
