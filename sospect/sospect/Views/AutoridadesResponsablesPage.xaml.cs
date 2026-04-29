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
