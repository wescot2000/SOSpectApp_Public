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
