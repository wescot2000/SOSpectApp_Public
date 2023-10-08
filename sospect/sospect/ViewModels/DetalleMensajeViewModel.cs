using System;
using System.Collections.Generic;
using System.Windows.Input;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using sospect.Helpers;
using sospect.Models;
using sospect.Resources;
using sospect.Services;
using sospect.Utils;
using sospect.Views;
using Xamarin.Forms;

namespace sospect.ViewModels
{
    public class DetalleMensajeViewModel : BaseViewModel
    {
        private DetalleMensajes _DetalleMensaje;
        public DetalleMensajes DetalleMensaje
        {
            get => this._DetalleMensaje;
            set => this.SetValue(ref this._DetalleMensaje, value);
        }

        public string ToLabel
        {
            get => $"{AppResources.LblPara} {DetalleMensaje.para}";
        }

        public string FromLabel
        {
            get => $"{AppResources.LblDe} {DetalleMensaje.remitente}";
        }

        public DetalleMensajeViewModel(long mensajeID)
        {
            DetalleMensaje = new DetalleMensajes();
            LoadMessageDetail(mensajeID);
        }

        public ICommand NavigateToAlarmCommand => new Command<long?>(OnNavigateToAlarmCommand);

        private void OnNavigateToAlarmCommand(long? alarma_id)
        {
            App.Current.MainPage.Navigation.PushAsync(new DescribirPage(alarma_id));
        }

        private async void LoadMessageDetail(long mensajeID)
        {
            var LabelOK = TranslateExtension.Translate("LabelOK");
            var LabelInformacion = TranslateExtension.Translate("LabelInformacion");
            var MensajeError = TranslateExtension.Translate("MensajeError");

            DetalleMensajeRequest request = new DetalleMensajeRequest()
            {
                IdiomaDispositivo = IdiomUtil.ObtenerCodigoDeIdioma(),
                PMensajeId = mensajeID,
                PUserIdThirdparty = App.persona.user_id_thirdparty
            };

            IsRunning = true;
            try
            {
                DetalleMensaje = await ApiService.ObtenerDetalleMensajes(request);
                OnPropertyChanged(nameof(ToLabel));
                OnPropertyChanged(nameof(FromLabel));
            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert(LabelInformacion, MensajeError, LabelOK);
                var properties = new Dictionary<string, string> {
                        { "Object", "DetalleMensajeViewModel" },
                        { "Method", "ObtenerDetalleMensajes" }
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
