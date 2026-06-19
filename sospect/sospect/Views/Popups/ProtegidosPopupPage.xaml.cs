// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
using System.Collections.Generic;
using Microsoft.Maui.Controls;
using CommunityToolkit.Maui.Views;
using sospect.Models;
using sospect.ViewModels;
using System.Windows.Input;
using Microsoft.Maui.Controls.Xaml;
using sospect.Views;
using sospect.Helpers;

namespace sospect.Views.Popups
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ProtegidosPopupPage : Popup
    {
        private ProtegidosPopupPageViewModel _viewModel;

        public ProtegidosPopupPage()
        {
            InitializeComponent();

            // Pasar referencia de este popup al ViewModel
            _viewModel = new ProtegidosPopupPageViewModel(this);
            BindingContext = _viewModel;

            // Suscribirse al mensaje de cerrar popup
            MessagingCenter.Subscribe<ProtegidosPopupPageViewModel>(this, "ClosePopup", async (sender) =>
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
                CrashlyticsHelper.LogError(ex, "ProtegidosPopupPage", "ClosePopupSafely");
            }
        }

        // Cleanup cuando se cierra el popup
        private void CleanupSubscriptions()
        {
            try
            {
                MessagingCenter.Unsubscribe<ProtegidosPopupPageViewModel>(this, "ClosePopup");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error limpiando suscripciones: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "ProtegidosPopupPage", "CleanupSubscriptions");
            }
        }

        // Método que se llama cuando el popup se está cerrando
        public void OnPopupClosed()
        {
            CleanupSubscriptions();
        }
    }
}

