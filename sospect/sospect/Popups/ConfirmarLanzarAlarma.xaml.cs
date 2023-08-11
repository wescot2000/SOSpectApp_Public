using System;
using System.Collections.Generic;
using Xamarin.Forms;
using Rg.Plugins.Popup.Pages;
using Rg.Plugins.Popup.Services;
using sospect.Models;
using sospect.ViewModels;

namespace sospect.Popups
{
    public partial class ConfirmarLanzarAlarma : PopupPage
    {
        LanzarAlarmaViewModel vm;

        public ConfirmarLanzarAlarma(double latitude, double longitude)
        {
            InitializeComponent();
            vm = new LanzarAlarmaViewModel(App.persona.user_id_thirdparty, latitude, longitude);
            BindingContext = vm;
        }

        async void Cancelar_Alarma(System.Object sender, System.EventArgs e)
        {
            await PopupNavigation.Instance.PopAsync();
        }
    }
}

