// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

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
using Microsoft.Maui.Controls;
using sospect.Helpers;
using sospect.Interfaces;

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
                CrashlyticsHelper.LogError(ex, "MessagesViewModel", "GetMessagesAsync");
                return new List<Mensajes>();
            }
            finally
            {
                IsRunning = false;
            }
            
        }

        private async Task MarkAllAsRead()
        {
            var LabelOK = await TranslateExtension.TranslateAsync("LabelOK");
            var LabelInformacion = await TranslateExtension.TranslateAsync("LabelInformacion");
            var MensajeError = await TranslateExtension.TranslateAsync("MensajeError");

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
                await ModernAlerts.ShowWarning(LabelInformacion, MensajeError);
                CrashlyticsHelper.LogError(ex, "MessagesViewModel", "MarkAllAsRead");
            }
            finally
            {
                IsRunning = false;
            }
            
        }
    }
}


