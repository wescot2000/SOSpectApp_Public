
using System;
using System.Collections.Generic;
using Rg.Plugins.Popup.Pages;
using Rg.Plugins.Popup.Services;
using sospect.Models;
using sospect.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace sospect.Popups
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CerrarAlarmaPopUp : PopupPage
    {
        public CerrarAlarmaPopUp(AlarmaCercana alarmaCercana)
        {
            InitializeComponent();
            BindingContext = new CerrarAlarmaPopUpViewModel(alarmaCercana);
        }
        private void OnHuboCapturaToggled(object sender, ToggledEventArgs e)
        {
            if (e.Value) // Si el switch "¿Hubo captura?" se ha activado
            {
                FalsaAlarmaSwitch.IsToggled = false; // Desactivar el switch "¿Era falsa alarma?"
                FalsaAlarmaSwitch.IsEnabled = false; // Deshabilitar el switch "¿Era falsa alarma?"
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
            }
            else
            {
                CapturaSwitch.IsEnabled = true; // Habilitar el switch "¿Hubo captura?"
            }
        }

    }
}

