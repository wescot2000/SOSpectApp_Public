using System.Reflection;
using System.Resources;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;

namespace sospect.Models
{
    public class PromEfectivoAlarmasReporteBasResponse
    {
        [JsonProperty("metrica")]
        public string metrica { get; set; }

        [JsonProperty("total_alarmas")]
        public long total_alarmas { get; set; }

        [JsonProperty("alarmas_ciertas")]
        public long alarmas_ciertas { get; set; }

        [JsonProperty("alarmas_falsas")]
        public long alarmas_falsas { get; set; }

        [JsonProperty("porcentaje_ciertas")]
        public decimal porcentaje_ciertas { get; set; }

        public string DescripcionTraducida
        {
            get
            {
                string key = metrica.Replace(" ", string.Empty);
                ResourceManager resourceManager = new ResourceManager("sospect.Resources.AppResources", typeof(App).GetTypeInfo().Assembly);
                return resourceManager.GetString(key, CultureInfo.CurrentCulture);
            }
        }
    }
}
