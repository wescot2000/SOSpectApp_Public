// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using Newtonsoft.Json;

namespace sospect.DTOs
{
    /// <summary>
    /// DTO PURO - Solo para transporte y caché de fotos/videos de alarmas.
    /// NO hereda de BaseViewModel, NO implementa INotifyPropertyChanged.
    /// Se serializa/deserializa directamente desde API y caché local.
    /// </summary>
    public class FotoDescripcionAlarmaDto
    {
        [JsonProperty("foto_id")]
        public long FotoId { get; set; }

        [JsonProperty("url_foto")]
        public string UrlFoto { get; set; }

        [JsonProperty("thumbnail_url")]
        public string ThumbnailUrl { get; set; }

        [JsonProperty("nombre_archivo_original")]
        public string NombreArchivoOriginal { get; set; }

        [JsonProperty("tipo_mime")]
        public string TipoMime { get; set; }

        [JsonProperty("es_video")]
        public bool EsVideo { get; set; }

        [JsonProperty("tamano_bytes")]
        public long? TamanoBytes { get; set; }

        [JsonProperty("ancho_pixels")]
        public int? AnchoPixels { get; set; }

        [JsonProperty("alto_pixels")]
        public int? AltoPixels { get; set; }

        [JsonProperty("orden")]
        public int? Orden { get; set; }

        [JsonProperty("fecha_subida")]
        public DateTimeOffset FechaSubida { get; set; }
    }
}


