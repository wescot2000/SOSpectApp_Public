using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using sospect.Helpers;
using sospect.Models;
using sospect.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace sospect.ViewModels
{
    public class BaseCierreAlarmaViewModel : BaseViewModel
    {
        protected const int MAX_MEDIA_FILES = 5;
        protected const int MaxImageWidth = 1920;
        protected const int JpegQuality = 75;

        protected AlarmaCercana AlarmaSeleccionada { get; set; }

        private string _descripcionCierre;
        public string DescripcionCierre
        {
            get => _descripcionCierre;
            set => SetValue(ref _descripcionCierre, value);
        }

        private ObservableCollection<MediaFile> _mediaFiles = new ObservableCollection<MediaFile>();
        public ObservableCollection<MediaFile> MediaFiles
        {
            get => _mediaFiles;
            set => SetValue(ref _mediaFiles, value);
        }

        private bool _hasMediaFiles;
        public bool HasMediaFiles
        {
            get => _hasMediaFiles;
            set => SetValue(ref _hasMediaFiles, value);
        }

        public ICommand AddPhotoCommand { get; }
        public ICommand AddGalleryCommand { get; }
        public ICommand AddVideoCommand { get; }
        public ICommand RemoveMediaCommand { get; }
        public ICommand CancelarCommand { get; }

        public BaseCierreAlarmaViewModel(AlarmaCercana alarmaCercana)
        {
            AlarmaSeleccionada = alarmaCercana;

            AddPhotoCommand = new Command(async () => await AddPhoto());
            AddGalleryCommand = new Command(async () => await AddGallery());
            AddVideoCommand = new Command(async () => await AddVideo());
            RemoveMediaCommand = new Command<MediaFile>(RemoveMedia);
            CancelarCommand = new Command(async () => await Cancelar());
        }

        private async Task Cancelar()
        {
            try
            {
                var navigation = GetCurrentNavigation();
                if (navigation != null)
                {
                    await navigation.PopAsync();
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "BaseCierreAlarmaViewModel", "Cancelar");
            }
        }

        private async Task AddPhoto()
        {
            try
            {
                var cameraStatus = await Permissions.CheckStatusAsync<Permissions.Camera>();
                if (cameraStatus != PermissionStatus.Granted)
                {
                    cameraStatus = await Permissions.RequestAsync<Permissions.Camera>();
                }

                if (cameraStatus != PermissionStatus.Granted)
                {
                    var errorTitle = await TranslateExtension.TranslateAsync("LabelError");
                    var errorMsg = await TranslateExtension.TranslateAsync("LblPermisoCamaraNecesario");
                    await ModernAlerts.ShowError(errorTitle, errorMsg);
                    return;
                }

                if (!MediaPicker.Default.IsCaptureSupported)
                {
                    var errorTitle = await TranslateExtension.TranslateAsync("LabelError");
                    var errorMsg = await TranslateExtension.TranslateAsync("LblCamaraNoDisponible");
                    await ModernAlerts.ShowError(errorTitle, errorMsg);
                    return;
                }

                var titleCapture = await TranslateExtension.TranslateAsync("LblTomarFotoAlarma");
                var photo = await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions
                {
                    Title = titleCapture
                });

                if (photo != null)
                {
                    await ProcessMediaFile(photo, false);
                }
            }
            catch (FeatureNotSupportedException fnsEx)
            {
                CrashlyticsHelper.LogError(fnsEx, "BaseCierreAlarmaViewModel", "AddPhoto");
                var errorTitle = await TranslateExtension.TranslateAsync("LabelError");
                var errorMsg = await TranslateExtension.TranslateAsync("LblCamaraNoCompatible");
                await ModernAlerts.ShowError(errorTitle, errorMsg);
            }
            catch (PermissionException permEx)
            {
                CrashlyticsHelper.LogError(permEx, "BaseCierreAlarmaViewModel", "AddPhoto");
                var errorTitle = await TranslateExtension.TranslateAsync("LabelError");
                var errorMsg = await TranslateExtension.TranslateAsync("LblPermisoCamaraNecesario");
                await ModernAlerts.ShowError(errorTitle, errorMsg);
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "BaseCierreAlarmaViewModel", "AddPhoto");
                var errorTitle = await TranslateExtension.TranslateAsync("LabelError");
                var errorMsg = await TranslateExtension.TranslateAsync("LblErrorCapturarFoto");
                await ModernAlerts.ShowError(errorTitle, errorMsg);
            }
        }

        private async Task AddGallery()
        {
            try
            {
                var titlePick = await TranslateExtension.TranslateAsync("LblSeleccionarFotoAlarma");
                var photo = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
                {
                    Title = titlePick
                });

                if (photo != null)
                {
                    await ProcessMediaFile(photo, false);
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "BaseCierreAlarmaViewModel", "AddGallery");
                var errorTitle = await TranslateExtension.TranslateAsync("LabelError");
                var errorMsg = await TranslateExtension.TranslateAsync("LblErrorSeleccionarFoto");
                await ModernAlerts.ShowError(errorTitle, errorMsg);
            }
        }

        private async Task AddVideo()
        {
            try
            {
                var titlePick = await TranslateExtension.TranslateAsync("LblSeleccionarVideoAlarma");
                var video = await MediaPicker.Default.PickVideoAsync(new MediaPickerOptions
                {
                    Title = titlePick
                });

                if (video != null)
                {
                    await ProcessMediaFile(video, true);
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "BaseCierreAlarmaViewModel", "AddVideo");
                var errorTitle = await TranslateExtension.TranslateAsync("LabelError");
                var errorMsg = await TranslateExtension.TranslateAsync("LblErrorSeleccionarVideo");
                await ModernAlerts.ShowError(errorTitle, errorMsg);
            }
        }

        public async Task ProcessMediaFile(FileResult media, bool isVideo)
        {
            if (media == null)
                return;

            if (MediaFiles.Count >= MAX_MEDIA_FILES)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var warningTitle = await TranslateExtension.TranslateAsync("LblLimiteAlcanzado");
                    var warningMsg = await TranslateExtension.TranslateAsync("LblSoloPuedesAgregar");
                    await ModernAlerts.ShowWarning(warningTitle, $"{warningMsg} {MAX_MEDIA_FILES} {await TranslateExtension.TranslateAsync("LblFotosVideosPorAlarma")}");
                });
                return;
            }

            try
            {
                var cacheDir = FileSystem.CacheDirectory;
                if (!Directory.Exists(cacheDir))
                {
                    Directory.CreateDirectory(cacheDir);
                }

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(media.FileName)}";
                var newFile = Path.Combine(cacheDir, fileName);

                if (!isVideo)
                {
                    // Copiar archivo original a ubicación temporal
                    var tempOriginal = Path.Combine(cacheDir, $"temp_{Guid.NewGuid()}{Path.GetExtension(media.FileName)}");
                    using (var sourceStream = await media.OpenReadAsync())
                    using (var tempStream = File.Create(tempOriginal))
                    {
                        await sourceStream.CopyToAsync(tempStream);
                    }

                    // Procesar con corrección de orientación EXIF
                    var result = await ImageHelper.CompressAndFixOrientationAsync(
                        tempOriginal,
                        newFile,
                        MaxImageWidth,
                        JpegQuality / 100f);

                    // Eliminar archivo temporal
                    try
                    {
                        if (File.Exists(tempOriginal))
                            File.Delete(tempOriginal);
                    }
                    catch { }

                    if (string.IsNullOrEmpty(result))
                    {
                        // Fallback: copiar sin procesar
                        using (var sourceStream = await media.OpenReadAsync())
                        using (var destStream = File.Create(newFile))
                        {
                            await sourceStream.CopyToAsync(destStream);
                        }
                    }
                }
                else
                {
                    // Para videos, guardar sin modificar
                    using (var stream = await media.OpenReadAsync())
                    using (var newStream = File.Create(newFile))
                    {
                        await stream.CopyToAsync(newStream);
                        await newStream.FlushAsync();
                    }
                }

                if (!File.Exists(newFile))
                {
                    var errorMsg = await TranslateExtension.TranslateAsync("LblArchivoNoSeGuardo");
                    throw new Exception(errorMsg);
                }

                var fileInfo = new FileInfo(newFile);

                const long maxFileSize = 100 * 1024 * 1024; // 100MB
                if (fileInfo.Length > maxFileSize)
                {
                    File.Delete(newFile);

                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        var warningTitle = await TranslateExtension.TranslateAsync("LblArchivoMuyGrande");
                        var warningMsg = await TranslateExtension.TranslateAsync("LblTamanoMaximo100MB");
                        await ModernAlerts.ShowWarning(warningTitle, warningMsg);
                    });
                    return;
                }

                var mediaFile = new MediaFile
                {
                    FilePath = newFile,
                    ThumbnailPath = newFile,
                    IsVideo = isVideo,
                    FileSizeBytes = fileInfo.Length
                };

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    MediaFiles.Add(mediaFile);
                    HasMediaFiles = MediaFiles.Any();
                });
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "BaseCierreAlarmaViewModel", "ProcessMediaFile");

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var errorTitle = await TranslateExtension.TranslateAsync("LabelError");
                    var errorMsg = await TranslateExtension.TranslateAsync("LblErrorProcesarArchivo");
                    await ModernAlerts.ShowError(errorTitle, errorMsg);
                });
            }
        }

        public async Task ProcessMediaFileFromPath(string filePath, bool isVideo)
        {
            System.Diagnostics.Debug.WriteLine($"BaseCierre ProcessMediaFileFromPath: STARTED - {filePath}");

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                System.Diagnostics.Debug.WriteLine($"BaseCierre ProcessMediaFileFromPath: File does not exist or empty path");
                return;
            }

            if (MediaFiles.Count >= MAX_MEDIA_FILES)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var warningTitle = await TranslateExtension.TranslateAsync("LblLimiteAlcanzado");
                    var warningMsg = await TranslateExtension.TranslateAsync("LblSoloPuedesAgregar");
                    await ModernAlerts.ShowWarning(warningTitle,
                        $"{warningMsg} {MAX_MEDIA_FILES} {await TranslateExtension.TranslateAsync("LblFotosVideosPorAlarma")}");
                });
                return;
            }

            try
            {
                var fileInfo = new FileInfo(filePath);
                System.Diagnostics.Debug.WriteLine($"BaseCierre ProcessMediaFileFromPath: File size: {fileInfo.Length} bytes");

                const long maxFileSize = 100 * 1024 * 1024; // 100MB
                if (fileInfo.Length > maxFileSize)
                {
                    File.Delete(filePath);
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        var warningTitle = await TranslateExtension.TranslateAsync("LblArchivoMuyGrande");
                        var warningMsg = await TranslateExtension.TranslateAsync("LblTamanoMaximo100MB");
                        await ModernAlerts.ShowWarning(warningTitle, warningMsg);
                    });
                    return;
                }

                // En iOS los videos necesitan un thumbnail JPEG generado del primer frame
                var thumbnailPath = isVideo
                    ? await VideoHelper.GenerateVideoThumbnailAsync(filePath)
                    : filePath;

                System.Diagnostics.Debug.WriteLine($"BaseCierre ProcessMediaFileFromPath: thumbnailPath={thumbnailPath}");

                var mediaFile = new MediaFile
                {
                    FilePath = filePath,
                    ThumbnailPath = thumbnailPath,
                    IsVideo = isVideo,
                    FileSizeBytes = fileInfo.Length
                };

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    MediaFiles.Add(mediaFile);
                    HasMediaFiles = MediaFiles.Any();
                    System.Diagnostics.Debug.WriteLine($"BaseCierre ProcessMediaFileFromPath: File added. Total: {MediaFiles.Count}");
                });
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "BaseCierreAlarmaViewModel", "ProcessMediaFileFromPath");
                System.Diagnostics.Debug.WriteLine($"BaseCierre ProcessMediaFileFromPath ERROR: {ex.Message}");

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var errorTitle = await TranslateExtension.TranslateAsync("LabelError");
                    var errorMsg = await TranslateExtension.TranslateAsync("LblErrorProcesarArchivo");
                    await ModernAlerts.ShowError(errorTitle, errorMsg);
                });
            }
        }

        private void RemoveMedia(MediaFile mediaFile)
        {
            if (mediaFile != null && MediaFiles.Contains(mediaFile))
            {
                try
                {
                    if (File.Exists(mediaFile.FilePath))
                    {
                        File.Delete(mediaFile.FilePath);
                    }

                    MediaFiles.Remove(mediaFile);
                    HasMediaFiles = MediaFiles.Any();
                }
                catch (Exception ex)
                {
                    CrashlyticsHelper.LogError(ex, "BaseCierreAlarmaViewModel", "RemoveMedia");
                }
            }
        }

        protected async Task<List<FotoAlarmaDto>> ConvertMediaFilesToDtos()
        {
            var fotos = new List<FotoAlarmaDto>();
            int orden = 1;

            foreach (var mediaFile in MediaFiles)
            {
                try
                {
                    byte[] bytes = File.ReadAllBytes(mediaFile.FilePath);
                    string base64 = Convert.ToBase64String(bytes);

                    string? thumbnailBase64 = null;
                    if (mediaFile.IsVideo)
                        thumbnailBase64 = await VideoHelper.GenerateVideoThumbnailBase64Async(mediaFile.FilePath);

                    var fotoDto = new FotoAlarmaDto
                    {
                        Base64Data = base64,
                        NombreArchivoOriginal = Path.GetFileName(mediaFile.FilePath),
                        TipoMime = mediaFile.IsVideo
                            ? VideoHelper.GetVideoMimeType(mediaFile.FilePath)
                            : "image/jpeg",
                        TamanoBytes = mediaFile.FileSizeBytes,
                        EsVideo = mediaFile.IsVideo,
                        Orden = orden++,
                        ThumbnailBase64 = thumbnailBase64
                    };

                    fotos.Add(fotoDto);
                }
                catch (Exception ex)
                {
                    CrashlyticsHelper.LogError(ex, "BaseCierreAlarmaViewModel", "ConvertMediaFilesToDtos");
                }
            }

            return fotos;
        }

        protected void CleanupMediaFiles()
        {
            if (MediaFiles != null && MediaFiles.Any())
            {
                foreach (var mediaFile in MediaFiles.ToList())
                {
                    try
                    {
                        if (File.Exists(mediaFile.FilePath))
                        {
                            File.Delete(mediaFile.FilePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error limpiando archivo temporal: {ex.Message}");
                    }
                }
                MediaFiles.Clear();
                HasMediaFiles = false;
            }
        }

        protected INavigation GetCurrentNavigation()
        {
            try
            {
                var currentPage = Application.Current?.MainPage;

                if (currentPage is NavigationPage navPage)
                {
                    return navPage.Navigation;
                }

                if (currentPage is TabbedPage tabbedPage)
                {
                    if (tabbedPage.CurrentPage is NavigationPage tabNavPage)
                    {
                        return tabNavPage.Navigation;
                    }
                    return tabbedPage.Navigation;
                }

                return currentPage?.Navigation;
            }
            catch
            {
                return null;
            }
        }
    }
}
