// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace sospect.Models
{
    public class SuperPower
    {
        
        public int cantidad_poderes { get; set; }
        public decimal valor_usd { get; set; }
        public string ProductId { get; set; }
        public string productDesc { get; set; }
        public string LocalizedPrice { get; set; }  
    }

    public class SuperPowersResponse
    {
        public bool isSuccess { get; set; }
        public ObservableCollection<SuperPower> data { get; set; }
        public string message { get; set; }
    }
}


