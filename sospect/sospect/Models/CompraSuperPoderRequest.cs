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
    public class CompraSuperPoderRequest
    {
        public string p_user_id_thirdparty { get; set; }
        public int cantidad { get; set; }
        public decimal valor { get; set; }
        public string ip_transaccion { get; set; }
        public string p_tipo_transaccion { get; set; }
        public string p_purchase_token { get; set; }
    }
}


