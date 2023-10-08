
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
    public partial class AtenderAlarmaPopup : PopupPage
    {
        public AtenderAlarmaPopup(AlarmaCercana alarmaCercana)
        {
            InitializeComponent();
            BindingContext = new AtenderAlarmaPopupViewModel(alarmaCercana);
        }
       
    }
}

