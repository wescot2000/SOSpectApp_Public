using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace sospect.Models
{
    public class WipeDataRequest
    {
        [JsonProperty("p_user_id_thirdparty")]
        public string p_user_id_thirdparty { get; set; }
    }

}
