namespace sospect.Models
{
    public class FotoAlarmaDto
    {
        public string? Base64Data { get; set; }
        public string? S3Key { get; set; }
        public string NombreArchivoOriginal { get; set; }
        public string TipoMime { get; set; }
        public long TamanoBytes { get; set; }
        public bool EsVideo { get; set; }
        public int Orden { get; set; }
        public string? ThumbnailBase64 { get; set; }
    }
}
