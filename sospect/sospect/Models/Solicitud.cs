using System;
using System.Collections.Generic;
using System.Text;

namespace sospect.Models
{
    public class Solicitud
    {
        public string user_id_thirdparty { get; set; }
        public string login { get; set; }
        public string fecha_solicitud { get; set; }
        public string user_id_thirdparty_protector { get; set; }
    }
}
