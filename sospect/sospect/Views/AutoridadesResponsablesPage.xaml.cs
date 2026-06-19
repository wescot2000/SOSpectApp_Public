// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

// Views/AutoridadesResponsablesPage.xaml.cs
// Creado: 2026-03-05
// Módulo: Autoridades Políticas Responsables

using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using sospect.ViewModels;

namespace sospect.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AutoridadesResponsablesPage : ContentPage
    {
        private readonly AutoridadesResponsablesViewModel _viewModel;

        public AutoridadesResponsablesPage(long alarmaId, string? tituloAlarma = null)
        {
            InitializeComponent();
            _viewModel = new AutoridadesResponsablesViewModel(alarmaId, tituloAlarma);
            BindingContext = _viewModel;
        }
    }
}


