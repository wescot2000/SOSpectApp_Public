using System;
using System.Collections.Generic;
using System.Text;

namespace sospect.Models
{
    public class ApiResponse
    {
        public bool IsSuccess { get; set; }
        public Solicitud[] Data { get; set; }
        public string Message { get; set; }
    }

}
