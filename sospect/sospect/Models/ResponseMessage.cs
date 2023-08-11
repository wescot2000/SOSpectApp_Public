using System;
namespace sospect.Models
{
    public class ResponseMessage
    {
        public bool IsSuccess { get; set; }
        public string? Data { get; set; }
        public string? Message { get; set; }
    }
}

