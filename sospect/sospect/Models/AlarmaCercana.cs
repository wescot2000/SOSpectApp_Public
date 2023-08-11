using System;
using sospect.Helpers;
using sospect.ViewModels;

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
        public string? descripciontipoalarma { get; set; }
        public long tipoalarma_id { get; set; }
        public short TiempoRefrescoUbicacion { get; set; }
        public bool usuariocalificoalarma { get; set; }
        public string? calificacionalarmausuario { get; set; }
        public bool flag_propietario_alarma { get; set; }
        public bool EsAlarmaActiva { get; set; }
        public long? alarma_id_padre { get; set; }
        public decimal? calificacion_alarma { get; set; }

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

