// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Controls;
using Newtonsoft.Json;
using sospect.Helpers;

#if ANDROID
using Android.Graphics;
#endif

namespace sospect.Models
{
    /// <summary>
    /// Modelo de TipoAlarma con soporte para iconos desde API y carga nativa de recursos
    /// </summary>
    public class TipoAlarma : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // ==================== PROPIEDADES DEL API ====================

        private int _tipoalarmaId;
        [JsonProperty("tipoalarma_id")] // API retorna snake_case
        public int TipoalarmaId
        {
            get => _tipoalarmaId;
            set
            {
                _tipoalarmaId = value;
                OnPropertyChanged();
            }
        }

        private string _descripciontipoalarma;
        [JsonProperty("descripciontipoalarma")]
        public string Descripciontipoalarma
        {
            get => _descripciontipoalarma;
            set
            {
                _descripciontipoalarma = value;
                OnPropertyChanged();
            }
        }

        private string _icono;
        [JsonProperty("icono")]
        public string Icono
        {
            get => _icono;
            set
            {
                _icono = value;
                _iconoPathSinExtensionCache = null; // OPTIMIZACIÓN: Invalidar caché al cambiar icono
                OnPropertyChanged();
                OnPropertyChanged(nameof(IconoPathSinExtension));
            }
        }

        private string _colorFondoFeed;
        [JsonProperty("color_fondo_feed")]
        public string ColorFondoFeed
        {
            get => _colorFondoFeed;
            set
            {
                _colorFondoFeed = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ColorFondoFeedMaui));
            }
        }

        // ==================== PROPIEDADES COMPUTADAS ====================

        // OPTIMIZACIÓN: Caché para evitar 1,284+ llamadas repetidas a IconoPathSinExtension
        private string _iconoPathSinExtensionCache = null;

        /// <summary>
        /// Retorna el nombre del icono sin extensión (para usar en bindings)
        /// OPTIMIZADO: Usa caché para evitar cálculos repetidos (reducción ~99%)
        /// </summary>
        public string IconoPathSinExtension
        {
            get
            {
                // Retornar caché si existe
                if (_iconoPathSinExtensionCache != null)
                    return _iconoPathSinExtensionCache;

                if (string.IsNullOrEmpty(Icono))
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"TipoAlarma.IconoPathSinExtension: Icono es NULL para tipo {TipoalarmaId}");
#endif
                    _iconoPathSinExtensionCache = "sospecticon"; // Fallback
                    return _iconoPathSinExtensionCache;
                }

                _iconoPathSinExtensionCache = Icono
                    .Replace(".png", "")
                    .Replace(".PNG", "")
                    .ToLower();

#if DEBUG
                // Solo logear una vez al calcular (no en cada acceso)
                System.Diagnostics.Debug.WriteLine($"TipoAlarma.IconoPathSinExtension: ID={TipoalarmaId}, Icono calculado='{_iconoPathSinExtensionCache}'");
#endif

                return _iconoPathSinExtensionCache;
            }
        }

        /// <summary>
        /// Descripción traducida usando AppResources basada en el ID del tipo de alarma.
        /// Si no se encuentra la traducción, usa el valor de la base de datos como fallback.
        /// </summary>
        public string DescripcionTraducida
        {
            get
            {
                try
                {
                    // Intentar obtener la traducción desde AppResources usando la clave Alarm_{ID}
                    string key = $"Alarm_{TipoalarmaId}";
                    string traduccion = sospect.Resources.AppResources.ResourceManager.GetString(key,
                        System.Globalization.CultureInfo.CurrentCulture);

                    // Si se encontró la traducción, usarla
                    if (!string.IsNullOrEmpty(traduccion))
                    {
                        System.Diagnostics.Debug.WriteLine($"TipoAlarma: Traducción encontrada para {key}: {traduccion}");
                        return traduccion;
                    }

                    // Si no se encuentra traducción, usar el valor de la base de datos
                    System.Diagnostics.Debug.WriteLine($"TipoAlarma: No se encontró traducción para {key}, usando valor de BD: {Descripciontipoalarma}");
                    return Descripciontipoalarma ?? string.Empty;
                }
                catch (Exception ex)
                {
                    // En caso de error, usar el valor de la base de datos
                    System.Diagnostics.Debug.WriteLine($"TipoAlarma: Error obteniendo traducción: {ex.Message}");
                    return Descripciontipoalarma ?? string.Empty;
                }
            }
        }

        /// <summary>
        /// Retorna el color de fondo como Color de MAUI. Si no está definido usa gris claro.
        /// </summary>
        public Microsoft.Maui.Graphics.Color ColorFondoFeedMaui
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ColorFondoFeed))
                    return Microsoft.Maui.Graphics.Color.FromArgb("#F5F5F5"); // Gris claro por defecto

                try
                {
                    return Microsoft.Maui.Graphics.Color.FromArgb(ColorFondoFeed);
                }
                catch
                {
                    return Microsoft.Maui.Graphics.Color.FromArgb("#F5F5F5"); // Fallback gris claro
                }
            }
        }

        // ==================== IMAGESOURCE PRE-CARGADO (OPCIONAL - PLAN B) ====================

        private ImageSource _iconoImageSource;

        /// <summary>
        /// ImageSource pre-cargado del icono (para usar en CollectionView si el Converter falla)
        /// </summary>
        public ImageSource IconoImageSource
        {
            get
            {
                if (_iconoImageSource == null && !string.IsNullOrEmpty(IconoPathSinExtension))
                {
                    _iconoImageSource = CargarIcono(IconoPathSinExtension);
                }
                return _iconoImageSource;
            }
            set
            {
                _iconoImageSource = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Carga el icono desde recursos nativos de Android (Plan B si el Converter falla)
        /// </summary>
        private ImageSource CargarIcono(string iconName)
        {
            if (string.IsNullOrEmpty(iconName))
            {
                System.Diagnostics.Debug.WriteLine("TipoAlarma.CargarIcono: iconName vacío");
                return null;
            }

#if ANDROID
            try
            {
                var context = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity ?? 
                             Microsoft.Maui.ApplicationModel.Platform.AppContext;
                
                if (context == null)
                {
                    System.Diagnostics.Debug.WriteLine("TipoAlarma.CargarIcono: Context es null");
                    return null;
                }

                var cleanName = iconName.ToLower();
                System.Diagnostics.Debug.WriteLine($"TipoAlarma.CargarIcono: Buscando '{cleanName}' para tipo {TipoalarmaId}");

                // Buscar recurso
                var resourceId = context.Resources.GetIdentifier(cleanName, "drawable", context.PackageName);
                
                if (resourceId == 0)
                {
                    resourceId = context.Resources.GetIdentifier(cleanName, "mipmap", context.PackageName);
                }

                if (resourceId == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"TipoAlarma.CargarIcono:  Recurso '{cleanName}' NO encontrado");
                    
                    // Fallback
                    resourceId = context.Resources.GetIdentifier("sospecticon", "drawable", context.PackageName);
                    if (resourceId == 0)
                    {
                        resourceId = context.Resources.GetIdentifier("sospecticon", "mipmap", context.PackageName);
                    }
                    
                    if (resourceId == 0)
                    {
                        System.Diagnostics.Debug.WriteLine("TipoAlarma.CargarIcono:  Fallback 'sospecticon' tampoco encontrado");
                        return null;
                    }
                    
                    System.Diagnostics.Debug.WriteLine("TipoAlarma.CargarIcono: Usando fallback 'sospecticon'");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"TipoAlarma.CargarIcono:  Recurso '{cleanName}' encontrado! ID={resourceId}");
                }

                // Crear ImageSource desde URI del recurso
                var resourceUri = $"android.resource://{context.PackageName}/{resourceId}";
                var imageSource = ImageSource.FromUri(new Uri(resourceUri));
                
                System.Diagnostics.Debug.WriteLine($"TipoAlarma.CargarIcono:  ImageSource creado para tipo {TipoalarmaId}");
                
                return imageSource;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TipoAlarma.CargarIcono ERROR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                CrashlyticsHelper.LogError(ex, "TipoAlarma", "CargarIcono");
                return null;
            }
#else
            // En iOS u otras plataformas
            System.Diagnostics.Debug.WriteLine($"TipoAlarma.CargarIcono: Plataforma no-Android, usando '{iconName}.png'");
            return ImageSource.FromFile($"{iconName}.png");
#endif
        }

        /// <summary>
        /// Método público para forzar recarga del icono si es necesario
        /// </summary>
        public void RecargarIcono()
        {
            System.Diagnostics.Debug.WriteLine($"TipoAlarma.RecargarIcono: Forzando recarga para tipo {TipoalarmaId}");
            _iconoImageSource = null;
            OnPropertyChanged(nameof(IconoImageSource));
        }

        // ==================== INOTIFYPROPERTYCHANGED ====================

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // ==================== CONSTRUCTORES ====================

        public TipoAlarma()
        {
        }

        public TipoAlarma(int id, string descripcion, string icono)
        {
            TipoalarmaId = id;
            Descripciontipoalarma = descripcion;
            Icono = icono;
        }

        // ==================== MÉTODOS AUXILIARES ====================

        public override string ToString()
        {
            return $"TipoAlarma[ID={TipoalarmaId}, Descripcion={Descripciontipoalarma}, Icono={Icono}]";
        }
    }
}

