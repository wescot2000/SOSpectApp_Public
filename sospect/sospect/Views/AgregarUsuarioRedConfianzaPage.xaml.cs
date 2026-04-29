using sospect.Models;
using sospect.ViewModels;
using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace sospect.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AgregarUsuarioRedConfianzaPage : ContentPage
    {
        public AgregarUsuarioRedConfianzaPage()
        {
            InitializeComponent();
            BindingContext = new AgregarUsuarioRedConfianzaPageViewModel();
        }
    }
}
