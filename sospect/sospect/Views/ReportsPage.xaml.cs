using System;
using System.Collections.Generic;
using System.Windows.Input;
using sospect.ViewModels;
using Microsoft.Maui.Controls;
using sospect.Views.Popups;

namespace sospect.Views
{
    public partial class ReportsPage : ContentPage
    {
        private ReportsPageViewModel _viewModel;

        public ReportsPage()
        {
            InitializeComponent();
            _viewModel = new ReportsPageViewModel(Navigation);
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.CargarDashboardEmprendedorAsync();
        }
    }
}
