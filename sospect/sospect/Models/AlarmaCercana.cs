using System;
using sospect.Helpers;
using sospect.ViewModels;
using System.Reflection;
using System.Resources;
using System.Globalization;
using Newtonsoft.Json;

namespace sospect.Models
{
    public class AlarmaCercana : BaseViewModel
    {
        public string? user_id_thirdparty { get; set; }
        public string? user_id_creador_alarma { get; set; }
        public string? login_usuario_notificar { get; set; }
        public decimal latitud_alarma { get; set; }
        public decimal longitud_alarma { get; set; }
        public decimal latitud_entrada { get; set; }
        public decimal longitud_entrada { get; set; }
        public string? tipo_subscr_activa_usuario { get; set; }
        public DateTime? fecha_activacion_subscr { get; set; }
        public DateTime? fecha_finalizacion_subscr { get; set; }
        public decimal distancia_en_metros { get; set; }
        public long alarma_id { get; set; }
        public DateTime? fecha_alarma { get; set; }
        private string? _descripciontipoalarma;
        [JsonProperty("descripciontipoalarma")]
        public string? descripciontipoalarma
        {
            get
            {
                if (string.IsNullOrEmpty(_descripciontipoalarma))
                    return string.Empty;

                string key = _descripciontipoalarma.Replace(" ", string.Empty);
                ResourceManager resourceManager = new ResourceManager("sospect.Resources.AppResources", typeof(App).GetTypeInfo().Assembly);
                return resourceManager.GetString(key, CultureInfo.CurrentCulture) ?? _descripciontipoalarma;
            }
            set => _descripciontipoalarma = value;
        }
        public long tipoalarma_id { get; set; }
        public short TiempoRefrescoUbicacion { get; set; }
        public bool usuariocalificoalarma { get; set; }
        public string? calificacionalarmausuario { get; set; }
        public bool flag_propietario_alarma { get; set; }
        public bool EsAlarmaActiva { get; set; }
        public long? alarma_id_padre { get; set; }
        public decimal? calificacion_alarma { get; set; }
        public bool estado_alarma { get; set; }
        public bool Flag_hubo_captura { get; set; }
        public bool flag_alarma_siendo_atendida { get; set; }
        public int cantidad_agentes_atendiendo { get; set; }
        public int cantidad_interacciones { get; set; }
        public bool flag_es_policia { get; set; }

        private string _credibilidadAlarma;
        public string CredibilidadAlarma
        {
            get => _credibilidadAlarma;
            set => SetValue(ref _credibilidadAlarma, value);
        }

        public void CalcularCredibilidad()
        {
            var LblCredibilidadAlarmaActual = TranslateExtension.Translate("LblCredibilidadAlarmaActual");
            CredibilidadAlarma = $"{LblCredibilidadAlarmaActual} {calificacion_alarma}%";
        }
    }
}

