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

        // 2026-04-12: ResourceManager estático Lazy para evitar fallo en iOS donde
        //             crear una nueva instancia por getter causa null silencioso.
        private static readonly Lazy<ResourceManager> _resMgr =
            new Lazy<ResourceManager>(() => new ResourceManager(
                "sospect.Resources.AppResources",
                typeof(PromEfectivoAlarmasReporteBasResponse).GetTypeInfo().Assembly));

        public string DescripcionTraducida
        {
            get
            {
                string key = metrica?.Replace(" ", string.Empty) ?? string.Empty;
                try
                {
                    string result = _resMgr.Value.GetString(key, CultureInfo.CurrentCulture);
                    if (!string.IsNullOrEmpty(result)) return result;
                    // Fallback cultura invariante — cubre iOS con es-CO, es-US, etc.
                    string neutral = _resMgr.Value.GetString(key, CultureInfo.InvariantCulture);
                    return !string.IsNullOrEmpty(neutral) ? neutral : (!string.IsNullOrEmpty(metrica) ? metrica : key);
                }
                catch
                {
                    return !string.IsNullOrEmpty(metrica) ? metrica : key;
                }
            }
        }
    }
}
