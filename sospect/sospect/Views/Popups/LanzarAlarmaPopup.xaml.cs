using System;
using System.Collections.Generic;
using System.Reflection;
using System.Timers;
using CommunityToolkit.Maui.Views;
using sospect.Models;
using sospect.ViewModels;
using Microsoft.Maui.Controls;

namespace sospect.Views.Popups
{
    public partial class LanzarAlarmaPopup : Popup
    {
        LanzarAlarmaViewModel vm;

        public LanzarAlarmaPopup(string thirdPartyId, double latitude, double longitude)
        {
            InitializeComponent();
            vm = new LanzarAlarmaViewModel(thirdPartyId, latitude, longitude);
            vm.CuentaRegresivaAlarma = "5";
            BindingContext = vm;
        }

        private async void OnClose(object sender, EventArgs e)
        {
            CloseAsync();
        }


        async void CancelarAlarma(System.Object sender, System.EventArgs e)
        {
            CloseAsync();
            vm.IsTimeRunning = false;
        }
    }
}

