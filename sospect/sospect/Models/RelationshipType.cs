using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using sospect.Helpers;


namespace sospect.Models
{
    public class RelationshipType
    {
        [JsonProperty("tiporelacion_id")]
        public int TiporelacionId { get; set; }

        [JsonProperty("descripciontiporel")]
        public string DescripcionTiporel { get; set; }

        public string TranslatedDescription
        {
            get
            {
                switch (TiporelacionId)
                {
                    case 183:
                        return TranslateExtension.Translate("LblFamilia");
                    case 184:
                        return TranslateExtension.Translate("LblAmigo");
                    case 185:
                        return TranslateExtension.Translate("LblCompanero");
                    case 186:
                        return TranslateExtension.Translate("LblOtroSinDefinir");
                    default:
                        return DescripcionTiporel;
                }
            }
        }
    }
}

