// Views/ReporteAutoridadPage.xaml.cs
// Módulo político: Code-behind para la página de reporte de desempeño de un político.
// Creado: 2026-03-09

using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using sospect.DTOs;
using sospect.ViewModels;

namespace sospect.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ReporteAutoridadPage : ContentPage
    {
        public ReporteAutoridadPage(AutoridadResponsableDto autoridad)
        {
            InitializeComponent();
            BindingContext = new ReporteAutoridadViewModel(autoridad, Navigation);
        }
    }
}
