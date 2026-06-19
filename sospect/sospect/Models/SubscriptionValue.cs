// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;
using Newtonsoft.Json;

namespace sospect.Models
{
    public class SubscriptionValuesResponse
    {
        public bool isSuccess { get; set; }
        public ObservableCollection<SubscriptionValue> data { get; set; }
        public string message { get; set; }
    }

    public class SubscriptionValue
    {
        [JsonProperty("tipo_subscr_id")]
        public int TipoSubscrId { get; set; }

        [JsonProperty("descripcion_tipo")]
        public string DescripcionTipo { get; set; }

        [JsonProperty("cantidad_poderes_requeridos")]
        public int CantidadPoderesRequeridos { get; set; }

        [JsonProperty("cantidad_subscripcion")]
        public int CantidadSubscripcion { get; set; }

        [JsonProperty("tiempo_subscripcion_dias")]
        public int TiempoSubscripcionDias { get; set; }

        [JsonProperty("texto")]
        public string Texto { get; set; }
    }
}


