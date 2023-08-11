using System;
using System.Collections.Generic;
using System.Windows.Input;
using sospect.ViewModels;
using Xamarin.Forms;
using sospect.Popups;

namespace sospect.Views
{
    public partial class ReportsPage : ContentPage
    {
        private ReportsPageViewModel _viewModel;
        public ReportsPage()
        {
            InitializeComponent();

            BindingContext = new ReportsPageViewModel(Navigation);

        }

    }
}

