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
