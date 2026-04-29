namespace sospect.Models
{
    public class AlarmaCuadranteDto
    {
        public decimal lat_cuadrante { get; set; }
        public decimal lng_cuadrante { get; set; }
        public short tipoalarma_id { get; set; }
        public string? descripciontipoalarma { get; set; }
        public string? color_fondo_feed { get; set; }
        public int cantidad_alarmas { get; set; }
        public DateTime? ultima_alarma { get; set; }
    }
}
