namespace sospect.Models
{
    public class CrearAlarmaPromocionalResponse
    {
        public bool IsSuccess { get; set; }
        public long AlarmaId { get; set; }
        public int SaldoResultante { get; set; }
        public string Message { get; set; }
    }
}
