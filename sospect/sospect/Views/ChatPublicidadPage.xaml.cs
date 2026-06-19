// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using sospect.Resources;
using sospect.Helpers;
using sospect.Models;
using sospect.AuthHelpers;
using sospect.Services;
using sospect.Views.Popups;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Maui.Extensions;

namespace sospect.Views
{
    public partial class ChatPublicidadPage : ContentPage
    {
        private long _chatId;
        private long _alarmaId;
        private bool _estaActivo = false;
        private bool _proveedorAcepto = false;
        private bool _interesadoAcepto = false;
        private bool _soyProveedor = false;
        private DateTime? _fechaProveedorAcepto = null;
        private DateTime? _fechaInteresadoAcepto = null;
        private bool _chatYaExiste = false; // Para cuando el creador entra a un chat existente
        private bool _terminosAceptadosEnVolante = false; // Términos ya aceptados en DetallePromocionVistaPage
        private System.Timers.Timer _refreshTimer; // Timer para refresco automático de mensajes
        private string _interesadoUserId = null; // user_id_thirdparty del interesado (para título dinámico)
        private string _nombreNegocio = null; // nombre_emprendimiento del proveedor (para título dinámico)
        // 2026-04-12: Track del último mensaje_id renderizado para evitar Children.Clear() en Android
        private long _ultimoMensajeIdRenderizado = 0;

        // Colección de mensajes enlazada al CollectionView (reemplaza StackLayout dinámico)
        private readonly ObservableCollection<MensajeChatDto> _mensajes = new ObservableCollection<MensajeChatDto>();

        // Lista local de archivos multimedia pendientes de envío (igual que DescribirAlarmaPage)
        private const int MaxImageWidth = 1024;
        private const int JpegQuality = 60;
        private List<MediaFile> _mediaFiles = new List<MediaFile>();

        /// <summary>
        /// Constructor para el TOMADOR (interesado) - Inicia o abre chat existente.
        /// terminosAceptadosEnVolante=true cuando el usuario ya aceptó los términos en DetallePromocionVistaPage.
        /// nombreNegocio: nombre del emprendimiento para establecer el título inmediatamente (sin esperar al API).
        /// </summary>
        public ChatPublicidadPage(long alarmaId, bool terminosAceptadosEnVolante = false, string nombreNegocio = null)
        {
            InitializeComponent();
            MensajesCollectionView.ItemsSource = _mensajes;
            _alarmaId = alarmaId;
            _chatId = 0; // Se obtendrá al iniciar chat
            _soyProveedor = false;
            _chatYaExiste = false;
            _terminosAceptadosEnVolante = terminosAceptadosEnVolante;
            // Establecer título inmediatamente si ya conocemos el nombre del negocio
            if (!string.IsNullOrEmpty(nombreNegocio))
            {
                _nombreNegocio = nombreNegocio;
                this.Title = nombreNegocio;
            }
            System.Diagnostics.Debug.WriteLine($"ChatPublicidadPage: Constructor TOMADOR para alarma {alarmaId}, terminosVolante={terminosAceptadosEnVolante}, nombreNegocio={nombreNegocio ?? "(null)"}");
        }

        /// <summary>
        /// Constructor para el CREADOR (proveedor) - Abre chat existente directamente
        /// Agregado: 02-02-2026
        /// </summary>
        public ChatPublicidadPage(long alarmaId, long chatId)
        {
            InitializeComponent();
            MensajesCollectionView.ItemsSource = _mensajes;
            _alarmaId = alarmaId;
            _chatId = chatId;
            _soyProveedor = true; // El creador siempre entra por este constructor
            _chatYaExiste = true;
            _estaActivo = true; // Si tiene chatId, el chat ya está activo
            System.Diagnostics.Debug.WriteLine($"ChatPublicidadPage: Constructor CREADOR para alarma {alarmaId}, chat {chatId}");
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Suprimir notificaciones push de esta alarma mientras el chat esté abierto
            App.ChatPublicidadAbierto = true;
            App.ChatPublicidadAlarmaIdActivo = _alarmaId;

            // 2026-04-13: Si ya hay mensajes renderizados (re-appearing por SospectTabs o vuelta de
            // una popup/cámara), NO hacer recarga completa — solo refresco incremental.
            // La recarga completa hace Children.Clear() que en Android edge-to-edge deja el
            // StackLayout con altura 0 hasta el próximo toque del usuario.
            bool esReaparicion = (_ultimoMensajeIdRenderizado > 0);

            if (_chatYaExiste && _chatId > 0)
            {
                // CREADOR: Chat ya existe, solo cargar datos y mensajes
                System.Diagnostics.Debug.WriteLine($"ChatPublicidadPage.OnAppearing: Chat existente {_chatId} reaparicion={esReaparicion}");
                if (!esReaparicion)
                    await CargarDetalleChatExistenteAsync();
                await CargarMensajesAsync(esRecargaCompleta: !esReaparicion);
                if (!esReaparicion)
                    await ScrollAlFinalAsync();
            }
            else
            {
                // TOMADOR: Iniciar o abrir chat
                System.Diagnostics.Debug.WriteLine($"ChatPublicidadPage.OnAppearing: Iniciando chat como tomador reaparicion={esReaparicion}");
                if (!esReaparicion)
                    await CargarDatosChatAsync();
                await CargarMensajesAsync(esRecargaCompleta: !esReaparicion);
                if (!esReaparicion)
                    await ScrollAlFinalAsync();
            }

            // Iniciar timer de refresco automático cada 30 segundos
            IniciarTimerRefresco();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            // Restaurar flags de supresión de notificaciones
            App.ChatPublicidadAbierto = false;
            App.ChatPublicidadAlarmaIdActivo = 0;

            // Detener timer de refresco
            DetenerTimerRefresco();
        }

        private void IniciarTimerRefresco()
        {
            DetenerTimerRefresco();
            _refreshTimer = new System.Timers.Timer(30000); // 30 segundos
            _refreshTimer.Elapsed += async (s, e) =>
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    try
                    {
                        if (_chatId > 0)
                            await CargarMensajesAsync();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"ChatPublicidadPage.Timer: Error: {ex.Message}");
                    }
                });
            };
            _refreshTimer.AutoReset = true;
            _refreshTimer.Start();
        }

        private void DetenerTimerRefresco()
        {
            if (_refreshTimer != null)
            {
                _refreshTimer.Stop();
                _refreshTimer.Dispose();
                _refreshTimer = null;
            }
        }

        /// <summary>
        /// Establece el título de la página dinámicamente según el rol:
        /// - Creador (proveedor): ve el user_id del interesado truncado a 22 chars + "..."
        /// - Cliente (interesado): ve el nombre del negocio; si vacío, el user_id del proveedor truncado
        /// 2026-04-10: eliminado \n en título (no funciona en navigation bar de iOS/Android);
        ///             truncado con "..." para que no quede cortado abruptamente.
        /// </summary>
        private void ActualizarTituloDinamico()
        {
            if (_soyProveedor)
            {
                // Creador ve el user_id del interesado truncado
                if (!string.IsNullOrEmpty(_interesadoUserId))
                    this.Title = _interesadoUserId.Length > 22
                        ? _interesadoUserId.Substring(0, 22) + "..."
                        : _interesadoUserId;
            }
            else
            {
                // Cliente ve el nombre del negocio (si existe), sino el user_id del proveedor truncado
                if (!string.IsNullOrEmpty(_nombreNegocio))
                    this.Title = _nombreNegocio;
                else if (!string.IsNullOrEmpty(_interesadoUserId))
                    this.Title = _interesadoUserId.Length > 22
                        ? _interesadoUserId.Substring(0, 22) + "..."
                        : _interesadoUserId;
            }
        }

        /// <summary>
        /// Scroll al final del chat (mensaje más reciente)
        /// </summary>
        private Task ScrollAlFinalAsync()
        {
            try
            {
                if (_mensajes.Count > 0)
                {
                    var ultimo = _mensajes[_mensajes.Count - 1];
                    MensajesCollectionView.ScrollTo(ultimo, position: ScrollToPosition.End, animate: false);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ChatPublicidadPage.ScrollAlFinalAsync: Error: {ex.Message}");
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Carga el detalle de un chat existente (para el creador)
        /// Agregado: 02-02-2026
        /// </summary>
        private async Task CargarDetalleChatExistenteAsync()
        {
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsRunning = true;

                using (var client = new JWTHttpClient(new System.Net.Http.HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                }, false))
                {
                    // Obtener detalle completo del chat
                    string url = AppConfiguration.ApiHost + "/ChatPublicidad/ObtenerDetalleChat";

                    var requestData = new
                    {
                        chat_id = _chatId,
                        user_id_thirdparty = App.persona.user_id_thirdparty
                    };

                    var content = new System.Net.Http.StringContent(
                        JsonConvert.SerializeObject(requestData),
                        System.Text.Encoding.UTF8,
                        "application/json"
                    );

                    var response = await client.PostAPIAsync(url, content);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var responseObj = JsonConvert.DeserializeObject<JObject>(responseContent);

                        if (responseObj["isSuccess"]?.ToObject<bool>() == true)
                        {
                            var data = responseObj["data"];

                            // Estado del chat
                            string estado = data?["estado"]?.ToString() ?? "active";
                            _estaActivo = (estado == "active");

                            // Descripción promocional
                            var descripcion = data?["descripcionPromocion"]?.ToString();
                            if (!string.IsNullOrEmpty(descripcion))
                            {
                                LblTextoPromocional.Text = descripcion;
                            }
                            else
                            {
                                LblTextoPromocional.Text = "Promoción local";
                            }

                            // Logo
                            var urlLogo = data?["urlLogo"]?.ToString();
                            if (!string.IsNullOrEmpty(urlLogo))
                            {
                                ImgLogo.Source = ImageSource.FromUri(new Uri(urlLogo));
                                ImgLogo.IsVisible = true;
                            }

                            // Domicilio habilitado
                            var domicilioHabilitado = data?["domicilioHabilitado"]?.ToObject<bool>() ?? false;
                            LblDomicilio.IsVisible = domicilioHabilitado;

                            // Fechas de aceptación
                            _fechaProveedorAcepto = data?["fechaProveedorAcepto"]?.ToObject<DateTime?>();
                            _fechaInteresadoAcepto = data?["fechaInteresadoAcepto"]?.ToObject<DateTime?>();

                            // Título dinámico: creador ve user_id del interesado
                            _interesadoUserId = data?["interesadoUserId"]?.ToString();
                            _nombreNegocio = data?["nombreNegocio"]?.ToString();
                            ActualizarTituloDinamico();

                            // Actualizar UI
                            ActualizarEstadoChat();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ChatPublicidadPage", "CargarDetalleChatExistenteAsync");
                Console.WriteLine($"[ChatPub] CargarDetalleChatExistenteAsync EXCEPCION: {ex.Message}");
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
            }
        }

        private async Task CargarDatosChatAsync()
        {
            try
            {
                Console.WriteLine($"[ChatPub] CargarDatosChatAsync START - alarmaId={_alarmaId}, terminosVolante={_terminosAceptadosEnVolante}");
                Console.WriteLine($"[ChatPub] App.persona={App.persona?.user_id_thirdparty ?? "NULL"}");

                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsRunning = true;

                using (var client = new JWTHttpClient(new System.Net.Http.HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                }, false))
                {
                    string url = AppConfiguration.ApiHost + "/ChatPublicidad/IniciarChat";
                    Console.WriteLine($"[ChatPub] URL={url}");

                    var requestData = new
                    {
                        alarma_id = _alarmaId,
                        user_id_thirdparty_interesado = App.persona.user_id_thirdparty
                    };

                    Console.WriteLine($"[ChatPub] Request: alarma_id={_alarmaId}, user={App.persona.user_id_thirdparty}");

                    var content = new System.Net.Http.StringContent(
                        JsonConvert.SerializeObject(requestData),
                        System.Text.Encoding.UTF8,
                        "application/json"
                    );

                    var response = await client.PostAPIAsync(url, content);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    Console.WriteLine($"[ChatPub] HTTP StatusCode={(int)response.StatusCode} {response.StatusCode}");
                    Console.WriteLine($"[ChatPub] Response body={responseContent}");

                    if (response.IsSuccessStatusCode)
                    {
                        var responseObj = JsonConvert.DeserializeObject<JObject>(responseContent);
                        bool isSuccess = responseObj["isSuccess"]?.ToObject<bool>() ?? false;
                        Console.WriteLine($"[ChatPub] isSuccess={isSuccess}");

                        if (isSuccess)
                        {
                            var dataObj = responseObj["data"];

                            _chatId = dataObj["chatId"]?.ToObject<long>() ?? 0;
                            string estado = dataObj["estado"]?.ToString() ?? "pending";
                            _estaActivo = (estado == "active");
                            _proveedorAcepto = dataObj["proveedorAceptoTerminos"]?.ToObject<bool>() ?? false;
                            _interesadoAcepto = dataObj["interesadoAceptoTerminos"]?.ToObject<bool>() ?? false;

                            Console.WriteLine($"[ChatPub] chatId={_chatId}, estado={estado}, estaActivo={_estaActivo}, proveedorAcepto={_proveedorAcepto}, interesadoAcepto={_interesadoAcepto}");

                            // Determinar si soy el proveedor
                            _soyProveedor = false; // El interesado siempre es quien inicia el chat

                            // Obtener descripción de la alarma
                            Console.WriteLine($"[ChatPub] Llamando ObtenerDescripcionAlarmaAsync para chat {_chatId}");
                            await ObtenerDescripcionAlarmaAsync(client);

                            // Si el usuario ya aceptó términos en DetallePromocionVistaPage,
                            // aceptar automáticamente sin mostrar el botón de aceptación
                            if (_terminosAceptadosEnVolante && !_interesadoAcepto && _chatId > 0)
                            {
                                Console.WriteLine($"[ChatPub] Auto-aceptando términos (aceptados en volante) para chat {_chatId}");
                                await AceptarTerminosAutoAsync(client);
                            }

                            // Actualizar UI según estado
                            Console.WriteLine($"[ChatPub] Llamando ActualizarEstadoChat, estaActivo={_estaActivo}");
                            ActualizarEstadoChat();
                            Console.WriteLine($"[ChatPub] CargarDatosChatAsync completado OK");
                        }
                        else
                        {
                            string mensaje = responseObj["message"]?.ToString();
                            Console.WriteLine($"[ChatPub] isSuccess=false, message='{mensaje}'");
                            var labelError = await TranslateExtension.TranslateAsync("LabelError");
                            var labelErrorDesconocido = await TranslateExtension.TranslateAsync("LabelErrorDesconocido");
                            await ModernAlerts.ShowError(labelError, string.IsNullOrEmpty(mensaje) ? labelErrorDesconocido : mensaje);
                            await Navigation.PopAsync();
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[ChatPub] HTTP error {(int)response.StatusCode}: {responseContent}");
                        var labelError = await TranslateExtension.TranslateAsync("LabelError");
                        var chatErrorIniciar = await TranslateExtension.TranslateAsync("ChatErrorIniciar");
                        string detalle = string.IsNullOrWhiteSpace(responseContent) ? $"HTTP {(int)response.StatusCode}" : responseContent;
                        await ModernAlerts.ShowError(labelError, $"{chatErrorIniciar}: {detalle}");
                        await Navigation.PopAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChatPub] EXCEPCION {ex.GetType().Name}: {ex.Message}");
                Console.WriteLine($"[ChatPub] StackTrace: {ex.StackTrace}");
                CrashlyticsHelper.LogError(ex, "ChatPublicidadPage", "CargarDatosChatAsync");
                var labelError = await TranslateExtension.TranslateAsync("LabelError");
                var chatErrorCargar = await TranslateExtension.TranslateAsync("ChatErrorCargar");
                string msgError = string.IsNullOrEmpty(ex.Message) ? ex.GetType().Name : ex.Message;
                await ModernAlerts.ShowError(labelError, $"{chatErrorCargar}: {msgError}");
                await Navigation.PopAsync();
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
            }
        }

        /// <summary>
        /// Auto-acepta los términos del chat cuando el interesado ya los aceptó en DetallePromocionVistaPage.
        /// Evita mostrar el botón de aceptación dentro del chat.
        /// </summary>
        private async Task AceptarTerminosAutoAsync(JWTHttpClient client)
        {
            try
            {
                Console.WriteLine($"[ChatPub-AutoTerminos] AceptarTerminosAutoAsync - chatId={_chatId}");
                string url = AppConfiguration.ApiHost + "/ChatPublicidad/AceptarTerminos";
                var requestData = new
                {
                    chat_id = _chatId,
                    user_id_thirdparty = App.persona.user_id_thirdparty
                };
                var content = new System.Net.Http.StringContent(
                    JsonConvert.SerializeObject(requestData),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );
                var response = await client.PostAPIAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[ChatPub-AutoTerminos] HTTP={((int)response.StatusCode)}, body={responseContent}");
                if (response.IsSuccessStatusCode)
                {
                    var responseObj = JsonConvert.DeserializeObject<JObject>(responseContent);
                    if (responseObj["isSuccess"]?.ToObject<bool>() == true)
                    {
                        _interesadoAcepto = true;
                        _estaActivo = true;
                        Console.WriteLine($"[ChatPub-AutoTerminos] Términos auto-aceptados OK para chat {_chatId}");
                    }
                    else
                    {
                        Console.WriteLine($"[ChatPub-AutoTerminos] isSuccess=false, message={responseObj["message"]}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChatPub-AutoTerminos] EXCEPCION {ex.GetType().Name}: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "ChatPublicidadPage", "AceptarTerminosAutoAsync");
            }
        }

        private async Task ObtenerDescripcionAlarmaAsync(JWTHttpClient client)
        {
            try
            {
                // Obtener detalle completo del chat con información promocional
                // 2026-04-10: cambiado de GET a POST para pasar user_id_thirdparty y recibir nombreNegocio correctamente
                string url = AppConfiguration.ApiHost + "/ChatPublicidad/ObtenerDetalleChat";
                Console.WriteLine($"[ChatPub-Detalle] ObtenerDescripcionAlarmaAsync - URL={url}, chat_id={_chatId}");

                var requestData = new
                {
                    chat_id = _chatId,
                    user_id_thirdparty = App.persona.user_id_thirdparty
                };
                var content = new System.Net.Http.StringContent(
                    JsonConvert.SerializeObject(requestData),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await client.PostAPIAsync(url, content);
                Console.WriteLine($"[ChatPub-Detalle] HTTP StatusCode={(int)response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[ChatPub-Detalle] Response body={responseContent}");
                    var responseObj = JsonConvert.DeserializeObject<JObject>(responseContent);

                    if (responseObj["isSuccess"]?.ToObject<bool>() == true)
                    {
                        var data = responseObj["data"];

                        // Descripción promocional
                        var descripcion = data?["descripcionPromocion"]?.ToString();
                        if (!string.IsNullOrEmpty(descripcion))
                        {
                            LblTextoPromocional.Text = descripcion;
                        }
                        else
                        {
                            LblTextoPromocional.Text = "Promoción local";
                        }

                        // Logo (prioridad: logo de subscripción, luego logo de emprendimiento)
                        var urlLogo = data?["urlLogo"]?.ToString();
                        if (!string.IsNullOrEmpty(urlLogo))
                        {
                            ImgLogo.Source = ImageSource.FromUri(new Uri(urlLogo));
                            ImgLogo.IsVisible = true;
                        }

                        // Domicilio habilitado
                        var domicilioHabilitado = data?["domicilioHabilitado"]?.ToObject<bool>() ?? false;
                        LblDomicilio.IsVisible = domicilioHabilitado;

                        // Fechas de aceptación de términos
                        _fechaProveedorAcepto = data?["fechaProveedorAcepto"]?.ToObject<DateTime?>();
                        _fechaInteresadoAcepto = data?["fechaInteresadoAcepto"]?.ToObject<DateTime?>();

                        // Título dinámico: cliente ve nombre del negocio
                        _interesadoUserId = data?["interesadoUserId"]?.ToString();
                        _nombreNegocio = data?["nombreNegocio"]?.ToString();
                        ActualizarTituloDinamico();

                        // Cargar fotos (si existen)
                        var fotos = data?["fotos"]?.ToObject<List<string>>();
                        if (fotos != null && fotos.Count > 0)
                        {
                            // TODO: Mostrar galería de fotos en el header
                            // Por ahora solo mostramos la primera foto si no hay logo
                            if (string.IsNullOrEmpty(urlLogo) && fotos.Count > 0)
                            {
                                ImgLogo.Source = ImageSource.FromUri(new Uri(fotos[0]));
                                ImgLogo.IsVisible = true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChatPub-Detalle] EXCEPCION {ex.GetType().Name}: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "ChatPublicidadPage", "ObtenerDescripcionAlarmaAsync");
                LblTextoPromocional.Text = "Promoción local";
            }
        }

        private async void ActualizarEstadoChat()
        {
            if (_estaActivo)
            {
                // Chat activo - habilitar inputs
                FramePendiente.IsVisible = false;
                EntryMensaje.IsEnabled = true;
                BtnEnviar.IsEnabled = true;
                // Border no tiene IsEnabled que bloquee TapGestureRecognizer: usamos InputTransparent + opacidad
                BtnAdjuntar.InputTransparent = false;
                BtnAdjuntar.Opacity = 1.0;

                // Mostrar recordatorio visual de aceptación de términos
                await MostrarRecordatorioAceptacionTerminos();
            }
            else
            {
                // Chat pendiente - mostrar estado y deshabilitar inputs
                FramePendiente.IsVisible = true;
                EntryMensaje.IsEnabled = false;
                BtnEnviar.IsEnabled = false;
                BtnAdjuntar.InputTransparent = true;
                BtnAdjuntar.Opacity = 0.4;

                // Actualizar labels de estado
                var chatAceptado = await TranslateExtension.TranslateAsync("ChatAceptado");
                var chatPendiente = await TranslateExtension.TranslateAsync("ChatPendiente");

                LblProveedorEstado.Text = _proveedorAcepto
                    ? chatAceptado
                    : chatPendiente;

                LblUsuarioEstado.Text = _interesadoAcepto
                    ? chatAceptado
                    : chatPendiente;

                // Mostrar botón de aceptar solo si el usuario no ha aceptado
                if (!_soyProveedor && !_interesadoAcepto)
                {
                    BtnAceptarTerminos.IsVisible = true;
                }
                else if (_soyProveedor && !_proveedorAcepto)
                {
                    BtnAceptarTerminos.IsVisible = true;
                }
                else
                {
                    BtnAceptarTerminos.IsVisible = false;
                }
            }
        }

        private async Task MostrarRecordatorioAceptacionTerminos()
        {
            // Mostrar recordatorio solo si el chat está activo
            if (!_estaActivo) return;

            if (_soyProveedor && _fechaProveedorAcepto.HasValue)
            {
                // Vista de proveedor: mostrar cuándo aceptó términos
                var chatRecordatorioProveedorAcepto = await TranslateExtension.TranslateAsync("ChatRecordatorioProveedorAcepto");
                LblRecordatorioProveedor.Text = string.Format(chatRecordatorioProveedorAcepto, _fechaProveedorAcepto.Value.ToString("dd/MM/yyyy HH:mm"));
                FrameRecordatorioProveedor.IsVisible = true;
                FrameRecordatorioInteresado.IsVisible = false;
            }
            else if (!_soyProveedor && _fechaInteresadoAcepto.HasValue)
            {
                // Vista de interesado: mostrar cuándo aceptó términos
                var chatRecordatorioInteresadoAcepto = await TranslateExtension.TranslateAsync("ChatRecordatorioInteresadoAcepto");
                LblRecordatorioInteresado.Text = string.Format(chatRecordatorioInteresadoAcepto, _fechaInteresadoAcepto.Value.ToString("dd/MM/yyyy HH:mm"));
                FrameRecordatorioInteresado.IsVisible = true;
                FrameRecordatorioProveedor.IsVisible = false;
            }
            else
            {
                // No mostrar recordatorio
                FrameRecordatorioProveedor.IsVisible = false;
                FrameRecordatorioInteresado.IsVisible = false;
            }
        }

        /// <summary>
        /// Carga mensajes del servidor.
        /// 2026-04-12: esRecargaCompleta=true solo en la carga inicial (OnAppearing).
        /// En refrescos del timer (esRecargaCompleta=false) solo se agregan mensajes nuevos
        /// sin limpiar el container, para evitar el bug de Android donde Children.Clear()
        /// dentro de un ScrollView causa que los globos desaparezcan hasta el próximo toque.
        /// </summary>
        private async Task CargarMensajesAsync(bool esRecargaCompleta = false)
        {
            Console.WriteLine($"[ChatBubbles] CargarMensajesAsync START esRecargaCompleta={esRecargaCompleta} chatId={_chatId} ultimoId={_ultimoMensajeIdRenderizado} count={_mensajes.Count}");
            try
            {
                if (_chatId == 0) return; // Chat no inicializado

                using (var client = new JWTHttpClient(new System.Net.Http.HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                }, false))
                {
                    string url = AppConfiguration.ApiHost + "/ChatPublicidad/ObtenerMensajes";

                    var requestData = new
                    {
                        chat_id = _chatId,
                        user_id_thirdparty = App.persona.user_id_thirdparty,
                        limite = 100,
                        offset = 0,
                        // 2026-04-10: idioma del receptor para que el API traduzca si es necesario
                        idioma_dispositivo = App.persona.Idioma ?? "es"
                    };

                    var content = new System.Net.Http.StringContent(
                        JsonConvert.SerializeObject(requestData),
                        System.Text.Encoding.UTF8,
                        "application/json"
                    );

                    var response = await client.PostAPIAsync(url, content);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    Console.WriteLine($"[ChatBubbles] HTTP={response.StatusCode} bodyLen={responseContent?.Length}");

                    if (response.IsSuccessStatusCode)
                    {
                        var responseObj = JsonConvert.DeserializeObject<JObject>(responseContent);

                        if (responseObj["isSuccess"]?.ToObject<bool>() == true)
                        {
                            var mensajesArray = responseObj["data"]?["mensajes"]?.ToObject<List<MensajeChatDto>>();
                            Console.WriteLine($"[ChatBubbles] mensajesArray count={mensajesArray?.Count ?? 0}");

                            if (mensajesArray != null && mensajesArray.Count > 0)
                            {
                                // Los mensajes vienen ordenados por fecha DESC, revertir para mostrar del más antiguo al más nuevo
                                mensajesArray.Reverse();

                                if (esRecargaCompleta)
                                {
                                    // Carga inicial: reemplazar toda la colección
                                    Console.WriteLine($"[ChatBubbles] RECARGA COMPLETA: repoblar {mensajesArray.Count} mensajes");
                                    _mensajes.Clear();
                                    _ultimoMensajeIdRenderizado = 0;
                                    foreach (var msg in mensajesArray)
                                    {
                                        _mensajes.Add(msg);
                                        if (msg.MensajeId > _ultimoMensajeIdRenderizado)
                                            _ultimoMensajeIdRenderizado = msg.MensajeId;
                                    }
                                    Console.WriteLine($"[ChatBubbles] RECARGA COMPLETA FIN: count={_mensajes.Count} ultimoId={_ultimoMensajeIdRenderizado}");
                                }
                                else
                                {
                                    // Refresco del timer: solo agregar mensajes nuevos
                                    Console.WriteLine($"[ChatBubbles] TIMER REFRESCO: count={_mensajes.Count} ultimoId={_ultimoMensajeIdRenderizado}");
                                    bool hayNuevos = false;
                                    foreach (var msg in mensajesArray)
                                    {
                                        if (msg.MensajeId > _ultimoMensajeIdRenderizado)
                                        {
                                            _mensajes.Add(msg);
                                            _ultimoMensajeIdRenderizado = msg.MensajeId;
                                            hayNuevos = true;
                                        }
                                    }
                                    Console.WriteLine($"[ChatBubbles] TIMER FIN: hayNuevos={hayNuevos} count={_mensajes.Count}");
                                    if (hayNuevos)
                                        await ScrollAlFinalAsync();
                                    return;
                                }
                            }
                            else if (esRecargaCompleta)
                            {
                                Console.WriteLine($"[ChatBubbles] Sin mensajes del servidor, limpiando colección");
                                _mensajes.Clear();
                                _ultimoMensajeIdRenderizado = 0;
                            }

                            await ScrollAlFinalAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ChatPublicidadPage", "CargarMensajesAsync");
                System.Diagnostics.Debug.WriteLine($"Error al cargar mensajes: {ex.Message}");
            }
        }

        /// <summary>
        /// Abre el visor de foto/video (VerFotoPopup) al tocar una imagen o video en el CollectionView.
        /// El CommandParameter del TapGestureRecognizer en el DataTemplate es el MensajeChatDto.
        /// </summary>
        private async void OnMensajeImagenTapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is not MensajeChatDto mensaje) return;
            try
            {
                var fotoDto = new FotoDescripcionAlarma
                {
                    UrlFoto = mensaje.UrlMedia,
                    ThumbnailUrl = mensaje.UrlMedia,
                    EsVideo = mensaje.TipoMedia == "video",
                    TipoMime = mensaje.TipoMedia == "video" ? "video/mp4" : "image/jpeg",
                    NombreArchivoOriginal = mensaje.TipoMedia == "video" ? "video.mp4" : "foto.jpg",
                    FechaSubida = new DateTimeOffset(mensaje.FechaEnvio)
                };
                var popup = new VerFotoPopup(fotoDto);
                await Application.Current.MainPage.ShowPopupAsync(popup);
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ChatPublicidadPage", "OnMensajeImagenTapped");
            }
        }

        private async void OnAceptarTerminosClicked(object sender, EventArgs e)
        {
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsRunning = true;

                using (var client = new JWTHttpClient(new System.Net.Http.HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                }, false))
                {
                    string url = AppConfiguration.ApiHost + "/ChatPublicidad/AceptarTerminos";

                    var requestData = new
                    {
                        chat_id = _chatId,
                        user_id_thirdparty = App.persona.user_id_thirdparty
                    };

                    var content = new System.Net.Http.StringContent(
                        JsonConvert.SerializeObject(requestData),
                        System.Text.Encoding.UTF8,
                        "application/json"
                    );

                    var response = await client.PostAPIAsync(url, content);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var responseObj = JsonConvert.DeserializeObject<JObject>(responseContent);

                        if (responseObj["isSuccess"]?.ToObject<bool>() == true)
                        {
                            var dataObj = responseObj["data"];
                            string estado = dataObj["estadoChat"]?.ToString() ?? "pending";
                            _estaActivo = (estado == "active");
                            _interesadoAcepto = true;

                            var chatActivo = await TranslateExtension.TranslateAsync("ChatActivo");
                            var chatTerminosAceptados = await TranslateExtension.TranslateAsync("ChatTerminosAceptados");
                            await ModernAlerts.ShowSuccess(chatActivo, chatTerminosAceptados);
                            ActualizarEstadoChat();
                        }
                        else
                        {
                            var labelError = await TranslateExtension.TranslateAsync("LabelError");
                            var labelErrorDesconocido = await TranslateExtension.TranslateAsync("LabelErrorDesconocido");
                            string mensaje = responseObj["message"]?.ToString() ?? labelErrorDesconocido;
                            await ModernAlerts.ShowError(labelError, mensaje);
                        }
                    }
                    else
                    {
                        var labelError = await TranslateExtension.TranslateAsync("LabelError");
                        var chatErrorAceptandoTerminos = await TranslateExtension.TranslateAsync("ChatErrorAceptandoTerminos");
                        await ModernAlerts.ShowError(labelError, $"{chatErrorAceptandoTerminos}: {responseContent}");
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ChatPublicidadPage", "OnAceptarTerminosClicked");
                var labelError = await TranslateExtension.TranslateAsync("LabelError");
                var chatErrorAceptandoTerminos = await TranslateExtension.TranslateAsync("ChatErrorAceptandoTerminos");
                await ModernAlerts.ShowError(labelError, $"{chatErrorAceptandoTerminos}: {ex.Message}");
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
            }
        }

        private async void OnEnviarClicked(object sender, EventArgs e)
        {
            // Permitir enviar si hay texto O si hay archivos adjuntos
            bool hayTexto = !string.IsNullOrWhiteSpace(EntryMensaje.Text);
            bool hayMedia = _mediaFiles.Count > 0;
            if (!hayTexto && !hayMedia)
                return;

            if (!_estaActivo)
            {
                var chatActivo = await TranslateExtension.TranslateAsync("ChatActivo");
                var chatNoActivo = await TranslateExtension.TranslateAsync("ChatNoActivo");
                await ModernAlerts.ShowWarning(chatActivo, chatNoActivo);
                return;
            }

            try
            {
                string contenido = EntryMensaje.Text ?? "";
                EntryMensaje.Text = string.Empty;

                // Capturar y limpiar la lista de media antes del envío
                var mediaAEnviar = new List<MediaFile>(_mediaFiles);
                _mediaFiles.Clear();
                ActualizarCarruselMedia();

                // NOTA: Ya no se hace optimistic update aquí.
                // Con CollectionView + ObservableCollection el refresco post-envío es inmediato
                // y agregar un item temporal (sin MensajeId real) causaría duplicados al hacer
                // el refresco incremental que compara por MensajeId.

                // Si hay archivos multimedia, enviar uno por uno (igual que DescribirAlarmaPage)
                // El API ya soporta url_media y tipo_media en EnviarMensaje
                if (mediaAEnviar.Count > 0)
                {
                    foreach (var mediaFile in mediaAEnviar)
                    {
                        try
                        {
                            string mimeType = mediaFile.IsVideo ? VideoHelper.GetVideoMimeType(mediaFile.FilePath) : "image/jpeg";
                            string extension = Path.GetExtension(mediaFile.FilePath);
                            if (string.IsNullOrEmpty(extension))
                                extension = mediaFile.IsVideo ? ".mp4" : ".jpg";

                            // Obtener presigned URL de S3
                            var presignedResp = await ApiService.SolicitarPresignedUrlAsync(mimeType, extension);
                            if (presignedResp == null || string.IsNullOrEmpty(presignedResp.PresignedUrl))
                            {
                                System.Diagnostics.Debug.WriteLine("[ChatPub] No se obtuvo presigned URL");
                                continue;
                            }

                            // Subir archivo a S3
                            await ApiService.SubirArchivoAPresignedUrlAsync(presignedResp.PresignedUrl, mediaFile.FilePath, mimeType);

                            // Construir URL pública del archivo (quitar query string del presigned URL)
                            string urlPublica = presignedResp.PresignedUrl.Split('?')[0];

                            // Enviar mensaje con media al servidor
                            await EnviarMensajeApiAsync(contenido, urlPublica, mediaFile.IsVideo ? "video" : "image");

                            // Después del primer archivo, el texto ya se envió; los siguientes van sin texto
                            contenido = "";
                        }
                        catch (Exception exMedia)
                        {
                            CrashlyticsHelper.LogError(exMedia, "ChatPublicidadPage", "EnviarMedia");
                            System.Diagnostics.Debug.WriteLine($"[ChatPub] Error enviando media: {exMedia.Message}");
                        }
                    }
                }
                else
                {
                    // Solo texto
                    await EnviarMensajeApiAsync(contenido, null, null);
                }

                // Refresco incremental: trae el mensaje guardado del servidor y lo agrega a la colección.
                await CargarMensajesAsync(esRecargaCompleta: false);
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ChatPublicidadPage", "OnEnviarClicked");
                var labelError = await TranslateExtension.TranslateAsync("LabelError");
                var chatErrorEnviandoMensaje = await TranslateExtension.TranslateAsync("ChatErrorEnviandoMensaje");
                await ModernAlerts.ShowError(labelError, $"{chatErrorEnviandoMensaje}: {ex.Message}");
            }
        }

        /// <summary>
        /// Llama al endpoint POST /ChatPublicidad/EnviarMensaje.
        /// url_media y tipo_media son opcionales (null para mensajes solo texto).
        /// </summary>
        private async Task EnviarMensajeApiAsync(string contenido, string? urlMedia, string? tipoMedia)
        {
            using (var client = new JWTHttpClient(new System.Net.Http.HttpClientHandler()
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            }, false))
            {
                string url = AppConfiguration.ApiHost + "/ChatPublicidad/EnviarMensaje";

                var requestData = new
                {
                    chat_id = _chatId,
                    user_id_thirdparty_remitente = App.persona.user_id_thirdparty,
                    contenido = contenido,
                    url_media = urlMedia,
                    tipo_media = tipoMedia,
                    idioma_dispositivo = App.persona.Idioma ?? "es"
                };

                var content = new System.Net.Http.StringContent(
                    JsonConvert.SerializeObject(requestData),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await client.PostAPIAsync(url, content);
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"[ChatPub] EnviarMensaje error: {body}");
                }
            }
        }

        private async void OnAdjuntarClicked(object sender, EventArgs e)
        {
            try
            {
                var result = await ModernAlerts.ShowThreeOptions(
                    await TranslateExtension.TranslateAsync("LblAgregarMedia"),
                    await TranslateExtension.TranslateAsync("LblSeleccionarTipoMedia"),
                    await TranslateExtension.TranslateAsync("LblTomarFoto"),
                    await TranslateExtension.TranslateAsync("LblGrabarVideo"),
                    await TranslateExtension.TranslateAsync("LblSeleccionarFoto")
                );

                if (result == ThreeOptionResult.Option1)
                {
                    // Tomar foto con CameraPopup (igual que DescribirAlarmaPage)
                    var cameraPopup = new CameraPopup();
                    cameraPopup.PhotoCaptured += async (s, photoPath) =>
                    {
                        await MediaGalleryHelper.SaveToGalleryAsync(photoPath, isVideo: false);
                        await ProcesarArchivoMediaAsync(photoPath, false);
                    };
                    await Application.Current.MainPage.ShowPopupAsync(cameraPopup);
                }
                else if (result == ThreeOptionResult.Option2)
                {
                    // Grabar video (igual que DescribirAlarmaPage)
#if IOS || MACCATALYST
                    var videoResult = await MediaPicker.CaptureVideoAsync();
                    if (videoResult != null)
                    {
                        var processingText = await TranslateExtension.TranslateAsync("LblProcesandoVideo");
                        await ModernAlerts.ShowProcessingAsync(processingText, async () =>
                        {
                            var cacheDir = FileSystem.CacheDirectory;
                            // Preservar extensión original (.mov en iPhone, .mp4 en otros)
                            var ext = Path.GetExtension(videoResult.FileName)?.ToLowerInvariant();
                            if (string.IsNullOrEmpty(ext) || ext == ".") ext = ".mp4";
                            var videoPath = Path.Combine(cacheDir, $"{Guid.NewGuid()}{ext}");
                            using (var source = await videoResult.OpenReadAsync())
                            using (var dest = File.Create(videoPath))
                            {
                                await source.CopyToAsync(dest);
                            }
                            await MediaGalleryHelper.SaveToGalleryAsync(videoPath, isVideo: true);
                            await ProcesarArchivoMediaAsync(videoPath, false);
                        });
                    }
#else
                    var videoCameraPopup = new VideoCameraPopup();
                    videoCameraPopup.VideoCaptured += async (s, videoPath) =>
                    {
                        await MediaGalleryHelper.SaveToGalleryAsync(videoPath, isVideo: true);
                        await ProcesarArchivoMediaAsync(videoPath, true);
                    };
                    await Application.Current.MainPage.ShowPopupAsync(videoCameraPopup);
#endif
                }
                else if (result == ThreeOptionResult.Option3)
                {
                    // Seleccionar de galería (igual que DescribirAlarmaPage)
                    var photo = await MediaPicker.PickPhotoAsync();
                    if (photo != null)
                    {
                        var processingText = await TranslateExtension.TranslateAsync("LblProcesandoFoto");
                        await ModernAlerts.ShowProcessingAsync(processingText, async () =>
                        {
                            var cacheDir = FileSystem.CacheDirectory;
                            var ext = Path.GetExtension(photo.FileName);
                            if (string.IsNullOrEmpty(ext)) ext = ".jpg";
                            var cachedPath = Path.Combine(cacheDir, $"{Guid.NewGuid()}{ext}");
                            using (var source = await photo.OpenReadAsync())
                            using (var dest = File.Create(cachedPath))
                            {
                                await source.CopyToAsync(dest);
                            }
                            await ProcesarArchivoMediaAsync(cachedPath, true);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ChatPublicidadPage", "OnAdjuntarClicked");
                var labelError = await TranslateExtension.TranslateAsync("LabelError");
                var chatErrorAdjuntar = await TranslateExtension.TranslateAsync("ChatErrorAdjuntar");
                await ModernAlerts.ShowError(labelError, $"{chatErrorAdjuntar}: {ex.Message}");
            }
        }

        /// <summary>
        /// Procesa un archivo multimedia (foto o video) y lo agrega al carrusel de vista previa.
        /// Lógica idéntica a DescribirAlarmaPopUpViewModel.ProcessMediaFileFromPath.
        /// </summary>
        private async Task ProcesarArchivoMediaAsync(string filePath, bool isFromGallery)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                    return;

                bool isVideo = filePath.ToLower().EndsWith(".mp4") ||
                               filePath.ToLower().EndsWith(".mov") ||
                               filePath.ToLower().EndsWith(".avi");

                string finalPath = filePath;

                // Si es foto de galería, comprimir y corregir orientación EXIF (igual que DescribirAlarmaPage)
                if (!isVideo && isFromGallery)
                {
                    var cacheDir = FileSystem.CacheDirectory;
                    var compressedPath = Path.Combine(cacheDir, $"{Guid.NewGuid()}.jpg");
                    var result = await ImageHelper.CompressAndFixOrientationAsync(
                        filePath,
                        compressedPath,
                        MaxImageWidth,
                        JpegQuality / 100f);
                    if (!string.IsNullOrEmpty(result))
                        finalPath = result;
                }

                var fileInfo = new FileInfo(finalPath);

                // En iOS los videos necesitan un thumbnail JPEG generado del primer frame
                var thumbnailPath = isVideo
                    ? await VideoHelper.GenerateVideoThumbnailAsync(finalPath)
                    : finalPath;

                var mediaFile = new MediaFile
                {
                    FilePath = finalPath,
                    ThumbnailPath = thumbnailPath,
                    IsVideo = isVideo,
                    FileSizeBytes = fileInfo.Length
                };

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    _mediaFiles.Add(mediaFile);
                    ActualizarCarruselMedia();
                });
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ChatPublicidadPage", "ProcesarArchivoMediaAsync");
            }
        }

        /// <summary>
        /// Elimina un archivo del carrusel cuando el usuario toca el botón X.
        /// </summary>
        private void OnRemoveMediaTapped(object sender, TappedEventArgs e)
        {
            try
            {
                if (e.Parameter is MediaFile mediaFile)
                {
                    _mediaFiles.Remove(mediaFile);
                    ActualizarCarruselMedia();
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ChatPublicidadPage", "OnRemoveMediaTapped");
            }
        }

        /// <summary>
        /// Actualiza la visibilidad y el ItemsSource del carrusel de media.
        /// </summary>
        private void ActualizarCarruselMedia()
        {
            MediaScrollView.IsVisible = _mediaFiles.Count > 0;
            MediaCollectionView.ItemsSource = null;
            MediaCollectionView.ItemsSource = _mediaFiles;
        }

        private async void OnVerEnMapaClicked(object sender, EventArgs e)
        {
            try
            {
                // Cerrar chat page
                await Navigation.PopAsync();

                // TODO: Enviar mensaje al mapa para centrar en la ubicación de la alarma
                // MessagingCenter.Send(this, "CenterMapOnLocation", new Location(latitud, longitud));
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ChatPublicidadPage", "OnVerEnMapaClicked");
                var labelError = await TranslateExtension.TranslateAsync("LabelError");
                var chatErrorNavegar = await TranslateExtension.TranslateAsync("ChatErrorNavegar");
                await ModernAlerts.ShowError(labelError, $"{chatErrorNavegar}: {ex.Message}");
            }
        }
    }
}


