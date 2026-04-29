using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using sospect.Views;
using sospect.Helpers;
using CommunityToolkit.Maui.Views;

namespace sospect.ViewModels
{
    public class SuperPowersPopupViewModel : BaseViewModel
    {
        private readonly Popup _currentPopup;

        public ICommand ShowSubscriptionOptionsCommand { get; }
        public ICommand PurchaseSuperPowersCommand { get; }
        public ICommand CloseCommand { get; }

        public SuperPowersPopupViewModel(Popup popup = null)
        {
            _currentPopup = popup;
            ShowSubscriptionOptionsCommand = new Command(async () => await ShowSubscriptionOptions());
            PurchaseSuperPowersCommand = new Command(async () => await PurchaseSuperPowers());
            CloseCommand = new Command(async () => await ClosePopup());
        }

        private async Task ShowSubscriptionOptions()
        {
            try
            {
                await ClosePopup();

                // Pequeño delay para asegurar que el popup se cerró
                await Task.Delay(100);

                var navigation = GetCurrentNavigation();
                if (navigation != null)
                    await navigation.PushAsync(new SubscriptionValuesPage());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en ShowSubscriptionOptions: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");

                CrashlyticsHelper.LogError(ex, "SuperPowersPopupViewModel", "ShowSubscriptionOptions");

                var errorTitle = await TranslateExtension.TranslateAsync("LabelError") ?? "Error";
                var errorMessage = await TranslateExtension.TranslateAsync("LabelErrorReiniciar") ?? "Surgió un error inesperado. Por favor reinicia SOSpect. Si persiste el error contacta a soporte@wescot.com.co";
                await ModernAlerts.ShowError(errorTitle, errorMessage);
            }
        }

        private async Task PurchaseSuperPowers()
        {
            try
            {
                await ClosePopup();

                // Pequeño delay para asegurar que el popup se cerró
                await Task.Delay(100);

                var navigation = GetCurrentNavigation();
                if (navigation != null)
                    await navigation.PushAsync(new PurchaseSuperPowersPage());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en PurchaseSuperPowers: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");

                CrashlyticsHelper.LogError(ex, "SuperPowersPopupViewModel", "PurchaseSuperPowers");

                var errorTitle = await TranslateExtension.TranslateAsync("LabelError") ?? "Error";
                var errorMessage = await TranslateExtension.TranslateAsync("LabelErrorReiniciar") ?? "Surgió un error inesperado. Por favor reinicia SOSpect. Si persiste el error contacta a soporte@wescot.com.co";
                await ModernAlerts.ShowError(errorTitle, errorMessage);
            }
        }

        private INavigation GetCurrentNavigation()
        {
            var mainPage = Application.Current?.MainPage;
            if (mainPage is TabbedPage tabbedPage)
            {
                var currentPage = tabbedPage.CurrentPage;
                if (currentPage is NavigationPage navPage)
                    return navPage.Navigation;
                return currentPage?.Navigation;
            }
            if (mainPage is NavigationPage navigationPage)
                return navigationPage.Navigation;
            return mainPage?.Navigation;
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

                MessagingCenter.Send(this, "ClosePopup");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cerrando popup: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "SuperPowersPopupViewModel", "ClosePopup");
                MessagingCenter.Send(this, "ClosePopup");
            }
        }
    }
}