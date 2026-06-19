// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using sospect.Helpers;
using sospect.Models;
using sospect.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace sospect.Views
{
    /// <summary>
    /// Página de lista de chats para el creador de la promoción (estilo WhatsApp)
    /// Muestra todos los chats activos ordenados por mensaje más reciente
    /// Agregado: 02-02-2026
    /// </summary>
    public partial class ListaChatsPromocionPage : ContentPage
    {
        private readonly long _alarmaId;
        private List<ChatResumenModel> _chats = new();

        public ListaChatsPromocionPage(long alarmaId)
        {
            InitializeComponent();
            _alarmaId = alarmaId;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CargarChatsAsync();
        }

        private async Task CargarChatsAsync()
        {
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;

            try
            {
                System.Diagnostics.Debug.WriteLine($"ListaChatsPromocionPage: Cargando chats para alarma {_alarmaId}");

                // Llamar API para obtener lista de chats
                var chats = await ApiService.ListarChatsPorAlarma(_alarmaId);

                if (chats != null && chats.Count > 0)
                {
                    _chats = chats;
                    ListaChats.ItemsSource = _chats;
                    System.Diagnostics.Debug.WriteLine($"ListaChatsPromocionPage: Se encontraron {chats.Count} chats");
                }
                else
                {
                    _chats = new List<ChatResumenModel>();
                    ListaChats.ItemsSource = _chats;
                    System.Diagnostics.Debug.WriteLine("ListaChatsPromocionPage: No se encontraron chats");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ListaChatsPromocionPage.CargarChatsAsync: Error: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "ListaChatsPromocionPage", "CargarChatsAsync");
                await ModernAlerts.ShowError("Error", TranslateExtension.Translate("ErrorGenerico"));
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
            }
        }

        /// <summary>
        /// Navega al chat tocado (via TapGestureRecognizer en el Frame del item)
        /// </summary>
        private async void OnChatItemTapped(object sender, EventArgs e)
        {
            try
            {
                if (sender is Frame frame && frame.BindingContext is ChatResumenModel chat)
                {
                    System.Diagnostics.Debug.WriteLine($"ListaChatsPromocionPage: Navegando a ChatPublicidadPage para chat {chat.ChatId}");

                    // Navegar a ChatPublicidadPage con el chat_id existente
                    await Navigation.PushAsync(new ChatPublicidadPage(_alarmaId, chat.ChatId));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ListaChatsPromocionPage.OnChatItemTapped: Error: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "ListaChatsPromocionPage", "OnChatItemTapped");
            }
        }
    }
}


