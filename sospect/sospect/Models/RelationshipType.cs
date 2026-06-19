// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

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



