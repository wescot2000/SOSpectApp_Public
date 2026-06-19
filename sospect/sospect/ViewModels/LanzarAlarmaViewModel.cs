// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
// using GalaSoft.MvvmLight.Command; // TODO: replace with CommunityToolkit MVVM
using Newtonsoft.Json;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Maui.Extensions;
// using Sharpnado.MaterialFrame; // TODO: remove or replace
using sospect.Extensions;
using sospect.Helpers;
using sospect.Interfaces;
using sospect.Models;
using sospect.Services;
using sospect.Utils;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Controls;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Platform;

namespace sospect.ViewModels
{
    public class LanzarAlarmaViewModel : BaseViewModel
    {

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private const int MAX_MEDIA_FILES = 5;
        private const int MaxImageWidth = 1024;   // Ancho máximo reducido para ~200KB
        private const int JpegQuality = 60;       // Calidad JPEG reducida (0-100)

        private string _descripcionAlarma;
        public string DescripcionAlarma
        {
            get => _descripcionAlarma;
            set => SetValue(ref _descripcionAlarma, value);
        }

        private ObservableCollection<TipoAlarma> _TiposAlarma;
        public ObservableCollection<TipoAlarma> TiposAlarma
        {
            get => this._TiposAlarma;
            set => this.SetValue(ref this._TiposAlarma, value);
        }

        private TipoAlarma _TipoAlarmaSeleccionado;
        public TipoAlarma TipoAlarmaSeleccionado
        {
            get => this._TipoAlarmaSeleccionado;
            set => this.SetValue(ref this._TipoAlarmaSeleccionado, value);
        }

        private double _latitude;
        public double Latitude
        {
            get => this._latitude;
            set => this.SetValue(ref this._latitude, value);
        }

        private bool _isInitialized = false;
        public bool IsInitialized
        {
            get => _isInitialized;
            set
            {
                this.SetValue(ref _isInitialized, value);
                OnPropertyChanged(nameof(CanSaveAlarm));
            }
        }

        private double _longitude;
        public double Longitude
        {
            get => this._longitude;
            set => this.SetValue(ref this._longitude, value);
        }

        private string _thirdPartyId;
        public string ThirdPartyId
        {
            get => this._thirdPartyId;
            set => this.SetValue(ref this._thirdPartyId, value);
        }

        private bool _IsTimeRunning;
        public bool IsTimeRunning
        {
            get => this._IsTimeRunning;
            set => this.SetValue(ref this._IsTimeRunning, value);
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

        private bool _isSavingAlarm;
        public bool IsSavingAlarm
        {
            get => _isSavingAlarm;
            set
            {
                SetValue(ref _isSavingAlarm, value);
                OnPropertyChanged(nameof(CanSaveAlarm));
            }
        }

        public bool CanSaveAlarm => IsInitialized && !IsSavingAlarm;

        private bool _compartirEnX;
        public bool CompartirEnX
        {
            get => _compartirEnX;
            set { SetValue(ref _compartirEnX, value); Preferences.Set("compartir_en_x", value); }
        }

        private bool _compartirEnFacebook;
        public bool CompartirEnFacebook
        {
            get => _compartirEnFacebook;
            set { SetValue(ref _compartirEnFacebook, value); Preferences.Set("compartir_en_facebook", value); }
        }

        // ─── Ruta de escape (opcional, solo cuando TipoAlarma == 2 — crimen cometido) ───
        private bool _mostrarBotonPuntoHuida;
        public bool MostrarBotonPuntoHuida
        {
            get => _mostrarBotonPuntoHuida;
            set => SetValue(ref _mostrarBotonPuntoHuida, value);
        }

        private double? _puntoHuidaLatitud;
        public double? PuntoHuidaLatitud
        {
            get => _puntoHuidaLatitud;
            set => SetValue(ref _puntoHuidaLatitud, value);
        }

        private double? _puntoHuidaLongitud;
        public double? PuntoHuidaLongitud
        {
            get => _puntoHuidaLongitud;
            set => SetValue(ref _puntoHuidaLongitud, value);
        }

        // true cuando el usuario ya marcó el punto en el minimapa
        private bool _puntoHuidaMarcado = false;

        public ICommand CancelarMinimapaCommand { get; }
        public ICommand HechoDireccionCommand { get; }

        public ICommand AddPhotoCommand { get; }
        public ICommand AddVideoCommand { get; }
        public ICommand RemoveMediaCommand { get; }

        public ICommand AddPhotoFromGalleryCommand { get; }

        public LanzarAlarmaViewModel(string thirdPartyId, double latitude, double longitude, int tipoAlarma = 1)
        {
            Latitude = latitude.Trim(6);
            Longitude = longitude.Trim(6);
            ThirdPartyId = thirdPartyId;
            IsTimeRunning = true;
            RegistrarAlarmaCommand = new Command(async () => await RegistrarAlarma());
            // Cargar preferencia de compartir (pre-checked por defecto)
            _compartirEnX = Preferences.Get("compartir_en_x", true);
            _compartirEnFacebook = Preferences.Get("compartir_en_facebook", true);

            _ = CargarTiposAlarma(tipoAlarma);
            AddPhotoCommand = new Command(async () => await AddPhoto());
            AddPhotoFromGalleryCommand = new Command(async () => await AddPhotoFromGallery());
            AddVideoCommand = new Command(async () => await AddVideo());
            RemoveMediaCommand = new Command<MediaFile>(RemoveMedia);

            // Comandos del minimapa de ruta de escape
            CancelarMinimapaCommand = new Command(async () =>
            {
                try
                {
                    await Application.Current.MainPage.ClosePopupAsync();
                }
                catch (Exception ex)
                {
                    CrashlyticsHelper.LogError(ex, "LanzarAlarmaViewModel", "CancelarMinimapaCommand");
                }
            });

            HechoDireccionCommand = new Command(async (mapa) =>
            {
                try
                {
                    if (mapa is sospect.CustomRenderers.MiniMapa mapaDireccionHuida
                        && mapaDireccionHuida.CurrentMapPosition != null)
                    {
                        PuntoHuidaLatitud  = mapaDireccionHuida.CurrentMapPosition.Latitude;
                        PuntoHuidaLongitud = mapaDireccionHuida.CurrentMapPosition.Longitude;
                        _puntoHuidaMarcado = true;
                        System.Diagnostics.Debug.WriteLine($"[LanzarAlarmaVM] Punto de huida marcado — Lat={PuntoHuidaLatitud}, Lon={PuntoHuidaLongitud}");
                        await Application.Current.MainPage.ClosePopupAsync();
                    }
                    else
                    {
                        var infor        = TranslateExtension.Translate("LabelInformacion");
                        var selecciona   = TranslateExtension.Translate("SeleccionaRuta");
                        await ModernAlerts.ShowInfo(infor, selecciona);
                    }
                }
                catch (Exception ex)
                {
                    CrashlyticsHelper.LogError(ex, "LanzarAlarmaViewModel", "HechoDireccionCommand");
                }
            });

            // Actualizar visibilidad del botón cuando el tipo de alarma cambia
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(TipoAlarmaSeleccionado))
                {
                    // Solo en Android muestra tipo 2 (crimen cometido).
                    // iOS no tiene tipo 2 sin convenio; si alguna vez lo tiene, también aplica.
                    MostrarBotonPuntoHuida = TipoAlarmaSeleccionado?.TipoalarmaId == 2;
                }
            };
        }

        private const int MaxRetries = 5;

        private async Task AddPhotoFromGallery()
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
                CrashlyticsHelper.LogError(ex, "LanzarAlarmaViewModel", "AddPhotoFromGallery");
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
                CrashlyticsHelper.LogError(ex, "LanzarAlarmaViewModel", "AddVideo");
                var errorTitle = await TranslateExtension.TranslateAsync("LabelError");
                var errorMsg = await TranslateExtension.TranslateAsync("LblErrorSeleccionarVideo");
                await ModernAlerts.ShowError(errorTitle, errorMsg);
            }
        }

        private async Task ProcessMediaFile(FileResult media, bool isVideo)
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
                // CRÍTICO: Crear directorio de caché si no existe
                var cacheDir = FileSystem.CacheDirectory;
                if (!Directory.Exists(cacheDir))
                {
                    Directory.CreateDirectory(cacheDir);
                }

                // Generar nombre único para evitar colisiones
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(media.FileName)}";
                var newFile = Path.Combine(cacheDir, fileName);

                System.Diagnostics.Debug.WriteLine($"ProcessMediaFile: Guardando en {newFile}");

                // Guardar el archivo en cache local (con compresión y corrección de orientación para fotos)
                if (!isVideo)
                {
                    // Usar el helper para comprimir y corregir orientación EXIF
                    System.Diagnostics.Debug.WriteLine($"ProcessMediaFile: Procesando imagen con corrección de orientación EXIF");

                    // Primero copiar el archivo original a una ubicación temporal
                    var tempOriginal = Path.Combine(cacheDir, $"temp_{Guid.NewGuid()}{Path.GetExtension(media.FileName)}");
                    using (var sourceStream = await media.OpenReadAsync())
                    using (var tempStream = File.Create(tempOriginal))
                    {
                        await sourceStream.CopyToAsync(tempStream);
                    }

                    // Procesar con corrección de orientación
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
                        System.Diagnostics.Debug.WriteLine($"ProcessMediaFile: Fallback - copiando sin procesar");
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

                // Verificar que el archivo se guardó correctamente
                if (!File.Exists(newFile))
                {
                    var errorMsg = await TranslateExtension.TranslateAsync("LblArchivoNoSeGuardo");
                    throw new Exception(errorMsg);
                }

                var fileInfo = new FileInfo(newFile);
                System.Diagnostics.Debug.WriteLine($"ProcessMediaFile: Archivo guardado - Tamaño final: {fileInfo.Length / 1024}KB");

                const long maxFileSize = 100 * 1024 * 1024; // 100MB en bytes
                if (fileInfo.Length > maxFileSize)
                {
                    // Eliminar archivo que excede el límite
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

                // IMPORTANTE: Agregar en el hilo principal
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    MediaFiles.Add(mediaFile);
                    HasMediaFiles = MediaFiles.Any();
                    System.Diagnostics.Debug.WriteLine($"ProcessMediaFile: Archivo agregado a MediaFiles. Total: {MediaFiles.Count}");
                });
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "LanzarAlarmaViewModel", "ProcessMediaFile");
                System.Diagnostics.Debug.WriteLine($"ProcessMediaFile ERROR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"ProcessMediaFile STACK: {ex.StackTrace}");

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var errorTitle = await TranslateExtension.TranslateAsync("LabelError");
                    var errorMsg = await TranslateExtension.TranslateAsync("LblErrorProcesarArchivo");
                    await ModernAlerts.ShowError(errorTitle, errorMsg);
                });
            }
        }

        private async Task AddPhoto()
        {
            System.Diagnostics.Debug.WriteLine("AddPhoto: INICIADO");

            try
            {
                // PRIMERO: Verificar permisos de cámara
                System.Diagnostics.Debug.WriteLine("AddPhoto: Verificando permisos de cámara");
                var cameraStatus = await Permissions.CheckStatusAsync<Permissions.Camera>();

                if (cameraStatus != PermissionStatus.Granted)
                {
                    System.Diagnostics.Debug.WriteLine("AddPhoto: Solicitando permisos de cámara");
                    cameraStatus = await Permissions.RequestAsync<Permissions.Camera>();
                }

                if (cameraStatus != PermissionStatus.Granted)
                {
                    System.Diagnostics.Debug.WriteLine("AddPhoto: Permisos de cámara DENEGADOS");
                    var errorTitle = await TranslateExtension.TranslateAsync("LabelError");
                    var errorMsg = await TranslateExtension.TranslateAsync("LblPermisoCamaraNecesario");
                    await ModernAlerts.ShowError(errorTitle, errorMsg);
                    return;
                }

                System.Diagnostics.Debug.WriteLine("AddPhoto: Permisos de cámara OK");

                // SEGUNDO: Verificar si la cámara está disponible
                if (!MediaPicker.Default.IsCaptureSupported)
                {
                    System.Diagnostics.Debug.WriteLine("AddPhoto: Cámara NO DISPONIBLE en este dispositivo");
                    var errorTitle = await TranslateExtension.TranslateAsync("LabelError");
                    var errorMsg = await TranslateExtension.TranslateAsync("LblCamaraNoDisponible");
                    await ModernAlerts.ShowError(errorTitle, errorMsg);
                    return;
                }

                System.Diagnostics.Debug.WriteLine("AddPhoto: Cámara disponible, capturando foto...");

                // TERCERO: Capturar la foto
                var titleCapture = await TranslateExtension.TranslateAsync("LblTomarFotoAlarma");
                var photo = await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions
                {
                    Title = titleCapture
                });

                if (photo != null)
                {
                    System.Diagnostics.Debug.WriteLine($"AddPhoto: Foto capturada - {photo.FileName}");
                    await ProcessMediaFile(photo, false);
                    System.Diagnostics.Debug.WriteLine("AddPhoto: ProcessMediaFile completado exitosamente");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("AddPhoto: Usuario canceló la captura de foto");
                }
            }
            catch (FeatureNotSupportedException fnsEx)
            {
                System.Diagnostics.Debug.WriteLine($"AddPhoto: FeatureNotSupportedException - {fnsEx.Message}");
                var errorTitle = await TranslateExtension.TranslateAsync("LabelError");
                var errorMsg = await TranslateExtension.TranslateAsync("LblCamaraNoCompatible");
                await ModernAlerts.ShowError(errorTitle, errorMsg);
                CrashlyticsHelper.LogError(fnsEx, "LanzarAlarmaViewModel", "AddPhoto");
            }
            catch (PermissionException permEx)
            {
                System.Diagnostics.Debug.WriteLine($"AddPhoto: PermissionException - {permEx.Message}");
                var errorTitle = await TranslateExtension.TranslateAsync("LabelError");
                var errorMsg = await TranslateExtension.TranslateAsync("LblPermisoCamaraNecesario");
                await ModernAlerts.ShowError(errorTitle, errorMsg);
                CrashlyticsHelper.LogError(permEx, "LanzarAlarmaViewModel", "AddPhoto");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AddPhoto: Exception - {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"AddPhoto: StackTrace - {ex.StackTrace}");
                CrashlyticsHelper.LogError(ex, "LanzarAlarmaViewModel", "AddPhoto");
                var errorTitle = await TranslateExtension.TranslateAsync("LabelError");
                var errorMsg = await TranslateExtension.TranslateAsync("LblErrorCapturarFoto");
                await ModernAlerts.ShowError(errorTitle, errorMsg);
            }
            finally
            {
                System.Diagnostics.Debug.WriteLine("AddPhoto: FINALIZADO");
            }
        }

        private void RemoveMedia(MediaFile mediaFile)
        {
            if (mediaFile != null && MediaFiles.Contains(mediaFile))
            {
                try
                {
                    // Eliminar archivo físico
                    if (File.Exists(mediaFile.FilePath))
                    {
                        File.Delete(mediaFile.FilePath);
                    }

                    MediaFiles.Remove(mediaFile);
                    HasMediaFiles = MediaFiles.Any();
                }
                catch (Exception ex)
                {
                    CrashlyticsHelper.LogError(ex, "LanzarAlarmaViewModel", "RemoveMedia");
                }
            }
        }

        private async Task CargarTiposAlarma(int tipoAlarmaId)
        {
            var LabelOK = await TranslateExtension.TranslateAsync("LabelOK");
            var LabelError = await TranslateExtension.TranslateAsync("LabelError");
            var LblHabilitaInternetReintenta = await TranslateExtension.TranslateAsync("LblHabilitaInternetReintenta");

            if (IsRunning)
            {
                return;
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                IsRunning = true;
            });

            List<TipoAlarma> tiposAlarma = null;

            for (int retry = 0; retry < MaxRetries; retry++)
            {
                try
                {
                    tiposAlarma = await ApiService.ObtenerTiposAlarmaCompletos();

                    if (tiposAlarma != null && tiposAlarma.Any())
                    {
                        // LOG DE VERIFICACIÓN
                        System.Diagnostics.Debug.WriteLine($"=== TIPOS ALARMA EN VIEWMODEL ===");
                        foreach (var tipo in tiposAlarma)
                        {
                            System.Diagnostics.Debug.WriteLine($"ID={tipo.TipoalarmaId}, Icono='{tipo.Icono ?? "NULL"}'");
                        }
                        System.Diagnostics.Debug.WriteLine($"=== FIN ===");

                        break;
                    }
                }
                catch (Exception ex)
                {
                    CrashlyticsHelper.LogError(ex, "LanzarAlarmaViewModel", "CargarTiposAlarma");

                    if (retry == MaxRetries - 1)
                    {
                        MainThread.BeginInvokeOnMainThread(async () =>
                        {
                            await ModernAlerts.ShowError(LabelError, LblHabilitaInternetReintenta);
                        });
                        IsRunning = false;
                        return;
                    }

                    await Task.Delay(2000);
                }
            }

            // CAMBIO: Validar tiposAlarma (no idsTiposAlarma)
            if (tiposAlarma == null || !tiposAlarma.Any())
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await ModernAlerts.ShowError(LabelError, LblHabilitaInternetReintenta);
                });
                IsRunning = false;
                return;
            }

            // ELIMINAR ESTAS LÍNEAS - Ya no son necesarias
            // var tiposAlarma = idsTiposAlarma.Select(id => new TipoAlarma { TipoalarmaId = id }).ToList();

            // Leer el flag_convenio desde las preferencias
            var parametrosGuardados = Preferences.Get("ParametrosUsuario", "");
            bool tieneConvenio = false;

            if (!string.IsNullOrEmpty(parametrosGuardados))
            {
                var parametros = JsonConvert.DeserializeObject<ParametrosUsuario>(parametrosGuardados);
                tieneConvenio = parametros.flag_convenio;
            }

            // Filtrar los tipos de alarma según la plataforma y el flag_convenio
            if (DeviceInfo.Platform == DevicePlatform.iOS && !tieneConvenio)
            {
                tiposAlarma = tiposAlarma.Where(x =>
                    x.TipoalarmaId != 2 && // Delito o crimen cometido
                    x.TipoalarmaId != 3 && // Riña callejera
                    x.TipoalarmaId != 6 && // Disturbios o protestas
                    x.TipoalarmaId != 8    // Violencia Intrafamiliar
                ).ToList();
            }

            TiposAlarma = new ObservableCollection<TipoAlarma>(tiposAlarma);

            if (TiposAlarma.Any())
            {
                TipoAlarmaSeleccionado = TiposAlarma.FirstOrDefault(x => x.TipoalarmaId == tipoAlarmaId);
            }

            IsInitialized = true;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                IsRunning = false;
            });
        }



        public async Task RegistrarAlarma()
        {
            if (!IsInitialized || IsRunning)
            {
                return;
            }

            MainThread.BeginInvokeOnMainThread(() => { IsRunning = true; });

            // Notificar que se está procesando (para el HomeViewModel)
            MessagingCenter.Send(this, "ModificarVariable", true);

            var AlarmaEnviada = await TranslateExtension.TranslateAsync("AlarmaEnviada");
            var MsgTrasAlarmaEnviada = await TranslateExtension.TranslateAsync("MsgTrasAlarmaEnviada");
            var LabelOK = await TranslateExtension.TranslateAsync("LabelOK");
            var LabelError = await TranslateExtension.TranslateAsync("LabelError");
            var LblSeleccioneTipoAlarma = await TranslateExtension.TranslateAsync("LblSeleccioneTipoAlarma");
            var LabelInformacion = await TranslateExtension.TranslateAsync("LabelInformacion");
            var MensajeError = await TranslateExtension.TranslateAsync("MensajeError");
            var MsgTrasAlarmaSinRiesgo = await TranslateExtension.TranslateAsync("MsgTrasAlarmaSinRiesgo");

            Alarma alarma = new Alarma()
            {
                p_tipoalarma_id = TipoAlarmaSeleccionado?.TipoalarmaId ?? 0,
                p_latitud = Latitude,
                p_longitud = Longitude,
                p_user_id_thirdparty = ThirdPartyId,
                p_alarma_id = null,
                ip_usuario = InternetUtil.GetPublicIpAddress(),
                idioma_dispositivo = IdiomUtil.ObtenerCodigoDeIdioma(),
                DescripcionInicial = DescripcionAlarma
            };

            if (TipoAlarmaSeleccionado == null || alarma.p_tipoalarma_id == 0)
            {
                await ModernAlerts.ShowError(LabelError, LblSeleccioneTipoAlarma);
                IsRunning = false;
                return;
            }

            // Activar indicador de carga INMEDIATAMENTE antes de procesar fotos
            IsSavingAlarm = true;

            try
            {
                // Subir archivos multimedia a S3 via URL pre-firmada y enviar solo los keys al API
                if (MediaFiles != null && MediaFiles.Any())
                {
                    alarma.Fotos = new List<FotoAlarmaDto>();
                    int orden = 1;

                    const int maxMediaRetries = 3;
                    foreach (var mediaFile in MediaFiles)
                    {
                        Exception lastMediaEx = null;
                        bool uploaded = false;

                        // Generar thumbnail antes del bucle de reintentos (operación local, no de red)
                        string? thumbnailBase64 = null;
                        if (mediaFile.IsVideo)
                        {
                            thumbnailBase64 = await VideoHelper.GenerateVideoThumbnailBase64Async(mediaFile.FilePath);
                            System.Diagnostics.Debug.WriteLine($"[RegistrarAlarma] Thumbnail generado={thumbnailBase64 != null}, video={Path.GetFileName(mediaFile.FilePath)}");
                        }

                        for (int mediaRetry = 0; mediaRetry < maxMediaRetries; mediaRetry++)
                        {
                            try
                            {
                                var mimeType = mediaFile.IsVideo ? VideoHelper.GetVideoMimeType(mediaFile.FilePath) : "image/jpeg";
                                var extension = Path.GetExtension(mediaFile.FilePath);
                                if (string.IsNullOrEmpty(extension))
                                    extension = mediaFile.IsVideo ? ".mp4" : ".jpg";

                                if (mediaRetry == 0)
                                    System.Diagnostics.Debug.WriteLine($"[RegistrarAlarma] Solicitando presigned URL para {Path.GetFileName(mediaFile.FilePath)} ({mediaFile.DisplaySize})");
                                else
                                    System.Diagnostics.Debug.WriteLine($"[RegistrarAlarma] Reintento {mediaRetry}/{maxMediaRetries - 1} para {Path.GetFileName(mediaFile.FilePath)}");

                                var presigned = await ApiService.SolicitarPresignedUrlAsync(mimeType, extension);
                                if (presigned == null)
                                    throw new Exception("No se pudo obtener URL de upload para el archivo multimedia");

                                System.Diagnostics.Debug.WriteLine($"[RegistrarAlarma] Subiendo a S3 directo: {presigned.S3Key}");
                                await ApiService.SubirArchivoAPresignedUrlAsync(presigned.PresignedUrl, mediaFile.FilePath, mimeType);
                                System.Diagnostics.Debug.WriteLine($"[RegistrarAlarma] Upload completado: {presigned.S3Key}");

                                var fotoDto = new FotoAlarmaDto
                                {
                                    S3Key = presigned.S3Key,
                                    NombreArchivoOriginal = Path.GetFileName(mediaFile.FilePath),
                                    TipoMime = mimeType,
                                    TamanoBytes = mediaFile.FileSizeBytes,
                                    EsVideo = mediaFile.IsVideo,
                                    Orden = orden++,
                                    ThumbnailBase64 = thumbnailBase64
                                };

                                alarma.Fotos.Add(fotoDto);
                                uploaded = true;
                                break;
                            }
                            catch (Exception exMedia)
                            {
                                lastMediaEx = exMedia;
                                System.Diagnostics.Debug.WriteLine($"[RegistrarAlarma] Error en intento {mediaRetry + 1} subiendo archivo: {exMedia.Message}");

                                if (mediaRetry < maxMediaRetries - 1)
                                    await Task.Delay(1500);
                            }
                        }

                        if (!uploaded)
                        {
                            CrashlyticsHelper.LogError(lastMediaEx, "LanzarAlarmaViewModel", "RegistrarAlarma_SubirMedia");
                            System.Diagnostics.Debug.WriteLine($"[RegistrarAlarma] Agotados {maxMediaRetries} intentos para {Path.GetFileName(mediaFile.FilePath)}");
                            throw lastMediaEx;
                        }
                    }

                    System.Diagnostics.Debug.WriteLine($"[RegistrarAlarma] Total archivos subidos a S3: {alarma.Fotos?.Count ?? 0}");
                }

                ResponseMessage response = await ApiService.InsertarAlarma(alarma);

                if (response.IsSuccess)
                {
                    System.Diagnostics.Debug.WriteLine("LanzarAlarmaViewModel: Alarma registrada exitosamente en el servidor");
                    // DIAGNÓSTICO - ALARMA REGISTRADA
                    System.Diagnostics.Debug.WriteLine($"[DIAG-LANZAR] ====== ALARMA REGISTRADA EXITOSAMENTE ======");

                    // NUEVO: Extraer el alarma_id de la respuesta
                    long? alarmaIdCreada = null;
                    if (!string.IsNullOrEmpty(response.Data) && long.TryParse(response.Data, out long parsedId))
                    {
                        alarmaIdCreada = parsedId;
                        System.Diagnostics.Debug.WriteLine($"LanzarAlarmaViewModel: alarma_id recibido del servidor: {alarmaIdCreada}");
                        System.Diagnostics.Debug.WriteLine($"[DIAG-LANZAR] alarma_id del servidor: {alarmaIdCreada}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"LanzarAlarmaViewModel: No se pudo extraer alarma_id de response.Data: '{response.Data}'");
                    }

                    // PASO 1: Cerrar el popup INMEDIATAMENTE
                    MessagingCenter.Send<LanzarAlarmaViewModel>(this, "CerrarPopup");

                    // PASO 2: Dar tiempo para que el popup se cierre
                    await Task.Delay(100);

                    // PASO 3: Mostrar mensaje de éxito
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        // Determinar el mensaje según el tipo de alarma
                        string mensajeAMostrar = (TipoAlarmaSeleccionado.TipoalarmaId == 4 || TipoAlarmaSeleccionado.TipoalarmaId == 5)
                            ? MsgTrasAlarmaSinRiesgo
                            : MsgTrasAlarmaEnviada;

                        await ModernAlerts.ShowSuccess(AlarmaEnviada, mensajeAMostrar);
                    });

                    IsTimeRunning = false;

                    // Operaciones post-lanzamiento:
                    // - Alarma hija tipo-9 (ruta de escape): awaited ANTES del refresh del mapa,
                    //   porque RefrescarDespuesDeAlarma llama a ObtenerPinesMapa y necesita que el
                    //   pin hijo ya exista en la BD para poder dibujar la polyline padre→hijo.
                    //   Su latencia es ~130ms (no bloquea perceptiblemente al usuario).
                    // - ObtenerAlarma + InsertarAlarmaEnCacheLocal: fire-and-forget porque
                    //   InsertarAlarmaEnCacheLocal escribe ~275KB a disco y era el culpable del
                    //   bloqueo de 31 segundos en el hilo principal (2813 frames Choreographer).
                    if (alarmaIdCreada.HasValue)
                    {
                        var alarmaId       = alarmaIdCreada.Value;
                        var puntoMarcado   = _puntoHuidaMarcado;
                        var latEscape      = PuntoHuidaLatitud;
                        var lonEscape      = PuntoHuidaLongitud;
                        var thirdParty     = ThirdPartyId;
                        var ip             = InternetUtil.GetPublicIpAddress();
                        var idioma         = IdiomUtil.ObtenerCodigoDeIdioma();

                        // PASO 3a: Alarma hija tipo-9 — awaited para que el pin exista en BD
                        //          antes de que RefrescarDespuesDeAlarma consulte ObtenerPinesMapa
                        if (puntoMarcado && latEscape.HasValue && lonEscape.HasValue)
                        {
                            try
                            {
                                System.Diagnostics.Debug.WriteLine($"[LanzarAlarmaVM] Lanzando alarma hija tipo-9 para padre {alarmaId}");
                                var descripcionEscape = new DescribirAlarma
                                {
                                    alarma_id             = alarmaId,
                                    p_tipoalarma_id       = 9,
                                    latitud_escape        = latEscape.Value,
                                    longitud_escape       = lonEscape.Value,
                                    p_user_id_thirdparty  = thirdParty,
                                    ip_usuario            = ip,
                                    idioma_descripcion    = idioma,
                                    DescripcionAlarma     = null,
                                    DescripcionSospechoso = null,
                                    DescripcionVehiculo   = null,
                                    DescripcionArmas      = null,
                                };
                                var respEscape = await ApiService.DescribirAlarma(descripcionEscape);
                                if (respEscape.IsSuccess)
                                    System.Diagnostics.Debug.WriteLine($"[LanzarAlarmaVM] Alarma hija tipo-9 creada exitosamente");
                                else
                                    System.Diagnostics.Debug.WriteLine($"[LanzarAlarmaVM] Error creando alarma hija tipo-9: {respEscape.Message}");
                            }
                            catch (Exception exEscape)
                            {
                                System.Diagnostics.Debug.WriteLine($"[LanzarAlarmaVM] Error lanzando alarma hija tipo-9: {exEscape.Message}");
                                CrashlyticsHelper.LogError(exEscape, "LanzarAlarmaViewModel", "LanzarAlarmaHijaTipo9");
                            }
                        }

                        // PASO 3b: ObtenerAlarma + InsertarAlarmaEnCacheLocal — fire-and-forget
                        //          (escritura de ~275KB a disco; no necesario para el mapa)
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                System.Diagnostics.Debug.WriteLine($"[LanzarAlarmaVM-BG] Consultando alarma {alarmaId} del servidor...");
                                var alarmasConsultadas = await ApiService.ObtenerAlarma(alarmaId);
                                if (alarmasConsultadas != null && alarmasConsultadas.Count > 0)
                                {
                                    var alarmaCompleta = alarmasConsultadas.First();
                                    System.Diagnostics.Debug.WriteLine($"[LanzarAlarmaVM-BG] Alarma {alarmaCompleta.alarma_id} obtenida — insertando en cache local");
                                    App.InsertarAlarmaEnCacheLocal(alarmaCompleta);
                                    System.Diagnostics.Debug.WriteLine($"[LanzarAlarmaVM-BG] InsertarAlarmaEnCacheLocal completado");
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($"[LanzarAlarmaVM-BG] No se pudo obtener la alarma {alarmaId} del servidor");
                                }
                            }
                            catch (Exception exConsulta)
                            {
                                System.Diagnostics.Debug.WriteLine($"[LanzarAlarmaVM-BG] Error consultando alarma: {exConsulta.Message}");
                                CrashlyticsHelper.LogError(exConsulta, "LanzarAlarmaViewModel", "ObtenerAlarmaBackground");
                            }
                        });
                    }

                    // PASO 4: Dar un poco más de tiempo antes de refrescar el mapa
                    await Task.Delay(150);

                    // NOTA: Ya NO enviamos mensajes MessagingCenter porque:
                    // 1. La alarma ya está insertada en App.AlarmasCacheadas
                    // 2. Llamamos directamente a homePage.RefrescarDespuesDeAlarma() (más abajo)
                    // 3. RefrescarConGestos está diseñado para zoom-out/exploración (NO para lanzar alarma)
                    // Ver manual: 0620-regla-de-asimetría-intencional-mapa-vs-pestañas.md

                    // PASO 5: BÚSQUEDA ESPECÍFICA PARA SOSPECT TABS (llamada directa a HomePage)
                    try
                    {
                        System.Diagnostics.Debug.WriteLine("LanzarAlarmaViewModel: Iniciando búsqueda específica para SospectTabs");

                        var currentPage = Application.Current.MainPage;
                        sospect.Views.HomePage homePage = null;

                        System.Diagnostics.Debug.WriteLine($"LanzarAlarmaViewModel: MainPage tipo: {currentPage?.GetType()?.Name}");

                        if (currentPage is NavigationPage navPage)
                        {
                            System.Diagnostics.Debug.WriteLine($"LanzarAlarmaViewModel: NavigationPage detectado, CurrentPage: {navPage.CurrentPage?.GetType()?.Name}");

                            // Verificar si la página actual es SospectTabs
                            if (navPage.CurrentPage is TabbedPage sospectTabs)
                            {
                                System.Diagnostics.Debug.WriteLine($"LanzarAlarmaViewModel: SospectTabs encontrado con {sospectTabs.Children.Count} tabs");

                                // Buscar HomePage en las tabs
                                foreach (var tab in sospectTabs.Children)
                                {
                                    System.Diagnostics.Debug.WriteLine($"LanzarAlarmaViewModel: Examinando tab: {tab.GetType().Name}");

                                    if (tab is NavigationPage tabNavPage)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"LanzarAlarmaViewModel: Tab NavigationPage encontrado, CurrentPage: {tabNavPage.CurrentPage?.GetType()?.Name}");

                                        if (tabNavPage.CurrentPage is sospect.Views.HomePage pageInTab)
                                        {
                                            homePage = pageInTab;
                                            System.Diagnostics.Debug.WriteLine($"LanzarAlarmaViewModel: *** HomePage encontrado en tab NavigationPage - HashCode: {homePage.GetHashCode()} ***");
                                            break;
                                        }

                                        // También buscar en el stack de la tab
                                        var homePageInStack = tabNavPage.Navigation.NavigationStack.OfType<sospect.Views.HomePage>().FirstOrDefault();
                                        if (homePageInStack != null)
                                        {
                                            homePage = homePageInStack;
                                            System.Diagnostics.Debug.WriteLine($"LanzarAlarmaViewModel: *** HomePage encontrado en stack de tab - HashCode: {homePage.GetHashCode()} ***");
                                            break;
                                        }
                                    }
                                    else if (tab is sospect.Views.HomePage directHomePage)
                                    {
                                        homePage = directHomePage;
                                        System.Diagnostics.Debug.WriteLine($"LanzarAlarmaViewModel: *** HomePage encontrado como tab directa - HashCode: {homePage.GetHashCode()} ***");
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                // Búsqueda estándar en NavigationPage
                                homePage = navPage.Navigation.NavigationStack.OfType<sospect.Views.HomePage>().FirstOrDefault();
                                if (homePage == null)
                                {
                                    homePage = navPage.CurrentPage as sospect.Views.HomePage;
                                }
                            }
                        }
                        else if (currentPage is TabbedPage directTabbedPage)
                        {
                            System.Diagnostics.Debug.WriteLine("LanzarAlarmaViewModel: TabbedPage directo encontrado");

                            foreach (var tab in directTabbedPage.Children)
                            {
                                if (tab is NavigationPage tabNavPage)
                                {
                                    homePage = tabNavPage.CurrentPage as sospect.Views.HomePage;
                                    if (homePage != null) break;
                                }
                                else if (tab is sospect.Views.HomePage directHomePage)
                                {
                                    homePage = directHomePage;
                                    break;
                                }
                            }
                        }
                        else if (currentPage is sospect.Views.HomePage directHomePage)
                        {
                            homePage = directHomePage;
                            System.Diagnostics.Debug.WriteLine("LanzarAlarmaViewModel: HomePage como MainPage directo");
                        }

                        if (homePage != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"LanzarAlarmaViewModel: *** SUCCESS! HomePage encontrado - HashCode: {homePage.GetHashCode()} ***");
                            // DIAGNÓSTICO - ANTES DE REFRESCAR
                            System.Diagnostics.Debug.WriteLine($"[DIAG-LANZAR] Llamando RefrescarDespuesDeAlarma...");
                            System.Diagnostics.Debug.WriteLine($"[DIAG-LANZAR] HomePage encontrado: {homePage != null}, HashCode: {homePage?.GetHashCode()}");
                            await homePage.RefrescarDespuesDeAlarma();
                            System.Diagnostics.Debug.WriteLine("LanzarAlarmaViewModel: RefrescarDespuesDeAlarma ejecutado exitosamente");
                            System.Diagnostics.Debug.WriteLine($"[DIAG-LANZAR] RefrescarDespuesDeAlarma completado");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("LanzarAlarmaViewModel: *** ERROR - No se pudo encontrar HomePage ***");

                            // FALLBACK: Enviar mensaje adicional para SospectTabs
                            System.Diagnostics.Debug.WriteLine("LanzarAlarmaViewModel: Enviando mensaje fallback a SospectTabs");
                            MessagingCenter.Send<object, string>(this, "Refrescar", "FallbackRefresh");

                            // Log de diagnóstico detallado
                            if (Application.Current.MainPage is NavigationPage navPageDebug)
                            {
                                System.Diagnostics.Debug.WriteLine($"LanzarAlarmaViewModel: CurrentPage tipo: {navPageDebug.CurrentPage?.GetType()?.Name}");

                                if (navPageDebug.CurrentPage is TabbedPage tabbedDebug)
                                {
                                    System.Diagnostics.Debug.WriteLine($"LanzarAlarmaViewModel: TabbedPage con {tabbedDebug.Children.Count} tabs:");
                                    for (int i = 0; i < tabbedDebug.Children.Count; i++)
                                    {
                                        var child = tabbedDebug.Children[i];
                                        System.Diagnostics.Debug.WriteLine($"LanzarAlarmaViewModel: Tab {i}: {child.GetType().Name}");

                                        if (child is NavigationPage childNav)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"LanzarAlarmaViewModel: Tab {i} CurrentPage: {childNav.CurrentPage?.GetType()?.Name}");
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception directCallEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"LanzarAlarmaViewModel: Error en búsqueda de HomePage: {directCallEx.Message}");
                        System.Diagnostics.Debug.WriteLine($"LanzarAlarmaViewModel: StackTrace: {directCallEx.StackTrace}");
                    }

                    System.Diagnostics.Debug.WriteLine("LanzarAlarmaViewModel: Todos los métodos de refresco disparados");

                    // NUEVO: Limpiar archivos multimedia después de envío exitoso
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
                            catch (Exception exCleanup)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error limpiando archivo temporal: {exCleanup.Message}");
                            }
                        }
                        MediaFiles.Clear();
                        HasMediaFiles = false;
                    }

                    // Compartir en redes sociales si el usuario lo eligió
                    if (CompartirEnX || CompartirEnFacebook)
                    {
                        try
                        {
                            string tipoAlarmaTexto = TipoAlarmaSeleccionado?.DescripcionTraducida ?? string.Empty;
                            string descripcion = string.IsNullOrWhiteSpace(DescripcionAlarma)
                                ? tipoAlarmaTexto
                                : $"{tipoAlarmaTexto}: {DescripcionAlarma}";

                            string url = alarmaIdCreada.HasValue
                                ? $"{AppConfiguration.WebHost}/a/{alarmaIdCreada.Value}"
                                : AppConfiguration.WebHost;

                            string intro = TranslateExtension.Translate("TextoCompartirAlarmaIntro") ?? string.Empty;
                            string shareText = string.IsNullOrEmpty(intro)
                                ? $"{descripcion}\n{url}"
                                : $"{intro}\n{descripcion}\n{url}";

                            await Share.Default.RequestAsync(new ShareTextRequest
                            {
                                Text = shareText,
                                Title = tipoAlarmaTexto
                            });
                        }
                        catch (Exception exShare)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error al compartir alarma: {exShare.Message}");
                        }
                    }
                }
                else
                {
                    // Si hay error, mostrar mensaje pero no cerrar el popup
                    string errorMessage = response.Message ?? MensajeError;
                    await ModernAlerts.ShowError(LabelError, errorMessage);
                }
            }
            catch (Exception ex)
            {
                await ModernAlerts.ShowWarning(LabelInformacion, MensajeError);
                CrashlyticsHelper.LogError(ex, "LanzarAlarmaViewModel", "RegistrarAlarma");
            }
            finally
            {
                IsRunning = false;
                IsSavingAlarm = false;
                // CRÍTICO: También notificar que terminó el procesamiento
                MessagingCenter.Send(this, "ModificarVariable", false);
            }
        }

        // AGREGAR ESTE MÉTODO AL FINAL DE LA CLASE LanzarAlarmaViewModel

        public async Task ProcessMediaFileFromPath(string filePath, bool isVideo)
        {
            System.Diagnostics.Debug.WriteLine($"ProcessMediaFileFromPath: INICIADO - {filePath}");

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                System.Diagnostics.Debug.WriteLine($"ProcessMediaFileFromPath: Archivo no existe o ruta vacía");
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
                System.Diagnostics.Debug.WriteLine($"ProcessMediaFileFromPath: Tamaño del archivo: {fileInfo.Length} bytes");

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

                System.Diagnostics.Debug.WriteLine($"ProcessMediaFileFromPath: thumbnailPath={thumbnailPath}");

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
                    System.Diagnostics.Debug.WriteLine($"ProcessMediaFileFromPath: Archivo agregado a MediaFiles. Total: {MediaFiles.Count}");
                });
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "LanzarAlarmaViewModel", "ProcessMediaFileFromPath");
                System.Diagnostics.Debug.WriteLine($"ProcessMediaFileFromPath ERROR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"ProcessMediaFileFromPath STACK: {ex.StackTrace}");

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var errorTitle = await TranslateExtension.TranslateAsync("LabelError");
                    var errorMsg = await TranslateExtension.TranslateAsync("LblErrorProcesarArchivo");
                    await ModernAlerts.ShowError(errorTitle, errorMsg);
                });
            }
        }

        public ICommand RegistrarAlarmaCommand { get; }

        private string _CuentaRegresivaAlarma;
        public string CuentaRegresivaAlarma
        {
            get => this._CuentaRegresivaAlarma;
            set => this.SetValue(ref this._CuentaRegresivaAlarma, value);
        }
    }
}



