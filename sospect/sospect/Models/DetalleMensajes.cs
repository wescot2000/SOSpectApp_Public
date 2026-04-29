using System;
namespace sospect.Models
{
    public class DetalleMensajes
    {
        public long mensaje_id { get; set; }
        public string asunto { get; set; }
        public string estado { get; set; }
        public string texto { get; set; }
        public string para { get; set; }
        public string remitente { get; set; }
        public DateTime fecha_mensaje { get; set; }
        public string idioma_origen { get; set; }
        public long? alarma_id { get; set; }
        // Rediseño 2026-02-08: Metadata de alarma para mensajes enriquecidos
        public int? tipoalarma_id { get; set; }
        public string descripcion_alarma { get; set; }
        public string url_foto { get; set; }
        public int? distancia_metros { get; set; }
        public string url_logo { get; set; }
    }
}

