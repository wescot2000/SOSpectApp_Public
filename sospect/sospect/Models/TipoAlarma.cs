using System;
using Newtonsoft.Json;
using System.Resources;
using System.Globalization;
using System.Reflection;

namespace sospect.Models
{
    public class TipoAlarma
    {
        [JsonProperty("tipoalarma_id")]
        public int TipoalarmaId { get; set; }

        [JsonProperty("icono")]
        public string Icono { get; set; }

        public virtual string DescripcionTraducida
        {
            get
            {
                string key = $"Alarm_{TipoalarmaId}";
                ResourceManager resourceManager = new ResourceManager("sospect.Resources.AppResources", typeof(App).GetTypeInfo().Assembly);
                return resourceManager.GetString(key, CultureInfo.CurrentCulture);
            }
        }
    }
    public class TipoAlarmaEspecial : TipoAlarma
    {
        private string _descripcionTraducida;
        public override string DescripcionTraducida
        {
            get
            {
                return _descripcionTraducida;
            }
        }

        public TipoAlarmaEspecial(string descripcionTraducida, int tipoalarmaId)
        {
            _descripcionTraducida = descripcionTraducida;
            TipoalarmaId = tipoalarmaId;
        }
    }
}