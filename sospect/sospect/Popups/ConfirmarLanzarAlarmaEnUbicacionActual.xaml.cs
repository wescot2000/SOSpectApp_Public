using System;
using System.Collections.Generic;
using Rg.Plugins.Popup.Pages;
using Rg.Plugins.Popup.Services;
using sospect.ViewModels;
using Xamarin.Forms;

namespace sospect.Popups
{
    public partial class ConfirmarLanzarAlarmaEnUbicacionActual : PopupPage
    {
        public ConfirmarLanzarAlarmaEnUbicacionActual(double latitude, double longitude, int tipoAlarma)
        {
            InitializeComponent();
            BindingContext = new LanzarAlarmaViewModel(App.persona.user_id_thirdparty, latitude, longitude, tipoAlarma);
        }

        async void Cancelar_Alarma(System.Object sender, System.EventArgs e)
        {
            await PopupNavigation.Instance.PopAsync();
        }
    }
}

