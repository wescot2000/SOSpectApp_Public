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

        public string DescripcionTraducida
        {
            get
            {
                string key = $"Alarm_{TipoalarmaId}";
                ResourceManager resourceManager = new ResourceManager("sospect.Resources.AppResources", typeof(App).GetTypeInfo().Assembly);
                return resourceManager.GetString(key, CultureInfo.CurrentCulture);
            }
        }
    }

}
