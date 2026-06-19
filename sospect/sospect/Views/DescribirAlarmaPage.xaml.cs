// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;
using sospect.Helpers;
using sospect.Models;
using sospect.Services;
using sospect.ViewModels;
using sospect.Views.Popups;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace sospect.Views
{
    [Microsoft.Maui.Controls.Xaml.XamlCompilation(Microsoft.Maui.Controls.Xaml.XamlCompilationOptions.Compile)]
#if ANDROID
    [Android.Runtime.Preserve(AllMembers = true)]
#elif IOS || MACCATALYST
    [Foundation.Preserve(AllMembers = true)]
#endif
    public partial class DescribirAlarmaPage : ContentPage
    {
        private DescribirAlarmaPopUpViewModel _viewModel;
        private bool _isSelectorOpen = false;

        public DescribirAlarmaPage(AlarmaCercana alarmaCercana)
        {
            InitializeComponent();
            _viewModel = new DescribirAlarmaPopUpViewModel(alarmaCercana);
            BindingContext = _viewModel;
        }

        public DescribirAlarmaPage(DescribirAlarma alarmaAEditar)
        {
            InitializeComponent();
            _viewModel = new DescribirAlarmaPopUpViewModel(alarmaAEditar);
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // DEFENSA CONTRA RACE CONDITION: Si esta página se abrió para una alarma específica
            // (propietario desde el mapa), verificar con la API si hay votación activa.
            // Si la hay, redirigir a VerHistorialAlarmaPage sin permitir que el propietario
            // agregue nuevas descripciones durante el período de votación.
            if (_viewModel?.Alarma?.alarma_id > 0 && _viewModel.Alarma.flag_propietario_alarma)
            {
                bool tieneVotacion = _viewModel.Alarma.TieneVotacionActiva;

                if (!tieneVotacion)
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"[DescribirAlarmaPage] Verificando votación activa en API para alarma {_viewModel.Alarma.alarma_id}...");
                        var respuesta = await ApiService.ObtenerSolicitudCierreActiva(
                            _viewModel.Alarma.alarma_id,
                            App.persona.user_id_thirdparty);

                        if (respuesta?.IsSuccess == true && respuesta.Data != null)
                        {
                            tieneVotacion = true;
                            _viewModel.Alarma.TieneVotacionActiva = true;
                            System.Diagnostics.Debug.WriteLine($"[DescribirAlarmaPage] API confirmó votación activa para alarma {_viewModel.Alarma.alarma_id}");

                            // Propagar al caché global
                            ActualizarVotacionEnCache(_viewModel.Alarma.alarma_id);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[DescribirAlarmaPage] Error verificando votación: {ex.Message}");
                        CrashlyticsHelper.LogError(ex, "DescribirAlarmaPage", "VerificarVotacionActiva");
                    }
                }

                if (tieneVotacion)
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"[DescribirAlarmaPage] Propietario + votación activa -> redirigiendo a VerHistorialAlarmaPage");
                        var historialPage = new VerHistorialAlarmaPage(_viewModel.Alarma);
                        Navigation.InsertPageBefore(historialPage, this);
                        await Navigation.PopAsync(animated: false);
                    }
                    catch (Exception ex)
                    {
                        CrashlyticsHelper.LogError(ex, "DescribirAlarmaPage", "RedirectVotacion");
                        System.Diagnostics.Debug.WriteLine($"[DescribirAlarmaPage] Redirect error: {ex.Message}");
                    }
                    return;
                }
            }
        }

        /// <summary>
        /// Actualiza TieneVotacionActiva=true en los cachés globales.
        /// </summary>
        private static void ActualizarVotacionEnCache(long alarmaId)
        {
            try
            {
                var enCache = App.AlarmasCacheadas?.FirstOrDefault(a => a.alarma_id == alarmaId);
                if (enCache != null) enCache.TieneVotacionActiva = true;

                var enCacheParaTi = App.AlarmasCacheadasParaTi?.FirstOrDefault(a => a.alarma_id == alarmaId);
                if (enCacheParaTi != null) enCacheParaTi.TieneVotacionActiva = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DescribirAlarmaPage] Error actualizando caché: {ex.Message}");
            }
        }

        void Abrir_MapaClicked(System.Object sender, System.EventArgs e)
        {
            Application.Current.MainPage.ShowPopupAsync(new MinimapaPopUp(BindingContext));
        }

        private async void OnTipoAlarmaTapped(object sender, EventArgs e)
        {
            // Verificar si el picker está habilitado
            if (!_viewModel.PickerEnabled)
                return;

            if (_isSelectorOpen || !_viewModel.IsInitialized)
                return;

            // Usar lista filtrada si existe, sino usar la completa (para compatibilidad futura)
            var tiposParaMostrar = _viewModel.TiposAlarmaFiltradosParaCambio ?? _viewModel.TiposAlarma;

            // Si hay lista filtrada, indicar al popup que use exactamente esa lista
            bool usarListaExacta = _viewModel.TiposAlarmaFiltradosParaCambio != null;

            if (tiposParaMostrar == null || !tiposParaMostrar.Any())
                return;

            try
            {
                _isSelectorOpen = true;

                var pickerPopup = new TipoAlarmaPickerPopup(tiposParaMostrar, _viewModel.TipoAlarmaSeleccionado, usarListaExacta);

                pickerPopup.TipoAlarmaSelected += (s, tipoSeleccionado) =>
                {
                    _viewModel.TipoAlarmaSeleccionado = tipoSeleccionado;
                };

                await Application.Current.MainPage.ShowPopupAsync(pickerPopup);
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "DescribirAlarmaPage", "OnTipoAlarmaTapped");
                System.Diagnostics.Debug.WriteLine($"Error mostrando selector: {ex.Message}");
            }
            finally
            {
                _isSelectorOpen = false;
            }
        }

        private async void OnChatClicked(object sender, EventArgs e)
        {
            try
            {
                if (_viewModel?.Alarma != null)
                {
                    System.Diagnostics.Debug.WriteLine("Navegando al chat de la alarma...");

                    // VOTACIÓN DE CIERRE:
                    // - Propietario → VerHistorialAlarmaPage (solo lectura)
                    // - Otros       → CierreEncuestaPage (para votar)
                    if (_viewModel.Alarma.TieneVotacionActiva)
                    {
                        if (_viewModel.Alarma.flag_propietario_alarma)
                            await Navigation.PushAsync(new VerHistorialAlarmaPage(_viewModel.Alarma));
                        else
                            await Navigation.PushAsync(new CierreEncuestaPage(_viewModel.Alarma));
                        return;
                    }

                    await Navigation.PushAsync(new DetalleDescripcionAlarmaPage(_viewModel.Alarma));
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No hay alarma disponible para mostrar el chat");

                    var LabelInformacion = TranslateExtension.Translate("LabelInformacion");
                    var LabelSinAccesoAChat = TranslateExtension.Translate("LabelSinAccesoAChat");

                    await ModernAlerts.ShowWarning(LabelInformacion, LabelSinAccesoAChat);
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "DescribirAlarmaPage", "OnChatClicked");
                System.Diagnostics.Debug.WriteLine($"Error al navegar al chat: {ex.Message}");
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
                    System.Diagnostics.Debug.WriteLine("DescribirAlarmaPage: Abriendo CameraPopup para foto");

                    var cameraPopup = new CameraPopup();

                    cameraPopup.PhotoCaptured += async (s, photoPath) =>
                    {
                        System.Diagnostics.Debug.WriteLine($"DescribirAlarmaPage: Foto capturada: {photoPath}");

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
                    System.Diagnostics.Debug.WriteLine("DescribirAlarmaPage: iOS - usando MediaPicker.CaptureVideoAsync para video");

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
                            System.Diagnostics.Debug.WriteLine($"DescribirAlarmaPage: Video iOS copiado a caché: {videoPath}");
                            await MediaGalleryHelper.SaveToGalleryAsync(videoPath, isVideo: true);
                            await _viewModel.ProcessMediaFileFromPath(videoPath, true);
                        });
                    }
#else
                    // Android: usar VideoCameraPopup con cámara integrada
                    System.Diagnostics.Debug.WriteLine("DescribirAlarmaPage: Abriendo VideoCameraPopup para video");

                    var videoCameraPopup = new VideoCameraPopup();

                    videoCameraPopup.VideoCaptured += async (s, videoPath) =>
                    {
                        System.Diagnostics.Debug.WriteLine($"DescribirAlarmaPage: Video capturado: {videoPath}");

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
                            // isFromGallery=true activa corrección de orientación EXIF en el ViewModel.
                            var cacheDir = FileSystem.CacheDirectory;
                            var ext = Path.GetExtension(photo.FileName);
                            if (string.IsNullOrEmpty(ext)) ext = ".jpg";
                            var cachedPath = Path.Combine(cacheDir, $"{Guid.NewGuid()}{ext}");
                            using (var source = await photo.OpenReadAsync())
                            using (var dest = File.Create(cachedPath))
                            {
                                await source.CopyToAsync(dest);
                            }
                            System.Diagnostics.Debug.WriteLine($"DescribirAlarmaPage: Foto galería copiada a caché: {cachedPath}");
                            await _viewModel.ProcessMediaFileFromPath(cachedPath, true);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "DescribirAlarmaPage", "OnCameraButtonTapped");
                System.Diagnostics.Debug.WriteLine($"DescribirAlarmaPage ERROR: {ex.Message}");
            }
        }
    }
}

