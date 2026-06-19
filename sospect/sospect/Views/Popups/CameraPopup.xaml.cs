// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
using System.IO;
using CommunityToolkit.Maui.Views;
using sospect.Helpers;

namespace sospect.Views.Popups
{
    public partial class CameraPopup : Popup
    {
        public event EventHandler<string> PhotoCaptured;

        private const int MaxImageWidth = 1024;
        private const int JpegQuality = 60;
        private const int MaxFileSizeKB = 150;
        private bool _isProcessing = false;

        public CameraPopup()
        {
            InitializeComponent();
            InitializeCamera();
        }

        private async void InitializeCamera()
        {
            try
            {
                await Task.Delay(500);

                if (cameraView.Cameras?.Count > 0)
                {
                    var backCamera = cameraView.Cameras.FirstOrDefault(c => c.Position == Camera.MAUI.CameraPosition.Back);
                    cameraView.Camera = backCamera ?? cameraView.Cameras.First();

                    await cameraView.StartCameraAsync();

                    System.Diagnostics.Debug.WriteLine("CameraPopup: Cámara iniciada correctamente");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("CameraPopup: No se encontraron cámaras");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CameraPopup: Error al inicializar cámara: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "CameraPopup", "InitializeCamera");
            }
        }

        private async void OnCaptureClicked(object sender, EventArgs e)
        {
            if (_isProcessing)
            {
                System.Diagnostics.Debug.WriteLine("CameraPopup: Ya se está procesando una foto, ignorando clic");
                return;
            }

            _isProcessing = true;

            try
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    processingOverlay.IsVisible = true;
                });

                System.Diagnostics.Debug.WriteLine("CameraPopup: Iniciando captura de imagen con Camera.MAUI...");

                // Capturar imagen con Camera.MAUI
                var imageStream = await cameraView.TakePhotoAsync();

                if (imageStream != null && imageStream.Length > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"CameraPopup: Imagen capturada, tamaño original: {imageStream.Length / 1024}KB");

                    var cacheDir = FileSystem.CacheDirectory;
                    if (!Directory.Exists(cacheDir))
                    {
                        Directory.CreateDirectory(cacheDir);
                    }

                    var fileName = $"{Guid.NewGuid()}.jpg";
                    var capturedPhotoPath = Path.Combine(cacheDir, fileName);

                    // Procesar y comprimir la imagen
                    await ProcessAndSaveImage(imageStream, capturedPhotoPath);

                    imageStream.Dispose();

                    var finalSize = new FileInfo(capturedPhotoPath).Length;
                    System.Diagnostics.Debug.WriteLine($"CameraPopup: Foto guardada en {capturedPhotoPath}, tamaño final: {finalSize / 1024}KB");

                    PhotoCaptured?.Invoke(this, capturedPhotoPath);

                    await Task.Delay(300);
                    await StopCameraAndClose();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("CameraPopup: No se capturó ninguna imagen");

                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        processingOverlay.IsVisible = false;
                    });

                    var errorTitle = await TranslateExtension.TranslateAsync("LabelError");
                    var errorMsg = await TranslateExtension.TranslateAsync("LblErrorCapturarFoto");
                    await ModernAlerts.ShowError(errorTitle, errorMsg ?? "Error al capturar la foto");
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "CameraPopup", "OnCaptureClicked");
                System.Diagnostics.Debug.WriteLine($"CameraPopup ERROR: {ex.Message}");

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    processingOverlay.IsVisible = false;
                });

                var errorTitle = await TranslateExtension.TranslateAsync("LabelError");
                var errorMsg = await TranslateExtension.TranslateAsync("LblErrorCapturarFoto");
                await ModernAlerts.ShowError(errorTitle, errorMsg ?? "Error al capturar la foto");
            }
            finally
            {
                _isProcessing = false;
            }
        }

        private async Task ProcessAndSaveImage(Stream imageStream, string outputPath)
        {
#if ANDROID
            try
            {
                imageStream.Position = 0;

                // Decodificar la imagen original
                var options = new Android.Graphics.BitmapFactory.Options
                {
                    InJustDecodeBounds = true
                };
                Android.Graphics.BitmapFactory.DecodeStream(imageStream, null, options);

                // Calcular el factor de escala
                int originalWidth = options.OutWidth;
                int originalHeight = options.OutHeight;
                int sampleSize = 1;

                if (originalWidth > MaxImageWidth || originalHeight > MaxImageWidth)
                {
                    int halfWidth = originalWidth / 2;
                    int halfHeight = originalHeight / 2;

                    while ((halfWidth / sampleSize) >= MaxImageWidth || (halfHeight / sampleSize) >= MaxImageWidth)
                    {
                        sampleSize *= 2;
                    }
                }

                // Decodificar con el factor de escala
                options = new Android.Graphics.BitmapFactory.Options
                {
                    InSampleSize = sampleSize
                };

                imageStream.Position = 0;
                var bitmap = Android.Graphics.BitmapFactory.DecodeStream(imageStream, null, options);

                if (bitmap == null)
                {
                    System.Diagnostics.Debug.WriteLine("CameraPopup: No se pudo decodificar la imagen");
                    imageStream.Position = 0;
                    using (var fileStream = File.Create(outputPath))
                    {
                        await imageStream.CopyToAsync(fileStream);
                    }
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"CameraPopup: Imagen decodificada: {bitmap.Width}x{bitmap.Height}");

                // Rotar la imagen 90 grados a la derecha (corrección de orientación de Camera.MAUI)
                var matrix = new Android.Graphics.Matrix();
                matrix.PostRotate(90);
                var rotatedBitmap = Android.Graphics.Bitmap.CreateBitmap(bitmap, 0, 0, bitmap.Width, bitmap.Height, matrix, true);
                bitmap.Dispose();
                bitmap = rotatedBitmap;

                System.Diagnostics.Debug.WriteLine($"CameraPopup: Imagen rotada 90° a la derecha: {bitmap.Width}x{bitmap.Height}");

                // Redimensionar si es necesario
                if (bitmap.Width > MaxImageWidth || bitmap.Height > MaxImageWidth)
                {
                    float scale = Math.Min((float)MaxImageWidth / bitmap.Width, (float)MaxImageWidth / bitmap.Height);
                    int newWidth = (int)(bitmap.Width * scale);
                    int newHeight = (int)(bitmap.Height * scale);

                    var scaledBitmap = Android.Graphics.Bitmap.CreateScaledBitmap(bitmap, newWidth, newHeight, true);
                    bitmap.Dispose();
                    bitmap = scaledBitmap;

                    System.Diagnostics.Debug.WriteLine($"CameraPopup: Imagen redimensionada a: {bitmap.Width}x{bitmap.Height}");
                }

                // Comprimir con calidad ajustable para lograr < 300KB
                int quality = JpegQuality;
                byte[] compressedBytes;

                do
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        bitmap.Compress(Android.Graphics.Bitmap.CompressFormat.Jpeg, quality, memoryStream);
                        compressedBytes = memoryStream.ToArray();
                    }

                    System.Diagnostics.Debug.WriteLine($"CameraPopup: Comprimido con calidad {quality}: {compressedBytes.Length / 1024}KB");

                    if (compressedBytes.Length <= MaxFileSizeKB * 1024)
                        break;

                    quality -= 10;
                } while (quality >= 20);

                // Guardar al archivo
                await File.WriteAllBytesAsync(outputPath, compressedBytes);

                bitmap.Dispose();

                System.Diagnostics.Debug.WriteLine($"CameraPopup: Imagen guardada con calidad {quality}, tamaño: {compressedBytes.Length / 1024}KB");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CameraPopup: Error procesando imagen: {ex.Message}");

                // Fallback: guardar sin procesar
                imageStream.Position = 0;
                using (var fileStream = File.Create(outputPath))
                {
                    await imageStream.CopyToAsync(fileStream);
                }
            }
#else
            // Para otras plataformas, guardar directamente
            imageStream.Position = 0;
            using (var fileStream = File.Create(outputPath))
            {
                await imageStream.CopyToAsync(fileStream);
            }
#endif
        }

        private async Task StopCameraAndClose()
        {
            try
            {
                // Detener la cámara
                await cameraView.StopCameraAsync();

                // Esperar un momento para que los recursos se liberen
                await Task.Delay(300);

                // Forzar liberación de recursos nativos
                cameraView.Camera = null;

#if ANDROID
                // Forzar garbage collection para liberar recursos de cámara
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
#endif

                System.Diagnostics.Debug.WriteLine("CameraPopup: Cámara detenida y recursos liberados");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CameraPopup: Error al detener cámara: {ex.Message}");
            }

            await CloseAsync();
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("CameraPopup: Usuario canceló");
            await StopCameraAndClose();
        }

        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();

            if (Handler == null)
            {
                try
                {
                    cameraView.Camera = null;
                    cameraView?.StopCameraAsync();

#if ANDROID
                    // Forzar garbage collection para liberar recursos de cámara
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
#endif
                }
                catch { }
            }
        }
    }
}


