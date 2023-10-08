using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using System.Threading.Tasks;
using sospect.Models;
using sospect.Services;
using System.Linq;
using System.Windows.Input;
using Xamarin.Forms;
using sospect.Helpers;

namespace sospect.ViewModels
{

    public class MessagesViewModel : BaseViewModel
    {
        public ObservableCollection<Mensajes> Messages { get; set; }
        private bool _isListEmpty;

        public ICommand MarkAllAsReadCommand { get; private set; }

        public MessagesViewModel()
        {
            Messages = new ObservableCollection<Mensajes>();
            LoadMessages();

            MarkAllAsReadCommand = new Command(async () => await MarkAllAsRead());
        }
        public bool IsListEmpty
        {
            get { return _isListEmpty; }
            set { SetProperty(ref _isListEmpty, value); }
        }

        private async void LoadMessages()
        {
            var messages = await GetMessagesAsync();
            Messages.Clear();
            foreach (var message in messages)
            {
                Messages.Add(message);
                IsListEmpty = Messages.Count == 0;
            }
            IsRunning = false;
        }

        public async Task<List<Mensajes>> GetMessagesAsync()
        {
            IsRunning = true;
            try
            {
                var response = await ApiService.ObtenerMensajes();
                if (response is List<Mensajes> lstMensajes && lstMensajes.Any())
                {
                    EmptyState = false;
                    return lstMensajes;
                }
                else
                {
                    EmptyState = true;
                    return new List<Mensajes>();
                }
            }
            catch (Exception ex)
            {
                var properties = new Dictionary<string, string> {
                        { "Object", "MessagesViewModel" },
                        { "Method", "ObtenerMensajes" }
                    };
                Microsoft.AppCenter.Crashes.Crashes.TrackError(ex, properties);
                return new List<Mensajes>();
            }
            finally
            {
                IsRunning = false;
            }
            
        }

        private async Task MarkAllAsRead()
        {
            var LabelOK = TranslateExtension.Translate("LabelOK");
            var LabelInformacion = TranslateExtension.Translate("LabelInformacion");
            var MensajeError = TranslateExtension.Translate("MensajeError");

            IsRunning = true;
            MarcarMensajesLeidosRequest request = new MarcarMensajesLeidosRequest
            {
                PUserIdThirdparty = App.persona.user_id_thirdparty
            };
            try
            {
                var response = await ApiService.MarcaTodosLeidos(request);
                if (response)
                {
                    foreach (var message in Messages)
                    {
                        message.estado = false;
                    }
                    MessagingCenter.Send(this, "MarkAllAsReadSuccess");
                }
            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert(LabelInformacion, MensajeError, LabelOK);
                var properties = new Dictionary<string, string> {
                        { "Object", "MessagesViewModel" },
                        { "Method", "MarcaTodosLeidos" }
                    };
                Microsoft.AppCenter.Crashes.Crashes.TrackError(ex, properties);
            }
            finally
            {
                IsRunning = false;
            }
            
        }
    }
}