// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

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


