using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Storage;
using Newtonsoft.Json;
using sospect.ViewModels;
using sospect.Models;
using sospect.Resources;
using sospect.Helpers;
using sospect.Services;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Maui.Extensions;

namespace sospect.Views
{
    public partial class PromocionLocalPage : ContentPage
    {
        private PromocionLocalViewModel _viewModel;

        // Archivos multimedia y logo (manejados en la vista)
        private List<string> _rutasMultimedia = new List<string>();
        private string _rutaLogo = null;
        private bool _usarLogoPersonalizado = false; // Si false, usar logo de emprendimiento

        // Coordenadas de la ubicación (actualizadas cuando el usuario confirma en el modal de mapa)
        private double _latitud;
        private double _longitud;

        // Flag para que el popup de promo previa solo se muestre una vez por sesión de la página
        private bool _promoPreviaYaConsultada = false;

        public PromocionLocalPage(double latitud, double longitud)
        {
            InitializeComponent();

            _latitud = latitud;
            _longitud = longitud;
            _viewModel = new PromocionLocalViewModel(latitud, longitud);
            BindingContext = _viewModel;

            // Suscribirse a cambios de NitVerificado para ofrecer pre-llenado cuando se confirme el NIT
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Inicializar labels con textos traducidos
            await InicializarContadorTextoPush();
            await InicializarContadorDescripcion();
            await ActualizarLabelsConCostos();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            // Desuscribirse para evitar memory leaks
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        /// <summary>
        /// Se ejecuta cuando el ViewModel notifica un cambio de propiedad.
        /// Si NitVerificado pasa a true con un idEmprendimiento > 0, ofrece pre-llenar con la última promo.
        /// </summary>
        private async void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PromocionLocalViewModel.NitVerificado)
                && _viewModel.NitVerificado
                && _viewModel.IdEmprendimiento > 0
                && !_promoPreviaYaConsultada)
            {
                _promoPreviaYaConsultada = true;
                await OfrecerPreLlenarConUltimaPromocionAsync();
            }
        }

        /// <summary>
        /// Verifica si existe una promoción previa para el emprendimiento y,
        /// si el usuario acepta, pre-llena el formulario con esos datos.
        /// </summary>
        private async Task OfrecerPreLlenarConUltimaPromocionAsync()
        {
            try
            {
                var ultimaPromo = await ApiService.ObtenerUltimaPromocion(
                    App.persona.user_id_thirdparty,
                    _viewModel.IdEmprendimiento);

                if (ultimaPromo == null || !ultimaPromo.IsSuccess || !ultimaPromo.TienePromocionPrevia)
                    return; // Sin promo previa, dejar formulario vacío

                // Preguntar al usuario usando ModernAlerts (diseño estándar de la app)
                var msgUsar = await TranslateExtension.TranslateAsync("MsgUsarDatosUltimaPromocion");
                var lblSi = await TranslateExtension.TranslateAsync("LblSi");
                var lblNo = await TranslateExtension.TranslateAsync("LblNo");

                bool usarDatos = await ModernAlerts.ShowConfirmation(
                    _viewModel.NombreEmprendimiento,
                    msgUsar,
                    lblSi,
                    lblNo);

                if (!usarDatos) return;

                // Pre-llenar descripción
                if (!string.IsNullOrEmpty(ultimaPromo.DescripcionAlarma))
                {
                    EditorDescripcion.Text = ultimaPromo.DescripcionAlarma;
                    _viewModel.Descripcion = ultimaPromo.DescripcionAlarma;
                }

                // Pre-llenar sliders (asignar valor dispara el evento y actualiza labels)
                if (ultimaPromo.RadioMetros > 0)
                    SliderRadio.Value = ultimaPromo.RadioMetros;

                if (ultimaPromo.DuracionDias > 0)
                    SliderDuracion.Value = ultimaPromo.DuracionDias;

                // Pre-llenar texto push
                if (!string.IsNullOrEmpty(ultimaPromo.TextoPushPersonalizado))
                {
                    _viewModel.TextoPush = ultimaPromo.TextoPushPersonalizado;
                }

                // Pre-llenar switches (los toggles activan sus eventos que actualizan el ViewModel)
                // Términos de chat primero: deben estar activos antes de habilitar contacto/domicilio
                SwitchTerminosChat.IsToggled = ultimaPromo.ProveedorAceptoTerminosChat;
                _viewModel.AceptoTerminosChat = ultimaPromo.ProveedorAceptoTerminosChat;
                SwitchLogo.IsToggled = ultimaPromo.LogoHabilitado;
                SwitchContacto.IsToggled = ultimaPromo.ContactoHabilitado;
                SwitchDomicilio.IsToggled = ultimaPromo.DomicilioHabilitado;

                // Pre-llenar notificaciones push
                if (ultimaPromo.CantidadUsuariosPush > 0)
                {
                    SwitchPush.IsToggled = true;
                    PushContainer.IsVisible = true;
                    // Asegurar que el máximo del slider esté actualizado antes de asignar el valor
                    _viewModel.ActualizarUsuariosDisponibles();
                    int usuariosPush = Math.Min(ultimaPromo.CantidadUsuariosPush, _viewModel.UsuariosDisponibles);
                    SliderUsuariosPush.Maximum = Math.Max(_viewModel.UsuariosDisponibles, 1);
                    SliderUsuariosPush.Value = usuariosPush;
                    _viewModel.UsuariosPush = usuariosPush;
                    var promoNotificarA = await TranslateExtension.TranslateAsync("PromoNotificarA");
                    LblNotificarA.Text = string.Format(promoNotificarA, usuariosPush);
                }

                // Pre-llenar logo desde URL del emprendimiento (si logo habilitado y existe URL)
                if (ultimaPromo.LogoHabilitado && !string.IsNullOrEmpty(ultimaPromo.UrlLogo))
                {
                    // El logo del emprendimiento ya debería estar en _urlLogoExistente del ViewModel
                    // después de la verificación del NIT, pero por si acaso lo sobreescribimos
                    if (string.IsNullOrEmpty(_viewModel._urlLogoExistente))
                        _viewModel._urlLogoExistente = ultimaPromo.UrlLogo;
                }

                // Pre-llenar fotos: mostrar thumbnails de fotos previas en el contenedor multimedia
                if (ultimaPromo.Fotos != null && ultimaPromo.Fotos.Count > 0)
                {
                    await PreCargarFotosAnterioressAsync(ultimaPromo.Fotos);
                }

                // Actualizar costos y labels
                await ActualizarCostoDisplay();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PromoLocal] OfrecerPreLlenarConUltimaPromocionAsync EXCEPCION: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "PromocionLocalPage", "OfrecerPreLlenarConUltimaPromocionAsync");
            }
        }

        /// <summary>
        /// Muestra las fotos de la última promoción como imágenes informativas en el contenedor multimedia.
        /// Son URLs remotas (ya subidas), no rutas locales.
        /// </summary>
        private async Task PreCargarFotosAnterioressAsync(List<FotoUltimaPromocionDto> fotos)
        {
            try
            {
                // Limpiar el contenedor
                MultimediaContainer.Children.Clear();
                _rutasMultimedia.Clear();

                foreach (var foto in fotos)
                {
                    if (string.IsNullOrEmpty(foto.UrlFoto)) continue;

                    // Guardar la URL para que se use directamente como media adjunta
                    _rutasMultimedia.Add(foto.UrlFoto);

                    // Mostrar thumbnail en el contenedor visual
                    var imgView = new Image
                    {
                        Source = ImageSource.FromUri(new Uri(
                            !string.IsNullOrEmpty(foto.ThumbnailUrl) ? foto.ThumbnailUrl : foto.UrlFoto)),
                        HeightRequest = 80,
                        WidthRequest = 80,
                        Aspect = Aspect.AspectFill
                    };
                    MultimediaContainer.Children.Add(imgView);
                }

                // Notificar al ViewModel las rutas (URLs remotas)
                _viewModel.SetMultimedia(_rutasMultimedia, _rutaLogo, _usarLogoPersonalizado);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PromoLocal] PreCargarFotosAnterioressAsync EXCEPCION: {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task InicializarContadorTextoPush()
        {
            var promoCaracteresRestantes = await TranslateExtension.TranslateAsync("PromoCaracteresRestantes");
            LblContadorTextoPush.Text = string.Format(promoCaracteresRestantes, 200);
        }

        private async System.Threading.Tasks.Task InicializarContadorDescripcion()
        {
            var promoCaracteresRestantes = await TranslateExtension.TranslateAsync("PromoCaracteresRestantes");
            LblCaracteresRestantes.Text = string.Format(promoCaracteresRestantes, 500);
        }

        private async System.Threading.Tasks.Task ActualizarLabelsConCostos()
        {
            var promoAgregarLogo = await TranslateExtension.TranslateAsync("PromoAgregarLogo");
            var promoHabilitarChatPrivado = await TranslateExtension.TranslateAsync("PromoHabilitarChatPrivado");
            var promoPermitirDomicilio = await TranslateExtension.TranslateAsync("PromoPermitirDomicilio");
            var promoCostoNotificaciones = await TranslateExtension.TranslateAsync("PromoCostoNotificaciones");
            var promoSaldoActual = await TranslateExtension.TranslateAsync("PromoSaldoActual");
            var promoCostoActual = await TranslateExtension.TranslateAsync("PromoCostoActual");
            var promoSaldoDespues = await TranslateExtension.TranslateAsync("PromoSaldoDespues");
            var promoCostoTotal = await TranslateExtension.TranslateAsync("PromoCostoTotal");
            var promoRadioMetros = await TranslateExtension.TranslateAsync("PromoRadioMetros");
            var promoDuracionDias = await TranslateExtension.TranslateAsync("PromoDuracionDias");
            var promoNotificarA = await TranslateExtension.TranslateAsync("PromoNotificarA");

            // Actualizar labels con formato y costos dinámicos
            LblLogoText.Text = string.Format(promoAgregarLogo, _viewModel.CostoLogo);
            LblContactoText.Text = string.Format(promoHabilitarChatPrivado, _viewModel.CostoContacto);
            LblDomicilioText.Text = string.Format(promoPermitirDomicilio, _viewModel.CostoDomicilio);
            LblCostoPush.Text = string.Format(promoCostoNotificaciones, _viewModel.CostoPor50UsuariosPush);

            // Actualizar labels de saldo y costo
            LblSaldoActual.Text = string.Format(promoSaldoActual, _viewModel.SaldoInicial);
            LblCostoActual.Text = string.Format(promoCostoActual, _viewModel.CostoActual);
            LblSaldoDespues.Text = string.Format(promoSaldoDespues, _viewModel.SaldoResultante);
            LblCostoTotal.Text = string.Format(promoCostoTotal, _viewModel.CostoActual);

            // Actualizar labels de sliders
            LblRadioMetros.Text = string.Format(promoRadioMetros, _viewModel.RadioMetros);
            LblDuracionDias.Text = string.Format(promoDuracionDias, _viewModel.DuracionDias);
            LblNotificarA.Text = string.Format(promoNotificarA, _viewModel.UsuariosPush);

            // Actualizar estado del botón según saldo
            await ActualizarEstadoBotonLanzar();
        }

        private async System.Threading.Tasks.Task ActualizarEstadoBotonLanzar()
        {
            if (_viewModel.SaldoResultante < 0)
            {
                var promoComprarPoderes = await TranslateExtension.TranslateAsync("PromoComprarPoderes");
                var promoInsufficientBalance = await TranslateExtension.TranslateAsync("PromoInsufficientBalance");

                BtnLanzar.Text = promoComprarPoderes;
                BtnLanzar.BackgroundColor = Colors.Red;
                LblDeficit.Text = string.Format(promoInsufficientBalance, Math.Abs(_viewModel.SaldoResultante));
                LblDeficit.IsVisible = true;
            }
            else
            {
                var promoLanzarPromocion = await TranslateExtension.TranslateAsync("PromoLanzarPromocion");
                BtnLanzar.Text = promoLanzarPromocion;
                BtnLanzar.BackgroundColor = Color.FromArgb("#4CAF50");
                LblDeficit.IsVisible = false;
            }
        }

        // Event Handlers
        private async void OnDescripcionChanged(object sender, TextChangedEventArgs e)
        {
            if (EditorDescripcion.Text != null)
            {
                int caracteresRestantes = 500 - EditorDescripcion.Text.Length;
                var promoCaracteresRestantes = await TranslateExtension.TranslateAsync("PromoCaracteresRestantes");
                LblCaracteresRestantes.Text = string.Format(promoCaracteresRestantes, caracteresRestantes);
            }

            _viewModel.Descripcion = EditorDescripcion.Text;
        }

        private async void OnLogoToggled(object sender, ToggledEventArgs e)
        {
            _viewModel.LogoHabilitado = e.Value;
            LogoContainer.IsVisible = e.Value;

            // Si se habilita el logo y el emprendimiento tiene logo guardado, mostrarlo
            if (e.Value && !string.IsNullOrEmpty(_viewModel._urlLogoExistente) && !_usarLogoPersonalizado)
            {
                // Usar el logo del emprendimiento guardado (obtenido durante verificación NIT)
                ImgLogoPreview.Source = ImageSource.FromUri(new Uri(_viewModel._urlLogoExistente));
                ImgLogoPreview.IsVisible = true;
                LblInfoLogoGuardado.IsVisible = true;
                _rutaLogo = _viewModel._urlLogoExistente; // Usar URL directamente

                // Actualizar texto del botón
                var promoEditarLogo = await TranslateExtension.TranslateAsync("PromoEditarLogo");
                BtnAgregarLogo.Text = promoEditarLogo ?? "Cambiar Logo";

                // Ocultar el mensaje de "se guardará para futuras promociones"
                LblInfoGuardarLogo.IsVisible = false;
            }
            else if (e.Value)
            {
                // No tiene logo guardado, mostrar mensaje informativo
                LblInfoLogoGuardado.IsVisible = false;
                LblInfoGuardarLogo.IsVisible = true;
            }
            else
            {
                // Logo deshabilitado
                LblInfoLogoGuardado.IsVisible = false;
                LblInfoGuardarLogo.IsVisible = false;
            }

            await ActualizarCostoDisplay();
        }

        private async void OnRadioChanged(object sender, ValueChangedEventArgs e)
        {
            int radioRedondeado = (int)Math.Round(e.NewValue / 100) * 100; // Redondear a múltiplos de 100
            SliderRadio.Value = radioRedondeado;
            _viewModel.RadioMetros = radioRedondeado;

            var promoRadioMetros = await TranslateExtension.TranslateAsync("PromoRadioMetros");
            LblRadioMetros.Text = string.Format(promoRadioMetros, _viewModel.RadioMetros);

            // Actualizar usuarios disponibles
            _viewModel.ActualizarUsuariosDisponibles();
            await ActualizarLabelUsuariosDisponibles();

            // Actualizar máximo del slider de usuarios push
            SliderUsuariosPush.Maximum = _viewModel.UsuariosDisponibles;

            await ActualizarCostoDisplay();
        }

        private async void OnDuracionChanged(object sender, ValueChangedEventArgs e)
        {
            _viewModel.DuracionDias = (int)Math.Round(e.NewValue);
            SliderDuracion.Value = _viewModel.DuracionDias;

            var promoDuracionDias = await TranslateExtension.TranslateAsync("PromoDuracionDias");
            LblDuracionDias.Text = string.Format(promoDuracionDias, _viewModel.DuracionDias);

            await ActualizarCostoDisplay();
        }

        private async void OnTerminosChatToggled(object sender, ToggledEventArgs e)
        {
            _viewModel.AceptoTerminosChat = e.Value;

            // Si desmarca términos y tiene contacto o domicilio habilitado
            if (!e.Value && (_viewModel.ContactoHabilitado || _viewModel.DomicilioHabilitado))
            {
                // Deshabilitar automáticamente contacto y domicilio
                _viewModel.ContactoHabilitado = false;
                _viewModel.DomicilioHabilitado = false;

                // Actualizar los switches en la UI
                if (SwitchContacto != null) SwitchContacto.IsToggled = false;
                if (SwitchDomicilio != null) SwitchDomicilio.IsToggled = false;

                // Recalcular costo
                await ActualizarCostoDisplay();

                // Mostrar mensaje informativo
                var promoTerminosRequeridos = await TranslateExtension.TranslateAsync("PromoTerminosRequeridos");
                var promoSinTerminosNoContacto = await TranslateExtension.TranslateAsync("PromoSinTerminosNoContacto");
                await ModernAlerts.ShowWarning(
                    promoTerminosRequeridos,
                    promoSinTerminosNoContacto
                );
            }
        }

        private async void OnContactoToggled(object sender, ToggledEventArgs e)
        {
            // Validar que haya aceptado términos antes de permitir activar contacto
            if (e.Value && !_viewModel.AceptoTerminosChat)
            {
                // Rechazar el cambio
                if (sender is Switch switchContacto)
                {
                    switchContacto.IsToggled = false;
                }

                var promoTerminosRequeridos = await TranslateExtension.TranslateAsync("PromoTerminosRequeridos");
                var promoSinTerminosNoContacto = await TranslateExtension.TranslateAsync("PromoSinTerminosNoContacto");
                await ModernAlerts.ShowWarning(
                    promoTerminosRequeridos,
                    promoSinTerminosNoContacto
                );
                return;
            }

            _viewModel.ContactoHabilitado = e.Value;
            await ActualizarCostoDisplay();
        }

        private async void OnDomicilioToggled(object sender, ToggledEventArgs e)
        {
            // Validar que haya aceptado términos antes de permitir activar domicilio
            if (e.Value && !_viewModel.AceptoTerminosChat)
            {
                // Rechazar el cambio
                if (sender is Switch switchDomicilio)
                {
                    switchDomicilio.IsToggled = false;
                }

                var promoTerminosRequeridos = await TranslateExtension.TranslateAsync("PromoTerminosRequeridos");
                var promoSinTerminosNoContacto = await TranslateExtension.TranslateAsync("PromoSinTerminosNoContacto");
                await ModernAlerts.ShowWarning(
                    promoTerminosRequeridos,
                    promoSinTerminosNoContacto
                );
                return;
            }

            _viewModel.DomicilioHabilitado = e.Value;
            await ActualizarCostoDisplay();
        }

        private async void OnTextoPushChanged(object sender, TextChangedEventArgs e)
        {
            int caracteresRestantes = 200 - (e.NewTextValue?.Length ?? 0);
            var promoCaracteresRestantes = await TranslateExtension.TranslateAsync("PromoCaracteresRestantes");
            LblContadorTextoPush.Text = string.Format(promoCaracteresRestantes, caracteresRestantes);

            // AGREGADO 2026-01-29: Guardar el texto en el ViewModel para enviarlo al API
            _viewModel.TextoPush = e.NewTextValue;
        }

        private async void OnPushToggled(object sender, ToggledEventArgs e)
        {
            PushContainer.IsVisible = e.Value;
            if (!e.Value)
            {
                _viewModel.UsuariosPush = 0;
                SliderUsuariosPush.Value = 0;
            }
            await ActualizarCostoDisplay();
        }

        private async void OnUsuariosPushChanged(object sender, ValueChangedEventArgs e)
        {
            _viewModel.UsuariosPush = (int)Math.Round(e.NewValue);
            SliderUsuariosPush.Value = _viewModel.UsuariosPush;

            var promoNotificarA = await TranslateExtension.TranslateAsync("PromoNotificarA");
            LblNotificarA.Text = string.Format(promoNotificarA, _viewModel.UsuariosPush);

            // Calcular y mostrar costo de notificaciones push
            var promoCostoNotificaciones = await TranslateExtension.TranslateAsync("PromoCostoNotificaciones");
            LblCostoPush.Text = string.Format(promoCostoNotificaciones, _viewModel.CostoPor50UsuariosPush);

            await ActualizarCostoDisplay();
        }

        private async System.Threading.Tasks.Task ActualizarCostoDisplay()
        {
            var promoSaldoActual = await TranslateExtension.TranslateAsync("PromoSaldoActual");
            var promoCostoActual = await TranslateExtension.TranslateAsync("PromoCostoActual");
            var promoSaldoDespues = await TranslateExtension.TranslateAsync("PromoSaldoDespues");
            var promoCostoTotal = await TranslateExtension.TranslateAsync("PromoCostoTotal");

            // Actualizar labels
            LblSaldoActual.Text = string.Format(promoSaldoActual, _viewModel.SaldoInicial);
            LblCostoActual.Text = string.Format(promoCostoActual, _viewModel.CostoActual);
            LblSaldoDespues.Text = string.Format(promoSaldoDespues, _viewModel.SaldoResultante);
            LblCostoTotal.Text = string.Format(promoCostoTotal, _viewModel.CostoActual);

            await ActualizarEstadoBotonLanzar();
        }

        private async System.Threading.Tasks.Task ActualizarLabelUsuariosDisponibles()
        {
            var promoUsuariosDisponibles = await TranslateExtension.TranslateAsync("PromoUsuariosDisponibles");
            LblUsuariosDisponibles.Text = string.Format(
                promoUsuariosDisponibles,
                _viewModel.RadioMetros,
                _viewModel.UsuariosDisponibles
            );
        }

        private async void OnLanzarClicked(object sender, EventArgs e)
        {
            // Si saldo insuficiente, redirigir a compra de poderes
            if (_viewModel.SaldoResultante < 0)
            {
                int deficit = Math.Abs(_viewModel.SaldoResultante);
                var promoPoderesInsuficientes = await TranslateExtension.TranslateAsync("PromoPoderesInsuficientes");
                var promoNecesitasPoderes = await TranslateExtension.TranslateAsync("PromoNecesitasPoderes");

                // TODO: Navegar a PurchaseSuperPowersPage con filtro de deficit
                await ModernAlerts.ShowWarning(
                    promoPoderesInsuficientes,
                    string.Format(promoNecesitasPoderes, _viewModel.CostoActual)
                );
                return;
            }

            // Validar contenido
            if (string.IsNullOrWhiteSpace(_viewModel.Descripcion))
            {
                var labelError = await TranslateExtension.TranslateAsync("LabelError");
                var promoDescripcionVacia = await TranslateExtension.TranslateAsync("PromoDescripcionVacia");
                await ModernAlerts.ShowWarning(labelError, promoDescripcionVacia);
                return;
            }

            // Obtener el INavigation del stack Tab (mismo patrón que los popups que abren esta página)
            // PushModalAsync NO funciona aquí: PromocionLocalPage está en el stack Tab (PushAsync),
            // por lo que PopModalAsync en la página hija regresaría a SospectTabs y no a esta página.
            INavigation nav = null;
            if (Application.Current.MainPage is TabbedPage tabbedPage && tabbedPage.CurrentPage is NavigationPage navPage)
                nav = navPage.Navigation;
            else if (Application.Current.MainPage is NavigationPage np)
                nav = np.Navigation;

            if (nav == null)
            {
                System.Diagnostics.Debug.WriteLine("[PromocionLocalPage] ERROR: No se pudo obtener Navigation para ConfirmarUbicacionPromoPage");
                return;
            }

            // TCS para recibir el resultado (confirmado + coordenadas) cuando la página hija haga Pop
            var tcs = new TaskCompletionSource<(bool confirmado, double lat, double lon)>();
            var paginaMapa = new ConfirmarUbicacionPromoPage(_latitud, _longitud, tcs);

            // PushAsync en el mismo stack Tab → PopAsync en la página hija regresará aquí correctamente
            await nav.PushAsync(paginaMapa);

            // Bloquear hasta que el usuario confirme o cancele (el TCS se resuelve en ConfirmarUbicacionPromoPage)
            var resultado = await tcs.Task;

            // Si el usuario canceló, no lanzar
            if (!resultado.confirmado)
                return;

            // Actualizar coordenadas con el punto confirmado en la página de mapa
            _latitud = resultado.lat;
            _longitud = resultado.lon;
            _viewModel.ActualizarUbicacion(_latitud, _longitud);

            // Mostrar loading
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;
            BtnLanzar.IsEnabled = false;

            try
            {
                // Pasar multimedia al ViewModel antes de ejecutar el comando
                _viewModel.SetMultimedia(_rutasMultimedia, _rutaLogo, _usarLogoPersonalizado);

                // Ejecutar el comando del ViewModel directamente (ya es async)
                if (_viewModel.LanzarPromocionCommand.CanExecute(null))
                {
                    _viewModel.LanzarPromocionCommand.Execute(null);
                }
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
                BtnLanzar.IsEnabled = true;
            }
        }

        // Método para agregar multimedia (fotos y videos)
        private async void OnAgregarMultimediaClicked(object sender, EventArgs e)
        {
            // Verificar límite de 10 archivos
            if (_rutasMultimedia.Count >= 10)
            {
                var labelInformacion = await TranslateExtension.TranslateAsync("LabelInformacion");
                var promoLimiteMultimedia = await TranslateExtension.TranslateAsync("PromoLimiteMultimedia");
                await ModernAlerts.ShowWarning(labelInformacion, promoLimiteMultimedia);
                return;
            }

            try
            {
                var result = await ModernAlerts.ShowThreeOptions(
                    await TranslateExtension.TranslateAsync("LblAgregarMedia"),
                    await TranslateExtension.TranslateAsync("LblSeleccionarOrigen"),
                    await TranslateExtension.TranslateAsync("LblTomarFotoOVideo"),
                    await TranslateExtension.TranslateAsync("LblSeleccionarFoto"),
                    await TranslateExtension.TranslateAsync("LabelCancelar")
                );

                if (result == ThreeOptionResult.Option1)
                {
                    // Mostrar submenu: Foto o Video
                    var mediaType = await ModernAlerts.ShowThreeOptions(
                        await TranslateExtension.TranslateAsync("LblAgregarMedia"),
                        await TranslateExtension.TranslateAsync("LblSeleccionarTipoMedia"),
                        await TranslateExtension.TranslateAsync("LblTomarFoto"),
                        await TranslateExtension.TranslateAsync("LblGrabarVideo"),
                        await TranslateExtension.TranslateAsync("LabelCancelar")
                    );

                    if (mediaType == ThreeOptionResult.Option1)
                    {
                        // Tomar foto
                        var cameraPopup = new Popups.CameraPopup();

                        cameraPopup.PhotoCaptured += async (s, photoPath) =>
                        {
                            if (!string.IsNullOrEmpty(photoPath))
                            {
                                // Guardar en carrete del usuario
                                await MediaGalleryHelper.SaveToGalleryAsync(photoPath, isVideo: false);

                                _rutasMultimedia.Add(photoPath);
                                ActualizarVistaMultimedia();
                            }
                        };

                        await Application.Current.MainPage.ShowPopupAsync(cameraPopup);
                    }
                    else if (mediaType == ThreeOptionResult.Option2)
                    {
                        // Grabar video
                        var video = await MediaPicker.CaptureVideoAsync();

                        if (video != null)
                        {
                            // Guardar en carrete del usuario
                            await MediaGalleryHelper.SaveToGalleryAsync(video.FullPath, isVideo: true);

                            _rutasMultimedia.Add(video.FullPath);
                            ActualizarVistaMultimedia();
                        }
                    }
                }
                else if (result == ThreeOptionResult.Option2)
                {
                    // Seleccionar foto de galería
                    var photo = await MediaPicker.PickPhotoAsync();

                    if (photo != null)
                    {
                        // Procesar la foto con corrección de orientación EXIF (con indicador de procesamiento)
                        var processingText = await TranslateExtension.TranslateAsync("LblProcesandoFoto");
                        string processedPath = null;

                        await ModernAlerts.ShowProcessingAsync(processingText, async () =>
                        {
                            // iOS fix: photo.FullPath es una ruta temporal del sistema que puede
                            // volverse inaccesible después de que el picker se cierra.
                            // Copiar inmediatamente al caché usando OpenReadAsync() antes de procesar.
                            var cacheDir = FileSystem.CacheDirectory;
                            var rawPath = System.IO.Path.Combine(cacheDir, $"media_src_{Guid.NewGuid()}.jpg");
                            using (var srcStream = await photo.OpenReadAsync())
                            using (var dstStream = System.IO.File.Create(rawPath))
                            {
                                await srcStream.CopyToAsync(dstStream);
                            }
                            processedPath = await ProcessGalleryPhotoAsync(rawPath);
                        });

                        if (!string.IsNullOrEmpty(processedPath))
                        {
                            _rutasMultimedia.Add(processedPath);
                            ActualizarVistaMultimedia();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "PromocionLocalPage", "OnAgregarMultimediaClicked");
                var labelError = await TranslateExtension.TranslateAsync("LabelError");
                var promoErrorCreacion = await TranslateExtension.TranslateAsync("PromoErrorCreacion");
                await ModernAlerts.ShowError(labelError, $"{promoErrorCreacion}: {ex.Message}");
            }
        }

        // Método para agregar logo (solo fotos)
        private async void OnAgregarLogoClicked(object sender, EventArgs e)
        {
            try
            {
                var result = await ModernAlerts.ShowThreeOptions(
                    await TranslateExtension.TranslateAsync("LblAgregarMedia"),
                    await TranslateExtension.TranslateAsync("LblSeleccionarOrigen"),
                    await TranslateExtension.TranslateAsync("LblTomarFoto"),
                    await TranslateExtension.TranslateAsync("LblSeleccionarFoto"),
                    await TranslateExtension.TranslateAsync("LabelCancelar")
                );

                string selectedImagePath = null;

                if (result == ThreeOptionResult.Option1)
                {
                    // Tomar foto
                    var cameraPopup = new Popups.CameraPopup();
                    var tcs = new TaskCompletionSource<string>();

                    cameraPopup.PhotoCaptured += (s, photoPath) =>
                    {
                        tcs.TrySetResult(photoPath);
                    };

                    await Application.Current.MainPage.ShowPopupAsync(cameraPopup);
                    selectedImagePath = await tcs.Task;
                }
                else if (result == ThreeOptionResult.Option2)
                {
                    // Seleccionar foto de galería
                    var photo = await MediaPicker.PickPhotoAsync();
                    if (photo != null)
                    {
                        // Procesar la foto con corrección de orientación EXIF (con indicador de procesamiento)
                        var processingText = await TranslateExtension.TranslateAsync("LblProcesandoFoto");

                        await ModernAlerts.ShowProcessingAsync(processingText, async () =>
                        {
                            // iOS fix: photo.FullPath es una ruta temporal del sistema que puede
                            // volverse inaccesible después de que el picker se cierra.
                            // Copiar inmediatamente al caché usando OpenReadAsync() antes de procesar.
                            var cacheDir = FileSystem.CacheDirectory;
                            var rawPath = System.IO.Path.Combine(cacheDir, $"logo_src_{Guid.NewGuid()}.jpg");
                            using (var srcStream = await photo.OpenReadAsync())
                            using (var dstStream = System.IO.File.Create(rawPath))
                            {
                                await srcStream.CopyToAsync(dstStream);
                            }
                            selectedImagePath = await ProcessGalleryPhotoAsync(rawPath);
                        });
                    }
                }

                // Si se seleccionó una imagen, abrir el editor circular
                if (!string.IsNullOrEmpty(selectedImagePath))
                {
                    var editorPopup = new Popups.CircularLogoEditorPopup(selectedImagePath);
                    await Application.Current.MainPage.ShowPopupAsync(editorPopup);

                    if (editorPopup.Success && !string.IsNullOrEmpty(editorPopup.CroppedImagePath))
                    {
                        // Guardar el logo recortado en el carrete
                        await MediaGalleryHelper.SaveToGalleryAsync(editorPopup.CroppedImagePath, isVideo: false);

                        _rutaLogo = editorPopup.CroppedImagePath;
                        _usarLogoPersonalizado = true;
                        ImgLogoPreview.Source = ImageSource.FromFile(editorPopup.CroppedImagePath);
                        ImgLogoPreview.IsVisible = true;
                        LblInfoLogoGuardado.IsVisible = false;

                        // Actualizar texto del botón
                        var promoEditarLogo = await TranslateExtension.TranslateAsync("PromoEditarLogo");
                        BtnAgregarLogo.Text = promoEditarLogo ?? "Cambiar Logo";

                        System.Diagnostics.Debug.WriteLine($"[PromocionLocalPage] Logo circular guardado: {_rutaLogo}");
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "PromocionLocalPage", "OnAgregarLogoClicked");
                var labelError = await TranslateExtension.TranslateAsync("LabelError");
                var promoErrorCreacion = await TranslateExtension.TranslateAsync("PromoErrorCreacion");
                await ModernAlerts.ShowError(labelError, $"{promoErrorCreacion}: {ex.Message}");
            }
        }

        // Actualizar vista de multimedia con miniaturas (thumbnails)
        private async void ActualizarVistaMultimedia()
        {
            MultimediaContainer.Children.Clear();

            if (_rutasMultimedia.Count == 0)
                return;

            // Crear un ScrollView horizontal para las miniaturas
            var scrollView = new ScrollView
            {
                Orientation = ScrollOrientation.Horizontal,
                HeightRequest = 80,
                Margin = new Thickness(0, 5, 0, 10)
            };

            var horizontalStack = new HorizontalStackLayout
            {
                Spacing = 8
            };

            for (int i = 0; i < _rutasMultimedia.Count; i++)
            {
                var ruta = _rutasMultimedia[i];
                var esVideo = ruta.ToLower().EndsWith(".mp4") || ruta.ToLower().EndsWith(".mov") || ruta.ToLower().EndsWith(".avi");

                // Border para cada thumbnail
                var border = new Border
                {
                    StrokeThickness = 1,
                    Stroke = Color.FromArgb("#E0E0E0"),
                    HeightRequest = 70,
                    WidthRequest = 70,
                    Margin = new Thickness(2),
                    StrokeShape = new RoundRectangle { CornerRadius = 8 }
                };

                var grid = new Grid();

                // Thumbnail de la imagen/video
                var image = new Image
                {
                    Source = ImageSource.FromFile(ruta),
                    Aspect = Aspect.AspectFill
                };
                grid.Children.Add(image);

                // Overlay para videos (símbolo de play)
                if (esVideo)
                {
                    var playIcon = new Label
                    {
                        Text = "▶",
                        FontSize = 24,
                        TextColor = Colors.White,
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center
                    };
                    grid.Children.Add(playIcon);
                }

                // Botón X para eliminar
                int index = i; // Capturar índice
                var deleteButton = new Border
                {
                    BackgroundColor = Color.FromArgb("#80000000"),
                    HeightRequest = 24,
                    WidthRequest = 24,
                    HorizontalOptions = LayoutOptions.End,
                    VerticalOptions = LayoutOptions.Start,
                    Margin = new Thickness(4),
                    StrokeShape = new RoundRectangle { CornerRadius = 12 }
                };

                var deleteLabel = new Label
                {
                    Text = "✕",
                    FontSize = 14,
                    TextColor = Colors.White,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center
                };

                deleteButton.Content = deleteLabel;

                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += (s, e) =>
                {
                    _rutasMultimedia.RemoveAt(index);
                    ActualizarVistaMultimedia();
                };
                deleteButton.GestureRecognizers.Add(tapGesture);

                grid.Children.Add(deleteButton);

                border.Content = grid;
                horizontalStack.Children.Add(border);
            }

            scrollView.Content = horizontalStack;
            MultimediaContainer.Children.Add(scrollView);

            // Actualizar texto del botón con traducción
            var promoAgregarFotoVideo = await TranslateExtension.TranslateAsync("PromoAgregarFotoVideo");
            BtnAgregarMultimedia.Text = string.Format(promoAgregarFotoVideo, _rutasMultimedia.Count);
            BtnAgregarMultimedia.IsEnabled = _rutasMultimedia.Count < 10;
        }

        // Métodos públicos para que el ViewModel obtenga las rutas de multimedia
        public List<string> GetRutasMultimedia()
        {
            return new List<string>(_rutasMultimedia);
        }

        public string GetRutaLogo()
        {
            return _rutaLogo;
        }

        public bool UsarLogoPersonalizado()
        {
            return _usarLogoPersonalizado;
        }

        /// <summary>
        /// Procesa una foto de galería aplicando corrección de orientación EXIF y compresión.
        /// </summary>
        /// <param name="originalPath">Ruta original de la foto</param>
        /// <returns>Ruta del archivo procesado, o la original si falla el procesamiento</returns>
        private async Task<string> ProcessGalleryPhotoAsync(string originalPath)
        {
            try
            {
                if (string.IsNullOrEmpty(originalPath) || !System.IO.File.Exists(originalPath))
                {
                    return originalPath;
                }

                var cacheDir = FileSystem.CacheDirectory;
                if (!System.IO.Directory.Exists(cacheDir))
                {
                    System.IO.Directory.CreateDirectory(cacheDir);
                }

                var fileName = $"{Guid.NewGuid()}.jpg";
                var processedPath = System.IO.Path.Combine(cacheDir, fileName);

                System.Diagnostics.Debug.WriteLine($"[PromocionLocalPage] Procesando foto con corrección EXIF: {originalPath}");

                var result = await ImageHelper.CompressAndFixOrientationAsync(
                    originalPath,
                    processedPath,
                    maxWidth: 1024,
                    quality: 0.6f);

                if (!string.IsNullOrEmpty(result))
                {
                    System.Diagnostics.Debug.WriteLine($"[PromocionLocalPage] Foto procesada exitosamente: {result}");
                    return result;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[PromocionLocalPage] No se pudo procesar, usando original");
                    return originalPath;
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "PromocionLocalPage", "ProcessGalleryPhotoAsync");
                System.Diagnostics.Debug.WriteLine($"[PromocionLocalPage] Error procesando foto: {ex.Message}");
                return originalPath;
            }
        }
    }
}
