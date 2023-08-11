using System.Windows.Input;
using Xamarin.Forms;
using sospect.Helpers;
using sospect.Models;
using sospect.Services;
using sospect.Utils;
using sospect.CustomRenderers;
using System.Linq;
using Xamarin.Forms.Maps;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Xamarin.Essentials;
using sospect.Views;

namespace sospect.ViewModels
{
    public class ZoneSubscriptionViewModel : BaseViewModel
    {
        private int _poderesRequeridos;
        private ParametrosUsuario _parametros;
        public ICommand CreateZoneCommand { get; }

        public ZoneSubscriptionViewModel()
        {
            _ = Task.Run(() => LoadSubscriptionValues());
            _parametros = JsonConvert.DeserializeObject<ParametrosUsuario>(Preferences.Get("ParametrosUsuario", ""));
            CreateZoneCommand = new Command<MiniMapa>(CreateZone);
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

        private async void CreateZone(MiniMapa miniMap)
        {
            var LabelError = TranslateExtension.Translate("LabelError");
            var LblErrorZonaVigil = TranslateExtension.Translate("LblErrorZonaVigil");
            var LabelOK = TranslateExtension.Translate("LabelOK");
            var LblZonaCreada = TranslateExtension.Translate("LblZonaCreada");
            var LblSeleccionePuntoMapa = TranslateExtension.Translate("LblSeleccionePuntoMapa");

            if (miniMap.CustomPins.Any())
            {
                Pin pin = miniMap.CustomPins.FirstOrDefault();
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

                var nuevaZonaVRequest = new NuevaZonaVRequest
                {
                    p_user_id_thirdparty_protector = App.persona.user_id_thirdparty,
                    Latitud_zona = pin.Position.Latitude,
                    Longitud_zona = pin.Position.Longitude,
                    idioma = IdiomUtil.ObtenerCodigoDeIdioma()
                };
                IsRunning = true;
                var response = await ApiService.NuevaZonaVigilancia(nuevaZonaVRequest);
                IsRunning = false;

                if (response.IsSuccess)
                {
                    await Application.Current.MainPage.DisplayAlert(LabelOK, LblZonaCreada, LabelOK);
                    MessagingCenter.Send(this, "DatosActualizados");
                    await Application.Current.MainPage.Navigation.PopToRootAsync();
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert(LabelError, LblErrorZonaVigil, LabelOK);
                }
            }
            else
            {
                await App.Current.MainPage.DisplayAlert(LabelError, LblSeleccionePuntoMapa, LabelOK);
            }


        }

    }
}
