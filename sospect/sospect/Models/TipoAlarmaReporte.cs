using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Text;

namespace sospect.Models
{
    public class TipoAlarmaReporte
    {
        [JsonProperty("tipoalarma_id")]
        public int TipoalarmaId { get; set; }

        [JsonProperty("descripciontipoalarma")]
        public string Descripcion { get; set; }

        [JsonProperty("cantidadAlarmas")]
        public long CantidadAlarmas { get; set; }

        [JsonProperty("participacion")]
        public decimal Participacion { get; set; }

        [JsonProperty("fecha_inicio_reporte")]
        public DateTime FechaInicioReporte { get; set; }

        [JsonProperty("fecha_fin_reporte")]
        public DateTime FechaFinReporte { get; set; }

        // 2026-04-12: ResourceManager estático Lazy para evitar fallo en iOS donde
        //             crear una nueva instancia por getter causa null silencioso.
        private static readonly Lazy<ResourceManager> _resMgr =
            new Lazy<ResourceManager>(() => new ResourceManager(
                "sospect.Resources.AppResources",
                typeof(TipoAlarmaReporte).GetTypeInfo().Assembly));

        public string DescripcionTraducida
        {
            get
            {
                string key = $"Alarm_{TipoalarmaId}";
                try
                {
                    var culture = CultureInfo.CurrentCulture;
                    Console.WriteLine($"[TipoAlarmaReporte] key={key} culture={culture.Name}");

                    string result = _resMgr.Value.GetString(key, culture);
                    if (!string.IsNullOrEmpty(result))
                    {
                        Console.WriteLine($"[TipoAlarmaReporte] GetString='{result}'");
                        return result;
                    }

                    // Fallback 1: cultura invariante (neutro) — cubre iOS con es-CO, es-US, etc.
                    string neutral = _resMgr.Value.GetString(key, CultureInfo.InvariantCulture);
                    if (!string.IsNullOrEmpty(neutral))
                    {
                        Console.WriteLine($"[TipoAlarmaReporte] GetString(neutral)='{neutral}'");
                        return neutral;
                    }

                    // Fallback 2: descripción del servidor
                    Console.WriteLine($"[TipoAlarmaReporte] GetString=NULL, usando Descripcion='{Descripcion}'");
                    return !string.IsNullOrEmpty(Descripcion) ? Descripcion : key;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TipoAlarmaReporte] EXCEPCION key={key}: {ex.GetType().Name} - {ex.Message}");
                    return !string.IsNullOrEmpty(Descripcion) ? Descripcion : key;
                }
            }
        }
    }

}
