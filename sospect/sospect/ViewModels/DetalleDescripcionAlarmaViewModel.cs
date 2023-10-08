using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Newtonsoft.Json;
using Rg.Plugins.Popup.Services;
using sospect.Helpers;
using sospect.Models;
using sospect.Popups;
using sospect.Services;
using Xamarin.Forms;

namespace sospect.ViewModels
{
    public class DetalleDescripcionAlarmaViewModel : BaseViewModel
    {
        private ObservableCollection<DetalleDescripcionAlarma> _ListadoDescripcionesAlarmas;
        public ObservableCollection<DetalleDescripcionAlarma> ListadoDescripcionesAlarmas
        {
            get => this._ListadoDescripcionesAlarmas;
            set => this.SetValue(ref this._ListadoDescripcionesAlarmas, value);
        }

        private AlarmaCercana alarmaCercana;

        public DetalleDescripcionAlarmaViewModel(AlarmaCercana alarmaCercana)
        {
            this.alarmaCercana = alarmaCercana;
            _ = CargarListadoDetalleDescripcionesAlarma(alarmaCercana);

            MessagingCenter.Subscribe<object>("valor", "RegistroActualizado", async (sender) =>
            {
                await CargarListadoDetalleDescripcionesAlarma(this.alarmaCercana);
            });
        }

        public ICommand EditarDetalleAlarmaCommand => new Command<DetalleDescripcionAlarma>(OnEditarDetalleAlarmaCommand);

        private async void OnEditarDetalleAlarmaCommand(DetalleDescripcionAlarma obj)
        {
            DescribirAlarma describirAlarmaActualizacion = new DescribirAlarma()
            {
                alarma_id = obj.AlarmaId,
                p_user_id_thirdparty = App.persona.user_id_thirdparty,
                DescripcionAlarma = obj.Descripcionalarma,
                DescripcionArmas = obj.Descripcionarmas,
                DescripcionSospechoso = obj.Descripcionsospechoso,
                DescripcionVehiculo = obj.Descripcionvehiculo
            };

            await PopupNavigation.Instance.PushAsync(new DescribirAlarmaPopUp(describirAlarmaActualizacion));
        }

        public ICommand CalificarPositivoAlarmaCommand => new Command<DetalleDescripcionAlarma>(OnDescribirPositivoAlarmaCommand);

        private async void OnDescribirPositivoAlarmaCommand(DetalleDescripcionAlarma obj)
        {
            string calificacionRealizar = obj.CalificacionOtrasDescripciones == "Apagado" ? "Positivo" : obj.CalificacionOtrasDescripciones == "Positivo" ? "Apagado" : "Positivo";
            var LabelOK = TranslateExtension.Translate("LabelOK");
            var LabelInformacion = TranslateExtension.Translate("LabelInformacion");
            var MensajeError = TranslateExtension.Translate("MensajeError");

            CalificacionDescripcionAlarma calificacion = new CalificacionDescripcionAlarma()
            {
                CalificacionDescripcion = calificacionRealizar,
                Iddescripcion = obj.Iddescripcion,
                PUserIdThirdparty = App.persona.user_id_thirdparty
            };

            IsRunning = true;
            try
            {
                RespuestaCalificacionDescripcion response = await ApiService.CalificarDescripcionAlarma(calificacion);
                if (response != null)
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        obj.CalificacionOtrasDescripciones = calificacionRealizar;
                        obj.CalificacionDescripcion = response.Calificaciondescripcion;
                    });
                }
            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert(LabelInformacion, MensajeError, LabelOK);
                var properties = new Dictionary<string, string> {
                        { "Object", "DetalleDescripcionAlarmaViewModel" },
                        { "Method", "CalificarDescripcionAlarma-Positivo" }
                    };
                Microsoft.AppCenter.Crashes.Crashes.TrackError(ex, properties);

            }
            finally
            {
                IsRunning = false;
            }
        }

        public ICommand CalificarNegativoAlarmaCommand => new Command<DetalleDescripcionAlarma>(OnDescribirNegativoAlarmaCommand);

        private async void OnDescribirNegativoAlarmaCommand(DetalleDescripcionAlarma obj)
        {
            string calificacionRealizar = obj.CalificacionOtrasDescripciones == "Apagado" ? "Negativo" : obj.CalificacionOtrasDescripciones == "Negativo" ? "Apagado" : "Negativo";
            var LabelOK = TranslateExtension.Translate("LabelOK");
            var LabelInformacion = TranslateExtension.Translate("LabelInformacion");
            var MensajeError = TranslateExtension.Translate("MensajeError");

            CalificacionDescripcionAlarma calificacion = new CalificacionDescripcionAlarma()
            {
                CalificacionDescripcion = calificacionRealizar,
                Iddescripcion = obj.Iddescripcion,
                PUserIdThirdparty = App.persona.user_id_thirdparty
            };

            IsRunning = true;
            try
            {
                RespuestaCalificacionDescripcion response = await ApiService.CalificarDescripcionAlarma(calificacion);
                if (response != null)
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        obj.CalificacionOtrasDescripciones = calificacionRealizar;
                        obj.CalificacionDescripcion = response.Calificaciondescripcion;
                    });
                }
            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert(LabelInformacion, MensajeError, LabelOK);
                var properties = new Dictionary<string, string> {
                        { "Object", "DetalleDescripcionAlarmaViewModel" },
                        { "Method", "CalificarDescripcionAlarma-Negativo" }
                    };
                Microsoft.AppCenter.Crashes.Crashes.TrackError(ex, properties);
            }
            finally
            {
                IsRunning = false;
            }
            

            
        }

        private async Task CargarListadoDetalleDescripcionesAlarma(AlarmaCercana alarmaCercana)
        {
            var LabelOK = TranslateExtension.Translate("LabelOK");
            var LabelInformacion = TranslateExtension.Translate("LabelInformacion");
            var MensajeError = TranslateExtension.Translate("MensajeError");
            IsRunning = true;
            try
            {
                List<DetalleDescripcionAlarma> response = await ApiService.ListarDescripcionesAlarma(alarmaCercana);
                if (response.Any())
                {
                    ListadoDescripcionesAlarmas = new ObservableCollection<DetalleDescripcionAlarma>(response);
                    EmptyState = false;
                }
                else
                {
                    EmptyState = true;
                }
            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert(LabelInformacion, MensajeError, LabelOK);
                var properties = new Dictionary<string, string> {
                        { "Object", "DetalleDescripcionAlarmaViewModel" },
                        { "Method", "CalificarDescripcionAlarma-Negativo" }
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

