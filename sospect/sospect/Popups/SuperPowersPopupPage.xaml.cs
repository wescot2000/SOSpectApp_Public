using Rg.Plugins.Popup.Pages;
using Xamarin.Forms.Xaml;
using sospect.ViewModels;

namespace sospect.Popups
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SuperPowersPopupPage : PopupPage
    {
        public SuperPowersPopupPage()
        {
            InitializeComponent();
            BindingContext = new SuperPowersPopupViewModel();
        }
    }
}
