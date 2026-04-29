using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using sospect.ViewModels;

namespace sospect.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ConfiguracionParaTiPage : ContentPage
    {
        public ConfiguracionParaTiPage()
        {
            InitializeComponent();
            BindingContext = new ConfiguracionParaTiViewModel();
        }
    }
}
