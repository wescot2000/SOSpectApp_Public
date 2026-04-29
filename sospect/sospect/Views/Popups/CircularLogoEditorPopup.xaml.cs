using System;
using System.IO;
using CommunityToolkit.Maui.Views;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using sospect.Helpers;
using sospect.Resources;

namespace sospect.Views.Popups
{
    public partial class CircularLogoEditorPopup : Popup
    {
        private SKBitmap _originalBitmap;
        private float _zoomFactor = 1.0f;
        private float _offsetX = 0f;
        private float _offsetY = 0f;
        private const int OUTPUT_SIZE = 256; // Tamaño final del logo (256x256 px para mejor calidad)
        private const int CANVAS_SIZE = 280; // Tamaño del canvas en el preview (debe coincidir con XAML)

        // Variables para gestos de pan
        private float _lastPanX = 0f;
        private float _lastPanY = 0f;
        private bool _isPanning = false;

        // Variables para gestos de pinch (zoom con dos dedos)
        private float _startZoomFactor = 1.0f;

        // Variables para tracking del canvas real
        private int _actualCanvasWidth = CANVAS_SIZE;
        private int _actualCanvasHeight = CANVAS_SIZE;

        public string CroppedImagePath { get; private set; }
        public bool Success { get; private set; }

        public CircularLogoEditorPopup(string imagePath)
        {
            InitializeComponent();
            LoadImage(imagePath);
            _ = InitializeTranslations();

            // iOS fix: en iOS/Metal, InvalidateSurface() llamado desde el constructor se descarta
            // porque el canvas aún no está adjunto a la jerarquía de vistas (no hay superficie Metal).
            // Re-invalidar cuando el popup esté completamente visible y cuando el canvas cambie de tamaño.
            this.Opened += (s, e) => CanvasView?.InvalidateSurface();
            CanvasView.SizeChanged += OnCanvasSizeChanged;
        }

        private void OnCanvasSizeChanged(object sender, EventArgs e)
        {
            if (_originalBitmap != null)
                CanvasView?.InvalidateSurface();
        }

        private async Task InitializeTranslations()
        {
            var lblTitle = await TranslateExtension.TranslateAsync("LogoEditorTitle");
            var lblZoom = await TranslateExtension.TranslateAsync("LogoEditorZoom");
            var btnCancel = await TranslateExtension.TranslateAsync("LabelCancelar");
            var btnSave = await TranslateExtension.TranslateAsync("LogoEditorSave");

            LblTitle.Text = lblTitle;
            LblZoom.Text = lblZoom;
            BtnCancel.Text = btnCancel;
            BtnSave.Text = btnSave;
        }

        private void LoadImage(string imagePath)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[CircularLogoEditor] LoadImage - Iniciando carga de: {imagePath}");

                if (!File.Exists(imagePath))
                {
                    System.Diagnostics.Debug.WriteLine($"[CircularLogoEditor] ERROR - Archivo no existe: {imagePath}");
                    return;
                }

                var fileInfo = new FileInfo(imagePath);
                System.Diagnostics.Debug.WriteLine($"[CircularLogoEditor] Archivo encontrado - Tamaño: {fileInfo.Length} bytes");

                using (var stream = File.OpenRead(imagePath))
                {
                    _originalBitmap = SKBitmap.Decode(stream);
                }

                if (_originalBitmap != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[CircularLogoEditor] Imagen cargada - Dimensiones: {_originalBitmap.Width}x{_originalBitmap.Height}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[CircularLogoEditor] ERROR - SKBitmap.Decode devolvió null");
                }

                // Centrar la imagen
                CanvasView.InvalidateSurface();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CircularLogoEditor] ERROR en LoadImage: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[CircularLogoEditor] StackTrace: {ex.StackTrace}");
            }
        }

        private void OnCanvasViewPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;
            canvas.Clear(SKColors.White);

            if (_originalBitmap == null)
                return;

            var info = e.Info;
            int canvasWidth = info.Width;
            int canvasHeight = info.Height;

            // Guardar dimensiones reales del canvas para usar en el recorte
            _actualCanvasWidth = canvasWidth;
            _actualCanvasHeight = canvasHeight;

            // Calcular el radio del círculo de recorte (el área visible)
            float circleRadius = Math.Min(canvasWidth, canvasHeight) * 0.4f;
            float centerX = canvasWidth / 2f;
            float centerY = canvasHeight / 2f;

            // Calcular escala para que la imagen llene el canvas
            float scale = Math.Max(
                (float)canvasWidth / _originalBitmap.Width,
                (float)canvasHeight / _originalBitmap.Height
            ) * _zoomFactor;

            // Dimensiones de la imagen escalada
            float scaledWidth = _originalBitmap.Width * scale;
            float scaledHeight = _originalBitmap.Height * scale;

            // Posición de la imagen (centrada + offset)
            // NOTA: El offset se aplica en coordenadas de pantalla (píxeles del canvas)
            float left = centerX - (scaledWidth / 2) + _offsetX;
            float top = centerY - (scaledHeight / 2) + _offsetY;

            var destRect = new SKRect(left, top, left + scaledWidth, top + scaledHeight);

            // Dibujar la imagen
            canvas.DrawBitmap(_originalBitmap, destRect);

            // Dibujar overlay semi-transparente fuera del círculo
            using (var paint = new SKPaint())
            {
                paint.Color = new SKColor(0, 0, 0, 128); // Negro semi-transparente
                paint.Style = SKPaintStyle.Fill;

                using (var path = new SKPath())
                {
                    // Rectángulo completo
                    path.AddRect(new SKRect(0, 0, canvasWidth, canvasHeight));
                    // Círculo (área que NO se oscurece)
                    path.AddCircle(centerX, centerY, circleRadius);
                    path.FillType = SKPathFillType.EvenOdd; // Esto hace que el círculo sea un "agujero"

                    canvas.DrawPath(path, paint);
                }
            }

            // Dibujar el borde del círculo de recorte
            using (var paint = new SKPaint())
            {
                paint.Color = SKColors.White;
                paint.Style = SKPaintStyle.Stroke;
                paint.StrokeWidth = 3;
                paint.IsAntialias = true;

                canvas.DrawCircle(centerX, centerY, circleRadius, paint);
            }
        }

        private void OnZoomChanged(object sender, ValueChangedEventArgs e)
        {
            _zoomFactor = (float)e.NewValue;
            System.Diagnostics.Debug.WriteLine($"[CircularLogoEditor] Zoom slider changed: {_zoomFactor:F2}");
            CanvasView.InvalidateSurface();
        }

        /// <summary>
        /// Handler para gestos de Pan (arrastre con un dedo)
        /// </summary>
        private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
        {
            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    _isPanning = true;
                    _lastPanX = _offsetX;
                    _lastPanY = _offsetY;
                    System.Diagnostics.Debug.WriteLine($"[CircularLogoEditor] Pan STARTED - offset inicial: ({_offsetX:F1}, {_offsetY:F1})");
                    break;

                case GestureStatus.Running:
                    // Multiplicador para hacer el movimiento más sensible
                    float sensitivity = 2.0f;
                    _offsetX = _lastPanX + (float)(e.TotalX * sensitivity);
                    _offsetY = _lastPanY + (float)(e.TotalY * sensitivity);

                    // Limitar el offset para que la imagen no se salga completamente del círculo
                    float maxOffset = 200f; // Limitar el desplazamiento máximo
                    _offsetX = Math.Clamp(_offsetX, -maxOffset, maxOffset);
                    _offsetY = Math.Clamp(_offsetY, -maxOffset, maxOffset);

                    System.Diagnostics.Debug.WriteLine($"[CircularLogoEditor] Pan RUNNING - TotalX: {e.TotalX:F1}, TotalY: {e.TotalY:F1}, new offset: ({_offsetX:F1}, {_offsetY:F1})");
                    CanvasView.InvalidateSurface();
                    break;

                case GestureStatus.Completed:
                    _isPanning = false;
                    System.Diagnostics.Debug.WriteLine($"[CircularLogoEditor] Pan COMPLETED - offset final: ({_offsetX:F1}, {_offsetY:F1})");
                    break;

                case GestureStatus.Canceled:
                    _isPanning = false;
                    _offsetX = _lastPanX;
                    _offsetY = _lastPanY;
                    System.Diagnostics.Debug.WriteLine($"[CircularLogoEditor] Pan CANCELED - restaurando offset a: ({_offsetX:F1}, {_offsetY:F1})");
                    CanvasView.InvalidateSurface();
                    break;
            }
        }

        /// <summary>
        /// Handler para gestos de Pinch (zoom con dos dedos)
        /// </summary>
        private void OnPinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
        {
            switch (e.Status)
            {
                case GestureStatus.Started:
                    _startZoomFactor = _zoomFactor;
                    System.Diagnostics.Debug.WriteLine($"[CircularLogoEditor] Pinch STARTED - zoom inicial: {_startZoomFactor:F2}");
                    break;

                case GestureStatus.Running:
                    float newZoom = _startZoomFactor * (float)e.Scale;
                    // Limitar zoom entre 1.0 y 3.0
                    _zoomFactor = Math.Clamp(newZoom, 1.0f, 3.0f);

                    // Actualizar slider también
                    ZoomSlider.Value = _zoomFactor;

                    System.Diagnostics.Debug.WriteLine($"[CircularLogoEditor] Pinch RUNNING - Scale: {e.Scale:F2}, new zoom: {_zoomFactor:F2}");
                    CanvasView.InvalidateSurface();
                    break;

                case GestureStatus.Completed:
                    System.Diagnostics.Debug.WriteLine($"[CircularLogoEditor] Pinch COMPLETED - zoom final: {_zoomFactor:F2}");
                    break;
            }
        }

        /// <summary>
        /// Handler para eventos de touch directos de SkiaSharp
        /// </summary>
        private void OnCanvasTouch(object sender, SKTouchEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[CircularLogoEditor] Touch event - Type: {e.ActionType}, Location: ({e.Location.X:F1}, {e.Location.Y:F1}), InContact: {e.InContact}");

            // Marcar como manejado para que se sigan recibiendo eventos
            e.Handled = true;
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[CircularLogoEditor] === OnSaveClicked - Iniciando guardado ===");
                System.Diagnostics.Debug.WriteLine($"[CircularLogoEditor] Estado actual - Zoom: {_zoomFactor:F2}, OffsetX: {_offsetX:F1}, OffsetY: {_offsetY:F1}");
                System.Diagnostics.Debug.WriteLine($"[CircularLogoEditor] Canvas real: {_actualCanvasWidth}x{_actualCanvasHeight}");

                if (_originalBitmap == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[CircularLogoEditor] ERROR - No hay imagen cargada (_originalBitmap es null)");
                    var labelError = await TranslateExtension.TranslateAsync("LabelError");
                    var logoEditorNoImage = await TranslateExtension.TranslateAsync("LogoEditorNoImage");
                    await ModernAlerts.ShowError(labelError, logoEditorNoImage);
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[CircularLogoEditor] Imagen original: {_originalBitmap.Width}x{_originalBitmap.Height}");

                // Crear bitmap de salida
                using (var outputBitmap = new SKBitmap(OUTPUT_SIZE, OUTPUT_SIZE))
                using (var canvas = new SKCanvas(outputBitmap))
                {
                    canvas.Clear(SKColors.Transparent);

                    // Usar las mismas fórmulas que OnCanvasViewPaintSurface para consistencia
                    int canvasWidth = _actualCanvasWidth;
                    int canvasHeight = _actualCanvasHeight;

                    float circleRadius = Math.Min(canvasWidth, canvasHeight) * 0.4f;
                    float centerX = canvasWidth / 2f;
                    float centerY = canvasHeight / 2f;

                    // Escala de la imagen en el canvas de preview
                    float previewScale = Math.Max(
                        (float)canvasWidth / _originalBitmap.Width,
                        (float)canvasHeight / _originalBitmap.Height
                    ) * _zoomFactor;

                    System.Diagnostics.Debug.WriteLine($"[CircularLogoEditor] previewScale: {previewScale:F4}");
                    System.Diagnostics.Debug.WriteLine($"[CircularLogoEditor] circleRadius: {circleRadius:F1}, centerX: {centerX:F1}, centerY: {centerY:F1}");

                    // Calcular la posición de la imagen escalada en el canvas
                    float scaledWidth = _originalBitmap.Width * previewScale;
                    float scaledHeight = _originalBitmap.Height * previewScale;

                    // Posición del borde superior-izquierdo de la imagen en el canvas
                    float imageLeft = centerX - (scaledWidth / 2f) + _offsetX;
                    float imageTop = centerY - (scaledHeight / 2f) + _offsetY;

                    System.Diagnostics.Debug.WriteLine($"[CircularLogoEditor] Imagen en canvas - left: {imageLeft:F1}, top: {imageTop:F1}, scaledW: {scaledWidth:F1}, scaledH: {scaledHeight:F1}");

                    // El círculo de recorte está centrado en el canvas
                    // Necesitamos encontrar qué parte de la imagen original está dentro del círculo
                    float circleLeft = centerX - circleRadius;
                    float circleTop = centerY - circleRadius;
                    float circleDiameter = circleRadius * 2f;

                    System.Diagnostics.Debug.WriteLine($"[CircularLogoEditor] Círculo en canvas - left: {circleLeft:F1}, top: {circleTop:F1}, diameter: {circleDiameter:F1}");

                    // Convertir las coordenadas del círculo a coordenadas de la imagen original
                    // srcX = (canvasX - imageLeft) / previewScale
                    float srcLeft = (circleLeft - imageLeft) / previewScale;
                    float srcTop = (circleTop - imageTop) / previewScale;
                    float srcSize = circleDiameter / previewScale;

                    System.Diagnostics.Debug.WriteLine($"[CircularLogoEditor] Área de recorte en imagen original - left: {srcLeft:F1}, top: {srcTop:F1}, size: {srcSize:F1}");

                    // Crear el rectángulo fuente (área de la imagen original a recortar)
                    var srcRect = new SKRect(srcLeft, srcTop, srcLeft + srcSize, srcTop + srcSize);

                    // Validar que el rectángulo esté dentro de los límites de la imagen
                    srcRect = new SKRect(
                        Math.Max(0, srcRect.Left),
                        Math.Max(0, srcRect.Top),
                        Math.Min(_originalBitmap.Width, srcRect.Right),
                        Math.Min(_originalBitmap.Height, srcRect.Bottom)
                    );

                    System.Diagnostics.Debug.WriteLine($"[CircularLogoEditor] srcRect ajustado: L={srcRect.Left:F1}, T={srcRect.Top:F1}, R={srcRect.Right:F1}, B={srcRect.Bottom:F1}");

                    // Aplicar clip circular al canvas de salida
                    using (var clipPath = new SKPath())
                    {
                        clipPath.AddCircle(OUTPUT_SIZE / 2f, OUTPUT_SIZE / 2f, OUTPUT_SIZE / 2f);
                        canvas.ClipPath(clipPath);
                    }

                    // Calcular el destRect ajustado si srcRect fue recortado por los límites
                    var destRect = new SKRect(0, 0, OUTPUT_SIZE, OUTPUT_SIZE);

                    // Si el srcRect fue recortado, ajustar el destRect proporcionalmente
                    if (srcRect.Left > srcLeft || srcRect.Top > srcTop ||
                        srcRect.Right < srcLeft + srcSize || srcRect.Bottom < srcTop + srcSize)
                    {
                        float destLeft = (srcRect.Left - srcLeft) / srcSize * OUTPUT_SIZE;
                        float destTop = (srcRect.Top - srcTop) / srcSize * OUTPUT_SIZE;
                        float destRight = OUTPUT_SIZE - ((srcLeft + srcSize - srcRect.Right) / srcSize * OUTPUT_SIZE);
                        float destBottom = OUTPUT_SIZE - ((srcTop + srcSize - srcRect.Bottom) / srcSize * OUTPUT_SIZE);
                        destRect = new SKRect(destLeft, destTop, destRight, destBottom);

                        System.Diagnostics.Debug.WriteLine($"[CircularLogoEditor] destRect ajustado: L={destRect.Left:F1}, T={destRect.Top:F1}, R={destRect.Right:F1}, B={destRect.Bottom:F1}");
                    }

                    // Dibujar la imagen recortada
                    using (var paint = new SKPaint())
                    {
                        paint.IsAntialias = true;
                        paint.FilterQuality = SKFilterQuality.High;
                        canvas.DrawBitmap(_originalBitmap, srcRect, destRect, paint);
                    }

                    // Guardar en archivo temporal
                    string tempPath = Path.Combine(FileSystem.CacheDirectory, $"logo_{Guid.NewGuid()}.png");

                    using (var image = SKImage.FromBitmap(outputBitmap))
                    using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                    using (var stream = File.OpenWrite(tempPath))
                    {
                        data.SaveTo(stream);
                    }

                    CroppedImagePath = tempPath;
                    Success = true;

                    // Verificar que el archivo se guardó correctamente
                    if (File.Exists(tempPath))
                    {
                        var savedFileInfo = new FileInfo(tempPath);
                        System.Diagnostics.Debug.WriteLine($"[CircularLogoEditor] Logo guardado EXITOSAMENTE en: {tempPath}");
                        System.Diagnostics.Debug.WriteLine($"[CircularLogoEditor] Tamaño del archivo guardado: {savedFileInfo.Length} bytes");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[CircularLogoEditor] ADVERTENCIA - Archivo no existe después de guardar: {tempPath}");
                    }
                }

                await CloseAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CircularLogoEditor] ERROR en OnSaveClicked: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[CircularLogoEditor] StackTrace: {ex.StackTrace}");
                var labelError = await TranslateExtension.TranslateAsync("LabelError");
                var logoEditorSaveError = await TranslateExtension.TranslateAsync("LogoEditorSaveError");
                await ModernAlerts.ShowError(labelError, $"{logoEditorSaveError}: {ex.Message}");
            }
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            Success = false;
            await CloseAsync();
        }
    }
}
