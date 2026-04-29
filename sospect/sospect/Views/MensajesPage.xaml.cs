using System;
using System.Collections.Generic;
using System.Windows.Input;
using sospect.ViewModels;
using Microsoft.Maui.Controls;
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
                await ModernAlerts.ShowInfo(LabelInformacion, LblSeMarcaronComoLeidos);
            });
        }

        private async void MessagesListView_ItemTapped(object sender, ItemTappedEventArgs e)
        {
            var message = e.Item as Mensajes;
            if (message != null)
            {
                // Navegar al detalle del mensaje
                await Navigation.PushAsync(new MensajeDetallePage(message.mensaje_id));

                // Al volver del detalle, marcar el mensaje como leído en la UI
                // El API ya lo marcó como leído en la BD al cargar el detalle
                message.estado = false;
            }

            // Anula la selección del elemento en la lista
            if (sender is ListView listView)
            {
                listView.SelectedItem = null;
            }
        }

    }
}
