// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using sospect.Helpers;
using sospect.Models;
using sospect.ViewModels;
using sospect.Views.Popups;
using System;
using System.IO;

namespace sospect.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CierreMascotaPage : ContentPage
    {
        private CierreMascotaViewModel _viewModel;
        private bool _isSelectorOpen = false;

        public CierreMascotaPage(AlarmaCercana alarmaCercana)
        {
            InitializeComponent();
            _viewModel = new CierreMascotaViewModel(alarmaCercana);
            BindingContext = _viewModel;
        }

        private async void OnCameraButtonTapped(object sender, EventArgs e)
        {
            if (_isSelectorOpen)
                return;

            _isSelectorOpen = true;
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
                    // Tomar foto con CameraPopup (cámara propia, no se revienta al volver)
                    System.Diagnostics.Debug.WriteLine("CierreMascotaPage: Abriendo CameraPopup para foto");

                    var cameraPopup = new CameraPopup();

                    cameraPopup.PhotoCaptured += async (s, photoPath) =>
                    {
                        System.Diagnostics.Debug.WriteLine($"CierreMascotaPage: Foto capturada: {photoPath}");

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
                    System.Diagnostics.Debug.WriteLine("CierreMascotaPage: iOS - usando MediaPicker.CaptureVideoAsync para video");

                    var videoResult = await MediaPicker.CaptureVideoAsync();
                    if (videoResult != null)
                    {
                        var processingText = await TranslateExtension.TranslateAsync("LblProcesandoVideo");
                        await ModernAlerts.ShowProcessingAsync(processingText, async () =>
                        {
                            var cacheDir = FileSystem.CacheDirectory;
                            var ext = System.IO.Path.GetExtension(videoResult.FileName)?.ToLowerInvariant();
                            if (string.IsNullOrEmpty(ext) || ext == ".") ext = ".mp4";
                            var videoPath = System.IO.Path.Combine(cacheDir, $"{Guid.NewGuid()}{ext}");
                            using (var source = await videoResult.OpenReadAsync())
                            using (var dest = File.Create(videoPath))
                            {
                                await source.CopyToAsync(dest);
                            }
                            System.Diagnostics.Debug.WriteLine($"CierreMascotaPage: Video iOS copiado a caché: {videoPath}");
                            await MediaGalleryHelper.SaveToGalleryAsync(videoPath, isVideo: true);
                            await _viewModel.ProcessMediaFileFromPath(videoPath, true);
                        });
                    }
#else
                    // Android: usar VideoCameraPopup con cámara integrada
                    System.Diagnostics.Debug.WriteLine("CierreMascotaPage: Abriendo VideoCameraPopup para video");

                    var videoCameraPopup = new VideoCameraPopup();

                    videoCameraPopup.VideoCaptured += async (s, videoPath) =>
                    {
                        System.Diagnostics.Debug.WriteLine($"CierreMascotaPage: Video capturado: {videoPath}");

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
                            // que puede ser limpiada antes de que ProcessMediaFileFromPath la acceda.
                            var cacheDir = FileSystem.CacheDirectory;
                            var ext = System.IO.Path.GetExtension(photo.FileName);
                            if (string.IsNullOrEmpty(ext)) ext = ".jpg";
                            var cachedPath = System.IO.Path.Combine(cacheDir, $"{Guid.NewGuid()}{ext}");
                            using (var source = await photo.OpenReadAsync())
                            using (var dest = File.Create(cachedPath))
                            {
                                await source.CopyToAsync(dest);
                            }
                            System.Diagnostics.Debug.WriteLine($"CierreMascotaPage: Foto galería copiada a caché: {cachedPath}");
                            await _viewModel.ProcessMediaFileFromPath(cachedPath, false);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "CierreMascotaPage", "OnCameraButtonTapped");
                System.Diagnostics.Debug.WriteLine($"CierreMascotaPage OnCameraButtonTapped ERROR: {ex.Message}");
            }
            finally
            {
                _isSelectorOpen = false;
            }
        }
    }
}


