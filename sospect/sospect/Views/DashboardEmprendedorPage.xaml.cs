// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using Microsoft.Maui.Controls;
using sospect.ViewModels;

namespace sospect.Views
{
    /// <summary>
    /// Página de dashboard de gamificación para el emprendedor.
    /// Recibe el ReportsPageViewModel ya cargado para reutilizar los datos sin llamar a la API nuevamente.
    /// Creado: 2026-04-10
    /// </summary>
    public partial class DashboardEmprendedorPage : ContentPage
    {
        public DashboardEmprendedorPage(ReportsPageViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}


