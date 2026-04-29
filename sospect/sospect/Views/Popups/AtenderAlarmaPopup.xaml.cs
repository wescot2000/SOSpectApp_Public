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
    public partial class AtenderAlarmaPopup : Popup
    {
        public AtenderAlarmaPopup(AlarmaCercana alarmaCercana)
        {
            InitializeComponent();

            // CRÍTICO: Pasar la referencia del popup al ViewModel
            BindingContext = new AtenderAlarmaPopupViewModel(alarmaCercana, this);
        }
    }
}