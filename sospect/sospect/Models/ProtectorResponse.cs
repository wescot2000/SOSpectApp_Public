// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
using System.Collections.Generic;
using System.Text;

namespace sospect.Models
{
    public class ProtectorResponse
    {
        public bool isSuccess { get; set; }
        public List<ProtectorData> data { get; set; }
        public string message { get; set; }
    }

    public class ProtectorData
    {
        public string user_id_thirdParty_protector { get; set; }
        public string user_id_thirdParty_protegido { get; set; }
        public string login_protector { get; set; }
        public string login_protegido { get; set; }
        public DateTime fecha_activacion { get; set; }
        public DateTime fecha_finalizacion { get; set; }
        public bool flag_suspension_activa { get; set; }
        public DateTime? fecha_fin_suspension { get; set; }
    }
}


