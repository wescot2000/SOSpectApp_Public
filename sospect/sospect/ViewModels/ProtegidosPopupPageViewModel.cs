// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
using System.Windows.Input;
using CommunityToolkit.Maui.Views;
using sospect.Views;
using sospect.Helpers;
using Microsoft.Maui.Controls;

namespace sospect.ViewModels
{
    public class ProtegidosPopupPageViewModel : BaseViewModel
    {
        private readonly Popup _currentPopup;

        public ICommand AgregarProtegidoCommand { get; }
        public ICommand AprobarSolicitudCommand { get; }
        public ICommand CompletarSubscrProtegidoCommand { get; }
        public ICommand ListarProtegidosCommand { get; }
        public ICommand ListarProtectoresCommand { get; }
        public ICommand CloseCommand { get; }

        public ProtegidosPopupPageViewModel(Popup popup = null)
        {
            _currentPopup = popup;

            AgregarProtegidoCommand = new Command(async () => await AgregarProtegidoOptions());
            AprobarSolicitudCommand = new Command(async () => await AprobarSolicitudOptions());
            CompletarSubscrProtegidoCommand = new Command(async () => await CompletarSubscrProtegidoOptions());
            ListarProtegidosCommand = new Command(async () => await ListarProtegidosOptions());
            ListarProtectoresCommand = new Command(async () => await ListarProtectoresOptions());
            CloseCommand = new Command(async () => await ClosePopup());
        }

        private async Task AgregarProtegidoOptions()
        {
            try
            {
                await ClosePopup();
                await GetCurrentTabNavigation().PushAsync(new ProtectedSubscriptionPage());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en AgregarProtegidoOptions: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "ProtegidosPopupPageViewModel", "AgregarProtegidoOptions");

                var errorTitle = await TranslateExtension.TranslateAsync("LabelError") ?? "Error";
                var errorMessage = await TranslateExtension.TranslateAsync("LabelErrorReiniciar") ?? "Surgió un error inesperado. Por favor reinicia SOSpect. Si persiste el error contacta a soporte@wescot.com.co";

                await ModernAlerts.ShowError(errorTitle, errorMessage);
            }
        }

        private async Task AprobarSolicitudOptions()
        {
            try
            {
                await ClosePopup();
                await GetCurrentTabNavigation().PushAsync(new AprobarSolicitudesPage());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en AprobarSolicitudOptions: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "ProtegidosPopupPageViewModel", "AprobarSolicitudOptions");

                var errorTitle = await TranslateExtension.TranslateAsync("LabelError") ?? "Error";
                var errorMessage = await TranslateExtension.TranslateAsync("LabelErrorReiniciar") ?? "Surgió un error inesperado. Por favor reinicia SOSpect. Si persiste el error contacta a soporte@wescot.com.co";

                await ModernAlerts.ShowError(errorTitle, errorMessage);
            }
        }

        private async Task CompletarSubscrProtegidoOptions()
        {
            try
            {
                await ClosePopup();
                await GetCurrentTabNavigation().PushAsync(new CompleteSubscriptionPage());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en CompletarSubscrProtegidoOptions: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "ProtegidosPopupPageViewModel", "CompletarSubscrProtegidoOptions");

                var errorTitle = await TranslateExtension.TranslateAsync("LabelError") ?? "Error";
                var errorMessage = await TranslateExtension.TranslateAsync("LabelErrorReiniciar") ?? "Surgió un error inesperado. Por favor reinicia SOSpect. Si persiste el error contacta a soporte@wescot.com.co";

                await ModernAlerts.ShowError(errorTitle, errorMessage);
            }
        }

        private async Task ListarProtegidosOptions()
        {
            try
            {
                await ClosePopup();
                await GetCurrentTabNavigation().PushAsync(new ProtectedUsersPage());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en ListarProtegidosOptions: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "ProtegidosPopupPageViewModel", "ListarProtegidosOptions");

                var errorTitle = await TranslateExtension.TranslateAsync("LabelError") ?? "Error";
                var errorMessage = await TranslateExtension.TranslateAsync("LabelErrorReiniciar") ?? "Surgió un error inesperado. Por favor reinicia SOSpect. Si persiste el error contacta a soporte@wescot.com.co";

                await ModernAlerts.ShowError(errorTitle, errorMessage);
            }
        }

        private async Task ListarProtectoresOptions()
        {
            try
            {
                await ClosePopup();
                await GetCurrentTabNavigation().PushAsync(new MyProtectorsPage());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en ListarProtectoresOptions: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "ProtegidosPopupPageViewModel", "ListarProtectoresOptions");

                var errorTitle = await TranslateExtension.TranslateAsync("LabelError") ?? "Error";
                var errorMessage = await TranslateExtension.TranslateAsync("LabelErrorReiniciar") ?? "Surgió un error inesperado. Por favor reinicia SOSpect. Si persiste el error contacta a soporte@wescot.com.co";

                await ModernAlerts.ShowError(errorTitle, errorMessage);
            }
        }

        private async Task ClosePopup()
        {
            try
            {
                if (_currentPopup != null)
                {
                    await _currentPopup.CloseAsync();
                    return;
                }

                // Método alternativo
                MessagingCenter.Send(this, "ClosePopup");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cerrando popup: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "ProtegidosPopupPageViewModel", "ClosePopup");
                MessagingCenter.Send(this, "ClosePopup");
            }
        }

        /// <summary>
        /// Obtiene la navegación del tab actual cuando MainPage es un TabbedPage.
        /// Si MainPage es TabbedPage, obtiene la Navigation del CurrentPage (NavigationPage del tab activo).
        /// Si no, usa la navegación estándar de MainPage.
        /// </summary>
        private INavigation GetCurrentTabNavigation()
        {
            if (Application.Current.MainPage is TabbedPage tabbedPage)
            {
                // El CurrentPage de un TabbedPage es el NavigationPage del tab activo
                if (tabbedPage.CurrentPage is NavigationPage navPage)
                {
                    return navPage.Navigation;
                }
                // Fallback: si el tab actual no es NavigationPage, usar su Navigation directamente
                return tabbedPage.CurrentPage?.Navigation ?? tabbedPage.Navigation;
            }

            // Fallback para cuando MainPage no es TabbedPage (ej: NavigationPage directo)
            return Application.Current.MainPage.Navigation;
        }
    }
}

