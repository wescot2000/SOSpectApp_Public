using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Rg.Plugins.Popup.Services;
using sospect.Helpers;
using sospect.Models;
using sospect.Services;
using sospect.Views;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace sospect.ViewModels
{
    public class DescribirAlarmaViewModel : BaseViewModel
    {
        public long? AlarmaId { get; set; }
        public DescribirAlarmaViewModel(long? alarmaId = null)
        {
            this.AlarmaId = alarmaId;

            if (alarmaId != null)
            {
                Task.Run(async () =>
                {
                    await ObtenerAlarma(alarmaId);
                });
            }
            else
            {
                Task.Run(async () =>
                {
                    await ObtenerAlarmas();
                });
            }
        }

        public ICommand RefreshCommand => new Command(async () =>
        {
            if (AlarmaId != null)
            {
                await ObtenerAlarma(AlarmaId);
            }
            else
            {
                await ObtenerAlarmas();
            }
        });

        bool IsNavigating = false;

        private async Task ObtenerAlarma(long? alarmaId)
        {
            try
            {
                IsRunning = true;
                List<AlarmaCercana> response = await ApiService.ObtenerAlarma(alarmaId);
                IsRunning = false;

                if (response.Any())
                {
                    foreach (var alarma in response)
                    {
                        alarma.CalcularCredibilidad();
                    }

                    ListadoAlarmas = new ObservableCollection<AlarmaCercana>(response);
                    Preferences.Set("alarma_id", null);
                    EmptyState = false;
                }
                else
                {
                    EmptyState = true;
                }

            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert("Error", ex.Message, "Ok");
            }
            
        }

        public ICommand DescribirAlarmaCommand => new Command<AlarmaCercana>(OnDescribirAlarmaCommand);
        public ICommand VerDetallesAlarmaCommand => new Command<AlarmaCercana>(OnVerDetallesAlarmaCommand);
        public ICommand ConfirmarAlarmaCommand => new Command<AlarmaCercana>(OnConfirmarAlarmaCommand);
        public ICommand VerAlarmaEnMapaCommand => new Command<AlarmaCercana>(OnVerAlarmaEnMapaCommand);
        public ICommand CerrarAlarmaCommand => new Command<AlarmaCercana>(OnCerrarAlarmaCommand);
        public ICommand AtenderAlarmaCommand => new Command<AlarmaCercana>(OnAtenderAlarmaCommand);

        private void OnVerAlarmaEnMapaCommand(AlarmaCercana Alarma)
        {
            try
            {
                if (IsNavigating)
                {
                    return;
                }
                IsNavigating = true;

                ObservableCollection<AlarmaCercana> Alarmas = new ObservableCollection<AlarmaCercana>();
                Alarmas.Add(Alarma);

                App.Current.MainPage.Navigation.PushAsync(new HomePage(Alarmas));

                IsNavigating = false;
            }
            catch (Exception ex)
            {
                App.Current.MainPage.DisplayAlert("Error",ex.Message,"Ok");
            }
           
        }

        private async void OnConfirmarAlarmaCommand(AlarmaCercana obj)
        {
            if (IsNavigating)
            {
                return;
            }
            IsNavigating = true;

            var RecibeAlarma = TranslateExtension.Translate("RecibeAlarma");
            var LabelConfirmo = TranslateExtension.Translate("LabelConfirmo");
            var LabelNoSeguro = TranslateExtension.Translate("LabelNoSeguro");
            var LabelEsFalsa = TranslateExtension.Translate("LabelEsFalsa");
            var LabelConfirmar = TranslateExtension.Translate("LabelConfirmar");
            var LabelSi = TranslateExtension.Translate("LabelSi");
            var LabelNo = TranslateExtension.Translate("LabelNo");
            var LabelOK = TranslateExtension.Translate("LabelOK");
            var LabelInformacion = TranslateExtension.Translate("LabelInformacion");
            var AdvertenciaCalificacion = TranslateExtension.Translate("AdvertenciaCalificacion");
            var MensajeError = TranslateExtension.Translate("MensajeError");

            var action = await App.Current.MainPage.DisplayActionSheet(RecibeAlarma, null, null, LabelConfirmo, LabelNoSeguro, LabelEsFalsa);

            CalificarAlarma calificacionAlarma = new CalificarAlarma()
            {
                AlarmaId = obj.alarma_id,
                PUserIdThirdparty = App.persona.user_id_thirdparty
            };


            ResponseMessage? responseMessage = null;
            bool respuesta;
            try
            {
                switch (action)
                {
                    case var a when a == LabelConfirmo:
                        calificacionAlarma.VeracidadAlarma = true;

                        respuesta = await App.Current.MainPage.DisplayAlert(LabelConfirmar, AdvertenciaCalificacion, LabelSi, LabelNo);

                        if (respuesta)
                        {
                            responseMessage = await ApiService.CalificarAlarma(calificacionAlarma);
                        }
                        break;
                    case var a when a == LabelNoSeguro:
                        break;
                    case var a when a == LabelEsFalsa:
                        calificacionAlarma.VeracidadAlarma = false;


                        respuesta = await App.Current.MainPage.DisplayAlert(LabelConfirmar, AdvertenciaCalificacion, LabelSi, LabelNo);

                        if (respuesta)
                        {
                            IsRunning = true;
                            responseMessage = await ApiService.CalificarAlarma(calificacionAlarma);
                            IsRunning = false;
                        }
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert(LabelInformacion, MensajeError, LabelOK);
                var properties = new Dictionary<string, string> {
                        { "Object", "DescribirAlarmaViewModel" },
                        { "Method", "CalificarAlarma" }
                    };
                Microsoft.AppCenter.Crashes.Crashes.TrackError(ex, properties);
            }
            

            if (responseMessage != null)
            {
                var mensajeSalida = "";
                try
                {
                    mensajeSalida = responseMessage.Message == null ? null : TranslateExtension.Translate(responseMessage.Message.Replace(" ", ""));
                }
                catch (Exception)
                {
                    mensajeSalida = responseMessage.Message;
                }
                
                await App.Current.MainPage.DisplayAlert(LabelInformacion, mensajeSalida, LabelOK);
            }
            IsNavigating = false;
        }

        public string AlertaReportadaPorMi(bool flag_propietario_alarma)
        {
            var LblAlertaReportadaPorMi = TranslateExtension.Translate("LblAlertaReportadaPorMi");
            return flag_propietario_alarma ? LblAlertaReportadaPorMi : "";
        }


        private async void OnVerDetallesAlarmaCommand(AlarmaCercana alarmaCercana)
        {
            if (IsNavigating)
            {
                return;
            }
            IsNavigating = true;
            await App.Current.MainPage.Navigation.PushAsync(new DetalleDescripcionAlarmaPage(alarmaCercana));
            IsNavigating = false;
        }

        private async void OnDescribirAlarmaCommand(AlarmaCercana alarmaCercana)
        {
            if (IsNavigating)
            {
                return;
            }
            IsNavigating = true;

            await PopupNavigation.Instance.PushAsync(new Popups.DescribirAlarmaPopUp(alarmaCercana));
            
            IsNavigating = false;
        }

        private async void OnCerrarAlarmaCommand(AlarmaCercana alarmaCercana)
        {
            if (IsNavigating)
            {
                return;
            }
            IsNavigating = true;

            await PopupNavigation.Instance.PushAsync(new Popups.CerrarAlarmaPopUp(alarmaCercana));

            IsNavigating = false;
        }

        private async void OnAtenderAlarmaCommand(AlarmaCercana alarmaCercana)
        {
            if (IsNavigating)
            {
                return;
            }
            IsNavigating = true;

            await PopupNavigation.Instance.PushAsync(new Popups.AtenderAlarmaPopup(alarmaCercana));
            IsNavigating = false;
        }

        internal async Task ObtenerAlarmas()
        {
            var LabelOK = TranslateExtension.Translate("LabelOK");
            var LabelInformacion = TranslateExtension.Translate("LabelInformacion");
            var LblHabilitaGPSReintenta = TranslateExtension.Translate("LblHabilitaGPSReintenta");
            var MensajeError = TranslateExtension.Translate("MensajeError");

            IsRunning = true;

            if (App.ubicacionActual != null)
            {
                try
                {
                    App.ubicacionActual.PantallaOrigen = "DescribirAlarma";
                    ListadoAlarmas = new ObservableCollection<AlarmaCercana>(await ApiService.ActualizarUbicacion(App.ubicacionActual));

                    foreach (var alarma in ListadoAlarmas)
                    {
                        alarma.CalcularCredibilidad();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error: {ex.Message}");
                    await App.Current.MainPage.DisplayAlert(LabelInformacion, MensajeError, LabelOK);
                    var properties = new Dictionary<string, string> {
                        { "Object", "DescribirAlarmaViewModel" },
                        { "Method", "CalificarAlarma" }
                    };
                    Microsoft.AppCenter.Crashes.Crashes.TrackError(ex, properties);
                }
            }
            else
            {
                await App.Current.MainPage.DisplayAlert(LabelInformacion, LblHabilitaGPSReintenta, LabelOK);
            }

            EmptyState = ListadoAlarmas == null || !ListadoAlarmas.Any();

            IsRunning = false;
        }


        private ObservableCollection<AlarmaCercana> _ListadoAlarmas;
        public ObservableCollection<AlarmaCercana> ListadoAlarmas
        {
            get => this._ListadoAlarmas;
            set => this.SetValue(ref this._ListadoAlarmas, value);
        }
    }
}

