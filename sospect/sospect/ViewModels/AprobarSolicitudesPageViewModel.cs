// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Newtonsoft.Json;
using sospect.Models;
using Microsoft.Maui.Controls;
using sospect.Services;
using System.Threading.Tasks;
using sospect.Utils;
using sospect.Helpers;
using sospect.AuthHelpers;
using System.Linq;
using System.Collections.Generic;
using sospect.Interfaces;

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
            var LabelError = await TranslateExtension.TranslateAsync("LabelError");
            var LabelExito = await TranslateExtension.TranslateAsync("LabelExito");
            var LabelOK = await TranslateExtension.TranslateAsync("LabelOK");
            var LblSolicitudAprobada = await TranslateExtension.TranslateAsync("LblSolicitudAprobada");

            AprobarRechazarSolicitudModel requestModel = new AprobarRechazarSolicitudModel
            {
                p_user_id_thirdparty_protegido = solicitud.user_id_thirdparty,
                p_user_id_thirdparty_protector = solicitud.user_id_thirdparty_protector,
                Idioma = IdiomUtil.ObtenerCodigoDeIdioma()
            };

            IsRunning = true;
            try
            {
                ResponseMessage response = await ApiService.AprobarSolicitudAsync(requestModel);
                if (response.IsSuccess)
                {
                    await ModernAlerts.ShowSuccess(LabelExito, LblSolicitudAprobada);
                    Solicitudes.Remove(solicitud);
                    OnPropertyChanged(nameof(IsListEmpty));
                }
                else
                {
                    await ModernAlerts.ShowError(LabelError, response.Message);
                }
            }
            catch (System.Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "AprobarSolicitudesPageViewModel", "Aprobar");

                // Mostrar mensaje de error al usuario
                await ModernAlerts.ShowError(LabelError, ex.Message);
            }
            finally
            {
                IsRunning = false;
            }
        }

        private async void Rechazar(Solicitud solicitud)
        {
            var LabelError = await TranslateExtension.TranslateAsync("LabelError");
            var LabelExito = await TranslateExtension.TranslateAsync("LabelExito");
            var LabelOK = await TranslateExtension.TranslateAsync("LabelOK");
            var MensajeErrorRechazando = await TranslateExtension.TranslateAsync("MensajeErrorRechazando");
            var LblSolicitudRechazada = await TranslateExtension.TranslateAsync("LblSolicitudRechazada");

            // Validación de entrada
            if (solicitud == null)
            {
                await ModernAlerts.ShowError(LabelError, "Solicitud no válida");
                return;
            }

            AprobarRechazarSolicitudModel requestModel = new AprobarRechazarSolicitudModel
            {
                p_user_id_thirdparty_protegido = solicitud.user_id_thirdparty,
                // CORRECCIÓN CRÍTICA: Usar solicitud.user_id_thirdparty_protector en lugar de RegistroSolicitudPendiente
                p_user_id_thirdparty_protector = solicitud.user_id_thirdparty_protector,
                Idioma = IdiomUtil.ObtenerCodigoDeIdioma()
            };

            IsRunning = true;
            try
            {
                ResponseMessage response = await ApiService.RechazarSolicitudAsync(requestModel);
                if (response.IsSuccess)
                {
                    // CORRECCIÓN: Mostrar mensaje de éxito en lugar de error
                    await ModernAlerts.ShowSuccess(LabelExito, LblSolicitudRechazada);
                    Solicitudes.Remove(solicitud);
                    OnPropertyChanged(nameof(IsListEmpty));
                }
                else
                {
                    await ModernAlerts.ShowError(LabelError, response.Message ?? MensajeErrorRechazando);
                }
            }
            catch (System.Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "AprobarSolicitudesPageViewModel", "Rechazar");

                // Mostrar mensaje de error genérico al usuario
                await ModernAlerts.ShowError(LabelError, MensajeErrorRechazando);
            }
            finally
            {
                IsRunning = false;
            }
        }

        private async Task CargarSolicitudes()
        {
            IsRunning = true;
            try
            {
                // Validación de usuario
                if (App.persona?.user_id_thirdparty == null)
                {
                    CrashlyticsHelper.LogError(new System.InvalidOperationException("App.persona o user_id_thirdparty es null"),
                        "AprobarSolicitudesPageViewModel", "CargarSolicitudes");
                    return;
                }

                ApiResponse response = await ApiService.ObtenerSolicitudesPendientes(App.persona.user_id_thirdparty);
                if (response.IsSuccess && response.Data != null)
                {
                    foreach (var solicitud in response.Data)
                    {
                        if (solicitud != null)
                        {
                            Solicitudes.Add(solicitud);
                        }
                    }
                    OnPropertyChanged(nameof(IsListEmpty));
                }
                else
                {
                    CrashlyticsHelper.LogError(new System.InvalidOperationException("Failed to load solicitudes"),
                        "AprobarSolicitudesPageViewModel", "CargarSolicitudes");
                }
            }
            catch (System.Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "AprobarSolicitudesPageViewModel", "CargarSolicitudes");
            }
            finally
            {
                IsRunning = false;
            }
        }
    }
}

