using System;
namespace sospect.Models
{
    public class AgregarUsuarioRedConfianzaResponse
    {
        public bool IsSuccess { get; set; }
        public ResponseData Data { get; set; }
        public string Message { get; set; }
    }

    public class ResponseData
    {
        public string Result { get; set; }
        public string Message { get; set; }
    }
}
