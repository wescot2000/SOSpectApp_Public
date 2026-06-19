// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

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


