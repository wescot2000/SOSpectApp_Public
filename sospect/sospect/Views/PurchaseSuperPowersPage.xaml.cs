using sospect.ViewModels;
using Xamarin.Forms;

namespace sospect.Views
{
    public partial class PurchaseSuperPowersPage : ContentPage
    {
        public PurchaseSuperPowersPage()
        {
            InitializeComponent();
            BindingContext = new PurchaseSuperPowersViewModel();
        }
    }
}
