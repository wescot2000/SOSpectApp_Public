using System.Collections.Generic;

namespace sospect.Models
{
    public class CrearAlarmaPromocionalRequest
    {
        public string p_user_id_thirdparty { get; set; }
        public double p_latitud { get; set; }
        public double p_longitud { get; set; }
        public string ip_usuario { get; set; }
        public string idioma_dispositivo { get; set; }

        // Datos del emprendimiento
        public string nit_cedula_propietario { get; set; }
        public string nombre_emprendimiento { get; set; }
        public string nombre_propietario { get; set; }

        public string descripcion_promocional { get; set; }
        public int radio_metros { get; set; }
        public int duracion_dias { get; set; }
        public bool logo_habilitado { get; set; }
        public string url_logo { get; set; }
        public string LogoBase64 { get; set; }  // Logo en Base64 para subir a S3
        public bool contacto_habilitado { get; set; }
        public bool domicilio_habilitado { get; set; }
        public int usuarios_push { get; set; }
        public string texto_push { get; set; }
        public bool proveedor_acepto_terminos_chat { get; set; }
        public List<object> Fotos { get; set; }
    }
}
