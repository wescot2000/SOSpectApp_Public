// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

namespace sospect.Models
{
    public class ConsultarEmprendimientoResponse
    {
        public bool IsSuccess { get; set; }
        public bool existe { get; set; }
        public bool? es_propietario { get; set; }
        public EmprendimientoDto emprendimiento { get; set; }
        public string Message { get; set; }
    }

    public class EmprendimientoDto
    {
        public long id_emprendimiento { get; set; }
        public string nit_cedula_propietario { get; set; }
        public string nombre_emprendimiento { get; set; }
        public string nombre_propietario { get; set; }
        public string url_logo { get; set; }
        public string user_id_thirdparty_propietario { get; set; }
    }
}


