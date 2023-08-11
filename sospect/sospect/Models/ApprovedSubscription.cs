using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace sospect.Models
{
    public class ApprovedSubscription
    {
        [JsonProperty("user_id_thirdparty_protector")]
        public string UserIdProtector { get; set; }

        [JsonProperty("user_id_thirdparty_protegido")]
        public string UserIdProtegido { get; set; }

        [JsonProperty("login")]
        public string Aprobador { get; set; }

        [JsonProperty("fecha_aprobado")]
        public string FechaAprobado { get; set; }

        [JsonProperty("tiporelacion_id")]
        public int TipoRelacionId { get; set; }
    }


    public class ApprovedSubscriptionResponse
    {
        public bool IsSuccess { get; set; }
        public List<ApprovedSubscription> Data { get; set; }
        public string Message { get; set; }
    }


    public class ApprovedSubscriptionData
    {
        public string user_id_thirdparty_protector { get; set; }
        public string user_id_thirdparty_protegido { get; set; }
        public string Login { get; set; }
        public string Fecha_aprobado { get; set; }
        public int Tiporelacion_id { get; set; }
    }
    public class CompleteSubscriptionRequest
    {
        public string P_user_id_thirdparty_protector { get; set; }
        public string P_user_id_thirdparty_protegido { get; set; }
        public string idioma { get; set; }
    }

}
