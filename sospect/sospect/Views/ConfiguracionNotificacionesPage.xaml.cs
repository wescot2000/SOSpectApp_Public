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
