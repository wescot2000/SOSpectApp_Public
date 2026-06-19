// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using Microsoft.Maui.Controls;
using sospect.ViewModels;
using Microsoft.Maui.Controls.Xaml;

namespace sospect.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]

    public partial class ConfiguracionNotificacionesPage : ContentPage
    {
        private readonly ConfiguracionNotificacionesViewModel _viewModel;

        public ConfiguracionNotificacionesPage()
        {
            InitializeComponent();
            _viewModel = new ConfiguracionNotificacionesViewModel();
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.CargarConfiguracionesAsync();
        }
    }
}


