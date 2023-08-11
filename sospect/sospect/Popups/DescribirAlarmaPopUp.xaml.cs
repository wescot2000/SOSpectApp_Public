
using System;
using System.Collections.Generic;
using Rg.Plugins.Popup.Pages;
using Rg.Plugins.Popup.Services;
using sospect.Models;
using sospect.ViewModels;
using Xamarin.Forms;

namespace sospect.Popups
{
    public partial class DescribirAlarmaPopUp : PopupPage
    {
        public DescribirAlarmaPopUp(AlarmaCercana alarmaCercana)
        {
            InitializeComponent();
            BindingContext = new DescribirAlarmaPopUpViewModel(alarmaCercana);
        }

        public DescribirAlarmaPopUp(DescribirAlarma alarmaAEditar)
        {
            InitializeComponent();
            BindingContext = new DescribirAlarmaPopUpViewModel(alarmaAEditar);
        }

        void Abrir_MapaClicked(System.Object sender, System.EventArgs e)
        {
            PopupNavigation.Instance.PushAsync(new MinimapaPopUp(BindingContext));
        }
    }
}

