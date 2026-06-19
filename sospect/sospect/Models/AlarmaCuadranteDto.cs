// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

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


