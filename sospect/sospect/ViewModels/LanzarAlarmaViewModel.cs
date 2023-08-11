using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;
using Rg.Plugins.Popup.Services;
using sospect.Extensions;
using sospect.Helpers;
using sospect.Models;
using sospect.Services;
using sospect.Utils;
using Xamarin.Forms;

namespace sospect.ViewModels
{
    public class LanzarAlarmaViewModel : BaseViewModel
    {
        private ObservableCollection<TipoAlarma> _TiposAlarma;
        public ObservableCollection<TipoAlarma> TiposAlarma
        {
            get => this._TiposAlarma;
            set => this.SetValue(ref this._TiposAlarma, value);
        }

        private TipoAlarma _TipoAlarmaSeleccionado;
        public TipoAlarma TipoAlarmaSeleccionado
        {
            get => this._TipoAlarmaSeleccionado;
            set => this.SetValue(ref this._TipoAlarmaSeleccionado, value);
        }

        private double _latitude;
        public double Latitude
        {
            get => this._latitude;
            set => this.SetValue(ref this._latitude, value);
        }

        private double _longitude;
        public double Longitude
        {
            get => this._longitude;
            set => this.SetValue(ref this._longitude, value);
        }

        private string _thirdPartyId;
        public string ThirdPartyId
        {
            get => this._thirdPartyId;
            set => this.SetValue(ref this._thirdPartyId, value);
        }

        private bool _IsTimeRunning;
        public bool IsTimeRunning
        {
            get => this._IsTimeRunning;
            set => this.SetValue(ref this._IsTimeRunning, value);
        }

        public LanzarAlarmaViewModel(string thirdPartyId, double latitude, double longitude, int tipoAlarma = 1)
        {
            Latitude = latitude.Trim(6);
            Longitude = longitude.Trim(6);
            ThirdPartyId = thirdPartyId;
            IsTimeRunning = true;
            RegistrarAlarmaCommand = new RelayCommand(async () => await RegistrarAlarma());

            _ = CargarTiposAlarma(tipoAlarma);
        }

        bool IsBusy = false;
        private async Task CargarTiposAlarma(int tipoAlarmaId)
        {
            if (IsBusy)
            {
                return;
            }

            IsBusy = true;

            IsRunning = true;
            var idsTiposAlarma = await ApiService.ObtenerIdsTiposAlarma();
            IsRunning = false;

            var tiposAlarma = idsTiposAlarma.Select(id => new TipoAlarma { TipoalarmaId = id }).ToList();

            TiposAlarma = new ObservableCollection<TipoAlarma>(tiposAlarma);
            if (TiposAlarma.Any())
            {
                TipoAlarmaSeleccionado = TiposAlarma.Where(x => x.TipoalarmaId == tipoAlarmaId).First();
            }

            IsBusy = false;

        }



        public async Task RegistrarAlarma()
        {
            if (IsBusy)
            {
                return;
            }

            IsBusy = true;

            var AlarmaEnviada = TranslateExtension.Translate("AlarmaEnviada");
            var MsgTrasAlarmaEnviada = TranslateExtension.Translate("MsgTrasAlarmaEnviada");
            var LabelOK = TranslateExtension.Translate("LabelOK");
            var LabelError = TranslateExtension.Translate("LabelError");
            var LblSeleccioneTipoAlarma = TranslateExtension.Translate("LblSeleccioneTipoAlarma");
            

            Alarma alarma = new Alarma()
            {
                p_tipoalarma_id = TipoAlarmaSeleccionado.TipoalarmaId,
                p_latitud = Latitude,
                p_longitud = Longitude,
                p_user_id_thirdparty = ThirdPartyId,
                p_alarma_id = null,
                ip_usuario = InternetUtil.GetPublicIpAddress(),
                idioma_dispositivo = IdiomUtil.ObtenerCodigoDeIdioma()
            };

            if (alarma.p_tipoalarma_id == 0)
            {
                await App.Current.MainPage.DisplayAlert(LabelError, LblSeleccioneTipoAlarma, LabelOK);
                return;
            }

            IsRunning = true;
            ResponseMessage response = await ApiService.InsertarAlarma(alarma);
            IsRunning = false;

            if (response.IsSuccess)
            {
                if (PopupNavigation.Instance.PopupStack.Count > 0)
                {
                    await PopupNavigation.Instance.PopAsync();
                }
                await App.Current.MainPage.DisplayAlert(AlarmaEnviada, MsgTrasAlarmaEnviada, LabelOK);
                IsTimeRunning = false;
                MessagingCenter.Send<object, string>(this, "Refrescar", "");
            }

            IsBusy = false;
        }

        public ICommand RegistrarAlarmaCommand { get; }

        private string _CuentaRegresivaAlarma;
        public string CuentaRegresivaAlarma
        {
            get => this._CuentaRegresivaAlarma;
            set => this.SetValue(ref this._CuentaRegresivaAlarma, value);
        }
    }
}

