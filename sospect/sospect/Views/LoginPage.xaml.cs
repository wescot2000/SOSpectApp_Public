using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using sospect.AppConstants;
using sospect.AuthHelpers;
using sospect.Interfaces;
using sospect.Models;
using sospect.Services;
using sospect.ViewModels;
using Xamarin.Essentials;
using Xamarin.Forms;

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
