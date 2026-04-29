using CommunityToolkit.Maui.Views;
using sospect.Models;
using sospect.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace sospect.Views.Popups
{
    public partial class VerFotoPopup : Popup
    {
        // ── Estado de zoom ──────────────────────────────────────────────
        private double _currentScale = 1.0;
        private double _startScale = 1.0;
        private double _xOffset = 0;
        private double _yOffset = 0;
        private double _startX = 0;
        private double _startY = 0;

        private const double MinScale = 1.0;
        private const double MaxScale = 4.0;

        // ── Estado de pantalla completa ─────────────────────────────────
        private bool _isFullscreen = false;

        // ── Referencia a la imagen activa en el carrusel ────────────────
        private Image _activeCarouselImage = null;

        // ── PanGestureRecognizer del carrusel (se agrega/quita por código) ──
        private PanGestureRecognizer _carouselPanRecognizer = null;

        // ── Constructor antiguo (retrocompatibilidad) ───────────────────
        public VerFotoPopup(FotoDescripcionAlarma foto)
        {
            InitializeComponent();
            var viewModel = new VerFotoViewModel(foto);
            BindingContext = viewModel;

            _ = viewModel.InicializarTraduccionesAsync();
            ConfigurarVideo(foto);
        }

        // ── Constructor nuevo: múltiples fotos con descripción ──────────
        public VerFotoPopup(List<FotoDescripcionAlarma> fotos, int indiceInicial, string textoDescripcion, DateTime fechaDescripcion)
        {
            InitializeComponent();

            var fotosConHtml = fotos.Select(f => new FotoConHtml(f)).ToList();
            var viewModel = new VerFotoViewModel(fotosConHtml, indiceInicial, textoDescripcion, fechaDescripcion);
            BindingContext = viewModel;

            _ = viewModel.InicializarTraduccionesAsync();

            // Subscribir al cambio de posición para resetear el zoom al deslizar
            mediaCarousel.PositionChanged += OnCarouselPositionChanged;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (mediaCarousel != null && fotosConHtml != null && indiceInicial >= 0 && indiceInicial < fotosConHtml.Count)
                    mediaCarousel.Position = indiceInicial;
            });
        }

        // ──────────────────────────────────────────────────────────────
        // RESET DE ZOOM AL CAMBIAR DE FOTO EN EL CARRUSEL
        // ──────────────────────────────────────────────────────────────
        private void OnCarouselPositionChanged(object sender, PositionChangedEventArgs e)
        {
            ResetZoomState();
        }

        // ──────────────────────────────────────────────────────────────
        // PANTALLA COMPLETA
        // ──────────────────────────────────────────────────────────────
        private void OnFullscreenClicked(object sender, EventArgs e)
        {
            ToggleFullscreen();
        }

        private void OnImageDoubleTapped(object sender, TappedEventArgs e)
        {
            // Doble tap: si hay zoom activo lo resetea; si no, activa/desactiva fullscreen
            if (_currentScale > MinScale + 0.05)
                ResetZoom(sender as Image);
            else
                ToggleFullscreen();
        }

        private void ToggleFullscreen()
        {
            _isFullscreen = !_isFullscreen;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (_isFullscreen)
                {
                    var density = DeviceDisplay.Current.MainDisplayInfo.Density;
                    var screenWidth  = DeviceDisplay.Current.MainDisplayInfo.Width  / density;
                    var screenHeight = DeviceDisplay.Current.MainDisplayInfo.Height / density;

                    PopupBorder.WidthRequest  = screenWidth;
                    PopupBorder.HeightRequest = screenHeight;
                    PopupBorder.StrokeShape   = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 0 };

                    // Ocultar header/footer; mostrar overlay flotante
                    HeaderGrid.IsVisible        = false;
                    FooterGrid.IsVisible        = false;
                    FullscreenOverlay.IsVisible = true;

                    // En fullscreen deshabilitar swipe para que no interfiera con el zoom
                    if (mediaCarousel != null)
                        mediaCarousel.IsSwipeEnabled = false;
                }
                else
                {
                    PopupBorder.WidthRequest  = 350;
                    PopupBorder.HeightRequest = 500;
                    PopupBorder.StrokeShape   = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 };

                    HeaderGrid.IsVisible        = true;
                    FooterGrid.IsVisible        = true;
                    FullscreenOverlay.IsVisible = false;

                    // Rehabilitar swipe al salir de fullscreen
                    if (mediaCarousel != null)
                        mediaCarousel.IsSwipeEnabled = true;

                    // Resetear zoom al salir de fullscreen
                    ResetZoomState();
                }
            });
        }

        // ──────────────────────────────────────────────────────────────
        // ZOOM EN MODO SINGLE
        // ──────────────────────────────────────────────────────────────
        private void OnSingleImagePinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
        {
            if (SingleImage != null)
                HandlePinch(SingleImage, e);
        }

        private void OnSingleImagePanUpdated(object sender, PanUpdatedEventArgs e)
        {
            if (SingleImage != null)
                HandlePan(SingleImage, e);
        }

        // ──────────────────────────────────────────────────────────────
        // ZOOM EN MODO CARRUSEL
        // ──────────────────────────────────────────────────────────────
        private void OnCarouselImagePinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
        {
            if (sender is not Image image) return;

            _activeCarouselImage = image;

            // Al iniciar pinch: deshabilitar swipe del carrusel para evitar conflicto
            if (e.Status == GestureStatus.Started && mediaCarousel != null)
                mediaCarousel.IsSwipeEnabled = false;

            HandlePinch(image, e);

            // Al terminar pinch:
            // - Si hay zoom activo: agregar PanGestureRecognizer a la imagen para arrastrar
            // - Si no hay zoom: rehabilitar swipe del carrusel
            if (e.Status == GestureStatus.Completed || e.Status == GestureStatus.Canceled)
            {
                if (_currentScale > MinScale + 0.05)
                {
                    // Hay zoom → habilitar pan en la imagen, deshabilitar swipe carrusel
                    AgregarPanAlCarrusel(image);
                }
                else
                {
                    // Sin zoom → quitar pan, rehabilitar swipe carrusel
                    QuitarPanDelCarrusel(image);
                    if (!_isFullscreen && mediaCarousel != null)
                        mediaCarousel.IsSwipeEnabled = true;
                }
            }
        }

        private void AgregarPanAlCarrusel(Image image)
        {
            if (_carouselPanRecognizer != null) return; // ya existe
            _carouselPanRecognizer = new PanGestureRecognizer();
            _carouselPanRecognizer.PanUpdated += OnCarouselImagePanUpdated;
            image.GestureRecognizers.Add(_carouselPanRecognizer);
        }

        private void QuitarPanDelCarrusel(Image image)
        {
            if (_carouselPanRecognizer == null) return;
            _carouselPanRecognizer.PanUpdated -= OnCarouselImagePanUpdated;
            image.GestureRecognizers.Remove(_carouselPanRecognizer);
            _carouselPanRecognizer = null;
        }

        private void OnCarouselImagePanUpdated(object sender, PanUpdatedEventArgs e)
        {
            if (sender is Image image)
            {
                _activeCarouselImage = image;
                HandlePan(image, e);
            }
        }

        // ──────────────────────────────────────────────────────────────
        // LÓGICA COMÚN DE PINCH (fluida, centrada en punto de contacto)
        // ──────────────────────────────────────────────────────────────
        private void HandlePinch(Image image, PinchGestureUpdatedEventArgs e)
        {
            switch (e.Status)
            {
                case GestureStatus.Started:
                    _startScale = _currentScale;
                    // Centrar el anchor en el punto de contacto
                    image.AnchorX = e.ScaleOrigin.X;
                    image.AnchorY = e.ScaleOrigin.Y;
                    break;

                case GestureStatus.Running:
                    double newScale = Math.Clamp(_startScale * e.Scale, MinScale, MaxScale);
                    _currentScale = newScale;
                    image.Scale = newScale;
                    break;

                case GestureStatus.Completed:
                    if (_currentScale <= MinScale + 0.05)
                        ResetZoom(image);
                    break;

                case GestureStatus.Canceled:
                    ResetZoom(image);
                    break;
            }
        }

        // ──────────────────────────────────────────────────────────────
        // LÓGICA COMÚN DE PAN (arrastrar cuando hay zoom)
        // ──────────────────────────────────────────────────────────────
        private void HandlePan(Image image, PanUpdatedEventArgs e)
        {
            // Pan solo funciona cuando la imagen tiene zoom aplicado
            if (_currentScale <= MinScale + 0.05) return;

            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    _startX = _xOffset;
                    _startY = _yOffset;
                    break;

                case GestureStatus.Running:
                    // Limitar el desplazamiento a los bordes de la imagen ampliada
                    double maxX = (image.Width  * (_currentScale - 1)) / 2;
                    double maxY = (image.Height * (_currentScale - 1)) / 2;

                    _xOffset = Math.Clamp(_startX + e.TotalX, -maxX, maxX);
                    _yOffset = Math.Clamp(_startY + e.TotalY, -maxY, maxY);

                    image.TranslationX = _xOffset;
                    image.TranslationY = _yOffset;
                    break;

                case GestureStatus.Completed:
                case GestureStatus.Canceled:
                    _startX = _xOffset;
                    _startY = _yOffset;
                    break;
            }
        }

        // ──────────────────────────────────────────────────────────────
        // RESET DE ZOOM
        // ──────────────────────────────────────────────────────────────
        private void ResetZoom(Image image)
        {
            if (image == null) return;

            _currentScale = MinScale;
            _xOffset = _startX = 0;
            _yOffset = _startY = 0;

            image.AnchorX = 0.5;
            image.AnchorY = 0.5;
            image.ScaleTo(MinScale, 150, Easing.CubicOut);
            image.TranslateTo(0, 0, 150, Easing.CubicOut);

            // Si era imagen del carrusel, quitar pan y rehabilitar swipe
            if (image == _activeCarouselImage)
            {
                QuitarPanDelCarrusel(image);
                _activeCarouselImage = null;
            }
            if (!_isFullscreen && mediaCarousel != null)
                mediaCarousel.IsSwipeEnabled = true;
        }

        private void ResetZoomState()
        {
            _currentScale = MinScale;
            _xOffset = _startX = 0;
            _yOffset = _startY = 0;

            if (SingleImage != null)
            {
                SingleImage.Scale        = MinScale;
                SingleImage.AnchorX      = 0.5;
                SingleImage.AnchorY      = 0.5;
                SingleImage.TranslationX = 0;
                SingleImage.TranslationY = 0;
            }

            if (_activeCarouselImage != null)
            {
                _activeCarouselImage.Scale        = MinScale;
                _activeCarouselImage.AnchorX      = 0.5;
                _activeCarouselImage.AnchorY      = 0.5;
                _activeCarouselImage.TranslationX = 0;
                _activeCarouselImage.TranslationY = 0;

                // Quitar el pan recognizer dinámico y rehabilitar swipe del carrusel
                QuitarPanDelCarrusel(_activeCarouselImage);
                _activeCarouselImage = null;
            }

            // Rehabilitar swipe del carrusel si no estamos en fullscreen
            if (!_isFullscreen && mediaCarousel != null)
                mediaCarousel.IsSwipeEnabled = true;
        }

        // ──────────────────────────────────────────────────────────────
        // VIDEO EN MODO SINGLE
        // ──────────────────────────────────────────────────────────────
        private void ConfigurarVideo(FotoDescripcionAlarma foto)
        {
            if (foto?.EsVideo == true)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    var webView = this.FindByName<WebView>("videoWebView");
                    if (webView != null)
                    {
                        webView.Source = new HtmlWebViewSource
                        {
                            Html = $@"<!DOCTYPE html>
<html>
<head>
    <meta name='viewport' content='width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no'>
    <style>
        body {{ margin:0; padding:0; background-color:black; display:flex; justify-content:center; align-items:center; height:100vh; }}
        video {{ width:100%; height:100%; object-fit:contain; }}
    </style>
</head>
<body>
    <video controls autoplay playsinline preload='metadata' poster='{foto.ThumbnailUrlOrOriginal}'>
        <source src='{foto.UrlFoto}' type='{foto.TipoMime}'>
    </video>
</body>
</html>"
                        };
                    }
                });
            }
        }

        // ──────────────────────────────────────────────────────────────
        // CERRAR
        // ──────────────────────────────────────────────────────────────
        private async void OnCerrarClicked(object sender, EventArgs e)
        {
            await CloseAsync();
        }
    }

    // ── Clase para encapsular foto con su HTML de video generado ────────
    public class FotoConHtml
    {
        private readonly FotoDescripcionAlarma _foto;

        public FotoConHtml(FotoDescripcionAlarma foto)
        {
            _foto = foto;
        }

        public string UrlFoto  => _foto.UrlFoto;
        public bool EsVideo    => _foto.EsVideo;
        public bool EsImagen   => !_foto.EsVideo;

        public HtmlWebViewSource HtmlSource
        {
            get
            {
                if (!EsVideo) return null;

                return new HtmlWebViewSource
                {
                    Html = $@"<!DOCTYPE html>
<html>
<head>
    <meta name='viewport' content='width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no'>
    <style>
        body {{ margin:0; padding:0; background-color:black; display:flex; justify-content:center; align-items:center; height:100vh; }}
        video {{ width:100%; height:100%; object-fit:contain; }}
    </style>
</head>
<body>
    <video controls autoplay playsinline preload='metadata' poster='{_foto.ThumbnailUrlOrOriginal}'>
        <source src='{_foto.UrlFoto}' type='{_foto.TipoMime}'>
    </video>
</body>
</html>"
                };
            }
        }
    }

    // ── ViewModel simple para formatear los datos de la foto ────────────
    public class VerFotoViewModel : System.ComponentModel.INotifyPropertyChanged
    {
        private readonly FotoDescripcionAlarma _foto;
        private readonly List<FotoConHtml> _fotosConHtml;
        private readonly string _textoDescripcion;
        private readonly DateTime _fechaDescripcion;
        private readonly bool _modoMultiple;
        private string _labelVideo = "Video";
        private string _labelFoto  = "Foto";

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        public VerFotoViewModel(FotoDescripcionAlarma foto)
        {
            _foto             = foto;
            _fotosConHtml     = new List<FotoConHtml>();
            _textoDescripcion = string.Empty;
            _fechaDescripcion = foto.FechaSubida.DateTime;
            _modoMultiple     = false;
        }

        public VerFotoViewModel(List<FotoConHtml> fotosConHtml, int indiceInicial, string textoDescripcion, DateTime fechaDescripcion)
        {
            _fotosConHtml     = fotosConHtml ?? new List<FotoConHtml>();
            _foto             = null;
            _textoDescripcion = textoDescripcion;
            _fechaDescripcion = fechaDescripcion;
            _modoMultiple     = true;
        }

        public async Task InicializarTraduccionesAsync()
        {
            _labelVideo = await TranslateExtension.TranslateAsync("LabelVideo");
            _labelFoto  = await TranslateExtension.TranslateAsync("LabelFoto");
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Titulo)));
        }

        public string UrlFoto  => _foto?.UrlFoto ?? string.Empty;
        public bool EsVideo    => _foto?.EsVideo ?? false;
        public bool EsImagen   => !(_foto?.EsVideo ?? false);

        public List<FotoConHtml> Fotos    => _fotosConHtml;
        public bool ModoMultiple          => _modoMultiple;
        public bool ModoSingle            => !_modoMultiple;

        public string Titulo => _modoMultiple
            ? _fechaDescripcion.ToString("dd/MM/yyyy HH:mm")
            : _foto?.NombreArchivoOriginal ?? (_foto?.EsVideo == true ? _labelVideo : _labelFoto);

        public string TextoDescripcion    => _textoDescripcion ?? string.Empty;
        public bool TieneDescripcion      => !string.IsNullOrWhiteSpace(_textoDescripcion);

        public DateTime FechaSubida => _foto?.FechaSubida.DateTime ?? DateTime.Now;

        public string TamanoFormatted
        {
            get
            {
                if (_foto == null || !_foto.TamanoBytes.HasValue || _foto.TamanoBytes.Value == 0)
                    return string.Empty;

                double bytes = _foto.TamanoBytes.Value;
                string[] sizes = { "B", "KB", "MB", "GB" };
                int order = 0;
                while (bytes >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    bytes /= 1024;
                }
                return $"{bytes:0.##} {sizes[order]}";
            }
        }

        public bool TieneTamano =>
            _foto != null && _foto.TamanoBytes.HasValue && _foto.TamanoBytes.Value > 0;

        public string DimensionesFormatted =>
            (_foto?.AnchoPixels.HasValue == true && _foto?.AltoPixels.HasValue == true)
                ? $"{_foto.AnchoPixels}x{_foto.AltoPixels} px"
                : string.Empty;

        public bool TieneDimensiones =>
            _foto != null && _foto.AnchoPixels.HasValue && _foto.AltoPixels.HasValue;

        public bool MostrarIndicadores => _modoMultiple && _fotosConHtml.Count > 1;
        public int CantidadFotos       => _fotosConHtml?.Count ?? 0;
    }
}
