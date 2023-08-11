using System;
using System.Collections.Generic;
using System.Windows.Input;
using sospect.ViewModels;
using Xamarin.Forms;
using Newtonsoft.Json;
using sospect.Models;
using sospect.Helpers;

namespace sospect.Views
{
    public partial class MensajesPage : ContentPage
    {

        public MensajesPage(HomeViewModel homeViewModel)
        {
            var LabelInformacion = TranslateExtension.Translate("LabelInformacion");
            var LblSeMarcaronComoLeidos = TranslateExtension.Translate("LblSeMarcaronComoLeidos");
            var LabelOK = TranslateExtension.Translate("LabelOK");
            BindingContext = new MessagesViewModel();
            InitializeComponent();
            homeViewModel.NumeroDeNotificaciones = 0;

            MessagingCenter.Subscribe<MessagesViewModel>(this, "MarkAllAsReadSuccess", async (sender) =>
            {
                await DisplayAlert(LabelInformacion, LblSeMarcaronComoLeidos, LabelOK);
            });
        }

        private async void MessagesListView_ItemTapped(object sender, ItemTappedEventArgs e)
        {
            var message = e.Item as Mensajes;
            if (message != null)
            {
                // Reemplaza "MessageDetailPage" con el nombre real de la página de detalles del mensaje que vas a crear
                await Navigation.PushAsync(new MensajeDetallePage(message.mensaje_id));
            }

            // Anula la selección del elemento en la lista
            if (sender is ListView listView)
            {
                listView.SelectedItem = null;
            }
        }

    }
}
