using System;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls.Xaml;
using sospect.ViewModels;
using sospect.Helpers;

namespace sospect.Views.Popups
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SuperPowersPopupPage : Popup
    {
        private SuperPowersPopupViewModel _viewModel;

        public SuperPowersPopupPage()
        {
            InitializeComponent();

            // Pasar referencia de este popup al ViewModel
            _viewModel = new SuperPowersPopupViewModel(this);
            BindingContext = _viewModel;

            // Suscribirse al mensaje de cerrar popup
            MessagingCenter.Subscribe<SuperPowersPopupViewModel>(this, "ClosePopup", async (sender) =>
            {
                await ClosePopupSafely();
            });
        }

        private async Task ClosePopupSafely()
        {
            try
            {
                await this.CloseAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cerrando popup: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "SuperPowersPopupPage", "ClosePopupSafely");
            }
        }

        // Cleanup cuando se cierra el popup
        private void CleanupSubscriptions()
        {
            try
            {
                MessagingCenter.Unsubscribe<SuperPowersPopupViewModel>(this, "ClosePopup");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error limpiando suscripciones: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "SuperPowersPopupPage", "CleanupSubscriptions");
            }
        }

        // M�todo que se llama cuando el popup se est� cerrando
        public void OnPopupClosed()
        {
            CleanupSubscriptions();
        }
    }
}