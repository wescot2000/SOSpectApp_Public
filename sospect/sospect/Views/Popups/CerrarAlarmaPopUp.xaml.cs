// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
using System.Collections.Generic;
using CommunityToolkit.Maui.Views;
using sospect.Models;
using sospect.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace sospect.Views.Popups
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    [Obsolete("Reemplazado por CierreCapturaPage, CierreEncuestaPage, CierrePersonaPage, CierreMascotaPage y CierreBasicoPage. No eliminar hasta confirmar estabilidad.")]
    public partial class CerrarAlarmaPopUp : Popup
    {
        private bool _isClosing = false;
        private CerrarAlarmaPopUpViewModel _viewModel;

        public CerrarAlarmaPopUp(AlarmaCercana alarmaCercana)
        {
            InitializeComponent();
            _viewModel = new CerrarAlarmaPopUpViewModel(alarmaCercana);
            BindingContext = _viewModel;

            // Suscribirse al MessagingCenter para cerrar el popup
            MessagingCenter.Subscribe<CerrarAlarmaPopUpViewModel>(this, "CerrarPopup", async (sender) =>
            {
                if (!_isClosing)
                {
                    _isClosing = true;
                    await CloseAsync();
                }
            });
        }

        private void OnHuboCapturaToggled(object sender, ToggledEventArgs e)
        {
            if (e.Value) // Si el switch "¿Hubo captura?" se ha activado
            {
                FalsaAlarmaSwitch.IsToggled = false; // Desactivar el switch "¿Era falsa alarma?"
                FalsaAlarmaSwitch.IsEnabled = false; // Deshabilitar el switch "¿Era falsa alarma?"

                // Actualizar el ViewModel
                if (_viewModel != null)
                {
                    _viewModel.FlagEsFalsaAlarma = false;
                }
            }
            else
            {
                FalsaAlarmaSwitch.IsEnabled = true; // Habilitar el switch "¿Era falsa alarma?"
            }
        }

        private void OnFalsaAlarmaToggled(object sender, ToggledEventArgs e)
        {
            if (e.Value) // Si el switch "¿Era falsa alarma?" se ha activado
            {
                CapturaSwitch.IsToggled = false; // Desactivar el switch "¿Hubo captura?"
                CapturaSwitch.IsEnabled = false; // Deshabilitar el switch "¿Hubo captura?"

                // Actualizar el ViewModel
                if (_viewModel != null)
                {
                    _viewModel.FlagHuboCaptura = false;
                }
            }
            else
            {
                CapturaSwitch.IsEnabled = true; // Habilitar el switch "¿Hubo captura?"
            }
        }

        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();

            // Cleanup cuando el popup se cierra
            if (Handler == null)
            {
                MessagingCenter.Unsubscribe<CerrarAlarmaPopUpViewModel>(this, "CerrarPopup");
            }
        }
    }
}

