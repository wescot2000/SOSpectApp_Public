// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

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

