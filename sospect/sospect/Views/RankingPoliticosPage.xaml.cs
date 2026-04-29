// Views/RankingPoliticosPage.xaml.cs
// Módulo político: Code-behind para la página de ranking de políticos locales.
// Creado: 2026-04-07

using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using sospect.ViewModels;

namespace sospect.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class RankingPoliticosPage : ContentPage
    {
        public RankingPoliticosPage()
        {
            InitializeComponent();
            BindingContext = new RankingPoliticosViewModel();
        }
    }
}
