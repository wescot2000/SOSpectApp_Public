using sospect.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace sospect.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SubscriptionValuesPage : ContentPage
    {
        private SubscriptionValuesPageViewModel viewModel;

        public SubscriptionValuesPage()
        {
            InitializeComponent();
            BindingContext = new SubscriptionValuesPageViewModel();
        }
    }
}
