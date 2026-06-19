// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
using System.IO;
using System.Timers;
using CommunityToolkit.Maui.Views;
using sospect.Helpers;

namespace sospect.Views.Popups
{
    public partial class VideoCameraPopup : Popup
    {
        public event EventHandler<string> VideoCaptured;

        private const int MaxRecordingSeconds = 60;

        private bool _isRecording = false;
        private bool _isProcessing = false;
        private System.Timers.Timer _recordingTimer;
        private int _recordingSeconds = 0;
        private string _videoFilePath;

        public VideoCameraPopup()
        {
            InitializeComponent();
            InitializeTimer();
            InitializeCamera();
        }

        private void InitializeTimer()
        {
            _recordingTimer = new System.Timers.Timer(1000);
            _recordingTimer.Elapsed += OnRecordingTimerElapsed;
            _recordingTimer.AutoReset = true;
        }

        private async void InitializeCamera()
        {
            try
            {
                // Esperar a que el CameraView esté listo
                await Task.Delay(500);

                Console.WriteLine($"[VIDEO] InitializeCamera: NumCameras={cameraView.Cameras?.Count ?? 0}");

                // Obtener las cámaras disponibles
                if (cameraView.Cameras?.Count > 0)
                {
                    // Seleccionar la cámara trasera por defecto
                    var backCamera = cameraView.Cameras.FirstOrDefault(c => c.Position == Camera.MAUI.CameraPosition.Back);
                    cameraView.Camera = backCamera ?? cameraView.Cameras.First();

                    Console.WriteLine($"[VIDEO] InitializeCamera: Camera={cameraView.Camera?.Name}");

                    // Iniciar la cámara
                    var result = await cameraView.StartCameraAsync();
                    Console.WriteLine($"[VIDEO] InitializeCamera: StartCameraAsync result={result}");

                    System.Diagnostics.Debug.WriteLine("VideoCameraPopup: Cámara iniciada correctamente");
                }
                else
                {
                    Console.WriteLine("[VIDEO] InitializeCamera: No se encontraron cámaras");
                    System.Diagnostics.Debug.WriteLine("VideoCameraPopup: No se encontraron cámaras");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VIDEO] InitializeCamera EXCEPTION: {ex.GetType().Name}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"VideoCameraPopup: Error al inicializar cámara: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "VideoCameraPopup", "InitializeCamera");
            }
        }

        private void OnRecordingTimerElapsed(object sender, ElapsedEventArgs e)
        {
            _recordingSeconds++;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                var minutes = _recordingSeconds / 60;
                var seconds = _recordingSeconds % 60;
                timerLabel.Text = $"{minutes:D2}:{seconds:D2}";

                // Efecto de parpadeo del punto rojo
                recordingDot.Opacity = recordingDot.Opacity == 1 ? 0.3 : 1;
            });

            // Auto-detener si se alcanza el máximo
            if (_recordingSeconds >= MaxRecordingSeconds)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await StopRecordingAndSave();
                });
            }
        }

        private async void OnRecordButtonTapped(object sender, TappedEventArgs e)
        {
            if (_isProcessing)
                return;

            if (!_isRecording)
            {
                await StartRecording();
            }
            else
            {
                await StopRecordingAndSave();
            }
        }

        private async Task StartRecording()
        {
            if (_isRecording || _isProcessing)
                return;

            try
            {
                System.Diagnostics.Debug.WriteLine("VideoCameraPopup: Iniciando grabación...");
                Console.WriteLine("[VIDEO] StartRecording: inicio");

                // Verificar permiso de micrófono (requerido en iOS para grabar video)
                var micStatus = await Permissions.CheckStatusAsync<Permissions.Microphone>();
                Console.WriteLine($"[VIDEO] Permiso micrófono: {micStatus}");
                if (micStatus != PermissionStatus.Granted)
                {
                    micStatus = await Permissions.RequestAsync<Permissions.Microphone>();
                    Console.WriteLine($"[VIDEO] Permiso micrófono tras solicitud: {micStatus}");
                }

                if (micStatus != PermissionStatus.Granted)
                {
                    Console.WriteLine("[VIDEO] ERROR: Permiso de micrófono denegado");
                    var errorTitle = await TranslateExtension.TranslateAsync("LabelError");
                    var errorMsg = await TranslateExtension.TranslateAsync("LblErrorGrabarVideo");
                    await ModernAlerts.ShowError(errorTitle, errorMsg ?? "Error al iniciar la grabación de video");
                    return;
                }

                // Preparar ruta del archivo
                var cacheDir = FileSystem.CacheDirectory;
                if (!Directory.Exists(cacheDir))
                {
                    Directory.CreateDirectory(cacheDir);
                }

                _videoFilePath = Path.Combine(cacheDir, $"{Guid.NewGuid()}.mp4");
                Console.WriteLine($"[VIDEO] Archivo destino: {_videoFilePath}");
                Console.WriteLine($"[VIDEO] CameraView.Camera: {cameraView.Camera?.Name ?? "null"}");
                Console.WriteLine($"[VIDEO] CameraView.NumCameras: {cameraView.Cameras?.Count ?? 0}");

                // Iniciar grabación con Camera.MAUI
                var result = await cameraView.StartRecordingAsync(_videoFilePath, new Size(1920, 1080));
                Console.WriteLine($"[VIDEO] StartRecordingAsync result: {result}");

                if (result == Camera.MAUI.CameraResult.Success)
                {
                    _isRecording = true;
                    _recordingSeconds = 0;

                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        // Cambiar botón a cuadrado (detener)
                        recordButton.BackgroundColor = Colors.Red;
                        recordButtonShape.CornerRadius = 10;
                        recordButton.WidthRequest = 60;
                        recordButton.HeightRequest = 60;

                        // Mostrar indicador de grabación
                        recordingIndicatorPanel.IsVisible = true;
                        timerLabel.Text = "00:00";

                        // Actualizar estado
                        var stopText = await TranslateExtension.TranslateAsync("LblTocaParaDetener");
                        statusLabel.Text = stopText ?? "Toca para detener";
                        statusLabel.TextColor = Colors.Red;

                        // Deshabilitar botón cancelar durante grabación
                        cancelButton.IsEnabled = false;
                        cancelButton.Opacity = 0.5;
                    });

                    // Iniciar timer
                    _recordingTimer.Start();

                    System.Diagnostics.Debug.WriteLine("VideoCameraPopup: Grabación iniciada");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"VideoCameraPopup: Error al iniciar grabación: {result}");

                    var errorTitle = await TranslateExtension.TranslateAsync("LabelError");
                    var errorMsg = await TranslateExtension.TranslateAsync("LblErrorGrabarVideo");
                    await ModernAlerts.ShowError(errorTitle, errorMsg ?? "Error al iniciar la grabación de video");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"VideoCameraPopup ERROR al iniciar grabación: {ex.Message}");
                Console.WriteLine($"[VIDEO] EXCEPTION en StartRecording: {ex.GetType().Name}: {ex.Message}");
                Console.WriteLine($"[VIDEO] StackTrace: {ex.StackTrace}");
                CrashlyticsHelper.LogError(ex, "VideoCameraPopup", "StartRecording");

                var errorTitle = await TranslateExtension.TranslateAsync("LabelError");
                var errorMsg = await TranslateExtension.TranslateAsync("LblErrorGrabarVideo");
                await ModernAlerts.ShowError(errorTitle, errorMsg ?? "Error al iniciar la grabación de video");
            }
        }

        private async Task StopRecordingAndSave()
        {
            if (!_isRecording || _isProcessing)
                return;

            _isProcessing = true;
            _recordingTimer.Stop();

            try
            {
                System.Diagnostics.Debug.WriteLine("VideoCameraPopup: Deteniendo grabación...");

                // Mostrar overlay de procesamiento
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var processingText = await TranslateExtension.TranslateAsync("LblProcesandoVideo");
                    processingLabel.Text = processingText ?? "Procesando video...";
                    processingOverlay.IsVisible = true;
                });

                // Detener grabación
                var result = await cameraView.StopRecordingAsync();

                _isRecording = false;

                if (result == Camera.MAUI.CameraResult.Success && File.Exists(_videoFilePath))
                {
                    var fileSize = new FileInfo(_videoFilePath).Length;
                    System.Diagnostics.Debug.WriteLine($"VideoCameraPopup: Video guardado en {_videoFilePath}, tamaño: {fileSize / 1024}KB");

                    if (fileSize > 0)
                    {
                        // Disparar evento con la ruta del video
                        VideoCaptured?.Invoke(this, _videoFilePath);

                        await Task.Delay(300);
                        await CloseAsync();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("VideoCameraPopup: Video vacío");

                        var warningTitle = await TranslateExtension.TranslateAsync("LabelAdvertencia");
                        var warningMsg = await TranslateExtension.TranslateAsync("LblVideoVacio");
                        await ModernAlerts.ShowWarning(warningTitle, warningMsg ?? "No se capturó ningún video");

                        await ResetUI();
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"VideoCameraPopup: Error al detener grabación: {result}");

                    var errorTitle = await TranslateExtension.TranslateAsync("LabelError");
                    var errorMsg = await TranslateExtension.TranslateAsync("LblErrorGuardarVideo");
                    await ModernAlerts.ShowError(errorTitle, errorMsg ?? "Error al guardar el video");

                    await ResetUI();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"VideoCameraPopup ERROR al detener grabación: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "VideoCameraPopup", "StopRecordingAndSave");

                var errorTitle = await TranslateExtension.TranslateAsync("LabelError");
                var errorMsg = await TranslateExtension.TranslateAsync("LblErrorGuardarVideo");
                await ModernAlerts.ShowError(errorTitle, errorMsg ?? "Error al guardar el video");

                await ResetUI();
            }
            finally
            {
                _isProcessing = false;
            }
        }

        private async Task ResetUI()
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                // Restaurar botón a círculo rojo
                recordButton.BackgroundColor = Colors.Red;
                recordButtonShape.CornerRadius = 35;
                recordButton.WidthRequest = 70;
                recordButton.HeightRequest = 70;

                // Ocultar indicadores
                recordingIndicatorPanel.IsVisible = false;
                processingOverlay.IsVisible = false;

                // Restaurar estado
                var startText = await TranslateExtension.TranslateAsync("LblTocaParaGrabar");
                statusLabel.Text = startText ?? "Toca para grabar";
                statusLabel.TextColor = Color.FromArgb("#AAAAAA");

                // Habilitar botón cancelar
                cancelButton.IsEnabled = true;
                cancelButton.Opacity = 1;

                // Resetear timer
                timerLabel.Text = "00:00";
                recordingDot.Opacity = 1;
            });
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            if (_isRecording)
            {
                // Si está grabando, preguntar si quiere descartar
                var confirmTitle = await TranslateExtension.TranslateAsync("LabelConfirmar");
                var confirmMsg = await TranslateExtension.TranslateAsync("LblDescartarVideo");
                var yesText = await TranslateExtension.TranslateAsync("LabelSi");
                var noText = await TranslateExtension.TranslateAsync("LabelNo");

                var confirmar = await ModernAlerts.ShowConfirmation(
                    confirmTitle,
                    confirmMsg ?? "¿Desea descartar el video que está grabando?",
                    yesText,
                    noText,
                    useTranslations: false
                );

                if (confirmar)
                {
                    _recordingTimer.Stop();
                    _isRecording = false;

                    try
                    {
                        await cameraView.StopRecordingAsync();

                        // Esperar a que se liberen los recursos de grabación
                        await Task.Delay(200);

                        // Eliminar archivo temporal si existe
                        if (!string.IsNullOrEmpty(_videoFilePath) && File.Exists(_videoFilePath))
                        {
                            File.Delete(_videoFilePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"VideoCameraPopup: Error al cancelar grabación: {ex.Message}");
                    }

                    System.Diagnostics.Debug.WriteLine("VideoCameraPopup: Usuario canceló y descartó el video");
                    await StopCameraAndClose();
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("VideoCameraPopup: Usuario canceló");
                await StopCameraAndClose();
            }
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

                System.Diagnostics.Debug.WriteLine("VideoCameraPopup: Cámara detenida y recursos liberados");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"VideoCameraPopup: Error al detener cámara: {ex.Message}");
            }

            await CloseAsync();
        }

        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();

            if (Handler == null)
            {
                // Limpiar recursos
                _recordingTimer?.Stop();
                _recordingTimer?.Dispose();

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


