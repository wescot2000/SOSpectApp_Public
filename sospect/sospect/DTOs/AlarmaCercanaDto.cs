using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace sospect.DTOs
{
    /// <summary>
    /// DTO PURO - Solo para transporte y caché de alarmas.
    /// NO hereda de BaseViewModel, NO implementa INotifyPropertyChanged.
    /// Se serializa/deserializa directamente desde API y caché local.
    /// Contiene TODOS los campos que retorna vw_busca_alarmas_por_zona2.
    /// </summary>
    public class AlarmaCercanaDto
    {
        [JsonProperty("user_id_thirdparty")]
        public string UserIdThirdparty { get; set; }

        [JsonProperty("persona_id")]
        public long PersonaId { get; set; }

        [JsonProperty("user_id_creador_alarma")]
        public string UserIdCreadorAlarma { get; set; }

        [JsonProperty("login_usuario_notificar")]
        public string LoginUsuarioNotificar { get; set; }

        [JsonProperty("latitud_alarma")]
        public decimal LatitudAlarma { get; set; }

        [JsonProperty("longitud_alarma")]
        public decimal LongitudAlarma { get; set; }

        [JsonProperty("latitud_entrada")]
        public decimal LatitudEntrada { get; set; }

        [JsonProperty("longitud_entrada")]
        public decimal LongitudEntrada { get; set; }

        [JsonProperty("tipo_subscr_activa_usuario")]
        public string TipoSubscrActivaUsuario { get; set; }

        [JsonProperty("fecha_activacion_subscr")]
        public DateTime? FechaActivacionSubscr { get; set; }

        [JsonProperty("fecha_finalizacion_subscr")]
        public DateTime? FechaFinalizacionSubscr { get; set; }

        [JsonProperty("distancia_en_metros")]
        public decimal DistanciaEnMetros { get; set; }

        [JsonProperty("relacion_social")]
        public string RelacionSocial { get; set; }

        [JsonProperty("alarma_id")]
        public long AlarmaId { get; set; }

        [JsonProperty("fecha_alarma")]
        public DateTime? FechaAlarma { get; set; }

        [JsonProperty("descripciontipoalarma")]
        public string DescripcionTipoAlarma { get; set; }

        [JsonProperty("tipoalarma_id")]
        public long TipoAlarmaId { get; set; }

        /// <summary>
        /// ID de la categoría de alarma. 1=SEGURIDAD, 2=POLITICA, 3=INFRAESTRUCTURA, etc.
        /// Usado para decidir si mostrar el botón "Ver autoridades responsables".
        /// </summary>
        [JsonProperty("categoria_alarma_id")]
        public int? CategoriaAlarmaId { get; set; }

        /// <summary>
        /// Solo SEGURIDAD (id=1) y POLITICA (id=2) tienen autoridades responsables asignadas.
        /// </summary>
        public bool MostrarAutoridadesResponsables =>
            CategoriaAlarmaId == 1 || CategoriaAlarmaId == 2;

        [JsonProperty("color_fondo_feed")]
        public string ColorFondoFeed { get; set; }

        [JsonProperty("tiemporefrescoubicacion")]
        public short TiempoRefrescoUbicacion { get; set; }

        [JsonProperty("flag_propietario_alarma")]
        public bool FlagPropietarioAlarma { get; set; }

        [JsonProperty("calificacion_actual_alarma")]
        public decimal? CalificacionActualAlarma { get; set; }

        [JsonProperty("usuariocalificoalarma")]
        public bool UsuarioCalificoAlarma { get; set; }

        [JsonProperty("calificacionalarmausuario")]
        public string CalificacionAlarmaUsuario { get; set; }

        [JsonProperty("EsAlarmaActiva")]
        public bool EsAlarmaActiva { get; set; }

        [JsonProperty("alarma_id_padre")]
        public long? AlarmaIdPadre { get; set; }

        [JsonProperty("calificacion_alarma")]
        public decimal? CalificacionAlarma { get; set; }

        [JsonProperty("estado_alarma")]
        public bool EstadoAlarma { get; set; }

        [JsonProperty("Flag_hubo_captura")]
        public bool FlagHuboCaptura { get; set; }

        [JsonProperty("flag_alarma_siendo_atendida")]
        public bool FlagAlarmaSiendoAtendida { get; set; }

        [JsonProperty("cantidad_agentes_atendiendo")]
        public int CantidadAgentesAtendiendo { get; set; }

        [JsonProperty("cantidad_interacciones")]
        public int CantidadInteracciones { get; set; }

        [JsonProperty("flag_es_policia")]
        public bool FlagEsPolicia { get; set; }

        [JsonProperty("Descripcionalarma")]
        public string Descripcionalarma { get; set; }

        [JsonProperty("flag_red_confianza")]
        public bool FlagRedConfianza { get; set; }

        [JsonProperty("radio_alarmas_mts_actual")]
        public int? RadioAlarmasMtsActual { get; set; }

        [JsonProperty("radio_interes_metros")]
        public int? RadioInteresMetros { get; set; }

        [JsonProperty("minutos_vigencia")]
        public int? MinutosVigencia { get; set; }

        [JsonProperty("tipo_cierre")]
        public string TipoCierre { get; set; }

        [JsonProperty("cantidad_videos")]
        public int CantidadVideos { get; set; }

        [JsonProperty("cantidad_fotos")]
        public int CantidadFotos { get; set; }

        [JsonProperty("flag_visible_mapa")]
        public bool FlagVisibleMapa { get; set; }

        [JsonProperty("fotos")]
        public List<FotoDescripcionAlarmaDto> Fotos { get; set; }

        [JsonProperty("ranking_relevancia")]
        public decimal RankingRelevancia { get; set; }

        [JsonProperty("flag_visible_siguiendo")]
        public bool FlagVisibleSiguiendo { get; set; }

        [JsonProperty("flag_seguridad_personal")]
        public bool FlagSeguridadPersonal { get; set; }

        [JsonProperty("usuario_anonimizado")]
        public string UsuarioAnonimizado { get; set; }

        /// <summary>
        /// Flag que indica si hay cambios pendientes de sincronizar con la API.
        /// Se usa para Optimistic UI cuando el usuario crea una alarma.
        /// </summary>
        [JsonProperty("pendiente_sincronizacion")]
        public bool PendienteSincronizacion { get; set; }

        // ===== CAMPOS TERRITORIALES (Fase 1 - Territorialización) =====
        // Extraídos de Google Reverse Geocoding

        [JsonProperty("barrio")]
        public string Barrio { get; set; }

        [JsonProperty("ciudad")]
        public string Ciudad { get; set; }

        [JsonProperty("pais")]
        public string Pais { get; set; }

        // ===== CAMPOS SOCIALES (23-02-2026 - Red social) =====

        [JsonProperty("cantidad_likes")]
        public int CantidadLikes { get; set; }

        [JsonProperty("usuario_dio_like")]
        public bool UsuarioDioLike { get; set; }

        [JsonProperty("cantidad_reenvios")]
        public int CantidadReenvios { get; set; }

        [JsonProperty("usuario_reenvio")]
        public bool UsuarioReenvio { get; set; }

        [JsonProperty("flag_es_reenvio")]
        public bool FlagEsReenvio { get; set; }

        [JsonProperty("reenvio_user_id_anonimizado")]
        public string ReenvioUserIdAnonimizado { get; set; }

        [JsonProperty("cantidad_verdaderos")]
        public int CantidadVerdaderos { get; set; }

        [JsonProperty("cantidad_falsos")]
        public int CantidadFalsos { get; set; }

        // ===== CAMPOS PROMOCIONALES (Fase 2 - Monetización) =====
        // Retornados por vw_busca_alarmas_por_zona2 cuando is_advertising = true

        [JsonProperty("is_advertising")]
        public bool IsAdvertising { get; set; }

        [JsonProperty("publicidad_radio_metros")]
        public int? PublicidadRadioMetros { get; set; }

        [JsonProperty("publicidad_logo_habilitado")]
        public bool PublicidadLogoHabilitado { get; set; }

        [JsonProperty("publicidad_url_logo")]
        public string PublicidadUrlLogo { get; set; }

        [JsonProperty("publicidad_contacto_habilitado")]
        public bool PublicidadContactoHabilitado { get; set; }

        [JsonProperty("publicidad_domicilio_habilitado")]
        public bool PublicidadDomicilioHabilitado { get; set; }

        [JsonProperty("publicidad_texto_push")]
        public string PublicidadTextoPush { get; set; }

        [JsonProperty("publicidad_nombre_emprendimiento")]
        public string PublicidadNombreEmprendimiento { get; set; }

        // ===== VOTACIÓN DE CIERRE COMUNITARIO (2026-03-29) =====

        /// <summary>
        /// TRUE si la alarma tiene una solicitud de cierre activa (dentro del período de 24h de votación).
        /// </summary>
        [JsonProperty("tiene_votacion_activa")]
        public bool TieneVotacionActiva { get; set; }

        /// <summary>
        /// TRUE si el usuario consultante ya emitió su voto en la solicitud de cierre activa.
        /// </summary>
        [JsonProperty("usuario_ya_voto")]
        public bool UsuarioYaVoto { get; set; }
    }
}
