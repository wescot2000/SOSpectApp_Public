// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using Newtonsoft.Json;
using sospect.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using sospect.Services;
using sospect.Utils;
using sospect.Helpers;
using sospect.Views;
using System.Collections.Specialized;
using sospect.Interfaces;

namespace sospect.ViewModels
{
    public class MyProtectorsViewModel : BaseViewModel
    {
        private bool _isListEmpty;
        public ObservableCollection<ProtectorData> Protectors { get; set; }

        public Command SuspendProtectorCommand { get; set; }
        public Command DeleteProtectorCommand { get; set; }

        public MyProtectorsViewModel()
        {
            Protectors = new ObservableCollection<ProtectorData>();
            SuspendProtectorCommand = new Command<ProtectorData>(SuspendProtectorAsync);
            DeleteProtectorCommand = new Command<ProtectorData>(DeleteProtectorAsync);
            Protectors.CollectionChanged += OnProtectorsChanged;
            _ = Task.Run(LoadProtectorsAsync);
        }

        public bool IsListEmpty
        {
            get { return _isListEmpty; }
            set { SetProperty(ref _isListEmpty, value); }
        }

        /// <summary>
        /// Obtiene la navegación del tab activo cuando MainPage es TabbedPage.
        /// Necesario porque estas páginas se pushearon en el NavigationPage del tab,
        /// no en App.Current.MainPage.Navigation.
        /// </summary>
        private INavigation GetTabNavigation()
        {
            if (Application.Current.MainPage is TabbedPage tabbedPage &&
                tabbedPage.CurrentPage is NavigationPage navPage)
                return navPage.Navigation;
            return Application.Current.MainPage.Navigation;
        }

        private async Task LoadProtectorsAsync()
        {
            var LabelOK = await TranslateExtension.TranslateAsync("LabelOK");
            var LabelInformacion = await TranslateExtension.TranslateAsync("LabelInformacion");
            var MensajeError = await TranslateExtension.TranslateAsync("MensajeError");
            IsRunning = true;
            try
            {
                System.Diagnostics.Debug.WriteLine($"[DIAG-MyProtectorsVM] LoadProtectorsAsync INICIANDO");
                System.Diagnostics.Debug.WriteLine($"[DIAG-MyProtectorsVM] user_id_thirdparty: {App.persona?.user_id_thirdparty ?? "NULL"}");
                System.Diagnostics.Debug.WriteLine($"[DIAG-MyProtectorsVM] idioma: {IdiomUtil.ObtenerCodigoDeIdioma()}");

                var response = await ApiService.ObtenerProtectores(App.persona.user_id_thirdparty, IdiomUtil.ObtenerCodigoDeIdioma());

                System.Diagnostics.Debug.WriteLine($"[DIAG-MyProtectorsVM] Response recibido - isSuccess: {response?.isSuccess}, message: {response?.message ?? "null"}");
                System.Diagnostics.Debug.WriteLine($"[DIAG-MyProtectorsVM] Response data es null: {response?.data == null}, count: {response?.data?.Count ?? 0}");

                // Limpiar y recrear siempre
                Protectors.Clear();

                if (response.isSuccess && response.data != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[DIAG-MyProtectorsVM] Agregando {response.data.Count} protectores a la lista");
                    foreach (var protector in response.data)
                    {
                        System.Diagnostics.Debug.WriteLine($"[DIAG-MyProtectorsVM] Protector: {protector?.login_protector ?? "null"}, protegido: {protector?.login_protegido ?? "null"}");
                        Protectors.Add(protector);
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[DIAG-MyProtectorsVM] Response no exitoso o data nulo - NO se agregaron protectores");
                }

                System.Diagnostics.Debug.WriteLine($"[DIAG-MyProtectorsVM] LoadProtectorsAsync COMPLETADO - Total protectores: {Protectors.Count}");
                // El evento CollectionChanged actualizará IsListEmpty automáticamente
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DIAG-MyProtectorsVM] EXCEPCION en LoadProtectorsAsync: {ex.GetType().Name}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[DIAG-MyProtectorsVM] StackTrace: {ex.StackTrace}");
                await ModernAlerts.ShowWarning(LabelInformacion, MensajeError);
                CrashlyticsHelper.LogError(ex, "MyProtectorsViewModel", "LoadProtectorsAsync");

                // Asegurar lista vacía en error
                Protectors.Clear();
            }
            finally
            {
                IsRunning = false;
            }
        }

        private void OnProtectorsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            IsListEmpty = Protectors.Count == 0;
        }

        private async void SuspendProtectorAsync(ProtectorData protector)
        {
            var LblSuspenderProtectores = await TranslateExtension.TranslateAsync("LblSuspenderProtectores");
            var LabelSi = await TranslateExtension.TranslateAsync("LabelSi");
            var LabelNo = await TranslateExtension.TranslateAsync("LabelNo");
            var LabelOK = await TranslateExtension.TranslateAsync("LabelOK");
            var LabelInformacion = await TranslateExtension.TranslateAsync("LabelInformacion");
            var LblSuspensionRealizada = await TranslateExtension.TranslateAsync("LblSuspensionRealizada");
            var MensajeError = await TranslateExtension.TranslateAsync("MensajeError");
            var LabelError = await TranslateExtension.TranslateAsync("LabelError");

            var confirmationPage = new ConfirmationPage(LblSuspenderProtectores);
            confirmationPage.ConfirmationResult += async (sender, confirmed) =>
            {
                if (confirmed)
                {
                    SuspenderPermisoRequest request = new SuspenderPermisoRequest
                    {
                        UserIdThirdPartyProtegido = protector.user_id_thirdParty_protegido,
                        TiempoSuspension = 6,
                        Idioma = IdiomUtil.ObtenerCodigoDeIdioma()
                    };
                    IsRunning = true;
                    bool navigatedAway = false;
                    try
                    {
                        var response = await ApiService.SuspenderPermisoAProtector(request);

                        if (response.IsSuccess)
                        {
                            await ModernAlerts.ShowSuccess(LabelInformacion, LblSuspensionRealizada);
                            navigatedAway = true;
                            if (App.Current.MainPage.Navigation.ModalStack.Count > 0)
                                await App.Current.MainPage.Navigation.PopModalAsync();
                            await GetTabNavigation().PopToRootAsync();
                            return;
                        }
                        else
                        {
                            await ModernAlerts.ShowError(LabelError, response.Message);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (!navigatedAway)
                            await ModernAlerts.ShowWarning(LabelInformacion, MensajeError);
                        CrashlyticsHelper.LogError(ex, "MyProtectorsViewModel", "SuspendProtectorAsync");
                    }
                    finally
                    {
                        IsRunning = false;
                    }
                }
                // Solo cerrar el modal si la navegación exitosa no lo cerró ya
                if (App.Current.MainPage.Navigation.ModalStack.Count > 0)
                    await App.Current.MainPage.Navigation.PopModalAsync();
            };
            await App.Current.MainPage.Navigation.PushModalAsync(confirmationPage);
        }

        private async void DeleteProtectorAsync(ProtectorData protector)
        {
            var LabelSi = await TranslateExtension.TranslateAsync("LabelSi");
            var LabelNo = await TranslateExtension.TranslateAsync("LabelNo");
            var LabelOK = await TranslateExtension.TranslateAsync("LabelOK");
            var LabelInformacion = await TranslateExtension.TranslateAsync("LabelInformacion");
            var LabelSeguroEliminarProtector = await TranslateExtension.TranslateAsync("LabelSeguroEliminarProtector");
            var LabelProtectorEliminado = await TranslateExtension.TranslateAsync("LabelProtectorEliminado");
            var MensajeError = await TranslateExtension.TranslateAsync("MensajeError");
            var LabelError = await TranslateExtension.TranslateAsync("LabelError");

            var confirmationPage = new ConfirmationPage(LabelSeguroEliminarProtector);
            confirmationPage.ConfirmationResult += async (sender, confirmed) =>
            {
                if (confirmed)
                {
                    EliminarProtectorRequest request = new EliminarProtectorRequest
                    {
                        UserIdThirdPartyProtector = protector.user_id_thirdParty_protector,
                        UserIdThirdPartyProtegido = protector.user_id_thirdParty_protegido,
                        Idioma = IdiomUtil.ObtenerCodigoDeIdioma()
                    };

                    IsRunning = true;
                    bool navigatedAway = false;
                    try
                    {
                        var response = await ApiService.EliminarProtector(request);

                        if (response.IsSuccess)
                        {
                            await ModernAlerts.ShowSuccess(LabelInformacion, LabelProtectorEliminado);
                            navigatedAway = true;
                            if (App.Current.MainPage.Navigation.ModalStack.Count > 0)
                                await App.Current.MainPage.Navigation.PopModalAsync();
                            await GetTabNavigation().PopToRootAsync();
                            return;
                        }
                        else
                        {
                            await ModernAlerts.ShowError(LabelError, response.Message);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (!navigatedAway)
                            await ModernAlerts.ShowWarning(LabelInformacion, MensajeError);
                        CrashlyticsHelper.LogError(ex, "MyProtectorsViewModel", "DeleteProtectorAsync");
                    }
                    finally
                    {
                        IsRunning = false;
                    }
                }
                // Solo cerrar el modal si la navegación exitosa no lo cerró ya
                if (App.Current.MainPage.Navigation.ModalStack.Count > 0)
                    await App.Current.MainPage.Navigation.PopModalAsync();
            };

            await App.Current.MainPage.Navigation.PushModalAsync(confirmationPage);
        }
    }
}


