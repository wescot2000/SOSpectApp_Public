using sospect.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace sospect.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SubscriptionValuesPage : ContentPage
    {
        private SubscriptionValuesPageViewModel viewModel;

        public SubscriptionValuesPage()
        {
            InitializeComponent();
            BindingContext = new SubscriptionValuesPageViewModel();
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}
