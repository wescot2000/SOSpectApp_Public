namespace sospect.Models
{
    public class ObtenerConteosPorIntervalosRequest
    {
        public double latitud { get; set; }
        public double longitud { get; set; }
        public int radio_maximo_metros { get; set; }
        public int intervalo_metros { get; set; }
    }
}
