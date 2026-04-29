using System;
using System.Collections.Generic;

using System.Windows.Input;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using sospect.Helpers;
using sospect.Interfaces;
using sospect.Models;
using sospect.Resources;
using sospect.Services;
using sospect.Utils;
using sospect.Views;
using Microsoft.Maui.Controls;

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

        // Campos para traducciones cargadas async
        private string _lblVerAlertaCercana;
        private string _lblVerAlerta;

        public string ToLabel
        {
            get => $"{AppResources.LblPara} {DetalleMensaje.para}";
        }

        public string FromLabel
        {
            get => $"{AppResources.LblDe} {DetalleMensaje.remitente}";
        }

        // Rediseño 2026-02-08: Propiedades computadas para mensajes enriquecidos
        public bool HasPhoto => !string.IsNullOrEmpty(DetalleMensaje?.url_foto);
        public bool HasLogo => !string.IsNullOrEmpty(DetalleMensaje?.url_logo);
        public bool HasAlarmType => DetalleMensaje?.tipoalarma_id != null;

        public string AlarmTypeIconName
        {
            get
            {
                if (DetalleMensaje?.tipoalarma_id == null)
                    return null;

                // Buscar en la lista cacheada de tipos de alarma
                if (App.TiposAlarmaDisponibles != null)
                {
                    var tipo = App.TiposAlarmaDisponibles
                        .FirstOrDefault(t => t.TipoalarmaId == DetalleMensaje.tipoalarma_id);
                    if (tipo != null)
                        return tipo.IconoPathSinExtension;
                }
                return "sospecticon"; // Fallback
            }
        }

        public string AlarmTypeDescription
        {
            get
            {
                if (DetalleMensaje?.tipoalarma_id == null)
                    return null;

                // Reutilizar DescripcionTraducida del TipoAlarma cacheado
                if (App.TiposAlarmaDisponibles != null)
                {
                    var tipo = App.TiposAlarmaDisponibles
                        .FirstOrDefault(t => t.TipoalarmaId == DetalleMensaje.tipoalarma_id);
                    if (tipo != null)
                        return tipo.DescripcionTraducida;
                }
                return null;
            }
        }

        public string ActionButtonText
        {
            get
            {
                if (DetalleMensaje?.alarma_id == null)
                    return null;

                string tipoDesc = AlarmTypeDescription;

                if (tipoDesc != null && DetalleMensaje.distancia_metros != null
                    && !string.IsNullOrEmpty(_lblVerAlertaCercana))
                {
                    return string.Format(_lblVerAlertaCercana, tipoDesc, DetalleMensaje.distancia_metros);
                }

                if (tipoDesc != null && !string.IsNullOrEmpty(_lblVerAlerta))
                {
                    return $"{_lblVerAlerta}: {tipoDesc}";
                }

                return _lblVerAlerta;
            }
        }

        public DetalleMensajeViewModel(long mensajeID)
        {
            DetalleMensaje = new DetalleMensajes();
            LoadMessageDetail(mensajeID);
        }

        public ICommand NavigateToAlarmCommand => new Command<object>(OnNavigateToAlarmCommand);

        private async void OnNavigateToAlarmCommand(object parameter)
        {
            try
            {
                // Convertir el parámetro a long? de forma segura
                long? alarma_id = null;
                if (parameter != null)
                {
                    if (parameter is long longValue)
                    {
                        alarma_id = longValue;
                    }
                    else if (long.TryParse(parameter.ToString(), out long parsedValue))
                    {
                        alarma_id = parsedValue;
                    }
                }

                // Si no se pudo obtener el alarma_id del parámetro, intentar obtenerlo del DetalleMensaje
                if (alarma_id == null && DetalleMensaje?.alarma_id != null)
                {
                    alarma_id = DetalleMensaje.alarma_id;
                }

                System.Diagnostics.Debug.WriteLine($"OnNavigateToAlarmCommand: alarma_id = {alarma_id}");

                if (alarma_id == null)
                {
                    System.Diagnostics.Debug.WriteLine("OnNavigateToAlarmCommand: alarma_id es null, no se puede navegar");
                    return;
                }

                // Navegar a HistorialPage con la alarma específica
                INavigation navigation = GetCurrentNavigation();
                if (navigation != null)
                {
                    await navigation.PushAsync(new HistorialPage(alarma_id));
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("OnNavigateToAlarmCommand: Navigation no disponible");
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "DetalleMensajeViewModel", "OnNavigateToAlarmCommand");

                System.Diagnostics.Debug.WriteLine($"Error al navegar a HistorialPage: {ex.Message}");
            }
        }

        // CORRECCIÓN: Helper method para obtener Navigation desde TabbedPage
        private INavigation GetCurrentNavigation()
        {
            try
            {
                if (App.Current?.MainPage is Microsoft.Maui.Controls.TabbedPage tabbedPage)
                {
                    // Obtener la página actual del TabbedPage
                    var currentPage = tabbedPage.CurrentPage;

                    // Si es NavigationPage, devolver su Navigation
                    if (currentPage is NavigationPage navPage)
                    {
                        return navPage.Navigation;
                    }

                    // Si la página actual tiene Navigation, devolverlo
                    if (currentPage?.Navigation != null)
                    {
                        return currentPage.Navigation;
                    }
                }

                // Fallback: usar App.Current.MainPage.Navigation si está disponible
                if (App.Current?.MainPage?.Navigation != null)
                {
                    return App.Current.MainPage.Navigation;
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DetalleMensajeViewModel: Error obteniendo Navigation: {ex.Message}");
                return null;
            }
        }

        private async void LoadMessageDetail(long mensajeID)
        {
            var LabelOK = await TranslateExtension.TranslateAsync("LabelOK");
            var LabelInformacion = await TranslateExtension.TranslateAsync("LabelInformacion");
            var MensajeError = await TranslateExtension.TranslateAsync("MensajeError");
            _lblVerAlertaCercana = await TranslateExtension.TranslateAsync("LblVerAlertaCercana");
            _lblVerAlerta = await TranslateExtension.TranslateAsync("LblVerAlerta");

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
                // Rediseño 2026-02-08: Notificar propiedades de mensajes enriquecidos
                OnPropertyChanged(nameof(HasPhoto));
                OnPropertyChanged(nameof(HasLogo));
                OnPropertyChanged(nameof(HasAlarmType));
                OnPropertyChanged(nameof(AlarmTypeIconName));
                OnPropertyChanged(nameof(AlarmTypeDescription));
                OnPropertyChanged(nameof(ActionButtonText));
            }
            catch (Exception ex)
            {
                await ModernAlerts.ShowWarning(LabelInformacion, MensajeError);
                CrashlyticsHelper.LogError(ex, "DetalleMensajeViewModel", "LoadMessageDetail");
            }
            finally
            {
                IsRunning = false;
            }       
        }
    }
}
