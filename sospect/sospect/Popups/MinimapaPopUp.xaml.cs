using System;
using System.Collections.Generic;
using Rg.Plugins.Popup.Pages;
using sospect.CustomRenderers;
using Xamarin.Forms;
using Xamarin.Forms.Maps;

namespace sospect.Popups
{
    public partial class MinimapaPopUp : PopupPage
    {
        public MinimapaPopUp(object bindingContext)
        {
            InitializeComponent();
            BindingContext = bindingContext;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            Device.BeginInvokeOnMainThread(() =>
            {
                miMiniMapa.MoveToRegion(MapSpan.FromCenterAndRadius(
                    new Position(
                        App.ubicacionActual.latitud,
                        App.ubicacionActual.longitud),
                    Distance.FromMeters(100)));
            });
        }
    }
}

