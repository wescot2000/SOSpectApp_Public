using System.Windows.Input;
using Xamarin.Forms;
using sospect.Views;
using Rg.Plugins.Popup.Services;

namespace sospect.ViewModels
{
    public class SuperPowersPopupViewModel : BaseViewModel
    {
        public ICommand ShowSubscriptionOptionsCommand { get; }
        public ICommand PurchaseSuperPowersCommand { get; }
        public ICommand CloseCommand { get; }

        public SuperPowersPopupViewModel()
        {
            ShowSubscriptionOptionsCommand = new Command(ShowSubscriptionOptions);
            PurchaseSuperPowersCommand = new Command(PurchaseSuperPowers);
            CloseCommand = new Command(ClosePopup);
        }

        private async void ShowSubscriptionOptions()
        {
            await Application.Current.MainPage.Navigation.PushAsync(new SubscriptionValuesPage());
            await PopupNavigation.Instance.PopAsync(); // Cierra la ventana emergente antes de navegar a la nueva página.
        }

        private async void PurchaseSuperPowers()
        {
            await Application.Current.MainPage.Navigation.PushAsync(new PurchaseSuperPowersPage());
            await PopupNavigation.Instance.PopAsync(); // Cierra la ventana emergente antes de navegar a la nueva página.
        }

        private async void ClosePopup()
        {
            await PopupNavigation.Instance.PopAsync();
        }
    }
}
