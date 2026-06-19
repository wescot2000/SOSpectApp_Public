// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;
using Newtonsoft.Json;
using sospect.Helpers;
using sospect.Models;
using sospect.Services;
using sospect.Utils;
using System.Collections.Generic;

namespace sospect.ViewModels
{
    public class CierreEncuestaViewModel : BaseViewModel
    {
        private const int MAX_MEDIA_FILES = 5;
        private const int MaxImageWidth = 1920;
        private const int JpegQuality = 75;

        private readonly INavigation _navigation;
        private CancellationTokenSource _timerCts;

        // --- Alarma ---
        private AlarmaCercana _alarmaSeleccionada;
        public AlarmaCercana AlarmaSeleccionada
        {
            get => _alarmaSeleccionada;
            set => SetValue(ref _alarmaSeleccionada, value);
        }

        // --- Solicitud activa ---
        private SolicitudCierreResponse _solicitudActiva;
        public SolicitudCierreResponse SolicitudActiva
        {
            get => _solicitudActiva;
            set
            {
                SetValue(ref _solicitudActiva, value);
                OnPropertyChanged(nameof(TieneSolicitudActiva));
                OnPropertyChanged(nameof(PuedeVotar));
                OnPropertyChanged(nameof(TextoVotoUsuario));
                OnPropertyChanged(nameof(EsProponenteCierre));
                OnPropertyChanged(nameof(HasFotos));
            }
        }

        public bool TieneSolicitudActiva => SolicitudActiva != null;

        /// <summary>
        /// El usuario actual es quien propuso el cierre — no puede votar.
        /// </summary>
        public bool EsProponenteCierre
        {
            get
            {
                if (SolicitudActiva == null) return false;
                return SolicitudActiva.es_proponente;
            }
        }

        public bool PuedeVotar => SolicitudActiva != null && !SolicitudActiva.ya_voto_usuario && !EsProponenteCierre;

        public bool HasFotos => SolicitudActiva?.Fotos?.Any() == true;

        public string TextoVotoUsuario
        {
            get
            {
                if (SolicitudActiva == null || !SolicitudActiva.ya_voto_usuario)
                    return string.Empty;

                if (SolicitudActiva.voto_usuario == true)
                    return TranslateExtension.Translate("LblYaVotoAFavor");
                else
                    return TranslateExtension.Translate("LblYaVotoEnContra");
            }
        }

        // --- Propuesta ---
        private string _descripcionPropuesta;
        public string DescripcionPropuesta
        {
            get => _descripcionPropuesta;
            set => SetValue(ref _descripcionPropuesta, value);
        }

        // --- Media ---
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

        // --- Tiempo restante ---
        private string _tiempoRestante;
        public string TiempoRestante
        {
            get => _tiempoRestante;
            set => SetValue(ref _tiempoRestante, value);
        }

        // --- Commands ---
        public ICommand ProponerCierreCommand { get; }
        public ICommand VotarAFavorCommand { get; }
        public ICommand VotarEnContraCommand { get; }
        public ICommand CancelarCommand { get; }
        public ICommand RemoveMediaCommand { get; }
        public ICommand VerHistorialCommand { get; }

        public CierreEncuestaViewModel(AlarmaCercana alarma, INavigation navigation)
        {
            AlarmaSeleccionada = alarma;
            _navigation = navigation;

            ProponerCierreCommand = new Command(async () => await ProponerCierre());
            VotarAFavorCommand = new Command(async () => await VotarCierre(true));
            VotarEnContraCommand = new Command(async () => await VotarCierre(false));
            CancelarCommand = new Command(async () => await _navigation.PopAsync());
            RemoveMediaCommand = new Command<MediaFile>(RemoveMedia);
            VerHistorialCommand = new Command(async () => await VerHistorial());
        }

        // ==========================================
        // Cargar solicitud activa
        // ==========================================
        public async Task CargarSolicitudActiva()
        {
            if (IsRunning) return;

            try
            {
                IsRunning = true;

                var userId = App.persona.user_id_thirdparty;
                var response = await ApiService.ObtenerSolicitudCierreActiva(
                    AlarmaSeleccionada.alarma_id, userId);

                if (response.IsSuccess && response.Data != null)
                {
                    var solicitud = JsonConvert.DeserializeObject<SolicitudCierreResponse>(
                        response.Data.ToString());

                    // R6: Parsear fotos de la propuesta (cuando la API retorne fotos_json)
                    if (solicitud != null &&
                        !string.IsNullOrEmpty(solicitud.fotos_json) &&
                        solicitud.fotos_json != "[]")
                    {
                        try
                        {
                            solicitud.Fotos = JsonConvert.DeserializeObject<List<FotoDescripcionAlarma>>(solicitud.fotos_json)
                                             ?? new List<FotoDescripcionAlarma>();
                        }
                        catch (Exception exFotos)
                        {
                            System.Diagnostics.Debug.WriteLine($"CargarSolicitudActiva: Error parseando fotos_json: {exFotos.Message}");
                            solicitud.Fotos = new List<FotoDescripcionAlarma>();
                        }
                    }

                    SolicitudActiva = solicitud;

                    if (SolicitudActiva != null)
                    {
                        IniciarTimerTiempoRestante();
                    }
                }
                else
                {
                    SolicitudActiva = null;
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "CierreEncuestaViewModel", "CargarSolicitudActiva");
                System.Diagnostics.Debug.WriteLine($"CargarSolicitudActiva ERROR: {ex.Message}");
                SolicitudActiva = null;
            }
            finally
            {
                IsRunning = false;
            }
        }

        // ==========================================
        // Proponer cierre
        // ==========================================
        private async Task ProponerCierre()
        {
            if (IsRunning) return;

            var LabelError = await TranslateExtension.TranslateAsync("LabelError");

            // Validar descripcion
            if (string.IsNullOrWhiteSpace(DescripcionPropuesta))
            {
                var msgDescripcionRequerida = await TranslateExtension.TranslateAsync("LblDescripcionRequerida");
                await ModernAlerts.ShowWarning(LabelError, msgDescripcionRequerida);
                return;
            }

            try
            {
                IsRunning = true;

                // Convertir media files a DTOs
                List<FotoAlarmaDto> fotos = null;
                if (MediaFiles != null && MediaFiles.Any())
                {
                    fotos = await ConvertMediaFilesToDtos();
                }

                var request = new ProponerCierreRequest
                {
                    p_alarma_id = AlarmaSeleccionada.alarma_id,
                    p_user_id_thirdparty = App.persona.user_id_thirdparty,
                    p_descripcion = DescripcionPropuesta.Trim(),
                    p_idioma = IdiomUtil.ObtenerCodigoDeIdioma(),
                    Fotos = fotos
                };

                var response = await ApiService.ProponerCierre(request);

                if (response.IsSuccess)
                {
                    var tituloExito = await TranslateExtension.TranslateAsync("LblPropuestaCierreEnviada");
                    var msgExito = await TranslateExtension.TranslateAsync("LblPropuestaCierreExito");
                    await ModernAlerts.ShowSuccess(tituloExito, msgExito);

                    // Limpiar media files despues de envio exitoso
                    LimpiarMediaFiles();

                    // Actualizar flag TieneVotacionActiva en el caché local para que
                    // DescribirPage y HomePage redirijan correctamente sin esperar al refresh del API.
                    ActualizarFlagVotacionEnCache(AlarmaSeleccionada.alarma_id);

                    // R1: Navegar al tab de mapa (índice 0) tras propuesta exitosa.
                    await NavegarAlMapaAsync();
                }
                else
                {
                    string errorMsg = response.Message ?? await TranslateExtension.TranslateAsync("MensajeError");
                    await ModernAlerts.ShowError(LabelError, errorMsg);
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "CierreEncuestaViewModel", "ProponerCierre");
                System.Diagnostics.Debug.WriteLine($"ProponerCierre ERROR: {ex.Message}");

                var msgError = await TranslateExtension.TranslateAsync("MensajeError");
                await ModernAlerts.ShowError(LabelError, msgError);
            }
            finally
            {
                IsRunning = false;
            }
        }

        // ==========================================
        // Votar cierre
        // ==========================================
        private async Task VotarCierre(bool votoAFavor)
        {
            if (IsRunning || SolicitudActiva == null) return;

            try
            {
                IsRunning = true;

                var request = new VotarCierreRequest
                {
                    p_solicitud_id = SolicitudActiva.solicitud_id,
                    p_user_id_thirdparty = App.persona.user_id_thirdparty,
                    p_voto = votoAFavor
                };

                var response = await ApiService.VotarCierre(request);
                var LabelError = await TranslateExtension.TranslateAsync("LabelError");

                if (response.IsSuccess)
                {
                    var tituloExito = await TranslateExtension.TranslateAsync("LblVotoRegistrado");
                    var msgExito = await TranslateExtension.TranslateAsync("LblVotoRegistradoExito");
                    await ModernAlerts.ShowSuccess(tituloExito, msgExito);

                    // Actualización optimista: marcar voto inmediatamente en el objeto local
                    // para que la UI muestre los resultados sin esperar el roundtrip al API.
                    if (SolicitudActiva != null)
                    {
                        SolicitudActiva.ya_voto_usuario = true;
                        SolicitudActiva.voto_usuario = votoAFavor;
                        // Incrementar el contador localmente para feedback inmediato
                        if (votoAFavor)
                            SolicitudActiva.votos_si++;
                        else
                            SolicitudActiva.votos_no++;
                        // Forzar refresco de propiedades derivadas en el ViewModel
                        OnPropertyChanged(nameof(SolicitudActiva));
                        OnPropertyChanged(nameof(PuedeVotar));
                        OnPropertyChanged(nameof(TextoVotoUsuario));
                    }

                    // Recargar del API en background para sincronizar conteos reales
                    await CargarSolicitudActiva();
                }
                else
                {
                    // El API retorna el mensaje de error del stored procedure (p.ej. "ya votó")
                    string errorMsg = response.Message;
                    if (string.IsNullOrWhiteSpace(errorMsg))
                        errorMsg = await TranslateExtension.TranslateAsync("MensajeError");
                    await ModernAlerts.ShowError(LabelError, errorMsg);
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "CierreEncuestaViewModel", "VotarCierre");
                System.Diagnostics.Debug.WriteLine($"VotarCierre ERROR: {ex.Message}");

                var LabelError = await TranslateExtension.TranslateAsync("LabelError");
                var msgError = await TranslateExtension.TranslateAsync("MensajeError");
                await ModernAlerts.ShowError(LabelError, msgError);
            }
            finally
            {
                IsRunning = false;
            }
        }

        // ==========================================
        // Timer para tiempo restante
        // ==========================================
        private void IniciarTimerTiempoRestante()
        {
            DetenerTimer();
            _timerCts = new CancellationTokenSource();
            var token = _timerCts.Token;

            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        ActualizarTiempoRestante();
                        await Task.Delay(1000, token);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Timer ERROR: {ex.Message}");
                        break;
                    }
                }
            }, token);
        }

        private void ActualizarTiempoRestante()
        {
            if (SolicitudActiva?.fecha_limite_votacion == null)
            {
                TiempoRestante = "--:--:--";
                return;
            }

            var diferencia = SolicitudActiva.fecha_limite_votacion.Value - DateTime.UtcNow;

            if (diferencia.TotalSeconds <= 0)
            {
                TiempoRestante = TranslateExtension.Translate("LblVotacionFinalizada");
                DetenerTimer();
                return;
            }

            if (diferencia.TotalDays >= 1)
            {
                TiempoRestante = $"{(int)diferencia.TotalDays}d {diferencia.Hours:D2}:{diferencia.Minutes:D2}:{diferencia.Seconds:D2}";
            }
            else
            {
                TiempoRestante = $"{(int)diferencia.TotalHours:D2}:{diferencia.Minutes:D2}:{diferencia.Seconds:D2}";
            }
        }

        public void DetenerTimer()
        {
            try
            {
                _timerCts?.Cancel();
                _timerCts?.Dispose();
                _timerCts = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DetenerTimer ERROR: {ex.Message}");
            }
        }

        // ==========================================
        // Media handling
        // ==========================================
        public async Task ProcessMediaFileFromPath(string filePath, bool isVideo)
        {
            System.Diagnostics.Debug.WriteLine($"CierreEncuesta ProcessMediaFileFromPath: STARTED - {filePath}");

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                System.Diagnostics.Debug.WriteLine($"CierreEncuesta ProcessMediaFileFromPath: File does not exist or empty path");
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
                System.Diagnostics.Debug.WriteLine($"CierreEncuesta ProcessMediaFileFromPath: File size: {fileInfo.Length} bytes");

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

                System.Diagnostics.Debug.WriteLine($"CierreEncuesta ProcessMediaFileFromPath: thumbnailPath={thumbnailPath}");

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
                    System.Diagnostics.Debug.WriteLine($"CierreEncuesta ProcessMediaFileFromPath: File added. Total: {MediaFiles.Count}");
                });
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "CierreEncuestaViewModel", "ProcessMediaFileFromPath");
                System.Diagnostics.Debug.WriteLine($"CierreEncuesta ProcessMediaFileFromPath ERROR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"CierreEncuesta ProcessMediaFileFromPath STACK: {ex.StackTrace}");

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var errorTitle = await TranslateExtension.TranslateAsync("LabelError");
                    var errorMsg = await TranslateExtension.TranslateAsync("LblErrorProcesarArchivo");
                    await ModernAlerts.ShowError(errorTitle, errorMsg);
                });
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
                    await ModernAlerts.ShowWarning(warningTitle,
                        $"{warningMsg} {MAX_MEDIA_FILES} {await TranslateExtension.TranslateAsync("LblFotosVideosPorAlarma")}");
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

                System.Diagnostics.Debug.WriteLine($"CierreEncuesta ProcessMediaFile: Guardando en {newFile}");

                if (!isVideo)
                {
                    // Copiar a temporal para procesar
                    var tempOriginal = Path.Combine(cacheDir, $"temp_{Guid.NewGuid()}{Path.GetExtension(media.FileName)}");
                    using (var sourceStream = await media.OpenReadAsync())
                    using (var tempStream = File.Create(tempOriginal))
                    {
                        await sourceStream.CopyToAsync(tempStream);
                    }

                    // Procesar con correccion de orientacion
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
                        System.Diagnostics.Debug.WriteLine($"CierreEncuesta ProcessMediaFile: Fallback - copiando sin procesar");
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

                // Verificar que el archivo se guardo
                if (!File.Exists(newFile))
                {
                    var errorMsg = await TranslateExtension.TranslateAsync("LblArchivoNoSeGuardo");
                    throw new Exception(errorMsg);
                }

                var fileInfo = new FileInfo(newFile);
                System.Diagnostics.Debug.WriteLine($"CierreEncuesta ProcessMediaFile: Archivo guardado - Tamano: {fileInfo.Length / 1024}KB");

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
                    System.Diagnostics.Debug.WriteLine($"CierreEncuesta ProcessMediaFile: Total archivos: {MediaFiles.Count}");
                });
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "CierreEncuestaViewModel", "ProcessMediaFile");
                System.Diagnostics.Debug.WriteLine($"CierreEncuesta ProcessMediaFile ERROR: {ex.Message}");

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
                    CrashlyticsHelper.LogError(ex, "CierreEncuestaViewModel", "RemoveMedia");
                }
            }
        }

        private async Task<List<FotoAlarmaDto>> ConvertMediaFilesToDtos()
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
                    System.Diagnostics.Debug.WriteLine($"CierreEncuesta foto convertida: {fotoDto.NombreArchivoOriginal}, Tamano: {mediaFile.FileSizeBytes}");
                }
                catch (Exception ex)
                {
                    CrashlyticsHelper.LogError(ex, "CierreEncuestaViewModel", "ConvertMediaFilesToDtos");
                    System.Diagnostics.Debug.WriteLine($"Error convirtiendo archivo: {ex.Message}");
                }
            }

            return fotos;
        }

        private void LimpiarMediaFiles()
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

        // ==========================================
        // Ver historial de alarma (solo lectura)
        // ==========================================
        private async Task VerHistorial()
        {
            try
            {
                await _navigation.PushAsync(new sospect.Views.VerHistorialAlarmaPage(AlarmaSeleccionada));
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "CierreEncuestaViewModel", "VerHistorial");
                System.Diagnostics.Debug.WriteLine($"VerHistorial ERROR: {ex.Message}");
            }
        }

        // ==========================================
        // Actualizar flag TieneVotacionActiva en caché local
        // ==========================================
        private static void ActualizarFlagVotacionEnCache(long alarmaId)
        {
            try
            {
                // Cache A — Siguiendo / Mapa
                var alarmaA = App.AlarmasCacheadas?.FirstOrDefault(a => a.alarma_id == alarmaId);
                if (alarmaA != null)
                {
                    alarmaA.TieneVotacionActiva = true;
                    System.Diagnostics.Debug.WriteLine($"[VotacionCache] AlarmasCacheadas: alarma {alarmaId} TieneVotacionActiva=true");
                }

                // Cache B — Para Ti
                var alarmaB = App.AlarmasCacheadasParaTi?.FirstOrDefault(a => a.alarma_id == alarmaId);
                if (alarmaB != null)
                {
                    alarmaB.TieneVotacionActiva = true;
                    System.Diagnostics.Debug.WriteLine($"[VotacionCache] AlarmasCacheadasParaTi: alarma {alarmaId} TieneVotacionActiva=true");
                }

                // Registrar al usuario actual como proponente de cierre para esta alarma.
                // Permite que DescribirPage e HistorialPage lo redirijan a VerHistorialAlarmaPage
                // aunque flag_propietario_alarma sea false (no es dueño de la alarma).
                App.AlarmasProponenteCierre.Add(alarmaId);
                System.Diagnostics.Debug.WriteLine($"[VotacionCache] AlarmasProponenteCierre: alarma {alarmaId} registrada");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VotacionCache] ERROR: {ex.Message}");
            }
        }

        // ==========================================
        // R1: Navegar al mapa tras proponer cierre
        // ==========================================
        private async Task NavegarAlMapaAsync()
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    // Buscar el TabbedPage raíz y cambiar al tab de mapa (índice 0)
                    if (Application.Current?.MainPage is TabbedPage tabbedPage && tabbedPage.Children.Count > 0)
                    {
                        // Limpiar el stack del tab "Describir" (índice 1) para que al volver
                        // a ese tab el usuario llegue a DescribirPage y no a CierreEncuestaPage.
                        if (tabbedPage.Children.Count > 1)
                        {
                            var describirNavPage = tabbedPage.Children[1] as NavigationPage;
                            if (describirNavPage != null && describirNavPage.Navigation.NavigationStack.Count > 1)
                            {
                                await describirNavPage.Navigation.PopToRootAsync(animated: false);
                                System.Diagnostics.Debug.WriteLine("[NavegarAlMapa] Stack del tab Describir limpiado.");
                            }
                        }

                        var mapaNavPage = tabbedPage.Children[0] as NavigationPage;
                        if (mapaNavPage != null)
                        {
                            // Vaciar el stack del tab de mapa si tiene páginas apiladas
                            if (mapaNavPage.Navigation.NavigationStack.Count > 1)
                                await mapaNavPage.Navigation.PopToRootAsync(animated: false);

                            // Cambiar al tab del mapa
                            tabbedPage.CurrentPage = mapaNavPage;
                            return;
                        }
                    }

                    // Fallback: vaciar el stack de la navegación actual
                    System.Diagnostics.Debug.WriteLine("CierreEncuestaViewModel: TabbedPage no encontrado, usando fallback PopAsync");
                    var stack = _navigation.NavigationStack.ToList();
                    for (int i = stack.Count - 1; i > 0; i--)
                        await _navigation.PopAsync(false);
                    await _navigation.PopAsync(true);
                }
                catch (Exception ex)
                {
                    CrashlyticsHelper.LogError(ex, "CierreEncuestaViewModel", "NavegarAlMapaAsync");
                    System.Diagnostics.Debug.WriteLine($"NavegarAlMapaAsync ERROR: {ex.Message}");
                    try { await _navigation.PopAsync(); } catch { }
                }
            });
        }
    }
}


