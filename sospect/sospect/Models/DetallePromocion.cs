using Newtonsoft.Json;
using System;

namespace sospect.Models
{
    /// <summary>
    /// Modelo para el detalle de una suscripción promocional
    /// </summary>
    public class DetallePromocion
    {
        [JsonProperty("subscripcionId")]
        public long SubscripcionId { get; set; }

        [JsonProperty("alarmaId")]
        public long AlarmaId { get; set; }

        [JsonProperty("fechaActivacion")]
        public DateTime FechaActivacion { get; set; }

        [JsonProperty("fechaFinalizacion")]
        public DateTime FechaFinalizacion { get; set; }

        [JsonProperty("poderesConsumidos")]
        public int PoderesConsumidos { get; set; }

        [JsonProperty("radioMetros")]
        public int RadioMetros { get; set; }

        [JsonProperty("duracionDias")]
        public int DuracionDias { get; set; }

        [JsonProperty("logoHabilitado")]
        public bool LogoHabilitado { get; set; }

        [JsonProperty("urlLogo")]
        public string UrlLogo { get; set; }

        [JsonProperty("contactoHabilitado")]
        public bool ContactoHabilitado { get; set; }

        [JsonProperty("domicilioHabilitado")]
        public bool DomicilioHabilitado { get; set; }

        [JsonProperty("cantidadMediaAdjunta")]
        public int CantidadMediaAdjunta { get; set; }

        [JsonProperty("usuariosPushNotificados")]
        public int UsuariosPushNotificados { get; set; }

        [JsonProperty("textoPushPersonalizado")]
        public string TextoPushPersonalizado { get; set; }

        [JsonProperty("proveedorAceptoTerminosChat")]
        public bool ProveedorAceptoTerminosChat { get; set; }

        [JsonProperty("nombreEmprendimiento")]
        public string NombreEmprendimiento { get; set; }

        [JsonProperty("nitCedulaPropietario")]
        public string NitCedulaPropietario { get; set; }

        [JsonProperty("descripcionPromocional")]
        public string DescripcionPromocional { get; set; }

        [JsonProperty("estaVencida")]
        public bool EstaVencida { get; set; }

        [JsonProperty("estadisticasNotificacion")]
        public EstadisticasNotificacion EstadisticasNotificacion { get; set; }
    }

    /// <summary>
    /// Estadísticas de notificaciones para una promoción
    /// </summary>
    public class EstadisticasNotificacion
    {
        [JsonProperty("totalUsuarios")]
        public int TotalUsuarios { get; set; }

        [JsonProperty("enviosAceptados")]
        public int EnviosAceptados { get; set; }

        [JsonProperty("enviosRechazados")]
        public int EnviosRechazados { get; set; }

        [JsonProperty("enviosPendientes")]
        public int EnviosPendientes { get; set; }
    }
}
