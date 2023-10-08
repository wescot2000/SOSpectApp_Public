using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using sospect.Models;
using Newtonsoft.Json;
using Xamarin.Forms;
using sospect.Services;
using sospect.Utils;
using sospect.Helpers;
using sospect.Views;
using System.Collections.Specialized;

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
            _ = Task.Run(() => LoadProtectedUsersAsync());
        }

        private void OnProtectedUserChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            IsListEmpty = ProtectedUsers.Count == 0;
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
                

                if (users.Any())
                {
                    ProtectedUsers = new ObservableCollection<ProtectedUserData>(users);
                    IsListEmpty = ProtectedUsers.Count == 0;
                }
            }
            catch (Exception ex)
            {
                var properties = new Dictionary<string, string> {
                        { "Object", "ProtectedUsersViewModel" },
                        { "Method", "LoadProtectedUsersAsync" }
                        };
                Microsoft.AppCenter.Crashes.Crashes.TrackError(ex, properties);
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
                    var LblSubscrEliminada = TranslateExtension.Translate("LblSubscrEliminada");
                    var LblSubsEliminadaExitosamente = TranslateExtension.Translate("LblSubsEliminadaExitosamente");
                    var LabelOK = TranslateExtension.Translate("LabelOK");
                    EliminarProtegidoRequest eliminarProtegidoRequest = new EliminarProtegidoRequest()
                    {
                        p_user_id_thirdparty_protector = App.persona.user_id_thirdparty,
                        p_user_id_thirdparty_protegido = user.User_Id_ThirdParty_Protegido,
                        Idioma = IdiomUtil.ObtenerCodigoDeIdioma()
                    };

                    if (user != null)
                    {
                        IsRunning = true;
                        try
                        {
                            ResponseMessage response = await ApiService.DeleteProtectedUserAsync(eliminarProtegidoRequest);
                            
                            if (response.IsSuccess)
                            {
                                await App.Current.MainPage.DisplayAlert(LblSubscrEliminada, LblSubsEliminadaExitosamente, LabelOK);
                                ProtectedUsers.Remove(user);
                            }
                        }
                        catch (Exception ex)
                        {
                            var properties = new Dictionary<string, string> {
                                    { "Object", "ProtectedUsersViewModel" },
                                    { "Method", "DeleteProtectedUserAsync" }
                                };
                            Microsoft.AppCenter.Crashes.Crashes.TrackError(ex, properties);
                        }
                        finally
                        {
                            IsRunning = false;
                        }
                        
                    }
                }
                await App.Current.MainPage.Navigation.PopModalAsync();
            };

            await App.Current.MainPage.Navigation.PushModalAsync(confirmationPage);
        }
    }
}