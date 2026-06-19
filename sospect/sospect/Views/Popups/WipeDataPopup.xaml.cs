// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
using System.Collections.Generic;
using Microsoft.Maui.Controls;
using CommunityToolkit.Maui.Views;
using sospect.Models;
using sospect.ViewModels;

namespace sospect.Views.Popups
{
    public partial class WipeDataPopup : Popup
    {
        private WipeDataPopupViewModel _viewModel;

        public WipeDataPopup()
        {
            InitializeComponent();

            // CRÍTICO: Pasar la referencia del popup al ViewModel
            _viewModel = new WipeDataPopupViewModel(this);
            BindingContext = _viewModel;
        }
    }
}

