using sospect.ViewModels;
using Microsoft.Maui.Controls;

namespace sospect.Views
{
    public partial class PurchaseSuperPowersPage : ContentPage
    {
        public PurchaseSuperPowersPage()
        {
            InitializeComponent();
            BindingContext = new PurchaseSuperPowersViewModel();
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}
