// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
namespace sospect.Models
{
    public class ConsultarUsuarioParaRedConfianzaResponse
    {
        public bool IsSuccess { get; set; }
        public UserData Data { get; set; }
        public string Message { get; set; }
    }

    public class UserData
    {
        public string Result { get; set; }
        public string UserIdThirdparty { get; set; }
        public string Message { get; set; }
        public string NationalId { get; set; }
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public string Email { get; set; }
        public string Pais { get; set; }
        public string NumeroMovil { get; set; }
    }
}



