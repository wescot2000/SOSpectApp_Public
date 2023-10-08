using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;
using Rg.Plugins.Popup.Services;
using Sharpnado.MaterialFrame;
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

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

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

        private bool _isInitialized = false;
        public bool IsInitialized
        {
            get => _isInitialized;
            set => this.SetValue(ref _isInitialized, value);
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

        private const int MaxRetries = 5;

        private async Task CargarTiposAlarma(int tipoAlarmaId)
        {
            var LabelOK = TranslateExtension.Translate("LabelOK");
            var LabelError = TranslateExtension.Translate("LabelError");
            var LblHabilitaInternetReintenta = TranslateExtension.Translate("LblHabilitaInternetReintenta");

            if (IsRunning)
            {
                return;
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                IsRunning = true;
            });

            List<int> idsTiposAlarma = null;

            for (int retry = 0; retry < MaxRetries; retry++)
            {
                try
                {
                    idsTiposAlarma = await ApiService.ObtenerIdsTiposAlarma();
                    break;
                }
                catch
                {
                    if (retry == MaxRetries - 1)
                    {
                        Device.BeginInvokeOnMainThread(async () =>
                        {
                            await App.Current.MainPage.DisplayAlert(LabelError, LblHabilitaInternetReintenta, LabelOK);
                        });
                        IsRunning = false;
                        return;
                    }

                    await Task.Delay(2000);
                }
            }

            if (idsTiposAlarma == null || !idsTiposAlarma.Any())
            {
                Device.BeginInvokeOnMainThread(async () =>
                {
                    await App.Current.MainPage.DisplayAlert(LabelError, LblHabilitaInternetReintenta, LabelOK);
                });
                IsRunning = false;
                return;
            }

            var tiposAlarma = idsTiposAlarma.Select(id => new TipoAlarma { TipoalarmaId = id }).ToList();
            TiposAlarma = new ObservableCollection<TipoAlarma>(tiposAlarma);

            if (TiposAlarma.Any())
            {
                TipoAlarmaSeleccionado = TiposAlarma.FirstOrDefault(x => x.TipoalarmaId == tipoAlarmaId);
            }

            IsInitialized = true;

            Device.BeginInvokeOnMainThread(() =>
            {
                IsRunning = false;
            });
        }



        public async Task RegistrarAlarma()
        {
            if (!IsInitialized)
            {
                return;
            }


            await _semaphore.WaitAsync();

            var AlarmaEnviada = TranslateExtension.Translate("AlarmaEnviada");
            var MsgTrasAlarmaEnviada = TranslateExtension.Translate("MsgTrasAlarmaEnviada");
            var LabelOK = TranslateExtension.Translate("LabelOK");
            var LabelError = TranslateExtension.Translate("LabelError");
            var LblSeleccioneTipoAlarma = TranslateExtension.Translate("LblSeleccioneTipoAlarma");
            var LabelInformacion = TranslateExtension.Translate("LabelInformacion");
            var MensajeError = TranslateExtension.Translate("MensajeError");

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

            if (TipoAlarmaSeleccionado == null || alarma.p_tipoalarma_id == 0)
            {
                await App.Current.MainPage.DisplayAlert(LabelError, LblSeleccioneTipoAlarma, LabelOK);
                return;
            }

            
            try
            {
                IsRunning = true;
                ResponseMessage response = await ApiService.InsertarAlarma(alarma);
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
                _semaphore.Release(); // Libera el semáforo para que otros puedan usarlo.
            }
            
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

