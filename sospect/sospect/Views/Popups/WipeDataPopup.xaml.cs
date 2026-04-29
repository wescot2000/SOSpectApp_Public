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