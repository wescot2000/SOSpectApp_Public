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
    public partial class VerMapaPopup : Popup
    {
        public VerMapaPopup(AlarmaCercana alarmaCercana)
        {
            InitializeComponent();

            // SOLUCIÓN: Pasar referencia del popup al ViewModel para que pueda cerrarlo
            BindingContext = new VerMapaPopupViewModel(alarmaCercana, this);

            System.Diagnostics.Debug.WriteLine("VerMapaPopup: Popup creado con referencia al ViewModel");
        }
    }
}