// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
namespace sospect.Models
{
    public class DescribirAlarma
    {
        public string p_user_id_thirdparty { get; set; }
        public long alarma_id { get; set; }
        public string? DescripcionAlarma { get; set; }
        public string? DescripcionSospechoso { get; set; }
        public string? DescripcionVehiculo { get; set; }
        public string? DescripcionArmas { get; set; }
        public bool flag_propietario_alarma { get; set; }
        public long? p_tipoalarma_id { get; set; }
        public double? latitud_escape { get; set; }
        public double? longitud_escape { get; set; }
        public string? ip_usuario { get; set; }
        public string? idioma_descripcion { get; set; }
        public List<FotoAlarmaDto> Fotos { get; set; }  // Nueva propiedad para enviar fotos al API
    }
}



