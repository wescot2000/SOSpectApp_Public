using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Maui.Extensions;
using sospect.CustomRenderers;
using sospect.Extensions;
using sospect.Helpers;
using sospect.Interfaces;
using sospect.Models;
using sospect.Resources;
using sospect.Services;
using sospect.Utils;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Platform;
using sospect.Views.Popups;
using System.IO;

namespace sospect.ViewModels
{
#if ANDROID
    [Android.Runtime.Preserve(AllMembers = true)]
#elif IOS || MACCATALYST
    [Foundation.Preserve(AllMembers = true)]
#endif
    public class DescribirAlarmaPopUpViewModel : BaseViewModel
    {
        private const int MaxImageWidth = 1024;   // Ancho máximo reducido para ~200KB
        private const int JpegQuality = 60;       // Calidad JPEG reducida (0-100)

        // IDs de tipos de alarma que pueden cambiar a "Delito o crimen cometido"
        private static readonly long[] TIPOS_ORIGEN_PERMITIDOS = { 1, 3, 6, 8 };
        // 1 = Advertencia/peligro, 3 = Riña callejera, 6 = Disturbios/protestas, 8 = Violencia Intrafamiliar

        private const long ID_DELITO_CRIMEN = 2; // Delito o crimen cometido (tipo destino)

        // Definición del comando para cancelar el PopUpPage
        public Command CancelarCommand { get; set; }

        // Definición del comando para ejecutar una acción con la información del formulario del PopUpPage
        public Command HechoCommand { get; set; }
        public Command HechoDireccionCommand { get; set; }

        public Command CancelarMinimapaCommand { get; set; }

        private readonly IPopupService _popupService;

        private ObservableCollection<TipoAlarma> _TiposAlarma;
        public ObservableCollection<TipoAlarma> TiposAlarma
        {
            get => this._TiposAlarma;
            set => this.SetValue(ref this._TiposAlarma, value);
        }

        private ObservableCollection<TipoAlarma> _TiposAlarmaFiltradosParaCambio;
        public ObservableCollection<TipoAlarma> TiposAlarmaFiltradosParaCambio
        {
            get => this._TiposAlarmaFiltradosParaCambio;
            set => this.SetValue(ref this._TiposAlarmaFiltradosParaCambio, value);
        }

        private bool _isInitialized = false;
        public bool IsInitialized
        {
            get => _isInitialized;
            set => this.SetValue(ref _isInitialized, value);
        }

        private AlarmaCercana _alarma;
        public AlarmaCercana Alarma
        {
            get => _alarma;
            set
            {
                _alarma = value;
                OnPropertyChanged(nameof(Alarma));
            }
        }

        private TipoAlarma _TiposAlarmaSeleccionado;
        public TipoAlarma TipoAlarmaSeleccionado
        {
            get => this._TiposAlarmaSeleccionado;
            set => this.SetValue(ref this._TiposAlarmaSeleccionado, value);
        }

        private bool _flag_propietario_alarma;
        public bool flag_propietario_alarma
        {
            get => this._flag_propietario_alarma;
            set => this.SetValue(ref this._flag_propietario_alarma, value);
        }

        private bool _MostrarBotonPuntoHuida;
        public bool MostrarBotonPuntoHuida
        {
            get => this._MostrarBotonPuntoHuida;
            set => this.SetValue(ref this._MostrarBotonPuntoHuida, value);
        }

        private DescribirAlarma _DescripcionAlarma;
        public DescribirAlarma DescripcionAlarma
        {
            get => this._DescripcionAlarma;
            set => this.SetValue(ref this._DescripcionAlarma, value);
        }

        private bool _pickerEnabled = false;  // Inicia deshabilitado hasta que CargarTiposAlarma determine el valor correcto
        public bool PickerEnabled
        {
            get { return _pickerEnabled; }
            set
            {
                _pickerEnabled = value;
                OnPropertyChanged();
            }
        }

        // NUEVO: Propiedades para manejar fotos/videos
        private ObservableCollection<MediaFile> _mediaFiles;
        public ObservableCollection<MediaFile> MediaFiles
        {
            get => _mediaFiles;
            set
            {
                _mediaFiles = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasMediaFiles));
            }
        }

        public bool HasMediaFiles => MediaFiles != null && MediaFiles.Any();

        public Command AddPhotoFromGalleryCommand { get; set; }
        public Command<MediaFile> RemoveMediaCommand { get; set; }

        // CORRECCIÓN: Método para obtener Navigation del contexto actual
        private INavigation GetCurrentNavigation()
        {
            try
            {
                if (App.Current?.MainPage is Microsoft.Maui.Controls.TabbedPage tabbedPage)
                {
                    // Obtener la página actual del TabbedPage
                    var currentPage = tabbedPage.CurrentPage;

                    // Si es NavigationPage, devolver su Navigation
                    if (currentPage is NavigationPage navPage)
                    {
                        return navPage.Navigation;
                    }

                    // Si la página actual tiene Navigation, devolverlo
                    if (currentPage?.Navigation != null)
                    {
                        return currentPage.Navigation;
                    }
                }

                // Fallback: usar App.Current.MainPage.Navigation si está disponible
                if (App.Current?.MainPage?.Navigation != null)
                {
                    return App.Current.MainPage.Navigation;
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DescribirAlarmaPopUpViewModel: Error obteniendo Navigation: {ex.Message}");
                return null;
            }
        }

        public DescribirAlarmaPopUpViewModel(DescribirAlarma alarmaAEditar)
        {
            DescripcionAlarma = alarmaAEditar;
            var LabelInformacion = TranslateExtension.Translate("LabelInformacion");
            var LabelOK = TranslateExtension.Translate("LabelOK");
            var Insertiondone = TranslateExtension.Translate("Insertiondone");
            var MensajeError = TranslateExtension.Translate("MensajeError");
            _popupService = DependencyService.Get<IPopupService>() ?? new PopupService();

            // NUEVO: Inicializar colección de MediaFiles
            MediaFiles = new ObservableCollection<MediaFile>();
            MediaFiles.CollectionChanged += (s, e) => OnPropertyChanged(nameof(HasMediaFiles));

            // NUEVO: Comandos para fotos
            AddPhotoFromGalleryCommand = new Command(async () => await AddPhotoFromGallery());
            RemoveMediaCommand = new Command<MediaFile>(RemoveMedia);

            PropertyChanged += OnPropertyChanged;

            // Asignación de la acción que se ejecutará al pulsar el botón de cancelar en el PopUpPage
            CancelarCommand = new Command(async () =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("DescribirAlarmaPopUpViewModel: CancelarCommand iniciado");
                    // CORRECCIÓN: Usar GetCurrentNavigation para obtener el contexto de navegación correcto
                    INavigation navigation = GetCurrentNavigation();
                    if (navigation != null)
                    {
                        await navigation.PopAsync();
                        System.Diagnostics.Debug.WriteLine("DescribirAlarmaPopUpViewModel: Página cerrada exitosamente");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("DescribirAlarmaPopUpViewModel: Navigation no disponible");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"DescribirAlarmaPopUpViewModel: Error en CancelarCommand: {ex.Message}");
                    CrashlyticsHelper.LogError(ex, "DescribirAlarmaPopUpViewModel", "CancelarCommand");
                }
            });

            _ = CargarTiposAlarma(null, alarmaAEditar);

            HechoCommand = new Command(async () =>
            {
                if (IsRunning) return;
                IsRunning = true;
                try
                {
                    ResponseMessage response = await ApiService.ActualizarDescripcionAlarma(DescripcionAlarma);

                    await ModernAlerts.ShowSuccess(LabelInformacion, Insertiondone);

                    // CORRECCIÓN: Cerrar la página usando GetCurrentNavigation
                    INavigation navigation = GetCurrentNavigation();
                    if (navigation != null)
                    {
                        await navigation.PopAsync();
                    }

                    // Actualizar lista de descripción
                    MessagingCenter.Send<object>("valor", "RegistroActualizado");
                }
                catch (Exception ex)
                {
                    await ModernAlerts.ShowWarning(LabelInformacion, MensajeError);
                    CrashlyticsHelper.LogError(ex, "DescribirAlarmaPopUpViewModel", "ActualizarDescripcionAlarma");
                }
                finally
                {
                    IsRunning = false;
                }
            }, () => !IsRunning);
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TipoAlarmaSeleccionado))
            {
                if (Device.RuntimePlatform == Device.iOS)
                {
                    // MostrarBotonPuntoHuida solo se muestra si el tipo de alarma es compatible
                    MostrarBotonPuntoHuida = TipoAlarmaSeleccionado?.TipoalarmaId == 9; // Solo se permite la alarma 9
                }
                else
                {
                    // En otras plataformas, mantener la lógica existente
                    MostrarBotonPuntoHuida = TipoAlarmaSeleccionado?.TipoalarmaId == 2 || TipoAlarmaSeleccionado?.TipoalarmaId == 9;
                }
                OnPropertyChanged(nameof(MostrarBotonPuntoHuida));
            }
        }

        private async Task CargarTiposAlarma(AlarmaCercana alarmaCercana = null, DescribirAlarma describirAlarma = null)
        {
            var LabelInformacion = await TranslateExtension.TranslateAsync("LabelInformacion");
            var LabelOK = await TranslateExtension.TranslateAsync("LabelOK");
            var MensajeError = await TranslateExtension.TranslateAsync("MensajeError");

            IsRunning = true;
            IsInitialized = false; // Marcar como no inicializado al empezar la carga

            try
            {
                // CAMBIO: Obtener objetos TipoAlarma completos (con iconos) en lugar de solo IDs
                var tiposAlarma = await ApiService.ObtenerTiposAlarmaCompletos();

                // LOG DE VERIFICACIÓN
                System.Diagnostics.Debug.WriteLine($"=== TIPOS ALARMA EN DESCRIBIR VIEWMODEL ===");
                if (tiposAlarma != null)
                {
                    foreach (var tipo in tiposAlarma)
                    {
                        System.Diagnostics.Debug.WriteLine($"ID={tipo.TipoalarmaId}, Icono='{tipo.Icono ?? "NULL"}'");
                    }
                }
                System.Diagnostics.Debug.WriteLine($"=== FIN ===");

                // ELIMINAR: Ya no necesitamos crear objetos nuevos, ya vienen completos del API
                // var tiposAlarma = idsTiposAlarma.Select(id => new TipoAlarma { TipoalarmaId = id }).ToList();

                // Recuperar los parámetros de usuario desde Preferences
                var parametrosGuardados = Preferences.Get("ParametrosUsuario", "");
                var flagConvenio = false; // Valor por defecto

                if (!string.IsNullOrEmpty(parametrosGuardados))
                {
                    ParametrosUsuario parametros = JsonConvert.DeserializeObject<ParametrosUsuario>(parametrosGuardados);
                    flagConvenio = parametros.flag_convenio; // Leer el flag de convenio
                }

                // Lógica para filtrar los tipos de alarma
                if (Device.RuntimePlatform == Device.iOS && !flagConvenio)
                {
                    // Si es iOS y el flag de convenio está deshabilitado, filtrar tipos de alarma problemáticos
                    tiposAlarma = tiposAlarma.Where(x =>
                        x.TipoalarmaId != 2 && // Delito o crimen cometido
                        x.TipoalarmaId != 3 && // Riña callejera
                        x.TipoalarmaId != 6 && // Disturbios o protestas
                        x.TipoalarmaId != 8    // Violencia Intrafamiliar
                    ).ToList();
                }

                // Asignar la lista de objetos TipoAlarma a la propiedad TiposAlarma
                TiposAlarma = new ObservableCollection<TipoAlarma>(tiposAlarma);

                // Obtener el tipo de alarma original
                long tipoOriginalId = alarmaCercana?.tipoalarma_id ?? describirAlarma?.p_tipoalarma_id ?? 0;

                // Seleccionar el tipo actual
                // Tipo 9 (sospechoso huyendo) no viene en ListarTiposAlarma (excluido por diseño con WHERE tipoalarma_id<>9).
                // Crear el objeto directamente cuando la alarma ya es tipo 9, tal como se hacía en Xamarin.
                if (alarmaCercana != null && alarmaCercana.tipoalarma_id == 9)
                {
                    TipoAlarmaSeleccionado = new TipoAlarma { TipoalarmaId = 9 };
                    PickerEnabled = false;
                }
                else if (alarmaCercana != null)
                {
                    TipoAlarmaSeleccionado = TiposAlarma.FirstOrDefault(x => x.TipoalarmaId == alarmaCercana.tipoalarma_id);
                }
                else if (describirAlarma != null)
                {
                    TipoAlarmaSeleccionado = TiposAlarma.FirstOrDefault(x => x.TipoalarmaId == describirAlarma.p_tipoalarma_id);
                }

                // Si no se seleccionó un tipo válido, seleccionar el primero disponible
                if (TipoAlarmaSeleccionado == null && TiposAlarma.Any())
                {
                    TipoAlarmaSeleccionado = TiposAlarma.First();
                }

                // Determinar si el picker debe estar habilitado
                if (tipoOriginalId == 9)
                {
                    // Tipo 9 (sospechoso huyendo) - mantener lógica existente
                    PickerEnabled = false;
                }
                else if (TIPOS_ORIGEN_PERMITIDOS.Contains(tipoOriginalId))
                {
                    // El tipo original permite cambio a "Delito o crimen cometido"
                    var tipoDelito = tiposAlarma.FirstOrDefault(t => t.TipoalarmaId == ID_DELITO_CRIMEN);
                    if (tipoDelito != null)
                    {
                        PickerEnabled = true;
                        TiposAlarmaFiltradosParaCambio = new ObservableCollection<TipoAlarma> { tipoDelito };
                    }
                    else
                    {
                        // Si no existe el tipo 2, deshabilitar (caso raro, ej: iOS sin convenio)
                        PickerEnabled = false;
                    }
                }
                else
                {
                    // Tipo original no permite cambio
                    PickerEnabled = false;
                }

                // Actualiza el flag para mostrar el botón de huida según el tipo de alarma seleccionado
                MostrarBotonPuntoHuida = TipoAlarmaSeleccionado?.TipoalarmaId == 9 ||
                                         (Device.RuntimePlatform != Device.iOS &&
                                          (TipoAlarmaSeleccionado?.TipoalarmaId == 2 ||
                                           TipoAlarmaSeleccionado?.TipoalarmaId == 9)); // Permitir tipo 2 y 9 en otras plataformas

                IsInitialized = true; // Marcar como inicializado al completar la carga exitosamente
            }
            catch (Exception ex)
            {
                await ModernAlerts.ShowWarning(LabelInformacion, MensajeError);
                CrashlyticsHelper.LogError(ex, "DescribirAlarmaPopUpViewModel", "CargarTiposAlarma");
                IsInitialized = false; // Mantener como no inicializado en caso de error
            }
            finally
            {
                IsRunning = false;
            }
        }

        public DescribirAlarmaPopUpViewModel(AlarmaCercana alarmaCercana)
        {
            DescripcionAlarma = new DescribirAlarma();
            DescripcionAlarma.alarma_id = alarmaCercana.alarma_id;
            Alarma = alarmaCercana;
            //DescripcionAlarma.p_user_id_thirdparty = alarmaCercana.user_id_thirdparty;
            DescripcionAlarma.p_user_id_thirdparty = App.persona.user_id_thirdparty;
            DescripcionAlarma.ip_usuario = InternetUtil.GetPublicIpAddress();

            flag_propietario_alarma = alarmaCercana.flag_propietario_alarma;

            // NUEVO: Inicializar colección de MediaFiles
            MediaFiles = new ObservableCollection<MediaFile>();
            MediaFiles.CollectionChanged += (s, e) => OnPropertyChanged(nameof(HasMediaFiles));

            // NUEVO: Comandos para fotos
            AddPhotoFromGalleryCommand = new Command(async () => await AddPhotoFromGallery());
            RemoveMediaCommand = new Command<MediaFile>(RemoveMedia);

            _ = CargarTiposAlarma(alarmaCercana, null);

            PropertyChanged += OnPropertyChanged;
            var LabelInformacion = TranslateExtension.Translate("LabelInformacion");
            var Insertiondone = TranslateExtension.Translate("Insertiondone");
            var LabelOK = TranslateExtension.Translate("LabelOK");
            var MensajeError = TranslateExtension.Translate("MensajeError");

            // Asignación de la acción que se ejecutará al pulsar el botón de cancelar en el PopUpPage
            CancelarCommand = new Command(async () =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("DescribirAlarmaPopUpViewModel: CancelarCommand iniciado");
                    // CORRECCIÓN: Usar GetCurrentNavigation para obtener el contexto de navegación correcto
                    INavigation navigation = GetCurrentNavigation();
                    if (navigation != null)
                    {
                        await navigation.PopAsync();
                        System.Diagnostics.Debug.WriteLine("DescribirAlarmaPopUpViewModel: Página cerrada exitosamente");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("DescribirAlarmaPopUpViewModel: Navigation no disponible");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"DescribirAlarmaPopUpViewModel: Error en CancelarCommand: {ex.Message}");
                    CrashlyticsHelper.LogError(ex, "DescribirAlarmaPopUpViewModel", "CancelarCommand");
                }
            });

            // Comando para cancelar el minimapa popup
            CancelarMinimapaCommand = new Command(async () =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("ViewModel: CancelarMinimapaCommand iniciado");
                    await Application.Current.MainPage.ClosePopupAsync();
                    System.Diagnostics.Debug.WriteLine("ViewModel: Minimapa popup cerrado");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ViewModel: Error en CancelarMinimapaCommand: {ex.Message}");
                    CrashlyticsHelper.LogError(ex, "DescribirAlarmaPopUpViewModel", "CancelarMinimapaCommand");
                }
            });

            HechoDireccionCommand = new Command(async (mapa) =>
            {
                if (IsRunning) return;

                IsRunning = true;

                try
                {
                    if (mapa is MiniMapa mapaDireccionHuida && mapaDireccionHuida.CurrentMapPosition != null)
                    {
                        DescripcionAlarma.latitud_escape = mapaDireccionHuida.CurrentMapPosition.Latitude;
                        DescripcionAlarma.longitud_escape = mapaDireccionHuida.CurrentMapPosition.Longitude;

                        System.Diagnostics.Debug.WriteLine($"ViewModel: Coordenadas guardadas - Lat: {DescripcionAlarma.latitud_escape}, Lon: {DescripcionAlarma.longitud_escape}");

                        // CRÍTICO: Cerrar el minimapa popup
                        await Application.Current.MainPage.ClosePopupAsync();
                        System.Diagnostics.Debug.WriteLine("ViewModel: Minimapa popup cerrado exitosamente");
                    }
                    else
                    {
                        var Infor = TranslateExtension.Translate("LabelInformacion");
                        var SeleccionaRuta = TranslateExtension.Translate("SeleccionaRuta");
                        await ModernAlerts.ShowInfo(Infor, SeleccionaRuta);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ViewModel: Error en HechoDireccionCommand: {ex.Message}");
                    CrashlyticsHelper.LogError(ex, "DescribirAlarmaPopUpViewModel", "HechoDireccionCommand");
                }
                finally
                {
                    IsRunning = false;
                }
            }, (mapa) => !IsRunning);

            HechoCommand = new Command(async () =>
            {
                if (IsRunning) return;

                IsRunning = true;
                try
                {
                    DescripcionAlarma.p_tipoalarma_id = TipoAlarmaSeleccionado.TipoalarmaId;
                    DescripcionAlarma.idioma_descripcion = IdiomUtil.ObtenerCodigoDeIdioma();

                    // NUEVO: Convertir fotos a Base64 si existen
                    if (MediaFiles != null && MediaFiles.Any())
                    {
                        DescripcionAlarma.Fotos = new List<FotoAlarmaDto>();
                        int orden = 1;

                        foreach (var mediaFile in MediaFiles)
                        {
                            try
                            {
                                byte[] bytes = System.IO.File.ReadAllBytes(mediaFile.FilePath);
                                string base64 = Convert.ToBase64String(bytes);

                                string? thumbnailBase64 = null;
                                if (mediaFile.IsVideo)
                                    thumbnailBase64 = await VideoHelper.GenerateVideoThumbnailBase64Async(mediaFile.FilePath);

                                DescripcionAlarma.Fotos.Add(new FotoAlarmaDto
                                {
                                    Base64Data = base64,
                                    NombreArchivoOriginal = System.IO.Path.GetFileName(mediaFile.FilePath),
                                    TipoMime = mediaFile.IsVideo ? VideoHelper.GetVideoMimeType(mediaFile.FilePath) : "image/jpeg",
                                    TamanoBytes = mediaFile.FileSizeBytes,
                                    EsVideo = mediaFile.IsVideo,
                                    Orden = orden++,
                                    ThumbnailBase64 = thumbnailBase64
                                });

                                System.Diagnostics.Debug.WriteLine($"Foto convertida: {mediaFile.FilePath}");
                            }
                            catch (Exception exMedia)
                            {
                                CrashlyticsHelper.LogError(exMedia, "DescribirAlarmaPopUpViewModel", "ConvertirFotos");
                                System.Diagnostics.Debug.WriteLine($"Error convirtiendo foto: {exMedia.Message}");
                            }
                        }

                        System.Diagnostics.Debug.WriteLine($"Total fotos enviadas: {DescripcionAlarma.Fotos?.Count ?? 0}");
                    }

                    ResponseMessage response = await ApiService.DescribirAlarma(DescripcionAlarma);
                    await ModernAlerts.ShowSuccess(LabelInformacion, Insertiondone);
                    MessagingCenter.Send<App>((App)Application.Current, "DescripcionesActualizadas");

                    // CORRECCIÓN: Cerrar la página usando GetCurrentNavigation
                    INavigation navigation = GetCurrentNavigation();
                    if (navigation != null)
                    {
                        await navigation.PopAsync();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("DescribirAlarmaPopUpViewModel: Navigation no disponible después de guardar");
                    }
                }
                catch (Exception ex)
                {
                    await ModernAlerts.ShowWarning(LabelInformacion, MensajeError);
                    CrashlyticsHelper.LogError(ex, "DescribirAlarmaPopUpViewModel", "DescribirAlarma");
                }
                finally
                {
                    IsRunning = false;
                }
            }, () => !IsRunning);
        }

        // NUEVO: Método para agregar foto desde galería
        private async Task AddPhotoFromGallery()
        {
            try
            {
                var photo = await MediaPicker.PickPhotoAsync(new MediaPickerOptions
                {
                    Title = "Selecciona una foto"
                });

                if (photo != null)
                {
                    await ProcessMediaFileFromPath(photo.FullPath, true);
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "DescribirAlarmaPopUpViewModel", "AddPhotoFromGallery");
                System.Diagnostics.Debug.WriteLine($"Error seleccionando foto: {ex.Message}");
            }
        }

        // NUEVO: Método para procesar archivos multimedia
        public async Task ProcessMediaFileFromPath(string photoPath, bool isFromGallery)
        {
            try
            {
                if (string.IsNullOrEmpty(photoPath) || !File.Exists(photoPath))
                {
                    System.Diagnostics.Debug.WriteLine($"Archivo no válido: {photoPath}");
                    return;
                }

                bool isVideo = photoPath.ToLower().EndsWith(".mp4") ||
                              photoPath.ToLower().EndsWith(".mov") ||
                              photoPath.ToLower().EndsWith(".avi");

                string finalPath = photoPath;

                // Si es una foto desde la galería, comprimirla y corregir orientación EXIF
                if (!isVideo && isFromGallery)
                {
                    var cacheDir = FileSystem.CacheDirectory;
                    if (!Directory.Exists(cacheDir))
                    {
                        Directory.CreateDirectory(cacheDir);
                    }

                    var fileName = $"{Guid.NewGuid()}.jpg";
                    var compressedPath = Path.Combine(cacheDir, fileName);

                    System.Diagnostics.Debug.WriteLine($"DescribirAlarmaPopUpViewModel: Procesando foto con corrección de orientación EXIF...");

                    // Usar el helper para comprimir y corregir orientación
                    var result = await ImageHelper.CompressAndFixOrientationAsync(
                        photoPath,
                        compressedPath,
                        MaxImageWidth,
                        JpegQuality / 100f);

                    if (!string.IsNullOrEmpty(result))
                    {
                        finalPath = result;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"DescribirAlarmaPopUpViewModel: No se pudo procesar, usando original");
                    }
                }

                var fileInfo = new FileInfo(finalPath);
                long fileSizeBytes = fileInfo.Length;

                // En iOS los videos necesitan un thumbnail JPEG generado del primer frame
                var thumbnailPath = isVideo
                    ? await VideoHelper.GenerateVideoThumbnailAsync(finalPath)
                    : finalPath;

                var mediaFile = new MediaFile
                {
                    FilePath = finalPath,
                    ThumbnailPath = thumbnailPath,
                    IsVideo = isVideo,
                    FileSizeBytes = fileSizeBytes
                };

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    MediaFiles.Add(mediaFile);
                    System.Diagnostics.Debug.WriteLine($"Archivo agregado: {finalPath} ({mediaFile.DisplaySize})");
                });
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "DescribirAlarmaPopUpViewModel", "ProcessMediaFileFromPath");
                System.Diagnostics.Debug.WriteLine($"Error procesando archivo: {ex.Message}");
            }
        }

        // NUEVO: Método para remover un archivo multimedia
        private void RemoveMedia(MediaFile mediaFile)
        {
            if (mediaFile != null && MediaFiles.Contains(mediaFile))
            {
                MediaFiles.Remove(mediaFile);
                System.Diagnostics.Debug.WriteLine($"Archivo eliminado: {mediaFile.FilePath}");
            }
        }
    }
}