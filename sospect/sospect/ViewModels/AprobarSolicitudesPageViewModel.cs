using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Newtonsoft.Json;
using sospect.Models;
using Xamarin.Forms;
using sospect.Services;
using System.Threading.Tasks;
using sospect.Utils;
using sospect.Helpers;
using sospect.AuthHelpers;
using System.Linq;

namespace sospect.ViewModels
{
    public class AprobarSolicitudesPageViewModel : BaseViewModel
    {
        public ICommand AprobarCommand { get; set; }
        public ICommand RechazarCommand { get; set; }
        public bool IsListEmpty
        {
            get { return Solicitudes == null || !Solicitudes.Any(); }
        }



        private AprobarRechazarSolicitudModel _RegistroSolicitudPendiente;
        public AprobarRechazarSolicitudModel RegistroSolicitudPendiente
        {
            get { return _RegistroSolicitudPendiente; }
            set { SetProperty(ref _RegistroSolicitudPendiente, value); }
        }
        
        public ObservableCollection<Solicitud> Solicitudes { get; set; }

        public AprobarSolicitudesPageViewModel()
        {
            Solicitudes = new ObservableCollection<Solicitud>();
            AprobarCommand = new Command<Solicitud>(Aprobar);
            RechazarCommand = new Command<Solicitud>(Rechazar);

            _ = Task.Run(async () => await CargarSolicitudes());
        }

        private async void Aprobar(Solicitud solicitud)
        {
            var LabelError = TranslateExtension.Translate("LabelError");
            var LabelExito = TranslateExtension.Translate("LabelExito");
            var LabelOK = TranslateExtension.Translate("LabelOK");
            var MensajeErrorAprobando = TranslateExtension.Translate("MensajeErrorAprobando");
            var LblSolicitudAprobada = TranslateExtension.Translate("LblSolicitudAprobada");

            AprobarRechazarSolicitudModel requestModel = new AprobarRechazarSolicitudModel
            {
                p_user_id_thirdparty_protegido = solicitud.user_id_thirdparty,
                p_user_id_thirdparty_protector = solicitud.user_id_thirdparty_protector,
                Idioma = IdiomUtil.ObtenerCodigoDeIdioma()
            };

            IsRunning = true;
            ResponseMessage response = await ApiService.AprobarSolicitudAsync(requestModel);
            IsRunning = false;

            if (response.IsSuccess)
            {
                await Application.Current.MainPage.DisplayAlert(LabelExito, LblSolicitudAprobada, LabelOK);
                Solicitudes.Remove(solicitud);
                OnPropertyChanged(nameof(IsListEmpty));
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert(LabelError, MensajeErrorAprobando, LabelOK);
            }
        }

        private async void Rechazar(Solicitud solicitud)
        {
            var LabelError = TranslateExtension.Translate("LabelError");
            var LabelExito = TranslateExtension.Translate("LabelExito");
            var LabelOK = TranslateExtension.Translate("LabelOK");
            var MensajeErrorRechazando = TranslateExtension.Translate("MensajeErrorRechazando");
            var LblSolicitudRechazada = TranslateExtension.Translate("LblSolicitudRechazada");

            AprobarRechazarSolicitudModel requestModel = new AprobarRechazarSolicitudModel
            {
                p_user_id_thirdparty_protegido = solicitud.user_id_thirdparty,
                p_user_id_thirdparty_protector = RegistroSolicitudPendiente.p_user_id_thirdparty_protector,
                Idioma = IdiomUtil.ObtenerCodigoDeIdioma()
            };

            IsRunning = true;
            ResponseMessage response = await ApiService.RechazarSolicitudAsync(requestModel);
            IsRunning = false;

            if (response.IsSuccess)
            {
                await Application.Current.MainPage.DisplayAlert(LabelExito, LblSolicitudRechazada, LabelOK);
                Solicitudes.Remove(solicitud);
                OnPropertyChanged(nameof(IsListEmpty));
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert(LabelError, MensajeErrorRechazando, LabelOK);
            }
        }

        private async Task CargarSolicitudes()
        {
            IsRunning = true;
            ApiResponse response = await ApiService.ObtenerSolicitudesPendientes(App.persona.user_id_thirdparty);
            IsRunning = false;

            if (response.IsSuccess)
            {
                foreach (var solicitud in response.Data)
                {
                    Solicitudes.Add(solicitud);
                }
                OnPropertyChanged(nameof(IsListEmpty));
            }
        }
    }
}
