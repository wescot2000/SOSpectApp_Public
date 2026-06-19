// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text;
using System.Windows.Input;
using Newtonsoft.Json;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using sospect.ViewModels;
using sospect.Models;
using sospect.Helpers;

namespace sospect.Views
{
    public partial class SubscriptionsPage : ContentPage
    {
        public SubscriptionsPage()
        {
            InitializeComponent();

            BindingContext = new SubscriptionsPageViewModel();
        }

    }
}


