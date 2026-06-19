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
using sospect.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace sospect.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AprobarSolicitudesPage : ContentPage
    {
        private readonly AprobarSolicitudesPageViewModel _viewModel;

        public AprobarSolicitudesPage()
        {
            InitializeComponent();
            _viewModel = new AprobarSolicitudesPageViewModel();
            BindingContext = _viewModel;
        }
    }
}


