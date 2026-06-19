// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

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


