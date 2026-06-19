// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;
using sospect.Models;
using sospect.ViewModels;
using sospect.Helpers;
using System;
using System.IO;

namespace sospect.Views.Popups
{
    public partial class ConfirmarLanzarAlarmaEnUbicacionActual : Popup
    {
        private bool _isClosing = false;
        private bool _isSelectorOpen = false;
        private LanzarAlarmaViewModel _viewModel;

        public ConfirmarLanzarAlarmaEnUbicacionActual(double latitude, double longitude, int tipoAlarma)
        {
            InitializeComponent();
            _viewModel = new LanzarAlarmaViewModel(App.persona.user_id_thirdparty, latitude, longitude, tipoAlarma);
            BindingContext = _viewModel;

            MessagingCenter.Subscribe<LanzarAlarmaViewModel>(this, "CerrarPopup", async (sender) =>
            {
                if (!_isClosing)
                {
                    _isClosing = true;
                    await CloseAsync();
                }
            });
        }

        private async void OnTipoAlarmaTapped(object sender, EventArgs e)
        {
            if (_isSelectorOpen || !_viewModel.IsInitialized || _viewModel.TiposAlarma == null || !_viewModel.TiposAlarma.Any())
                return;

            try
            {
                _isSelectorOpen = true;

                // IMPORTANTE: usarListaExacta:true para que el picker respete la lista ya filtrada
                // por LanzarAlarmaViewModel (que aplica el convenio correctamente). Sin esto, el picker
                // usa App.TiposAlarmaDisponibles y filtra por VisibleEnAppIos, bloqueando tipos como
                // crimen (id=2) incluso cuando el país tiene convenio activo.
                var pickerPopup = new TipoAlarmaPickerPopup(_viewModel.TiposAlarma, _viewModel.TipoAlarmaSeleccionado, usarListaExacta: true);

                pickerPopup.TipoAlarmaSelected += async (s, tipoSeleccionado) =>
                {
                    try
                    {
                        // CRÍTICO: Si seleccionó "Promoción local" (id=13), abrir formulario de configuración
                        if (tipoSeleccionado.TipoalarmaId == 13)
                        {
                            // PASO 1: Cerrar el popup hijo (TipoAlarmaPickerPopup) primero
                            await pickerPopup.CloseAsync();
                            _isSelectorOpen = false;

                            // PASO 2: Cerrar el popup padre (ConfirmarLanzarAlarmaEnUbicacionActual)
                            if (!_isClosing)
                            {
                                _isClosing = true;
                                MessagingCenter.Unsubscribe<LanzarAlarmaViewModel>(this, "CerrarPopup");
                                await CloseAsync();
                            }

                            // PASO 3: Navegar a PromocionLocalPage
                            // Obtener el NavigationPage de la pestaña actual
                            INavigation navigation = null;
                            if (Application.Current.MainPage is TabbedPage tabbedPage && tabbedPage.CurrentPage is NavigationPage navPage)
                            {
                                navigation = navPage.Navigation;
                            }
                            else if (Application.Current.MainPage is NavigationPage np)
                            {
                                navigation = np.Navigation;
                            }

                            if (navigation != null)
                            {
                                await navigation.PushAsync(new PromocionLocalPage(_viewModel.Latitude, _viewModel.Longitude));
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine("ERROR: No se pudo obtener Navigation para PromocionLocalPage");
                            }
                        }
                        else
                        {
                            // Tipo de alarma normal: seleccionar y continuar
                            _viewModel.TipoAlarmaSeleccionado = tipoSeleccionado;
                            _isSelectorOpen = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ConfirmarLanzarAlarmaEnUbicacionActual] Error en TipoAlarmaSelected: {ex.Message}");
                        CrashlyticsHelper.LogError(ex, "ConfirmarLanzarAlarmaEnUbicacionActual", "TipoAlarmaSelected");
                        _isSelectorOpen = false;
                    }
                };

                await Application.Current.MainPage.ShowPopupAsync(pickerPopup);
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ConfirmarLanzarAlarmaEnUbicacionActual", "OnTipoAlarmaTapped");
                System.Diagnostics.Debug.WriteLine($"Error mostrando selector: {ex.Message}");
            }
            finally
            {
                _isSelectorOpen = false;
            }
        }

        private async void OnCameraButtonTapped(object sender, EventArgs e)
        {
            if (!_viewModel.IsInitialized)
                return;

            try
            {
                // UN solo popup con 3 opciones: Tomar foto, Grabar video, Seleccionar de galería
                var result = await ModernAlerts.ShowThreeOptions(
                    await TranslateExtension.TranslateAsync("LblAgregarMedia"),
                    await TranslateExtension.TranslateAsync("LblSeleccionarTipoMedia"),
                    await TranslateExtension.TranslateAsync("LblTomarFoto"),
                    await TranslateExtension.TranslateAsync("LblGrabarVideo"),
                    await TranslateExtension.TranslateAsync("LblSeleccionarFoto")
                );

                if (result == ThreeOptionResult.Option1)
                {
                    // Tomar foto con CameraPopup
                    System.Diagnostics.Debug.WriteLine("ConfirmarLanzarAlarmaEnUbicacionActual: Abriendo CameraPopup para foto");

                    var cameraPopup = new CameraPopup();

                    cameraPopup.PhotoCaptured += async (s, photoPath) =>
                    {
                        System.Diagnostics.Debug.WriteLine($"ConfirmarLanzarAlarmaEnUbicacionActual: Foto capturada: {photoPath}");

                        await MediaGalleryHelper.SaveToGalleryAsync(photoPath, isVideo: false);
                        await _viewModel.ProcessMediaFileFromPath(photoPath, false);
                    };

                    await Application.Current.MainPage.ShowPopupAsync(cameraPopup);
                }
                else if (result == ThreeOptionResult.Option2)
                {
#if IOS || MACCATALYST
                    // iOS: Camera.MAUI tiene limitaciones para grabación de video.
                    // Usar MediaPicker.CaptureVideoAsync() (cámara nativa del sistema) que es más confiable.
                    System.Diagnostics.Debug.WriteLine("ConfirmarLanzarAlarmaEnUbicacionActual: iOS - usando MediaPicker.CaptureVideoAsync para video");

                    var videoResult = await MediaPicker.CaptureVideoAsync();
                    if (videoResult != null)
                    {
                        var processingText = await TranslateExtension.TranslateAsync("LblProcesandoVideo");
                        await ModernAlerts.ShowProcessingAsync(processingText, async () =>
                        {
                            var cacheDir = FileSystem.CacheDirectory;
                            // Preservar extensión original (.mov en iPhone, .mp4 en otros)
                            var ext = System.IO.Path.GetExtension(videoResult.FileName)?.ToLowerInvariant();
                            if (string.IsNullOrEmpty(ext) || ext == ".") ext = ".mp4";
                            var videoPath = System.IO.Path.Combine(cacheDir, $"{Guid.NewGuid()}{ext}");
                            using (var source = await videoResult.OpenReadAsync())
                            using (var dest = System.IO.File.Create(videoPath))
                            {
                                await source.CopyToAsync(dest);
                            }
                            System.Diagnostics.Debug.WriteLine($"ConfirmarLanzarAlarmaEnUbicacionActual: Video iOS copiado a caché: {videoPath}");
                            await MediaGalleryHelper.SaveToGalleryAsync(videoPath, isVideo: true);
                            await _viewModel.ProcessMediaFileFromPath(videoPath, true);
                        });
                    }
#else
                    // Android: usar VideoCameraPopup con cámara integrada
                    System.Diagnostics.Debug.WriteLine("ConfirmarLanzarAlarmaEnUbicacionActual: Abriendo VideoCameraPopup para video");

                    var videoCameraPopup = new VideoCameraPopup();

                    videoCameraPopup.VideoCaptured += async (s, videoPath) =>
                    {
                        System.Diagnostics.Debug.WriteLine($"ConfirmarLanzarAlarmaEnUbicacionActual: Video capturado: {videoPath}");

                        await MediaGalleryHelper.SaveToGalleryAsync(videoPath, isVideo: true);
                        await _viewModel.ProcessMediaFileFromPath(videoPath, true);
                    };

                    await Application.Current.MainPage.ShowPopupAsync(videoCameraPopup);
#endif
                }
                else if (result == ThreeOptionResult.Option3)
                {
                    // Seleccionar de galería
                    var photo = await MediaPicker.PickPhotoAsync();

                    if (photo != null)
                    {
                        var processingText = await TranslateExtension.TranslateAsync("LblProcesandoFoto");
                        await ModernAlerts.ShowProcessingAsync(processingText, async () =>
                        {
                            // Copiar a caché primero: en iOS el FullPath del picker es una ruta temporal
                            // que puede ser limpiada antes de que ProcessMediaFileFromPath la acceda,
                            // causando File.Exists() == false y retorno silencioso sin thumbnail.
                            var cacheDir = FileSystem.CacheDirectory;
                            var ext = Path.GetExtension(photo.FileName);
                            if (string.IsNullOrEmpty(ext)) ext = ".jpg";
                            var cachedPath = Path.Combine(cacheDir, $"{Guid.NewGuid()}{ext}");
                            using (var source = await photo.OpenReadAsync())
                            using (var dest = File.Create(cachedPath))
                            {
                                await source.CopyToAsync(dest);
                            }
                            System.Diagnostics.Debug.WriteLine($"ConfirmarLanzarAlarmaEnUbicacionActual: Foto galería copiada a caché: {cachedPath}");
                            await _viewModel.ProcessMediaFileFromPath(cachedPath, false);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ConfirmarLanzarAlarmaEnUbicacionActual", "OnCameraButtonTapped");
                System.Diagnostics.Debug.WriteLine($"ConfirmarLanzarAlarmaEnUbicacionActual ERROR: {ex.Message}");
            }
        }

        private async void OnMarcarRutaEscapeTapped(object sender, EventArgs e)
        {
            try
            {
                await Application.Current.MainPage.ShowPopupAsync(new MinimapaPopUpLanzar(BindingContext));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ConfirmarLanzarAlarmaEnUbicacionActual] Error abriendo minimapa: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "ConfirmarLanzarAlarmaEnUbicacionActual", "OnMarcarRutaEscapeTapped");
            }
        }

        async void Cancelar_Alarma(object sender, EventArgs e)
        {
            if (!_isClosing)
            {
                _isClosing = true;

                try
                {
                    // Si hay un selector abierto, esperar a que se cierre o cerrarlo manualmente
                    if (_isSelectorOpen)
                    {
                        System.Diagnostics.Debug.WriteLine("[ConfirmarLanzarAlarmaEnUbicacionActual] Esperando a que se cierre el selector...");

                        // Intentar cerrar cualquier popup modal que esté en el stack
                        try
                        {
                            // Dar tiempo para que el popup hijo se cierre si está en proceso
                            await Task.Delay(300);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[ConfirmarLanzarAlarmaEnUbicacionActual] Error esperando cierre de popup hijo: {ex.Message}");
                        }
                    }

                    MessagingCenter.Unsubscribe<LanzarAlarmaViewModel>(this, "CerrarPopup");
                    await CloseAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ConfirmarLanzarAlarmaEnUbicacionActual] Error al cancelar alarma: {ex.Message}");
                    CrashlyticsHelper.LogError(ex, "ConfirmarLanzarAlarmaEnUbicacionActual", "Cancelar_Alarma");
                    _isClosing = false; // Resetear el flag en caso de error
                }
            }
        }

        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();
            if (Handler == null)
            {
                MessagingCenter.Unsubscribe<LanzarAlarmaViewModel>(this, "CerrarPopup");
            }
        }
    }
}

