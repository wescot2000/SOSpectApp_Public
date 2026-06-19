// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

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

