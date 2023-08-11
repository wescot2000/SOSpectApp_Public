using System.Reflection;
using System.Resources;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;

namespace sospect.Models
{
    public class MetricasBasicasReporte
    {
        [JsonProperty("metrica")]
        public string metrica { get; set; }

        [JsonProperty("cantidad")]
        public long cantidad { get; set; }

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
