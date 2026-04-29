using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Newtonsoft.Json;
using sospect.Helpers;
using Microsoft.Maui.Controls;
using sospect.Models;
using sospect.Services;
using sospect.Utils;
using System.Linq;
using sospect.Interfaces;

namespace sospect.ViewModels
{
    public class ProtectedSubscriptionPageViewModel : BaseViewModel
    {
        public string UserId { get; set; }
        public ObservableCollection<RelationshipType> RelationshipTypes { get; set; }
        public ICommand RequestPermissionCommand { get; set; }

        private RelationshipType _SelectedRelationshipType;
        public RelationshipType SelectedRelationshipType
        {
            get { return _SelectedRelationshipType; }
            set { SetProperty(ref _SelectedRelationshipType, value); }
        }

        private string _resultText;
        public string ResultText
        {
            get { return _resultText; }
            set { SetProperty(ref _resultText, value); }
        }

        private bool _isResultVisible;
        public bool IsResultVisible
        {
            get { return _isResultVisible; }
            set { SetProperty(ref _isResultVisible, value); }
        }

        public ProtectedSubscriptionPageViewModel()
        {
            RelationshipTypes = new ObservableCollection<RelationshipType>();
            RequestPermissionCommand = new Command(async () => await RequestPermissionAsync());
            MainThread.BeginInvokeOnMainThread(async () => await LoadRelationshipTypes());
        }


        private async Task LoadRelationshipTypes()
        {
            IsRunning = true;
            try
            {
                System.Diagnostics.Debug.WriteLine($"[DIAG-ProtectedSubscriptionVM] LoadRelationshipTypes INICIANDO");

                var relationshipTypes = await ApiService.ObtenerTiposRelaciones();

                System.Diagnostics.Debug.WriteLine($"[DIAG-ProtectedSubscriptionVM] relationshipTypes es null: {relationshipTypes == null}");
                System.Diagnostics.Debug.WriteLine($"[DIAG-ProtectedSubscriptionVM] relationshipTypes count: {relationshipTypes?.Count ?? 0}");

                if (relationshipTypes != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[DIAG-ProtectedSubscriptionVM] Agregando {relationshipTypes.Count} tipos de relacion");
                    foreach (RelationshipType relationshipType in relationshipTypes)
                    {
                        System.Diagnostics.Debug.WriteLine($"[DIAG-ProtectedSubscriptionVM] Tipo: Id={relationshipType?.TiporelacionId}, Desc={relationshipType?.DescripcionTiporel ?? "null"}");
                        RelationshipTypes.Add(relationshipType);
                    }

                    // Establecer el primer elemento de la lista como el valor predeterminado del Picker
                    SelectedRelationshipType = RelationshipTypes.FirstOrDefault(rt => rt.TiporelacionId == 183);
                    System.Diagnostics.Debug.WriteLine($"[DIAG-ProtectedSubscriptionVM] SelectedRelationshipType: {SelectedRelationshipType?.TiporelacionId}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[DIAG-ProtectedSubscriptionVM] relationshipTypes es NULL - posible error en API");
                }

                System.Diagnostics.Debug.WriteLine($"[DIAG-ProtectedSubscriptionVM] LoadRelationshipTypes COMPLETADO - Total: {RelationshipTypes.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DIAG-ProtectedSubscriptionVM] EXCEPCION en LoadRelationshipTypes: {ex.GetType().Name}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[DIAG-ProtectedSubscriptionVM] StackTrace: {ex.StackTrace}");
                CrashlyticsHelper.LogError(ex, "ProtectedSubscriptionPageViewModel", "LoadRelationshipTypes");
            }
            finally
            {
                IsRunning = false;
            }
        }



        private async Task RequestPermissionAsync()
        {
            System.Diagnostics.Debug.WriteLine($"[DIAG-RequestPermission] ===== INICIO RequestPermissionAsync =====");
            System.Diagnostics.Debug.WriteLine($"[DIAG-RequestPermission] UserId='{UserId}', SelectedRelationshipType={SelectedRelationshipType?.TiporelacionId}, IsRunning={IsRunning}");

            var LabelError = await TranslateExtension.TranslateAsync("LabelError");
            var LabelExito = await TranslateExtension.TranslateAsync("LabelExito");
            var LblCompleteCampos = await TranslateExtension.TranslateAsync("LblCompleteCampos");
            var LabelOK = await TranslateExtension.TranslateAsync("LabelOK");
            //var LblProcesarSolicitud = await TranslateExtension.TranslateAsync("LblProcesarSolicitud");
            var LabelInformacion = await TranslateExtension.TranslateAsync("LabelInformacion");
            //var LblSuspensionRealizada = await TranslateExtension.TranslateAsync("LblSuspensionRealizada");
            var MensajeError = await TranslateExtension.TranslateAsync("MensajeError");
            var MsgSolicitudProtegidoPendiente = await TranslateExtension.TranslateAsync("MsgSolicitudProtegidoPendiente");

            if (string.IsNullOrEmpty(UserId) || SelectedRelationshipType == null)
            {
                System.Diagnostics.Debug.WriteLine($"[DIAG-RequestPermission] VALIDACION FALLIDA - UserId vacío o relación nula. Retornando.");
                await ModernAlerts.ShowError(LabelError, LblCompleteCampos);
                return;
            }

            var requestPermissionModel = new RequestPermissionModel
            {
                PUserIdThirdpartyProtector = App.persona.user_id_thirdparty,
                PUserIdThirdpartyProtegido = UserId,
                TiempoSubscripcionDias = 182,
                Idioma = IdiomUtil.ObtenerCodigoDeIdioma(),
                TiporelacionId = SelectedRelationshipType.TiporelacionId
            };

            System.Diagnostics.Debug.WriteLine($"[DIAG-RequestPermission] Modelo creado: Protector='{requestPermissionModel.PUserIdThirdpartyProtector}', Protegido='{requestPermissionModel.PUserIdThirdpartyProtegido}', TiporelacionId={requestPermissionModel.TiporelacionId}, Idioma='{requestPermissionModel.Idioma}'");

            IsRunning = true;
            bool navigatedAway = false;
            System.Diagnostics.Debug.WriteLine($"[DIAG-RequestPermission] IsRunning=true. Llamando a ApiService.RequestPermissionAsync...");
            try
            {
                ResponseMessage response = await ApiService.RequestPermissionAsync(requestPermissionModel);

                System.Diagnostics.Debug.WriteLine($"[DIAG-RequestPermission] Respuesta recibida. response==null: {response == null}");
                System.Diagnostics.Debug.WriteLine($"[DIAG-RequestPermission] response.IsSuccess={response?.IsSuccess}, response.Message='{response?.Message}', response.Data='{response?.Data}'");

                if (response.IsSuccess)
                {
                    System.Diagnostics.Debug.WriteLine($"[DIAG-RequestPermission] IsSuccess=TRUE. Traduciendo mensaje de salida...");
                    var mensajeSalida = "";
                    try
                    {
                        mensajeSalida = response.Message == null ? null : TranslateExtension.Translate(response.Message.Replace(" ", ""));
                        System.Diagnostics.Debug.WriteLine($"[DIAG-RequestPermission] mensajeSalida traducido='{mensajeSalida}'");
                    }
                    catch (Exception exTrans)
                    {
                        mensajeSalida = response.Message;
                        System.Diagnostics.Debug.WriteLine($"[DIAG-RequestPermission] EXCEPCION al traducir mensaje: {exTrans.Message}. Usando mensaje original: '{mensajeSalida}'");
                    }
                    System.Diagnostics.Debug.WriteLine($"[DIAG-RequestPermission] ANTES de ShowSuccess...");
                    await ModernAlerts.ShowSuccess(LabelExito, mensajeSalida);
                    System.Diagnostics.Debug.WriteLine($"[DIAG-RequestPermission] DESPUES de ShowSuccess. ANTES de PopToRootAsync...");
                    navigatedAway = true;
                    await GetCurrentTabNavigation().PopAsync();
                    System.Diagnostics.Debug.WriteLine($"[DIAG-RequestPermission] DESPUES de PopToRootAsync.");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[DIAG-RequestPermission] IsSuccess=FALSE. response.Data='{response?.Data}', fallback='{MsgSolicitudProtegidoPendiente}'");
                    // response.Data contiene el mensaje descriptivo ya traducido por el servidor (ej: error de BD).
                    // Si viene vacío, se usa el fallback local MsgSolicitudProtegidoPendiente.
                    var mensajeDetalle = !string.IsNullOrWhiteSpace(response.Data)
                        ? response.Data
                        : MsgSolicitudProtegidoPendiente;
                    System.Diagnostics.Debug.WriteLine($"[DIAG-RequestPermission] mensajeDetalle a mostrar='{mensajeDetalle}'. ANTES de ShowError...");
                    await ModernAlerts.ShowError(LabelError, mensajeDetalle);
                    System.Diagnostics.Debug.WriteLine($"[DIAG-RequestPermission] DESPUES de ShowError.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DIAG-RequestPermission] EXCEPCION CAPTURADA: {ex.GetType().Name}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[DIAG-RequestPermission] StackTrace: {ex.StackTrace}");
                // Si ya navegamos exitosamente (PopAsync ejecutado), ignorar la excepción —
                // puede ser un error de contexto de navegación de iOS que no tiene impacto real.
                if (!navigatedAway)
                {
                    await ModernAlerts.ShowWarning(LabelInformacion, MensajeError);
                    System.Diagnostics.Debug.WriteLine($"[DIAG-RequestPermission] DESPUES de ShowWarning (catch).");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[DIAG-RequestPermission] Excepcion post-navegacion ignorada (navigatedAway=true).");
                }
                CrashlyticsHelper.LogError(ex, "ProtectedSubscriptionPageViewModel", "RequestPermissionAsync");
            }
            finally
            {
                IsRunning = false;
                System.Diagnostics.Debug.WriteLine($"[DIAG-RequestPermission] finally: IsRunning=false. ===== FIN RequestPermissionAsync =====");
            }

        }

        private INavigation GetCurrentTabNavigation()
        {
            if (Application.Current.MainPage is TabbedPage tabbedPage)
            {
                if (tabbedPage.CurrentPage is NavigationPage navPage)
                    return navPage.Navigation;
                return tabbedPage.CurrentPage?.Navigation ?? tabbedPage.Navigation;
            }
            return Application.Current.MainPage.Navigation;
        }
    }
}
