// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System.Windows.Input;
using Microsoft.Maui.Controls;
using sospect.Helpers;
using sospect.Models;
using sospect.Services;
using sospect.Utils;
using sospect.CustomRenderers;
using System.Linq;
using MapControl = Microsoft.Maui.Controls.Maps.Map;
using Microsoft.Maui.Controls.Maps;
using System.Threading.Tasks;
using Microsoft.Maui.Maps;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using sospect.Views;
using sospect.Interfaces;

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
                await ModernAlerts.ShowError(LabelError, ex.Message);
                CrashlyticsHelper.LogError(ex, "ZoneSubscriptionViewModel", "LoadSubscriptionValues");
            }
            finally
            {
                IsRunning = false;
            }
        }

        /// <summary>
        /// Obtiene la navegación del tab activo cuando MainPage es TabbedPage.
        /// Necesario porque esta página se pushea en el NavigationPage del tab,
        /// no en App.Current.MainPage.Navigation.
        /// </summary>
        private INavigation GetTabNavigation()
        {
            if (Application.Current.MainPage is TabbedPage tabbedPage &&
                tabbedPage.CurrentPage is NavigationPage navPage)
                return navPage.Navigation;
            return Application.Current.MainPage.Navigation;
        }

        private async void CreateZone(MiniMapa miniMap)
        {
            var LabelError = await TranslateExtension.TranslateAsync("LabelError");
            var LabelOK = await TranslateExtension.TranslateAsync("LabelOK");
            var LblZonaCreada = await TranslateExtension.TranslateAsync("LblZonaCreada");
            var LblSeleccionePuntoMapa = await TranslateExtension.TranslateAsync("LblSeleccionePuntoMapa");

            if (miniMap.CustomPins.Any())
            {
                CustomPin customPin = miniMap.CustomPins.FirstOrDefault();
                var saldoPoderesInsuficiente = await TranslateExtension.TranslateAsync("LblSaldoPoderesInsuficiente");
                var comprarPoderes = await TranslateExtension.TranslateAsync("LblComprarPoderes");
                var cancelar = await TranslateExtension.TranslateAsync("LabelCancelar");

                if (_parametros.SaldoPoderes < _poderesRequeridos)
                {
                    var answer = await ModernAlerts.ShowConfirmation(
                        saldoPoderesInsuficiente, "", comprarPoderes, cancelar, false);
                    if (answer)
                    {
                        await Application.Current.MainPage.Navigation.PushAsync(new PurchaseSuperPowersPage());
                    }
                    return;
                }

                var nuevaZonaVRequest = new NuevaZonaVRequest
                {
                    p_user_id_thirdparty_protector = App.persona.user_id_thirdparty,
                    Latitud_zona = customPin.Location.Latitude,
                    Longitud_zona = customPin.Location.Longitude,
                    idioma = IdiomUtil.ObtenerCodigoDeIdioma()
                };

                IsRunning = true;
                bool navigatedAway = false;
                try
                {
                    var response = await ApiService.NuevaZonaVigilancia(nuevaZonaVRequest);

                    if (response.IsSuccess)
                    {
                        await ModernAlerts.ShowSuccess(LabelOK, LblZonaCreada);
                        _ = HomeViewModel.RefrescarParametrosAsync();
                        MessagingCenter.Send(this, "DatosActualizados");
                        navigatedAway = true;
                        await GetTabNavigation().PopToRootAsync();
                    }
                    else
                    {
                        await ModernAlerts.ShowError(LabelError, response.Message);
                    }
                }
                catch (Exception ex)
                {
                    if (!navigatedAway)
                    {
                        var LabelInformacion = await TranslateExtension.TranslateAsync("LabelInformacion");
                        var MensajeError = await TranslateExtension.TranslateAsync("MensajeError");
                        await ModernAlerts.ShowWarning(LabelInformacion, MensajeError);
                    }
                    CrashlyticsHelper.LogError(ex, "ZoneSubscriptionViewModel", "CreateZone");
                }
                finally
                {
                    IsRunning = false;
                }
            }
            else
            {
                await ModernAlerts.ShowError(LabelError, LblSeleccionePuntoMapa);
            }
        }

    }
}


