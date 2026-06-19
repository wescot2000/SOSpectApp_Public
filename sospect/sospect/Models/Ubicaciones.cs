// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
namespace sospect.Models
{
    public class Ubicaciones
    {
        public string p_user_id_thirdparty { get; set; }
        public double latitud { get; set; }
        public double longitud { get; set; }
        public string Idioma { get; set; }
        public string PantallaOrigen { get; set; }
        public string Pais { get; set; }

        // NUEVO: Pestaña seleccionada en el diseño Twitter/X ("ParaTi" o "Siguiendo")
        public string TabSeleccionada { get; set; }
    }
}



