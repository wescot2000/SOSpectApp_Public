// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
using System.Collections.Generic;
using System.Windows.Input;
using sospect.ViewModels;
using Microsoft.Maui.Controls;
using sospect.Views.Popups;

namespace sospect.Views
{
    public partial class MenuPage : ContentPage
    {
        private MenuPageViewModel _viewModel;

        public MenuPage()
        {
            InitializeComponent();
            BindingContext = new MenuPageViewModel(Navigation);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            MenuPageViewModel.DatosActualizados += ActualizarDatos;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            MenuPageViewModel.DatosActualizados -= ActualizarDatos;
        }

        void ActualizarDatos(object sender, EventArgs e)
        {
            ((MenuPageViewModel)BindingContext).ActualizarDatos(sender, e);
        }
    }
}

