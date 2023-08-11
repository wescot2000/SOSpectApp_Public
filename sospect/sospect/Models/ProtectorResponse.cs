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
    }
}
