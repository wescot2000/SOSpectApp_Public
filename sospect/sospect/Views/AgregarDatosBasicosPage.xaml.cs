using sospect.Models;
using sospect.ViewModels;
using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using System.Text.RegularExpressions;

namespace sospect.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AgregarDatosBasicosPage : ContentPage
    {
        public AgregarDatosBasicosPage()
        {
            InitializeComponent();
            BindingContext = new AgregarDatosBasicosPageViewModel();
        }

        private void OnNombresTextChanged(object sender, TextChangedEventArgs e)
        {
            var isValid = Regex.IsMatch(e.NewTextValue, @"^[a-zA-Z\s]+$");
            NombresErrorLabel.IsVisible = !isValid;
        }

        private void OnApellidosTextChanged(object sender, TextChangedEventArgs e)
        {
            var isValid = Regex.IsMatch(e.NewTextValue, @"^[a-zA-Z\s]+$");
            ApellidosErrorLabel.IsVisible = !isValid;
        }

        private void OnNumeroMovilTextChanged(object sender, TextChangedEventArgs e)
        {
            var isValid = Regex.IsMatch(e.NewTextValue, @"^[0-9]+$");
            NumeroMovilErrorLabel.IsVisible = !isValid;
        }

        private void OnEmailTextChanged(object sender, TextChangedEventArgs e)
        {
            var isValid = Regex.IsMatch(e.NewTextValue, @"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$");
            EmailErrorLabel.IsVisible = !isValid;
        }

        // OnPaisTextChanged eliminado 2026-02-26: País ahora usa Picker, no Entry libre

        private void OnNationalIdTextChanged(object sender, TextChangedEventArgs e)
        {
            var isValid = Regex.IsMatch(e.NewTextValue, @"^[a-zA-Z0-9.-]+$");
            NationalIdErrorLabel.IsVisible = !isValid;
        }
    }
}
