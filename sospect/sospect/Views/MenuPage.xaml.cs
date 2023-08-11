using System;
using System.Collections.Generic;
using System.Windows.Input;
using sospect.ViewModels;
using Xamarin.Forms;
using sospect.Popups;

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

