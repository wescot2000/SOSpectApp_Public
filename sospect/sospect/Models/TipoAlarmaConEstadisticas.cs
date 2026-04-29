using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace sospect.Models
{
    /// <summary>
    /// Modelo de TipoAlarma con estadísticas de los últimos 30 días para el filtro de alarmas
    /// Fecha: 2025-12-20
    /// Referencia: ManualGeneralSOSpect.md [CAMBIO - 19-12-2025 14:00]
    ///
    /// IMPORTANTE: Este modelo sobrescribe TipoalarmaId para soportar AMBOS formatos JSON:
    /// - "tipoalarma_id" (snake_case) del endpoint ListarTiposAlarma
    /// - "TipoAlarmaId" (PascalCase) del endpoint ListarTiposAlarmaConEstadisticas
    /// </summary>
    public class TipoAlarmaConEstadisticas : TipoAlarma
    {
        // ==================== OVERRIDE PARA SOPORTAR AMBOS FORMATOS ====================

        /// <summary>
        /// Propiedad que acepta el formato PascalCase del endpoint ListarTiposAlarmaConEstadisticas.
        /// Esta propiedad sincroniza con la propiedad base TipoalarmaId para mantener compatibilidad.
        /// </summary>
        [JsonProperty("TipoAlarmaId")]  // Formato del endpoint ListarTiposAlarmaConEstadisticas (PascalCase)
        public int TipoAlarmaId
        {
            get => base.TipoalarmaId;
            set
            {
                base.TipoalarmaId = value;  // Sincronizar con la propiedad base
                OnPropertyChanged();
                OnPropertyChanged(nameof(TipoalarmaId)); // Notificar ambas propiedades
            }
        }

        // ==================== PROPIEDADES HEREDADAS CON FORMATO DIFERENTE ====================

        /// <summary>
        /// Propiedad que acepta el formato PascalCase para descripción del tipo de alarma.
        /// Sincroniza con la propiedad base Descripciontipoalarma.
        /// </summary>
        [JsonProperty("DescripcionTipoAlarma")]
        public string DescripcionTipoAlarma
        {
            get => base.Descripciontipoalarma;
            set
            {
                base.Descripciontipoalarma = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Descripciontipoalarma));
            }
        }

        /// <summary>
        /// Propiedad que acepta el formato PascalCase para icono.
        /// Sincroniza con la propiedad base Icono.
        /// </summary>
        [JsonProperty("Icono")]
        public string IconoPascalCase
        {
            get => base.Icono;
            set
            {
                base.Icono = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Icono));
                OnPropertyChanged(nameof(IconoPathSinExtension));
            }
        }

        // ==================== PROPIEDADES ADICIONALES DEL API ====================

        private string _shortAlias;
        [JsonProperty("ShortAlias")]  // API retorna PascalCase
        public string ShortAlias
        {
            get => _shortAlias;
            set
            {
                _shortAlias = value;
                OnPropertyChanged();
            }
        }

        private int? _radioInteresMetros;
        [JsonProperty("RadioInteresMetros")]  // API retorna PascalCase
        public int? RadioInteresMetros
        {
            get => _radioInteresMetros;
            set
            {
                _radioInteresMetros = value;
                OnPropertyChanged();
            }
        }

        private int? _minutosVigencia;
        [JsonProperty("MinutosVigencia")]  // API retorna PascalCase
        public int? MinutosVigencia
        {
            get => _minutosVigencia;
            set
            {
                _minutosVigencia = value;
                OnPropertyChanged();
            }
        }

        private int _cantidadUltimoMes;
        [JsonProperty("CantidadUltimoMes")]  // API retorna PascalCase
        public int CantidadUltimoMes
        {
            get => _cantidadUltimoMes;
            set
            {
                _cantidadUltimoMes = value;
                OnPropertyChanged();
            }
        }

        private decimal _porcentajeUltimoMes;
        [JsonProperty("PorcentajeUltimoMes")]  // API retorna PascalCase
        public decimal PorcentajeUltimoMes
        {
            get => _porcentajeUltimoMes;
            set
            {
                _porcentajeUltimoMes = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PorcentajeFormateado));
            }
        }

        private bool _esCaracteristico;
        [JsonProperty("EsCaracteristico")]  // API retorna PascalCase
        public bool EsCaracteristico
        {
            get => _esCaracteristico;
            set
            {
                _esCaracteristico = value;
                OnPropertyChanged();
            }
        }

        private bool _visibleEnAppAndroid = true;
        [JsonProperty("VisibleEnAppAndroid")]  // API retorna PascalCase
        public bool VisibleEnAppAndroid
        {
            get => _visibleEnAppAndroid;
            set
            {
                _visibleEnAppAndroid = value;
                OnPropertyChanged();
            }
        }

        private bool _visibleEnAppIos = true;
        [JsonProperty("VisibleEnAppIos")]  // API retorna PascalCase
        public bool VisibleEnAppIos
        {
            get => _visibleEnAppIos;
            set
            {
                _visibleEnAppIos = value;
                OnPropertyChanged();
            }
        }

        private bool _requiereMensajeAdvertenciaAndroid;
        [JsonProperty("RequiereMensajeAdvertenciaAndroid")]  // API retorna PascalCase
        public bool RequiereMensajeAdvertenciaAndroid
        {
            get => _requiereMensajeAdvertenciaAndroid;
            set
            {
                _requiereMensajeAdvertenciaAndroid = value;
                OnPropertyChanged();
            }
        }

        private bool _requiereMensajeAdvertenciaIos;
        [JsonProperty("RequiereMensajeAdvertenciaIos")]  // API retorna PascalCase
        public bool RequiereMensajeAdvertenciaIos
        {
            get => _requiereMensajeAdvertenciaIos;
            set
            {
                _requiereMensajeAdvertenciaIos = value;
                OnPropertyChanged();
            }
        }

        // ==================== PROPIEDADES PARA UI/FILTRO ====================

        private bool _estaHabilitado = true;
        /// <summary>
        /// Indica si este tipo está habilitado en el filtro (visible en mapa y "Para ti")
        /// NO afecta a "Siguiendo / En tu área"
        /// </summary>
        public bool EstaHabilitado
        {
            get => _estaHabilitado;
            set
            {
                _estaHabilitado = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Porcentaje formateado para mostrar en UI (ej: "12.5%")
        /// </summary>
        public string PorcentajeFormateado
        {
            get => $"{PorcentajeUltimoMes:0.0}%";
        }

        /// <summary>
        /// Texto combinado para mostrar en el filtro: "Crimen 45.3%"
        /// </summary>
        public string TextoFiltro
        {
            get => $"{ShortAliasTraducido} {PorcentajeFormateado}";
        }

        /// <summary>
        /// Short Alias traducido usando AppResources.
        /// Usa el texto del ShortAlias sin espacios como key (mismo patrón que DescripcionTraducida).
        /// Si no se encuentra la traducción, usa el valor de la base de datos.
        /// </summary>
        public string ShortAliasTraducido
        {
            get
            {
                try
                {
                    if (string.IsNullOrEmpty(ShortAlias))
                    {
                        System.Diagnostics.Debug.WriteLine($"[TipoAlarmaConEstadisticas] ShortAlias es NULL para tipo {TipoalarmaId}");
                        return string.Empty;
                    }

                    // Crear la key quitando espacios del ShortAlias (mismo patrón que las descripciones largas)
                    string key = ShortAlias.Replace(" ", "");

                    string traduccion = sospect.Resources.AppResources.ResourceManager.GetString(key,
                        System.Globalization.CultureInfo.CurrentCulture);

                    // Si se encontró la traducción, usarla
                    if (!string.IsNullOrEmpty(traduccion))
                    {
                        System.Diagnostics.Debug.WriteLine($"[TipoAlarmaConEstadisticas] ✅ Traducción ShortAlias encontrada para '{key}': {traduccion}");
                        return traduccion;
                    }

                    // Si no se encuentra traducción, usar el valor de la base de datos
                    System.Diagnostics.Debug.WriteLine($"[TipoAlarmaConEstadisticas] ⚠️ No se encontró traducción para '{key}', usando valor de BD: {ShortAlias}");
                    return ShortAlias;
                }
                catch (Exception ex)
                {
                    // En caso de error, usar el valor de la base de datos
                    System.Diagnostics.Debug.WriteLine($"[TipoAlarmaConEstadisticas] ❌ Error obteniendo traducción ShortAlias: {ex.Message}");
                    return ShortAlias ?? string.Empty;
                }
            }
        }

        // ==================== CONSTRUCTORES ====================

        public TipoAlarmaConEstadisticas() : base()
        {
        }

        // ==================== MÉTODOS AUXILIARES ====================

        public override string ToString()
        {
            return $"TipoAlarmaConEstadisticas[ID={TipoalarmaId}, ShortAlias={ShortAlias}, " +
                   $"Porcentaje={PorcentajeFormateado}, Habilitado={EstaHabilitado}]";
        }
    }
}
