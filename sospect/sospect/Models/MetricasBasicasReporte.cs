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

        // 2026-04-12: ResourceManager estático Lazy para evitar fallo en iOS donde
        //             crear una nueva instancia por getter causa null silencioso.
        private static readonly Lazy<ResourceManager> _resMgr =
            new Lazy<ResourceManager>(() => new ResourceManager(
                "sospect.Resources.AppResources",
                typeof(MetricasBasicasReporte).GetTypeInfo().Assembly));

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
