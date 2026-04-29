using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Media;
using sospect.Helpers;
using sospect.Models;
using sospect.ViewModels;
using sospect.Views.Popups;
using System;

namespace sospect.Views
{
    [Microsoft.Maui.Controls.Xaml.XamlCompilation(Microsoft.Maui.Controls.Xaml.XamlCompilationOptions.Compile)]
#if ANDROID
    [Android.Runtime.Preserve(AllMembers = true)]
#elif IOS || MACCATALYST
    [Foundation.Preserve(AllMembers = true)]
#endif
    public partial class CierreEncuestaPage : ContentPage
    {
        private CierreEncuestaViewModel _viewModel;

        public CierreEncuestaPage(AlarmaCercana alarma)
        {
            InitializeComponent();
            _viewModel = new CierreEncuestaViewModel(alarma, Navigation);
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            try
            {
                await _viewModel.CargarSolicitudActiva();
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "CierreEncuestaPage", "OnAppearing");
                System.Diagnostics.Debug.WriteLine($"CierreEncuestaPage.OnAppearing ERROR: {ex.Message}");
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _viewModel?.DetenerTimer();
        }

        private async void OnCameraButtonTapped(object sender, EventArgs e)
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
                    // Tomar foto con CameraPopup (camara propia, no del sistema)
                    System.Diagnostics.Debug.WriteLine("CierreEncuestaPage: Abriendo CameraPopup para foto");

                    var cameraPopup = new CameraPopup();

                    cameraPopup.PhotoCaptured += async (s, photoPath) =>
                    {
                        System.Diagnostics.Debug.WriteLine($"CierreEncuestaPage: Foto capturada: {photoPath}");

                        await MediaGalleryHelper.SaveToGalleryAsync(photoPath, isVideo: false);
                        await _viewModel.ProcessMediaFileFromPath(photoPath, false);
                    };

                    await Application.Current.MainPage.ShowPopupAsync(cameraPopup);
                }
                else if (result == ThreeOptionResult.Option2)
                {
#if IOS || MACCATALYST
                    // iOS: Camera.MAUI no soporta grabación de video confiablemente.
                    // Usar MediaPicker.CaptureVideoAsync() (cámara nativa del sistema).
                    System.Diagnostics.Debug.WriteLine("CierreEncuestaPage: iOS - usando MediaPicker.CaptureVideoAsync para video");

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
                            using (var dest = File.Create(videoPath))
                            {
                                await source.CopyToAsync(dest);
                            }
                            System.Diagnostics.Debug.WriteLine($"CierreEncuestaPage: Video iOS copiado a caché: {videoPath}");
                            await MediaGalleryHelper.SaveToGalleryAsync(videoPath, isVideo: true);
                            await _viewModel.ProcessMediaFileFromPath(videoPath, true);
                        });
                    }
#else
                    // Android: usar VideoCameraPopup con cámara integrada
                    System.Diagnostics.Debug.WriteLine("CierreEncuestaPage: Abriendo VideoCameraPopup para video");

                    var videoCameraPopup = new VideoCameraPopup();

                    videoCameraPopup.VideoCaptured += async (s, videoPath) =>
                    {
                        System.Diagnostics.Debug.WriteLine($"CierreEncuestaPage: Video capturado: {videoPath}");

                        await MediaGalleryHelper.SaveToGalleryAsync(videoPath, isVideo: true);
                        await _viewModel.ProcessMediaFileFromPath(videoPath, true);
                    };

                    await Application.Current.MainPage.ShowPopupAsync(videoCameraPopup);
#endif
                }
                else if (result == ThreeOptionResult.Option3)
                {
                    // Seleccionar de galeria
                    var photo = await MediaPicker.PickPhotoAsync();

                    if (photo != null)
                    {
                        var processingText = await TranslateExtension.TranslateAsync("LblProcesandoFoto");
                        await ModernAlerts.ShowProcessingAsync(processingText, async () =>
                        {
                            // Fotos de galeria necesitan compresion, usar ProcessMediaFile (FileResult)
                            await _viewModel.ProcessMediaFile(photo, false);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "CierreEncuestaPage", "OnCameraButtonTapped");
                System.Diagnostics.Debug.WriteLine($"CierreEncuestaPage OnCameraButtonTapped ERROR: {ex.Message}");
            }
        }
    }
}
