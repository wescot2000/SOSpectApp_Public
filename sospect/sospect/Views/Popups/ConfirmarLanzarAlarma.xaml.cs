// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;
using sospect.ViewModels;
using sospect.Views.Popups;
using System;
using System.IO;
using System.Linq;
using sospect.Helpers;

namespace sospect.Views.Popups
{
    public partial class ConfirmarLanzarAlarma : Popup
    {
        private LanzarAlarmaViewModel vm;
        private bool _isClosing = false;
        private bool _isSelectorOpen = false;
        private bool _isMapOpen = false;

        public ConfirmarLanzarAlarma(double latitude, double longitude)
        {
            InitializeComponent();
            vm = new LanzarAlarmaViewModel(App.persona.user_id_thirdparty, latitude, longitude);
            BindingContext = vm;

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
            if (_isSelectorOpen || !vm.IsInitialized || vm.TiposAlarma == null || !vm.TiposAlarma.Any())
                return;

            try
            {
                _isSelectorOpen = true;

                // IMPORTANTE: usarListaExacta:true para que el picker respete la lista ya filtrada
                // por LanzarAlarmaViewModel (que aplica el convenio correctamente). Sin esto, el picker
                // usa App.TiposAlarmaDisponibles y filtra por VisibleEnAppIos, bloqueando tipos como
                // crimen (id=2) incluso cuando el país tiene convenio activo.
                var pickerPopup = new TipoAlarmaPickerPopup(vm.TiposAlarma, vm.TipoAlarmaSeleccionado, usarListaExacta: true);

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

                            // PASO 2: Cerrar el popup padre (ConfirmarLanzarAlarma)
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
                                await navigation.PushAsync(new PromocionLocalPage(vm.Latitude, vm.Longitude));
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine("ERROR: No se pudo obtener Navigation para PromocionLocalPage");
                            }
                        }
                        else
                        {
                            // Tipo de alarma normal: seleccionar y continuar
                            vm.TipoAlarmaSeleccionado = tipoSeleccionado;
                            _isSelectorOpen = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ConfirmarLanzarAlarma] Error en TipoAlarmaSelected: {ex.Message}");
                        CrashlyticsHelper.LogError(ex, "ConfirmarLanzarAlarma", "TipoAlarmaSelected");
                        _isSelectorOpen = false;
                    }
                };

                await Application.Current.MainPage.ShowPopupAsync(pickerPopup);
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ConfirmarLanzarAlarma", "OnTipoAlarmaTapped");
                System.Diagnostics.Debug.WriteLine($"Error mostrando selector: {ex.Message}");
            }
            finally
            {
                _isSelectorOpen = false;
            }
        }

        private async void OnCameraButtonTapped(object sender, EventArgs e)
        {
            if (!vm.IsInitialized || _isSelectorOpen)
                return;

            _isSelectorOpen = true;
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
                    System.Diagnostics.Debug.WriteLine("ConfirmarLanzarAlarma: Abriendo CameraPopup para foto");

                    var cameraPopup = new CameraPopup();

                    cameraPopup.PhotoCaptured += async (s, photoPath) =>
                    {
                        System.Diagnostics.Debug.WriteLine($"ConfirmarLanzarAlarma: Foto capturada: {photoPath}");

                        await MediaGalleryHelper.SaveToGalleryAsync(photoPath, isVideo: false);
                        await vm.ProcessMediaFileFromPath(photoPath, false);
                    };

                    await Application.Current.MainPage.ShowPopupAsync(cameraPopup);
                }
                else if (result == ThreeOptionResult.Option2)
                {
#if IOS || MACCATALYST
                    // iOS: Camera.MAUI tiene limitaciones para grabación de video.
                    // Usar MediaPicker.CaptureVideoAsync() (cámara nativa del sistema) que es más confiable.
                    System.Diagnostics.Debug.WriteLine("ConfirmarLanzarAlarma: iOS - usando MediaPicker.CaptureVideoAsync para video");

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
                            System.Diagnostics.Debug.WriteLine($"ConfirmarLanzarAlarma: Video iOS copiado a caché: {videoPath}");
                            await MediaGalleryHelper.SaveToGalleryAsync(videoPath, isVideo: true);
                            await vm.ProcessMediaFileFromPath(videoPath, true);
                        });
                    }
#else
                    // Android: usar VideoCameraPopup con cámara integrada
                    System.Diagnostics.Debug.WriteLine("ConfirmarLanzarAlarma: Abriendo VideoCameraPopup para video");

                    var videoCameraPopup = new VideoCameraPopup();

                    videoCameraPopup.VideoCaptured += async (s, videoPath) =>
                    {
                        System.Diagnostics.Debug.WriteLine($"ConfirmarLanzarAlarma: Video capturado: {videoPath}");

                        await MediaGalleryHelper.SaveToGalleryAsync(videoPath, isVideo: true);
                        await vm.ProcessMediaFileFromPath(videoPath, true);
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
                            System.Diagnostics.Debug.WriteLine($"ConfirmarLanzarAlarma: Foto galería copiada a caché: {cachedPath}");
                            await vm.ProcessMediaFileFromPath(cachedPath, false);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ConfirmarLanzarAlarma", "OnCameraButtonTapped");
                System.Diagnostics.Debug.WriteLine($"ConfirmarLanzarAlarma ERROR: {ex.Message}");
            }
            finally
            {
                _isSelectorOpen = false;
            }
        }

        private async void OnMarcarRutaEscapeTapped(object sender, EventArgs e)
        {
            // Guard: evitar abrir múltiples instancias del minimapa si el usuario toca el botón
            // varias veces seguidas (ej: conexión lenta → el popup tarda en aparecer).
            // El doble-tap causaba un stack Popup A → Popup B → Popup C que terminaba en
            // PopupBlockedException al intentar cerrar (crash reportado en Crashlytics).
            if (_isMapOpen)
                return;

            _isMapOpen = true;
            try
            {
                await Application.Current.MainPage.ShowPopupAsync(new MinimapaPopUpLanzar(BindingContext));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ConfirmarLanzarAlarma] Error abriendo minimapa: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "ConfirmarLanzarAlarma", "OnMarcarRutaEscapeTapped");
            }
            finally
            {
                _isMapOpen = false;
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
                        System.Diagnostics.Debug.WriteLine("[ConfirmarLanzarAlarma] Esperando a que se cierre el selector...");

                        // Intentar cerrar cualquier popup modal que esté en el stack
                        try
                        {
                            // Dar tiempo para que el popup hijo se cierre si está en proceso
                            await Task.Delay(300);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[ConfirmarLanzarAlarma] Error esperando cierre de popup hijo: {ex.Message}");
                        }
                    }

                    MessagingCenter.Unsubscribe<LanzarAlarmaViewModel>(this, "CerrarPopup");
                    await CloseAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ConfirmarLanzarAlarma] Error al cancelar alarma: {ex.Message}");
                    CrashlyticsHelper.LogError(ex, "ConfirmarLanzarAlarma", "Cancelar_Alarma");
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

