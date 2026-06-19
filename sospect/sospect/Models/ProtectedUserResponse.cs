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


