using Newtonsoft.Json;
using sospect.ViewModels;
using sospect.DTOs;

namespace sospect.Models
{
    /// <summary>
    /// ViewModel para fotos de alarmas (solo para binding XAML).
    /// Hereda de BaseViewModel para INotifyPropertyChanged.
    /// NO debe ser serializado directamente - usar FotoDescripcionAlarmaDto para eso.
    /// </summary>
    public class FotoDescripcionAlarma : BaseViewModel
    {
        /// <summary>
        /// Constructor vacío para XAML
        /// </summary>
        public FotoDescripcionAlarma() { }

        /// <summary>
        /// Constructor desde DTO (conversión para UI)
        /// </summary>
        public FotoDescripcionAlarma(FotoDescripcionAlarmaDto dto)
        {
            FotoId = dto.FotoId;
            UrlFoto = dto.UrlFoto;
            ThumbnailUrl = dto.ThumbnailUrl;
            NombreArchivoOriginal = dto.NombreArchivoOriginal;
            TipoMime = dto.TipoMime;
            EsVideo = dto.EsVideo;
            TamanoBytes = dto.TamanoBytes;
            AnchoPixels = dto.AnchoPixels;
            AltoPixels = dto.AltoPixels;
            Orden = dto.Orden;
            FechaSubida = dto.FechaSubida;
        }

        [JsonProperty("foto_id")]
        public long FotoId { get; set; }

        private string _urlFoto;
        [JsonProperty("url_foto")]
        public string UrlFoto
        {
            get => _urlFoto;
            set => SetValue(ref _urlFoto, value);
        }

        private string _thumbnailUrl;
        [JsonProperty("thumbnail_url")]
        public string ThumbnailUrl
        {
            get => _thumbnailUrl;
            set => SetValue(ref _thumbnailUrl, value);
        }

        [JsonProperty("nombre_archivo_original")]
        public string NombreArchivoOriginal { get; set; }

        [JsonProperty("tipo_mime")]
        public string TipoMime { get; set; }

        [JsonProperty("tamano_bytes")]
        public long? TamanoBytes { get; set; }

        [JsonProperty("ancho_pixels")]
        public int? AnchoPixels { get; set; }

        [JsonProperty("alto_pixels")]
        public int? AltoPixels { get; set; }

        [JsonProperty("es_video")]
        public bool EsVideo { get; set; }

        [JsonProperty("orden")]
        public int? Orden { get; set; }

        [JsonProperty("fecha_subida")]
        public DateTimeOffset FechaSubida { get; set; }

        // LAZY LOADING: Cargar imagen solo cuando se solicita
        // Para usar en el binding de UI con carga diferida
        private string _thumbnailUrlOrOriginal;
        public string ThumbnailUrlOrOriginal
        {
            get
            {
                if (_thumbnailUrlOrOriginal == null)
                {
                    // Primera vez que se accede - inicializar con la URL correcta
                    _thumbnailUrlOrOriginal = !string.IsNullOrEmpty(ThumbnailUrl) ? ThumbnailUrl : UrlFoto;
                }
                return _thumbnailUrlOrOriginal;
            }
        }

        /// <summary>
        /// NUEVO: Cantidad total de fotos en el collage (para el converter de tamaño).
        /// Se establece desde el padre AlarmaCercana durante la conversión DTO→ViewModel.
        /// </summary>
        public int CantidadFotosTotal { get; set; }
    }
}
