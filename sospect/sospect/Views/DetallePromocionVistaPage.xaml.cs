using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using sospect.Helpers;
using sospect.Models;
using sospect.Services;
using sospect.Views.Popups;
using System;
using System.Linq;

namespace sospect.Views
{
    /// <summary>
    /// Página de detalle de promoción tipo "volante publicitario".
    /// Muestra logo, fotos, descripción, texto push, fecha de expiración y botones
    /// condicionales según el rol (creador vs cliente).
    /// Punto de entrada para el chat privado y la lista de chats del proveedor.
    /// Creado: 02-02-2026 | Actualizado: 2026-04-09
    /// </summary>
    public partial class DetallePromocionVistaPage : ContentPage
    {
        private readonly AlarmaCercana _alarma;
        private readonly bool _soyCreador;

        public DetallePromocionVistaPage(AlarmaCercana alarma)
        {
            InitializeComponent();
            _alarma = alarma;

            _soyCreador = !string.IsNullOrEmpty(App.persona?.user_id_thirdparty)
                && !string.IsNullOrEmpty(_alarma.user_id_creador_alarma)
                && App.persona.user_id_thirdparty == _alarma.user_id_creador_alarma;

            System.Diagnostics.Debug.WriteLine($"DetallePromocionVistaPage: Usuario={App.persona?.user_id_thirdparty}, Creador={_alarma.user_id_creador_alarma}, SoyCreador={_soyCreador}");

            ConfigurarVista();
        }

        private void ConfigurarVista()
        {
            try
            {
                // Nombre del emprendimiento
                LblNombreEmprendimiento.Text = !string.IsNullOrEmpty(_alarma.publicidad_nombre_emprendimiento)
                    ? _alarma.publicidad_nombre_emprendimiento
                    : (!string.IsNullOrEmpty(_alarma.user_id_creador_alarma)
                        ? _alarma.user_id_creador_alarma
                        : string.Empty);

                // Logo si está habilitado y tiene URL
                if (_alarma.publicidad_logo_habilitado && !string.IsNullOrEmpty(_alarma.publicidad_url_logo))
                {
                    ImgLogo.Source = ImageSource.FromUri(new Uri(_alarma.publicidad_url_logo));
                    BorderLogo.IsVisible = true;
                }

                // Fecha de expiración
                if (_alarma.fecha_finalizacion_subscr.HasValue)
                {
                    var lblExpiraEl = TranslateExtension.Translate("LblExpiraEl");
                    LblFechaExpiracion.Text = $"{lblExpiraEl} {_alarma.fecha_finalizacion_subscr.Value:dd/MM/yyyy}";
                    FrameFechaExpiracion.IsVisible = true;
                }

                // Fotos
                if (_alarma.Fotos != null && _alarma.Fotos.Any())
                {
                    CarouselFotos.ItemsSource = _alarma.Fotos;
                    FrameFotos.IsVisible = true;
                    FotosIndicator.IsVisible = _alarma.Fotos.Count > 1;
                }

                // Descripción
                LblDescripcion.Text = !string.IsNullOrEmpty(_alarma.Descripcionalarma)
                    ? _alarma.Descripcionalarma
                    : TranslateExtension.Translate("LblSinDescripcion");

                // Texto push personalizado
                if (!string.IsNullOrEmpty(_alarma.publicidad_texto_push))
                {
                    LblTextoPush.Text = _alarma.publicidad_texto_push;
                    FrameTextoPush.IsVisible = true;
                }

                // Indicador de domicilio
                FrameDomicilio.IsVisible = _alarma.publicidad_domicilio_habilitado;

                // Botones condicionales según rol
                BtnContactar.IsVisible = _alarma.publicidad_contacto_habilitado && !_soyCreador;
                BtnVerChats.IsVisible = _soyCreador && _alarma.publicidad_contacto_habilitado;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DetallePromocionVistaPage.ConfigurarVista: Error: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "DetallePromocionVistaPage", "ConfigurarVista");
            }
        }

        /// <summary>
        /// Botón "Chatear con el establecimiento" (usuario cliente).
        /// Verifica si ya aceptó términos; si no, muestra el popup antes de abrir el chat.
        /// </summary>
        private async void OnContactarClicked(object sender, EventArgs e)
        {
            try
            {
                BtnContactar.IsEnabled = false;
                LoadingTerminos.IsVisible = true;
                LoadingTerminos.IsRunning = true;

                // Verificar si ya aceptó términos para este chat/alarma
                var verificacion = await ApiService.VerificarAceptacionTerminos(
                    _alarma.alarma_id,
                    App.persona.user_id_thirdparty);

                if (!verificacion.IsSuccess)
                {
                    var labelInformacion = await TranslateExtension.TranslateAsync("LabelInformacion");
                    await ModernAlerts.ShowWarning(labelInformacion, verificacion.Message ?? "Error verificando términos");
                    return;
                }

                bool terminosAceptados = verificacion.YaAceptoTerminos;

                if (!terminosAceptados)
                {
                    // Mostrar popup de aceptación de términos
                    var popup = new AceptarTerminosChatPopup();
                    await Application.Current.MainPage.ShowPopupAsync(popup);

                    if (!popup.UsuarioAceptoTerminos)
                    {
                        // Usuario rechazó los términos
                        var chatNoDisponible = await TranslateExtension.TranslateAsync("ChatNoDisponible");
                        var debesAceptarTerminos = await TranslateExtension.TranslateAsync("DebesAceptarTerminos");
                        await ModernAlerts.ShowWarning(chatNoDisponible, debesAceptarTerminos);
                        return;
                    }

                    // Usuario aceptó: navegar con flag para auto-aceptar en ChatPublicidadPage
                    terminosAceptados = true;
                }

                // Pasar el nombre del negocio para que el título aparezca inmediatamente en iOS
                string nombreNegocio = !string.IsNullOrEmpty(_alarma.publicidad_nombre_emprendimiento)
                    ? _alarma.publicidad_nombre_emprendimiento
                    : _alarma.user_id_creador_alarma;
                System.Diagnostics.Debug.WriteLine($"DetallePromocionVistaPage: Navegando a ChatPublicidadPage para alarma {_alarma.alarma_id}, terminosAceptados={terminosAceptados}, nombreNegocio={nombreNegocio}");
                await Navigation.PushAsync(new ChatPublicidadPage(_alarma.alarma_id, terminosAceptados, nombreNegocio));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DetallePromocionVistaPage.OnContactarClicked: Error: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "DetallePromocionVistaPage", "OnContactarClicked");
                var labelError = await TranslateExtension.TranslateAsync("LabelError");
                await ModernAlerts.ShowError(labelError, ex.Message);
            }
            finally
            {
                BtnContactar.IsEnabled = true;
                LoadingTerminos.IsVisible = false;
                LoadingTerminos.IsRunning = false;
            }
        }

        /// <summary>
        /// Botón "Ir a ventana de chats" (usuario creador de la promoción).
        /// </summary>
        private async void OnVerChatsClicked(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"DetallePromocionVistaPage: Navegando a ListaChatsPromocionPage para alarma {_alarma.alarma_id}");
                await Navigation.PushAsync(new ListaChatsPromocionPage(_alarma.alarma_id));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DetallePromocionVistaPage.OnVerChatsClicked: Error: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "DetallePromocionVistaPage", "OnVerChatsClicked");
                var labelError = await TranslateExtension.TranslateAsync("LabelError");
                await ModernAlerts.ShowError(labelError, ex.Message);
            }
        }

        /// <summary>
        /// Botón "Ver en mapa" — abre Google Maps o Waze con la ubicación de la promoción.
        /// </summary>
        private async void OnVerEnMapaClicked(object sender, EventArgs e)
        {
            try
            {
                double latitud = (double)_alarma.latitud_alarma;
                double longitud = (double)_alarma.longitud_alarma;
                // InvariantCulture garantiza punto decimal en las URLs (evita coma por cultura es/CO)
                string latStr = latitud.ToString(System.Globalization.CultureInfo.InvariantCulture);
                string lonStr = longitud.ToString(System.Globalization.CultureInfo.InvariantCulture);
                string lugar = $"{latStr},{lonStr}";
                string googleMapsUrl = $"https://www.google.com/maps/dir/?api=1&destination={lugar}";
                string appleMapsUrl = $"http://maps.apple.com/?daddr={lugar}";
                // iOS requiere el scheme nativo waze:// para que la app reciba las coordenadas destino
                string wazeUrl = DeviceInfo.Platform == DevicePlatform.iOS
                    ? $"waze://?ll={latStr},{lonStr}&navigate=yes"
                    : $"https://waze.com/ul?ll={latStr},{lonStr}&navigate=yes";

                var lblElegirApp = await TranslateExtension.TranslateAsync("LblElegirApp");
                var lblSeleccioneApp = await TranslateExtension.TranslateAsync("LblSeleccioneAppNavegacion");
                var lblMapas = await TranslateExtension.TranslateAsync("LblMapas");

                bool usarWaze = await ModernAlerts.ShowConfirmation(
                    lblElegirApp,
                    lblSeleccioneApp,
                    "Waze",
                    lblMapas,
                    false);

                if (!usarWaze)
                {
                    // Maps
                    try
                    {
                        if (DeviceInfo.Platform == DevicePlatform.Android)
                            await Launcher.OpenAsync(googleMapsUrl);
                        else if (DeviceInfo.Platform == DevicePlatform.iOS)
                            await Launcher.OpenAsync(appleMapsUrl);
                    }
                    catch (Exception mapsEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"DetallePromocionVistaPage.OnVerEnMapaClicked: Error Maps: {mapsEx.Message}");
                        CrashlyticsHelper.LogError(mapsEx, "DetallePromocionVistaPage", "OnVerEnMapaClicked-Maps");
                    }
                }
                else
                {
                    // Waze con fallback a Maps
                    try
                    {
                        await Launcher.OpenAsync(wazeUrl);
                    }
                    catch (Exception wazeEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"DetallePromocionVistaPage.OnVerEnMapaClicked: Waze no disponible: {wazeEx.Message}");
                        CrashlyticsHelper.LogError(wazeEx, "DetallePromocionVistaPage", "OnVerEnMapaClicked-Waze");
                        try
                        {
                            if (DeviceInfo.Platform == DevicePlatform.Android)
                                await Launcher.OpenAsync(googleMapsUrl);
                            else if (DeviceInfo.Platform == DevicePlatform.iOS)
                                await Launcher.OpenAsync(appleMapsUrl);
                        }
                        catch (Exception mapsFallbackEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"DetallePromocionVistaPage.OnVerEnMapaClicked: Fallback Maps error: {mapsFallbackEx.Message}");
                            CrashlyticsHelper.LogError(mapsFallbackEx, "DetallePromocionVistaPage", "OnVerEnMapaClicked-MapsFallback");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DetallePromocionVistaPage.OnVerEnMapaClicked: Error: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "DetallePromocionVistaPage", "OnVerEnMapaClicked");
            }
        }
    }
}
