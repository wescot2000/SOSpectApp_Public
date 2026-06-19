// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace sospect.Models
{
    /// <summary>
    /// Modelo de emprendimiento para SOSpectAppMAUI
    /// Agregado: 13-01-2026 - Refactorización Multi-Emprendimiento
    /// </summary>
    public class Emprendimiento
    {
        [JsonProperty("id_emprendimiento")]
        public long IdEmprendimiento { get; set; }

        [JsonProperty("persona_id_modificadora")]
        public long PersonaIdModificadora { get; set; }

        [JsonProperty("nombre_emprendimiento")]
        public string NombreEmprendimiento { get; set; } = string.Empty;

        [JsonProperty("nit_cedula_propietario")]
        public string NitCedulaPropietario { get; set; } = string.Empty;

        [JsonProperty("nombre_propietario")]
        public string? NombrePropietario { get; set; }

        [JsonProperty("url_logo")]
        public string? UrlLogo { get; set; }

        [JsonProperty("flag_es_usuario_propietario")]
        public bool FlagEsUsuarioPropietario { get; set; }

        [JsonProperty("fecha_inicio")]
        public DateTime FechaInicio { get; set; }

        [JsonProperty("fecha_fin")]
        public DateTime? FechaFin { get; set; }

        // MÉTRICAS DE GAMIFICACIÓN
        [JsonProperty("reputacion_promedio")]
        public decimal ReputacionPromedio { get; set; }

        [JsonProperty("total_calificaciones")]
        public int TotalCalificaciones { get; set; }

        [JsonProperty("promedio_tiempo_respuesta_minutos")]
        public int PromedioTiempoRespuestaMinutos { get; set; }

        [JsonProperty("promedio_tiempo_entrega_horas")]
        public int PromedioTiempoEntregaHoras { get; set; }

        [JsonProperty("porcentaje_satisfaccion")]
        public decimal PorcentajeSatisfaccion { get; set; }

        [JsonProperty("total_chats_mes_actual")]
        public int TotalChatsMesActual { get; set; }

        [JsonProperty("total_transacciones_exitosas")]
        public int TotalTransaccionesExitosas { get; set; }

        [JsonProperty("badges_ganados")]
        public List<Badge> BadgesGanados { get; set; } = new();

        [JsonProperty("fecha_actualizacion_metricas")]
        public DateTime? FechaActualizacionMetricas { get; set; }

        /// <summary>
        /// Indica si el emprendimiento está activo (fecha_fin == null)
        /// </summary>
        public bool EstaActivo => !FechaFin.HasValue || FechaFin.Value > DateTime.Now;

        /// <summary>
        /// Texto de reputación formateado (para binding en UI)
        /// </summary>
        public string ReputacionTexto => $"{ReputacionPromedio:F2} ★ ({TotalCalificaciones})";

        /// <summary>
        /// URL del logo o imagen por defecto
        /// </summary>
        public string UrlLogoODefault => string.IsNullOrEmpty(UrlLogo)
            ? "default_business_logo.png"
            : UrlLogo;
    }

    /// <summary>
    /// Modelo de badge (logro) de un emprendimiento
    /// </summary>
    public class Badge
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [JsonProperty("icono")]
        public string Icono { get; set; } = string.Empty;

        [JsonProperty("descripcion")]
        public string? Descripcion { get; set; }
    }

    /// <summary>
    /// Request para crear o vincular un emprendimiento
    /// </summary>
    public class CrearOVincularEmprendimientoRequest
    {
        [JsonProperty("persona_id")]
        public long PersonaId { get; set; }

        [JsonProperty("nombre_emprendimiento")]
        public string? NombreEmprendimiento { get; set; }

        [JsonProperty("nit_cedula_propietario")]
        public string? NitCedulaPropietario { get; set; }

        [JsonProperty("nombre_propietario")]
        public string? NombrePropietario { get; set; }

        [JsonProperty("url_logo")]
        public string? UrlLogo { get; set; }

        [JsonProperty("flag_es_usuario_propietario")]
        public bool? FlagEsUsuarioPropietario { get; set; }
    }

    /// <summary>
    /// Request para obtener emprendimientos de un usuario
    /// </summary>
    public class ObtenerEmprendimientosRequest
    {
        [JsonProperty("persona_id")]
        public long PersonaId { get; set; }

        [JsonProperty("solo_activos")]
        public bool SoloActivos { get; set; } = true;
    }

    // ─── Dashboard de gamificación (2026-04-09) ────────────────────────────────

    public class MetricasEmprendedorDto
    {
        [JsonProperty("nombre")]
        public string Nombre { get; set; }

        [JsonProperty("reputacionPromedio")]
        public decimal ReputacionPromedio { get; set; }

        [JsonProperty("totalCalificaciones")]
        public int TotalCalificaciones { get; set; }

        [JsonProperty("promedioTiempoRespuestaMinutos")]
        public int? PromedioTiempoRespuestaMinutos { get; set; }

        [JsonProperty("porcentajeSatisfaccion")]
        public decimal? PorcentajeSatisfaccion { get; set; }

        [JsonProperty("totalChatsMesActual")]
        public int TotalChatsMesActual { get; set; }

        [JsonProperty("totalTransaccionesExitosas")]
        public int TotalTransaccionesExitosas { get; set; }

        [JsonProperty("puestoEnPais")]
        public int PuestoEnPais { get; set; }
    }

    public class EmprendedorResumenDto
    {
        [JsonProperty("nombre")]
        public string Nombre { get; set; }

        [JsonProperty("reputacion")]
        public decimal Reputacion { get; set; }

        [JsonProperty("puesto")]
        public int Puesto { get; set; }
    }

    public class DashboardEmprendedorResponse
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }

        [JsonProperty("tieneEmprendimiento")]
        public bool TieneEmprendimiento { get; set; }

        [JsonProperty("sinDatos")]
        public bool SinDatos { get; set; }

        [JsonProperty("pais")]
        public string Pais { get; set; }

        [JsonProperty("totalEmprendedoresEnPais")]
        public int TotalEmprendedoresEnPais { get; set; }

        [JsonProperty("miPuesto")]
        public int MiPuesto { get; set; }

        [JsonProperty("misMetricas")]
        public MetricasEmprendedorDto MisMetricas { get; set; }

        [JsonProperty("mejor")]
        public EmprendedorResumenDto Mejor { get; set; }

        [JsonProperty("peor")]
        public EmprendedorResumenDto Peor { get; set; }
    }

    // ─── Listado de emprendimientos (2026-04-10) ───────────────────────────────

    /// <summary>
    /// Item de la lista de emprendimientos activos del usuario.
    /// Respuesta del endpoint ObtenerEmprendimientosDelUsuario.
    /// </summary>
    public class EmprendimientoResumenDto
    {
        [JsonProperty("idEmprendimiento")]
        public long IdEmprendimiento { get; set; }

        [JsonProperty("nombreEmprendimiento")]
        public string NombreEmprendimiento { get; set; } = string.Empty;

        [JsonProperty("urlLogo")]
        public string UrlLogo { get; set; } = string.Empty;

        [JsonProperty("nitCedulaPropietario")]
        public string NitCedulaPropietario { get; set; } = string.Empty;

        /// <summary>
        /// Texto para mostrar en el selector: "Nombre (NIT)" o solo "Nombre" si no hay NIT
        /// </summary>
        public string NombreConNit => string.IsNullOrEmpty(NitCedulaPropietario)
            ? NombreEmprendimiento
            : $"{NombreEmprendimiento} ({NitCedulaPropietario})";
    }

    public class ObtenerEmprendimientosResponse
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;

        [JsonProperty("emprendimientos")]
        public List<EmprendimientoResumenDto> Emprendimientos { get; set; } = new();
    }

    // ─── Última promoción (2026-04-10) ────────────────────────────────────────

    /// <summary>
    /// Foto de la última promoción lanzada.
    /// </summary>
    public class FotoUltimaPromocionDto
    {
        [JsonProperty("fotoId")]
        public long FotoId { get; set; }

        [JsonProperty("urlFoto")]
        public string UrlFoto { get; set; } = string.Empty;

        [JsonProperty("thumbnailUrl")]
        public string ThumbnailUrl { get; set; } = string.Empty;

        [JsonProperty("esVideo")]
        public bool EsVideo { get; set; }

        [JsonProperty("orden")]
        public int Orden { get; set; }
    }

    /// <summary>
    /// Datos de la última promoción lanzada.
    /// Respuesta del endpoint ObtenerUltimaPromocion.
    /// </summary>
    public class UltimaPromocionResponse
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;

        [JsonProperty("tienePromocionPrevia")]
        public bool TienePromocionPrevia { get; set; }

        [JsonProperty("subscripcionId")]
        public long? SubscripcionId { get; set; }

        [JsonProperty("alarmaId")]
        public long? AlarmaId { get; set; }

        [JsonProperty("descripcionAlarma")]
        public string DescripcionAlarma { get; set; } = string.Empty;

        [JsonProperty("logoHabilitado")]
        public bool LogoHabilitado { get; set; }

        [JsonProperty("domicilioHabilitado")]
        public bool DomicilioHabilitado { get; set; }

        [JsonProperty("contactoHabilitado")]
        public bool ContactoHabilitado { get; set; }

        [JsonProperty("proveedorAceptoTerminosChat")]
        public bool ProveedorAceptoTerminosChat { get; set; }

        [JsonProperty("radioMetros")]
        public int RadioMetros { get; set; }

        [JsonProperty("duracionDias")]
        public int DuracionDias { get; set; }

        [JsonProperty("textoPushPersonalizado")]
        public string TextoPushPersonalizado { get; set; } = string.Empty;

        [JsonProperty("cantidadMediaAdjunta")]
        public int CantidadMediaAdjunta { get; set; }

        [JsonProperty("usuariosPushNotificados")]
        public int CantidadUsuariosPush { get; set; }

        [JsonProperty("urlLogo")]
        public string UrlLogo { get; set; } = string.Empty;

        [JsonProperty("fotos")]
        public List<FotoUltimaPromocionDto> Fotos { get; set; } = new();
    }
}


