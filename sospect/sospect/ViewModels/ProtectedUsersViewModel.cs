using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using sospect.Models;
using Newtonsoft.Json;
using Microsoft.Maui.Controls;
using sospect.Services;
using sospect.Utils;
using sospect.Helpers;
using sospect.Views;
using System.Collections.Specialized;
using sospect.Interfaces;

namespace sospect.ViewModels
{
    public class ProtectedUsersViewModel : BaseViewModel
    {
        private ObservableCollection<ProtectedUserData> _ProtectedUsers;
        private bool _isListEmpty;

        public ObservableCollection<ProtectedUserData> ProtectedUsers
        {
            get => this._ProtectedUsers;
            set => this.SetValue(ref this._ProtectedUsers, value);
        }

        public Command DeleteProtectedUserCommand { get; set; }

        public ProtectedUsersViewModel()
        {
            ProtectedUsers = new ObservableCollection<ProtectedUserData>();
            DeleteProtectedUserCommand = new Command<ProtectedUserData>(DeleteProtectedUserAsync);
            ProtectedUsers.CollectionChanged += OnProtectedUserChanged;

            // Inicializar IsListEmpty en true
            IsListEmpty = true;

            _ = Task.Run(() => LoadProtectedUsersAsync());
        }

        private void OnProtectedUserChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            IsListEmpty = ProtectedUsers.Count == 0;
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

        public bool IsListEmpty
        {
            get { return _isListEmpty; }
            set { SetProperty(ref _isListEmpty, value); }
        }

        private async Task LoadProtectedUsersAsync()
        {
            IsRunning = true;
            try
            {
                var users = await ApiService.GetProtectedUsersAsync();

                // SIEMPRE actualizar la colección (incluso si está vacía)
                if (users != null && users.Any())
                {
                    ProtectedUsers = new ObservableCollection<ProtectedUserData>(users);
                }
                else
                {
                    ProtectedUsers = new ObservableCollection<ProtectedUserData>();
                }

                // Forzar actualización (por si CollectionChanged no se dispara)
                IsListEmpty = ProtectedUsers.Count == 0;
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ProtectedUsersViewModel", "LoadProtectedUsersAsync");

                // En error, vaciar lista
                ProtectedUsers = new ObservableCollection<ProtectedUserData>();
                IsListEmpty = true;
            }
            finally
            {
                IsRunning = false;
            }
        }

        private async void DeleteProtectedUserAsync(ProtectedUserData user)
        {
            var LblSeguroEliminarProtegido = TranslateExtension.Translate("LblSeguroEliminarProtegido");

            var confirmationPage = new ConfirmationPage(LblSeguroEliminarProtegido);
            confirmationPage.ConfirmationResult += async (sender, confirmed) =>
            {
                if (confirmed)
                {
                    var LblSubscrEliminada = await TranslateExtension.TranslateAsync("LblSubscrEliminada");
                    var LabelError = await TranslateExtension.TranslateAsync("LabelError");
                    var LblSubsEliminadaExitosamente = await TranslateExtension.TranslateAsync("LblSubsEliminadaExitosamente");
                    var LabelOK = await TranslateExtension.TranslateAsync("LabelOK");
                    EliminarProtegidoRequest eliminarProtegidoRequest = new EliminarProtegidoRequest()
                    {
                        p_user_id_thirdparty_protector = App.persona.user_id_thirdparty,
                        p_user_id_thirdparty_protegido = user.User_Id_ThirdParty_Protegido,
                        Idioma = IdiomUtil.ObtenerCodigoDeIdioma()
                    };

                    if (user != null)
                    {
                        IsRunning = true;
                        bool navigatedAway = false;
                        try
                        {
                            ResponseMessage response = await ApiService.DeleteProtectedUserAsync(eliminarProtegidoRequest);

                            if (response.IsSuccess)
                            {
                                var LabelInformacion = await TranslateExtension.TranslateAsync("LabelInformacion");
                                await ModernAlerts.ShowSuccess(LabelInformacion, LblSubsEliminadaExitosamente);
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
                            {
                                var LabelInformacion = await TranslateExtension.TranslateAsync("LabelInformacion");
                                var MensajeError = await TranslateExtension.TranslateAsync("MensajeError");
                                await ModernAlerts.ShowWarning(LabelInformacion, MensajeError);
                            }
                            CrashlyticsHelper.LogError(ex, "ProtectedUsersViewModel", "DeleteProtectedUserAsync");
                        }
                        finally
                        {
                            IsRunning = false;
                        }
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