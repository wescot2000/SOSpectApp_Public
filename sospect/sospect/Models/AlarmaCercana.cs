// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using sospect.Helpers;
using sospect.ViewModels;
using sospect.Views.Popups;
using System.Reflection;
using System.Resources;
using System.Globalization;
using Newtonsoft.Json;
using sospect.DTOs;
using sospect.Resources;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;

namespace sospect.Models
{
    /// <summary>
    /// ViewModel para alarmas (solo para binding XAML).
    /// Hereda de BaseViewModel para INotifyPropertyChanged.
    /// NO debe ser serializado directamente - usar AlarmaCercanaDto para eso.
    /// </summary>
    public class AlarmaCercana : BaseViewModel
    {
        /// <summary>
        /// Constructor vacío para XAML
        /// </summary>
        public AlarmaCercana() { }

        /// <summary>
        /// Constructor desde DTO (conversión para UI)
        /// </summary>
        public AlarmaCercana(AlarmaCercanaDto dto)
        {
            user_id_thirdparty = dto.UserIdThirdparty;
            user_id_creador_alarma = dto.UserIdCreadorAlarma;
            login_usuario_notificar = dto.LoginUsuarioNotificar;
            latitud_alarma = dto.LatitudAlarma;
            longitud_alarma = dto.LongitudAlarma;
            latitud_entrada = dto.LatitudEntrada;
            longitud_entrada = dto.LongitudEntrada;
            tipo_subscr_activa_usuario = dto.TipoSubscrActivaUsuario;
            fecha_activacion_subscr = dto.FechaActivacionSubscr;
            fecha_finalizacion_subscr = dto.FechaFinalizacionSubscr;
            distancia_en_metros = dto.DistanciaEnMetros;
            alarma_id = dto.AlarmaId;
            fecha_alarma = dto.FechaAlarma;
            descripciontipoalarma = dto.DescripcionTipoAlarma;
            tipoalarma_id = dto.TipoAlarmaId;
            color_fondo_feed = dto.ColorFondoFeed;
            TiempoRefrescoUbicacion = dto.TiempoRefrescoUbicacion;
            flag_propietario_alarma = dto.FlagPropietarioAlarma;
            usuariocalificoalarma = dto.UsuarioCalificoAlarma;
            calificacionalarmausuario = dto.CalificacionAlarmaUsuario;
            EsAlarmaActiva = dto.EsAlarmaActiva;
            alarma_id_padre = dto.AlarmaIdPadre;
            calificacion_alarma = dto.CalificacionAlarma;
            estado_alarma = dto.EstadoAlarma;
            Flag_hubo_captura = dto.FlagHuboCaptura;
            flag_alarma_siendo_atendida = dto.FlagAlarmaSiendoAtendida;
            cantidad_agentes_atendiendo = dto.CantidadAgentesAtendiendo;
            cantidad_interacciones = dto.CantidadInteracciones;
            flag_es_policia = dto.FlagEsPolicia;
            Descripcionalarma = dto.Descripcionalarma;
            flag_red_confianza = dto.FlagRedConfianza;
            radio_alarmas_mts_actual = dto.RadioAlarmasMtsActual;
            radio_interes_metros = dto.RadioInteresMetros;
            minutos_vigencia = dto.MinutosVigencia;
            tipo_cierre = dto.TipoCierre;
            cantidad_videos = dto.CantidadVideos;
            cantidad_fotos = dto.CantidadFotos;
            flag_visible_mapa = dto.FlagVisibleMapa;
            flag_visible_siguiendo = dto.FlagVisibleSiguiendo;
            flag_seguridad_personal = dto.FlagSeguridadPersonal;
            ranking_relevancia = dto.RankingRelevancia;
            usuario_anonimizado = dto.UsuarioAnonimizado;
            pendiente_sincronizacion = dto.PendienteSincronizacion;

            // Categoría de alarma (para botón de autoridades responsables)
            CategoriaAlarmaId = dto.CategoriaAlarmaId;

            // Campos territoriales
            barrio = dto.Barrio;
            ciudad = dto.Ciudad;
            pais = dto.Pais;

            // Campos sociales (23-02-2026)
            cantidad_likes = dto.CantidadLikes;
            usuario_dio_like = dto.UsuarioDioLike;
            cantidad_reenvios = dto.CantidadReenvios;
            usuario_reenvio = dto.UsuarioReenvio;
            flag_es_reenvio = dto.FlagEsReenvio;
            reenvio_user_id_anonimizado = dto.ReenvioUserIdAnonimizado;
            cantidad_verdaderos = dto.CantidadVerdaderos;
            cantidad_falsos = dto.CantidadFalsos;

            // Campos promocionales
            is_advertising = dto.IsAdvertising;
            publicidad_radio_metros = dto.PublicidadRadioMetros;
            publicidad_logo_habilitado = dto.PublicidadLogoHabilitado;
            publicidad_url_logo = dto.PublicidadUrlLogo;
            publicidad_contacto_habilitado = dto.PublicidadContactoHabilitado;
            publicidad_domicilio_habilitado = dto.PublicidadDomicilioHabilitado;
            publicidad_texto_push = dto.PublicidadTextoPush;
            publicidad_nombre_emprendimiento = dto.PublicidadNombreEmprendimiento;

            // Votación de cierre comunitario (2026-03-29)
            TieneVotacionActiva = dto.TieneVotacionActiva;
            UsuarioYaVoto = dto.UsuarioYaVoto;

            // Convertir Fotos de DTO a ViewModel
            Fotos = dto.Fotos?.Select(f => new FotoDescripcionAlarma(f)).ToList()
                    ?? new List<FotoDescripcionAlarma>();

            // CRÍTICO: Establecer CantidadFotosTotal en cada foto para el converter de tamaño
            var cantidadTotal = Fotos.Count;
            foreach (var foto in Fotos)
            {
                foto.CantidadFotosTotal = cantidadTotal;
            }

            // DEBUG: Verificar conversión de fotos - SOLO EN DEBUG BUILD
#if DEBUG
            // OPTIMIZADO: Este log se elimina en Release para evitar 800+ líneas de log
            // System.Diagnostics.Debug.WriteLine($"[AlarmaCercana DTO→VM] ID={alarma_id}, DTO Fotos={dto.Fotos?.Count ?? 0}, VM Fotos={Fotos.Count}");
#endif
        }

        /// <summary>
        /// Convierte este ViewModel a DTO (para serialización)
        /// </summary>
        public AlarmaCercanaDto ToDto()
        {
            return new AlarmaCercanaDto
            {
                UserIdThirdparty = user_id_thirdparty,
                PersonaId = 0, // No se usa en el ViewModel
                UserIdCreadorAlarma = user_id_creador_alarma,
                LoginUsuarioNotificar = login_usuario_notificar,
                LatitudAlarma = latitud_alarma,
                LongitudAlarma = longitud_alarma,
                LatitudEntrada = latitud_entrada,
                LongitudEntrada = longitud_entrada,
                TipoSubscrActivaUsuario = tipo_subscr_activa_usuario,
                FechaActivacionSubscr = fecha_activacion_subscr,
                FechaFinalizacionSubscr = fecha_finalizacion_subscr,
                DistanciaEnMetros = distancia_en_metros,
                RelacionSocial = "MYSELF", // Valor por defecto
                AlarmaId = alarma_id,
                FechaAlarma = fecha_alarma,
                DescripcionTipoAlarma = descripciontipoalarma,
                TipoAlarmaId = tipoalarma_id,
                ColorFondoFeed = color_fondo_feed,
                TiempoRefrescoUbicacion = TiempoRefrescoUbicacion,
                FlagPropietarioAlarma = flag_propietario_alarma,
                CalificacionActualAlarma = calificacion_alarma,
                UsuarioCalificoAlarma = usuariocalificoalarma,
                CalificacionAlarmaUsuario = calificacionalarmausuario,
                EsAlarmaActiva = EsAlarmaActiva,
                AlarmaIdPadre = alarma_id_padre,
                CalificacionAlarma = calificacion_alarma,
                EstadoAlarma = estado_alarma,
                FlagHuboCaptura = Flag_hubo_captura,
                FlagAlarmaSiendoAtendida = flag_alarma_siendo_atendida,
                CantidadAgentesAtendiendo = cantidad_agentes_atendiendo,
                CantidadInteracciones = cantidad_interacciones,
                FlagEsPolicia = flag_es_policia,
                Descripcionalarma = Descripcionalarma,
                FlagRedConfianza = flag_red_confianza,
                RadioAlarmasMtsActual = radio_alarmas_mts_actual,
                RadioInteresMetros = radio_interes_metros,
                MinutosVigencia = minutos_vigencia,
                TipoCierre = tipo_cierre,
                CantidadVideos = cantidad_videos,
                CantidadFotos = cantidad_fotos,
                FlagVisibleMapa = flag_visible_mapa,
                FlagVisibleSiguiendo = flag_visible_siguiendo,
                FlagSeguridadPersonal = flag_seguridad_personal,
                RankingRelevancia = ranking_relevancia,
                UsuarioAnonimizado = usuario_anonimizado,
                PendienteSincronizacion = pendiente_sincronizacion,
                Barrio = barrio,
                Ciudad = ciudad,
                Pais = pais,
                CategoriaAlarmaId = CategoriaAlarmaId,
                IsAdvertising = is_advertising,
                PublicidadRadioMetros = publicidad_radio_metros,
                PublicidadLogoHabilitado = publicidad_logo_habilitado,
                PublicidadUrlLogo = publicidad_url_logo,
                PublicidadContactoHabilitado = publicidad_contacto_habilitado,
                PublicidadDomicilioHabilitado = publicidad_domicilio_habilitado,
                // Campos sociales (23-02-2026)
                CantidadLikes = cantidad_likes,
                UsuarioDioLike = usuario_dio_like,
                CantidadReenvios = cantidad_reenvios,
                UsuarioReenvio = usuario_reenvio,
                FlagEsReenvio = flag_es_reenvio,
                ReenvioUserIdAnonimizado = reenvio_user_id_anonimizado,
                CantidadVerdaderos = cantidad_verdaderos,
                CantidadFalsos = cantidad_falsos,
                Fotos = Fotos?.Select(f => new FotoDescripcionAlarmaDto
                {
                    FotoId = f.FotoId,
                    UrlFoto = f.UrlFoto,
                    ThumbnailUrl = f.ThumbnailUrl,
                    NombreArchivoOriginal = f.NombreArchivoOriginal,
                    TipoMime = f.TipoMime,
                    EsVideo = f.EsVideo,
                    TamanoBytes = f.TamanoBytes,
                    AnchoPixels = f.AnchoPixels,
                    AltoPixels = f.AltoPixels,
                    Orden = f.Orden,
                    FechaSubida = f.FechaSubida
                }).ToList() ?? new List<FotoDescripcionAlarmaDto>()
            };
        }


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

                try
                {
                    // Usar formato Alarm_{ID} igual que TipoAlarma.DescripcionTraducida
                    string key = $"Alarm_{tipoalarma_id}";
                    string traduccion = sospect.Resources.AppResources.ResourceManager.GetString(key,
                        CultureInfo.CurrentUICulture);

                    if (!string.IsNullOrEmpty(traduccion))
                        return traduccion;

                    // Fallback: valor crudo del API
                    return _descripciontipoalarma;
                }
                catch
                {
                    return _descripciontipoalarma;
                }
            }
            set => _descripciontipoalarma = value;
        }
        public long tipoalarma_id { get; set; }
        public string? color_fondo_feed { get; set; }
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
        public string? Descripcionalarma { get; set; }
        public bool flag_red_confianza { get; set; }

        // NUEVO: Radio configurado del usuario en metros (para filtrado client-side)
        public int? radio_alarmas_mts_actual { get; set; }

        // NUEVO: Radio de interés en metros parametrizado por tipo de alarma
        public int? radio_interes_metros { get; set; }

        /// <summary>
        /// ID de la categoría de alarma. 1=SEGURIDAD, 2=POLITICA, 3=INFRAESTRUCTURA, etc.
        /// Usado para decidir si mostrar el botón "Ver autoridades responsables".
        /// Implementa INPC propio para notificar también a MostrarAutoridadesResponsables.
        /// </summary>
        private int? _categoriaAlarmaId;
        public int? CategoriaAlarmaId
        {
            get => _categoriaAlarmaId;
            set
            {
                SetValue(ref _categoriaAlarmaId, value);
                OnPropertyChanged(nameof(MostrarAutoridadesResponsables));
            }
        }

        /// <summary>
        /// Solo SEGURIDAD (id=1) y POLITICA (id=2) muestran el botón de autoridades responsables.
        /// </summary>
        public bool MostrarAutoridadesResponsables =>
            CategoriaAlarmaId == 1 || CategoriaAlarmaId == 2;

        // NUEVO: Minutos de vigencia parametrizado por tipo de alarma
        public int? minutos_vigencia { get; set; }

        // NUEVO: Tipo de cierre para enrutamiento a pantalla correcta
        public string tipo_cierre { get; set; }

        // NUEVO: Usuario anonimizado para mostrar en tarjetas (XXX-YYYY)
        private string? _usuario_anonimizado;
        public string? usuario_anonimizado
        {
            get => _usuario_anonimizado;
            set
            {
                if (SetProperty(ref _usuario_anonimizado, value))
                {
                    OnPropertyChanged(nameof(UsuarioAnonimizadoFormateado));
                }
            }
        }

        // NUEVO: Usuario anonimizado formateado con etiqueta "Usuario: XXX-YYYY"
        public string UsuarioAnonimizadoFormateado
        {
            get
            {
                if (string.IsNullOrWhiteSpace(usuario_anonimizado))
                    return string.Empty;

                return $"{AppResources.LblUsuarioID} {usuario_anonimizado}";
            }
        }

        // NUEVO: Fotos de la alarma (primeras 5)
        private List<FotoDescripcionAlarma> _fotos;
        [JsonProperty("fotos_alarma")]
        public List<FotoDescripcionAlarma> Fotos
        {
            get => _fotos ?? new List<FotoDescripcionAlarma>();
            set
            {
                if (SetProperty(ref _fotos, value))
                {
                    // CRÍTICO: Notificar que las propiedades calculadas también cambiaron
                    OnPropertyChanged(nameof(TieneFotos));
                    OnPropertyChanged(nameof(CantidadFotos));
                }
            }
        }

        // NUEVO: Indica si la alarma tiene fotos
        public bool TieneFotos => Fotos?.Any() == true;

        // NUEVO: Cantidad de fotos (para el converter de tamaño)
        public int CantidadFotos => Fotos?.Count ?? 0;

        // NUEVO: Indica si hay más de 5 fotos (para mostrar overlay +N)
        public bool MoreThanFivePhotos => CantidadFotos > 5;

        // NUEVO: Cantidad de videos y fotos
        public int cantidad_videos { get; set; }
        public int cantidad_fotos { get; set; }

        // NUEVO: Ranking de relevancia (para feed Para Ti)
        public decimal ranking_relevancia { get; set; }

        // ===== FLAGS DE FILTRADO DESDE LA API (Críticos) =====
        // Estos flags vienen pre-calculados desde las vistas SQL
        // y determinan dónde debe mostrarse cada alarma

        /// <summary>
        /// Flag que indica si esta alarma debe mostrarse en el MAPA.
        /// Calculado en vw_busca_alarmas_por_zona2
        /// true = Alarma activa O cerrada en últimos 90 minutos
        /// </summary>
        public bool flag_visible_mapa { get; set; }

        /// <summary>
        /// Flag que indica si esta alarma debe mostrarse en feed "Siguiendo/Cerca de ti".
        /// Calculado en vw_notificacion_alarmas
        /// true = Alarma de seguridad personal (cercana, protegidos, propia, zona vigilancia)
        ///        Y (activa O cerrada en últimos 90 minutos)
        /// </summary>
        public bool flag_visible_siguiendo { get; set; }

        /// <summary>
        /// Flag que indica si esta alarma es de "seguridad personal" del usuario.
        /// Calculado en vw_alarmas_para_ti
        /// true = Alarma cercana, de protegidos, propia, o de zona de vigilancia
        /// false = Alarma viral/relevante según ranking (no es de seguridad personal)
        /// </summary>
        public bool flag_seguridad_personal { get; set; }

        /// <summary>
        /// Flag que indica si hay cambios pendientes de sincronizar con la API.
        /// Se usa para Optimistic UI cuando el usuario crea una alarma.
        /// true = La alarma fue creada localmente y aún no se confirmó con el servidor
        /// false = La alarma está sincronizada con el servidor
        /// </summary>
        public bool pendiente_sincronizacion { get; set; }

        // ===== CAMPOS TERRITORIALES (Fase 1 - Territorialización) =====
        // Extraídos de Google Reverse Geocoding

        /// <summary>
        /// Barrio/vecindario donde ocurrió la alarma (ej: "Chapinero", "El Poblado")
        /// </summary>
        public string barrio { get; set; }

        /// <summary>
        /// Ciudad donde ocurrió la alarma (ej: "Bogotá", "Medellín")
        /// </summary>
        public string ciudad { get; set; }

        /// <summary>
        /// País donde ocurrió la alarma (ej: "Colombia", "México")
        /// </summary>
        public string pais { get; set; }

        // ===== CAMPOS SOCIALES (23-02-2026 - Red social) =====

        private int _cantidad_likes;
        public int cantidad_likes
        {
            get => _cantidad_likes;
            set { if (SetProperty(ref _cantidad_likes, value)) OnPropertyChanged(nameof(LikesFormateado)); }
        }

        private bool _usuario_dio_like;
        public bool usuario_dio_like
        {
            get => _usuario_dio_like;
            set { if (SetProperty(ref _usuario_dio_like, value)) OnPropertyChanged(nameof(ColorIconoLike)); }
        }

        /// <summary>Color del icono de corazón: rojo si dio like, gris si no.</summary>
        public Microsoft.Maui.Graphics.Color ColorIconoLike =>
            usuario_dio_like
                ? Microsoft.Maui.Graphics.Color.FromArgb("#E74C3C")
                : Microsoft.Maui.Graphics.Color.FromArgb("#95A5A6");

        public string LikesFormateado => cantidad_likes > 0 ? cantidad_likes.ToString() : string.Empty;

        private int _cantidad_reenvios;
        public int cantidad_reenvios
        {
            get => _cantidad_reenvios;
            set { if (SetProperty(ref _cantidad_reenvios, value)) OnPropertyChanged(nameof(ReenviosFormateado)); }
        }

        private bool _usuario_reenvio;
        public bool usuario_reenvio
        {
            get => _usuario_reenvio;
            set { if (SetProperty(ref _usuario_reenvio, value)) OnPropertyChanged(nameof(ColorIconoReenvio)); }
        }

        /// <summary>Color del icono de reenvío: verde si reenvió, gris si no.</summary>
        public Microsoft.Maui.Graphics.Color ColorIconoReenvio =>
            usuario_reenvio
                ? Microsoft.Maui.Graphics.Color.FromArgb("#27AE60")
                : Microsoft.Maui.Graphics.Color.FromArgb("#95A5A6");

        public string ReenviosFormateado => cantidad_reenvios > 0 ? cantidad_reenvios.ToString() : string.Empty;

        public bool flag_es_reenvio { get; set; }
        public string? reenvio_user_id_anonimizado { get; set; }

        // Votos de veracidad (2026-02-26)
        private int _cantidad_verdaderos;
        public int cantidad_verdaderos
        {
            get => _cantidad_verdaderos;
            set { if (SetProperty(ref _cantidad_verdaderos, value)) { OnPropertyChanged(nameof(VerdaderosFormateado)); OnPropertyChanged(nameof(ColorIconoVerdadero)); } }
        }

        private int _cantidad_falsos;
        public int cantidad_falsos
        {
            get => _cantidad_falsos;
            set { if (SetProperty(ref _cantidad_falsos, value)) { OnPropertyChanged(nameof(FalsosFormateado)); OnPropertyChanged(nameof(ColorIconoFalso)); } }
        }

        private bool _usuario_voto_verdadero;
        public bool usuario_voto_verdadero
        {
            get => _usuario_voto_verdadero;
            set { if (SetProperty(ref _usuario_voto_verdadero, value)) OnPropertyChanged(nameof(ColorIconoVerdadero)); }
        }

        private bool _usuario_voto_falso;
        public bool usuario_voto_falso
        {
            get => _usuario_voto_falso;
            set { if (SetProperty(ref _usuario_voto_falso, value)) OnPropertyChanged(nameof(ColorIconoFalso)); }
        }

        public string VerdaderosFormateado => cantidad_verdaderos > 0 ? cantidad_verdaderos.ToString() : string.Empty;
        public string FalsosFormateado => cantidad_falsos > 0 ? cantidad_falsos.ToString() : string.Empty;

        /// <summary>Color del icono de verdadero: verde brillante si el usuario votó verdadero, verde suave si no.</summary>
        public Microsoft.Maui.Graphics.Color ColorIconoVerdadero =>
            usuario_voto_verdadero
                ? Microsoft.Maui.Graphics.Color.FromArgb("#1E8449")
                : Microsoft.Maui.Graphics.Color.FromArgb("#27AE60");

        /// <summary>Color del icono de falso: rojo brillante si el usuario votó falso, rojo suave si no.</summary>
        public Microsoft.Maui.Graphics.Color ColorIconoFalso =>
            usuario_voto_falso
                ? Microsoft.Maui.Graphics.Color.FromArgb("#922B21")
                : Microsoft.Maui.Graphics.Color.FromArgb("#E74C3C");

        // ===== CAMPOS PROMOCIONALES (Fase 2 - Monetización) =====

        /// <summary>
        /// Indica si esta alarma es de tipo publicitario/promocional
        /// </summary>
        public bool is_advertising { get; set; }

        /// <summary>
        /// Radio en metros de la promoción (si es alarma publicitaria)
        /// </summary>
        public int? publicidad_radio_metros { get; set; }

        /// <summary>
        /// Indica si el proveedor habilitó logo en la promoción
        /// </summary>
        public bool publicidad_logo_habilitado { get; set; }

        /// <summary>
        /// URL del logo del negocio en S3 (si está habilitado)
        /// </summary>
        public string publicidad_url_logo { get; set; }

        /// <summary>
        /// Indica si el proveedor habilitó chat privado para esta promoción (+5 poderes)
        /// </summary>
        public bool publicidad_contacto_habilitado { get; set; }

        /// <summary>
        /// Indica si el proveedor habilitó pedidos a domicilio para esta promoción (+5 poderes)
        /// </summary>
        public bool publicidad_domicilio_habilitado { get; set; }

        /// <summary>
        /// Texto personalizado de la notificación push enviada con esta promoción
        /// </summary>
        public string publicidad_texto_push { get; set; }

        /// <summary>
        /// Nombre del emprendimiento que lanzó esta promoción
        /// </summary>
        public string publicidad_nombre_emprendimiento { get; set; }

        private string _ubicacionTerritorial = string.Empty;
        /// <summary>
        /// Ubicación territorial formateada como "Barrio - Ciudad - Pais"
        /// Solo muestra los valores no nulos, separados por " - "
        /// </summary>
        public string UbicacionTerritorial
        {
            get => _ubicacionTerritorial;
            set => SetValue(ref _ubicacionTerritorial, value);
        }

        /// <summary>
        /// Actualiza la ubicación territorial formateada a partir de barrio, ciudad y pais.
        /// Solo asigna si el valor calculado es distinto al actual, evitando PropertyChanged innecesarios
        /// que generan presión de GC durante el scroll del CollectionView.
        /// </summary>
        public void ActualizarUbicacionTerritorial()
        {
            var partes = new List<string>();

            if (!string.IsNullOrWhiteSpace(barrio))
                partes.Add(barrio);

            if (!string.IsNullOrWhiteSpace(ciudad))
                partes.Add(ciudad);

            if (!string.IsNullOrWhiteSpace(pais))
                partes.Add(pais);

            var nuevo = partes.Any() ? string.Join(" - ", partes) : string.Empty;

            // Guard: no disparar PropertyChanged si el valor no cambió
            if (nuevo != _ubicacionTerritorial)
                UbicacionTerritorial = nuevo;
        }

        // ===== COMANDO PARA ABRIR CARRUSEL DE FOTOS =====
        // Vive en el modelo para evitar problemas de binding en DataTemplates independientes
        public ICommand AbrirCarruselFotosCommand => new Command<FotoDescripcionAlarma>(async (foto) =>
        {
            if (foto == null || Fotos == null || !Fotos.Any()) return;

            try
            {
                int indiceInicial = Fotos.FindIndex(f => f.FotoId == foto.FotoId);
                if (indiceInicial < 0) indiceInicial = 0;

                string textoDescripcion = Descripcionalarma ?? string.Empty;
                DateTime fechaAlarma = fecha_alarma.GetValueOrDefault();

                var currentPage = GetCurrentPage();
                if (currentPage != null)
                {
                    var popup = new VerFotoPopup(Fotos, indiceInicial, textoDescripcion, fechaAlarma);
                    await currentPage.ShowPopupAsync(popup);
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "AlarmaCercana", "AbrirCarruselFotosCommand");
            }
        });

        private string _credibilidadAlarma;
        public string CredibilidadAlarma
        {
            get => _credibilidadAlarma;
            set => SetValue(ref _credibilidadAlarma, value);
        }

        // Campo para detectar si calificacion_alarma cambió desde el último cálculo
        private decimal? _lastCalificacionCalculada = null;

        /// <summary>
        /// Calcula el texto de credibilidad de la alarma.
        /// Solo recalcula y asigna si calificacion_alarma cambió desde la última vez,
        /// evitando PropertyChanged innecesarios y presión de GC durante scroll.
        /// </summary>
        public void CalcularCredibilidad()
        {
            // Guard: no recalcular si el valor de entrada no cambió
            if (_lastCalificacionCalculada.HasValue && _lastCalificacionCalculada == calificacion_alarma)
                return;

            var LblCredibilidadAlarmaActual = TranslateExtension.Translate("LblCredibilidadAlarmaActual");
            var nuevo = $"{LblCredibilidadAlarmaActual} {calificacion_alarma}%";

            CredibilidadAlarma = nuevo;
            _lastCalificacionCalculada = calificacion_alarma;
        }

        /// <summary>
        /// Color de fondo calculado con prioridad:
        /// 1. Verde si flag_red_confianza = true
        /// 2. Color que viene directo del JSON (color_fondo_feed)
        /// 3. Gris claro por defecto (#F5F5F5)
        /// </summary>
        public Microsoft.Maui.Graphics.Color BackgroundColorFeed
        {
            get
            {
                // Prioridad 1: Red de confianza siempre verde
                if (flag_red_confianza)
                    return Microsoft.Maui.Graphics.Color.FromArgb("#F0FFF0");

                // Prioridad 2: Color que viene directo del JSON
                if (!string.IsNullOrWhiteSpace(color_fondo_feed))
                {
                    try
                    {
                        return Microsoft.Maui.Graphics.Color.FromArgb(color_fondo_feed);
                    }
                    catch
                    {
                        // Si el formato de color es inválido, usar gris claro
                        return Microsoft.Maui.Graphics.Color.FromArgb("#F5F5F5");
                    }
                }

                // Prioridad 3: Gris claro por defecto
                return Microsoft.Maui.Graphics.Color.FromArgb("#F5F5F5");
            }
        }

        /// <summary>
        /// Obtiene la página actualmente visible navegando por NavigationPage/TabbedPage/FlyoutPage.
        /// Necesario en iOS donde App.Current.MainPage es la raíz, no la página activa.
        /// </summary>
        private static Page GetCurrentPage()
        {
            var mainPage = Application.Current?.MainPage;
            Page currentPage = mainPage;
            while (currentPage != null)
            {
                if (currentPage is NavigationPage navPage && navPage.CurrentPage != null)
                    currentPage = navPage.CurrentPage;
                else if (currentPage is TabbedPage tabbedPage && tabbedPage.CurrentPage != null)
                    currentPage = tabbedPage.CurrentPage;
                else if (currentPage is FlyoutPage flyoutPage && flyoutPage.Detail != null)
                    currentPage = flyoutPage.Detail;
                else
                    break;
            }
            return currentPage;
        }

        // ===== VOTACIÓN DE CIERRE COMUNITARIO (2026-03-29) =====

        /// <summary>
        /// TRUE si la alarma tiene una solicitud de cierre activa (dentro del período de 24h de votación).
        /// Viene directamente de vw_alarmas_completa / vw_alarmas_para_ti vía DTO.
        /// </summary>
        private bool _tieneVotacionActiva;
        public bool TieneVotacionActiva
        {
            get => _tieneVotacionActiva;
            set
            {
                _tieneVotacionActiva = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneVotacionActivaSinVoto));
            }
        }

        /// <summary>
        /// TRUE si el usuario consultante ya emitió su voto en la solicitud de cierre activa.
        /// Viene directamente de vw_alarmas_completa / vw_alarmas_para_ti vía DTO.
        /// </summary>
        private bool _usuarioYaVoto;
        public bool UsuarioYaVoto
        {
            get => _usuarioYaVoto;
            set
            {
                _usuarioYaVoto = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneVotacionActivaSinVoto));
            }
        }

        /// <summary>
        /// TRUE si hay votación activa Y el usuario aún no ha votado.
        /// Usado para binding directo en los templates XAML de las tarjetas.
        /// </summary>
        public bool TieneVotacionActivaSinVoto => TieneVotacionActiva && !UsuarioYaVoto;

        /// <summary>
        /// TRUE si el usuario puede calificar como verdadera/falsa esta alarma:
        /// no es el propietario Y no hay votación de cierre activa.
        /// Reemplaza el binding manual a flag_propietario_alarma en los templates del feed.
        /// </summary>
        public bool PuedeCalificarVeracidad => !flag_propietario_alarma && !TieneVotacionActiva;
    }
}



