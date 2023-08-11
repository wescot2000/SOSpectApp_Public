using System;
using System.Collections.Generic;
using System.Reflection;
using System.Timers;
using Rg.Plugins.Popup.Pages;
using Rg.Plugins.Popup.Services;
using sospect.Models;
using sospect.ViewModels;
using Xamarin.Forms;

namespace sospect.Popups
{
    public partial class LanzarAlarmaPopup : PopupPage
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
            await PopupNavigation.Instance.PopAsync();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            Device.StartTimer(new TimeSpan(0, 0, 1), () =>
            {
                // do something every 1 seconds
                Device.BeginInvokeOnMainThread(async () =>
                {
                    // interact with UI elements
                    vm.CuentaRegresivaAlarma = (int.Parse(vm.CuentaRegresivaAlarma) - 1).ToString();
                    if (vm.CuentaRegresivaAlarma == "0" && vm.IsTimeRunning)
                    {
                        vm.IsTimeRunning = false;
                        if (PopupNavigation.Instance.PopupStack.Count > 0)
                        {
                            await PopupNavigation.Instance.PopAsync();
                        }
                        await vm.RegistrarAlarma();
                    }
                });
                return vm.IsTimeRunning; // runs again, or false to stop
            });
        }

        async void CancelarAlarma(System.Object sender, System.EventArgs e)
        {
            await PopupNavigation.Instance.PopAsync();
            vm.IsTimeRunning = false;
        }
    }
}

