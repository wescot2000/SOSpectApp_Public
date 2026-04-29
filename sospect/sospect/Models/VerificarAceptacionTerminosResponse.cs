namespace sospect.Models
{
    public class VerificarAceptacionTerminosResponse
    {
        public bool IsSuccess { get; set; }
        public long? ChatId { get; set; }
        public bool YaAceptoTerminos { get; set; }
        public string EstadoChat { get; set; }
        public bool EsProveedor { get; set; }
        public string Message { get; set; }
    }
}
