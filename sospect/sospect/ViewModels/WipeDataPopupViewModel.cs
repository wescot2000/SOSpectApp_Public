using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Views;
using sospect.CustomRenderers;
using sospect.Extensions;
using sospect.Helpers;
using sospect.Models;
using sospect.Resources;
using sospect.Services;
using sospect.Utils;
using sospect.Views;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
#if ANDROID
using Plugin.FirebasePushNotifications;
#endif
using sospect.Interfaces;

namespace sospect.ViewModels
{
    public class WipeDataPopupViewModel : BaseViewModel
    {
        // CRÍTICO: Referencia al popup para poder cerrarlo
        private readonly Popup _currentPopup;

        public Command CancelarCommand { get; set; }
        public Command ConfirmarWipeData { get; set; }

        private string _usuarioIdIngresado;
        public string UsuarioIdIngresado
        {
            get => _usuarioIdIngresado;
            set => SetProperty(ref _usuarioIdIngresado, value);
        }

        // MODIFICADO: Constructor recibe referencia al popup
        public WipeDataPopupViewModel(Popup popup = null)
        {
            _currentPopup = popup;

            var p_user_id_thirdparty = App.persona.user_id_thirdparty;

            var LabelInformacion = TranslateExtension.Translate("LabelInformacion");
            var LabelError = TranslateExtension.Translate("LabelError");
            var ElValorNoCorrespondeAsuID = TranslateExtension.Translate("ElValorNoCorrespondeAsuID");
            var SeEliminaronTodosSusDatos = TranslateExtension.Translate("SeEliminaronTodosSusDatos");
            var LabelOK = TranslateExtension.Translate("LabelOK");
            var FailureDeletingData = TranslateExtension.Translate("FailureDeletingData");

            ConfirmarWipeData = new Command(async () =>
            {
                if (IsRunning) return;

                if (UsuarioIdIngresado != App.persona.user_id_thirdparty)
                {
                    await ModernAlerts.ShowError(LabelError, ElValorNoCorrespondeAsuID);
                    return;
                }

                var request = new WipeDataRequest()
                {
                    p_user_id_thirdparty = p_user_id_thirdparty
                };

                MainThread.BeginInvokeOnMainThread(() => IsRunning = true);

                try
                {
                    ResponseMessage response = await ApiService.WipeUserData(request);

                    if (response.IsSuccess)
                    {
                        // CRÍTICO: Cerrar popup ANTES de mostrar alert
                        await _currentPopup?.CloseAsync();

                        await ModernAlerts.ShowSuccess(LabelInformacion, SeEliminaronTodosSusDatos);

                        await SecureStorage.SetAsync("access_token", string.Empty);
                        #if ANDROID
                        await IFirebasePushNotification.Current.UnregisterForPushNotificationsAsync();
                        #endif

                        Preferences.Clear();
                        App.Current.MainPage = new NavigationPage(new LoginPage()) { BarBackgroundColor = Colors.Black };
                    }
                    else
                    {
                        await ModernAlerts.ShowInfo(LabelInformacion, string.IsNullOrWhiteSpace(response?.Message) ? FailureDeletingData : response.Message);
                    }
                }
                catch (Exception ex)
                {
                    await ModernAlerts.ShowError(LabelInformacion, ex.Message);
                    CrashlyticsHelper.LogError(ex, "WipeDataPopupViewModel", "ConfirmarWipeData");
                }
                finally
                {
                    MainThread.BeginInvokeOnMainThread(() => IsRunning = false);
                }

            }, () => !IsRunning);

            // MODIFICADO: Cerrar popup al cancelar
            CancelarCommand = new Command(async () =>
            {
                if (IsRunning) return;

                // CRÍTICO: Cerrar el popup
                await _currentPopup?.CloseAsync();

            }, () => !IsRunning);
        }
    }
}