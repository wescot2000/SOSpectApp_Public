using System;
using System.Collections.Generic;
using System.Text;

namespace sospect.Models
{
    public class ProtectedUserResponse
    {
        public bool IsSuccess { get; set; }
        public List<ProtectedUserData> Data { get; set; }
        public string Message { get; set; }
    }

    public class ProtectedUserData
    {
        public string User_Id_ThirdParty_Protector { get; set; }
        public string User_Id_ThirdParty_Protegido { get; set; }
        public string Login_Protector { get; set; }
        public string Login_Protegido { get; set; }
        public DateTime Fecha_Activacion { get; set; }
        public DateTime Fecha_Finalizacion { get; set; }
    }
}
