using sospect.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace sospect.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class TermsAndConditionsPage : ContentPage
    {
        public TermsAndConditionsPage()
        {
            InitializeComponent();
            BindingContext = new TermsAndConditionsViewModel();
        }
    }
}
