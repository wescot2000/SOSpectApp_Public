using System;
using Microsoft.Maui.Controls;
using sospect.Models;
using sospect.ViewModels;

namespace sospect.Views
{
    public partial class LoginPage : ContentPage
    {
        public LoginPage()
        {
            InitializeComponent();
            Title = "SOSpect";
            BindingContext = new LoginViewModel();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (BindingContext is LoginViewModel vm && vm.IsRunning)
            {
                vm.IsRunning = false;
            }
        }
    }
}