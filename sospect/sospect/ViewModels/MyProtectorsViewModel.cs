using Newtonsoft.Json;
using sospect.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using sospect.Services;
using sospect.Utils;
using sospect.Helpers;
using sospect.Views;
using System.Collections.Specialized;

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
        private async Task LoadProtectorsAsync()
        {
            var LabelOK = TranslateExtension.Translate("LabelOK");
            var LabelInformacion = TranslateExtension.Translate("LabelInformacion");
            var MensajeError = TranslateExtension.Translate("MensajeError");
            IsRunning = true;
            try
            {
                var response = await ApiService.ObtenerProtectores(App.persona.user_id_thirdparty, IdiomUtil.ObtenerCodigoDeIdioma());
                if (response.isSuccess)
                {
                    foreach (var protector in response.data)
                    {
                        Protectors.Add(protector);
                        IsListEmpty = Protectors.Count == 0;
                    }
                }
            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert(LabelInformacion, MensajeError, LabelOK);
                var properties = new Dictionary<string, string> {
                        { "Object", "MyProtectorsViewModel" },
                        { "Method", "ObtenerProtectores" }
                    };
                Microsoft.AppCenter.Crashes.Crashes.TrackError(ex, properties);
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
            var LblSuspenderProtectores = TranslateExtension.Translate("LblSuspenderProtectores");
            var LabelSi = TranslateExtension.Translate("LabelSi");
            var LabelNo = TranslateExtension.Translate("LabelNo");
            var LabelOK = TranslateExtension.Translate("LabelOK");
            var LabelInformacion = TranslateExtension.Translate("LabelInformacion");
            var LblSuspensionRealizada = TranslateExtension.Translate("LblSuspensionRealizada");
            var MensajeError = TranslateExtension.Translate("MensajeError");

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
                    try
                    {
                        var response = await ApiService.SuspenderPermisoAProtector(request);
                        

                        if (response.IsSuccess)
                        {
                            await App.Current.MainPage.DisplayAlert(LabelInformacion, LblSuspensionRealizada, LabelOK);

                        }
                    }
                    catch (Exception ex)
                    {
                        await App.Current.MainPage.DisplayAlert(LabelInformacion, MensajeError, LabelOK);
                        var properties = new Dictionary<string, string> {
                        { "Object", "MyProtectorsViewModel" },
                        { "Method", "SuspendProtectorAsync" }
                        };
                        Microsoft.AppCenter.Crashes.Crashes.TrackError(ex, properties);
                    }
                    finally
                    {
                        IsRunning = false;
                    }
                }
                await App.Current.MainPage.Navigation.PopModalAsync();
            };
            await App.Current.MainPage.Navigation.PushModalAsync(confirmationPage);
        }

        private async void DeleteProtectorAsync(ProtectorData protector)
        {
            var LabelSi = TranslateExtension.Translate("LabelSi");
            var LabelNo = TranslateExtension.Translate("LabelNo");
            var LabelOK = TranslateExtension.Translate("LabelOK");
            var LabelInformacion = TranslateExtension.Translate("LabelInformacion");
            var LabelSeguroEliminarProtector = TranslateExtension.Translate("LabelSeguroEliminarProtector");
            var LabelProtectorEliminado = TranslateExtension.Translate("LabelProtectorEliminado");
            var MensajeError = TranslateExtension.Translate("MensajeError");

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
                    try
                    {
                        var response = await ApiService.EliminarProtector(request);
                        
                        if (response.IsSuccess)
                        {
                            // Actualizar la interfaz de usuario, si es necesario
                            Protectors.Remove(protector);
                            await App.Current.MainPage.DisplayAlert(LabelInformacion, LabelProtectorEliminado, LabelOK);

                        }
                    }
                    catch (Exception ex)
                    {
                        await App.Current.MainPage.DisplayAlert(LabelInformacion, MensajeError, LabelOK);
                        var properties = new Dictionary<string, string> {
                        { "Object", "MyProtectorsViewModel" },
                        { "Method", "EliminarProtector" }
                        };
                        Microsoft.AppCenter.Crashes.Crashes.TrackError(ex, properties);
                    }
                    finally
                    {
                        IsRunning = false;
                    }
                    
                }
                await App.Current.MainPage.Navigation.PopModalAsync();
            };

            await App.Current.MainPage.Navigation.PushModalAsync(confirmationPage);

        }
    }
}
