using System;
using System.Windows.Input;
using Rg.Plugins.Popup.Services;
using sospect.Views;
using Xamarin.Forms;

namespace sospect.ViewModels
{
    public class ProtegidosPopupPageViewModel : BaseViewModel
    {
        public ICommand AgregarProtegidoCommand { get; }
        public ICommand AprobarSolicitudCommand { get; }
        public ICommand CompletarSubscrProtegidoCommand { get; }
        public ICommand ListarProtegidosCommand { get; }
        public ICommand ListarProtectoresCommand { get; }
        public ICommand CloseCommand { get; }

        public ProtegidosPopupPageViewModel()
        {
            AgregarProtegidoCommand = new Command(AgregarProtegidoOptions);
            AprobarSolicitudCommand = new Command(AprobarSolicitudOptions);
            CompletarSubscrProtegidoCommand = new Command(CompletarSubscrProtegidoOptions);
            ListarProtegidosCommand = new Command(ListarProtegidosOptions);
            ListarProtectoresCommand = new Command(ListarProtectoresOptions);
            CloseCommand = new Command(ClosePopup);
        }

        private async void AgregarProtegidoOptions()
        {
            await App.Current.MainPage.Navigation.PushAsync(new ProtectedSubscriptionPage());
            await PopupNavigation.Instance.PopAsync(); // Cierra la ventana emergente antes de navegar a la nueva página.
        }

        private async void AprobarSolicitudOptions()
        {
            await App.Current.MainPage.Navigation.PushAsync(new AprobarSolicitudesPage());
            await PopupNavigation.Instance.PopAsync(); // Cierra la ventana emergente antes de navegar a la nueva página.
        }

        private async void CompletarSubscrProtegidoOptions()
        {
            await App.Current.MainPage.Navigation.PushAsync(new CompleteSubscriptionPage());
            await PopupNavigation.Instance.PopAsync(); // Cierra la ventana emergente antes de navegar a la nueva página.
        }

        private async void ListarProtegidosOptions()
        {
            await App.Current.MainPage.Navigation.PushAsync(new ProtectedUsersPage());
            await PopupNavigation.Instance.PopAsync(); // Cierra la ventana emergente antes de navegar a la nueva página.
        }

        private async void ListarProtectoresOptions()
        {
            await App.Current.MainPage.Navigation.PushAsync(new MyProtectorsPage());
            await PopupNavigation.Instance.PopAsync(); // Cierra la ventana emergente antes de navegar a la nueva página.
        }

        private async void ClosePopup()
        {
            await PopupNavigation.Instance.PopAsync();
        }
    }
}

