using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using sospect.Converters;
using sospect.Helpers;
using sospect.Models;

namespace sospect.Views.Popups
{
    /// <summary>
    /// Popup mejorado para seleccionar tipo de alarma.
    /// Basado en el diseño del FiltroAlarmasPopup con tarjetas visuales y porcentajes.
    /// Muestra:
    /// - Tipos característicos (Warning y Crime) arriba
    /// - Otros tipos ordenados por popularidad (porcentaje) en el medio
    /// - Promoción Local separado al final (uso comercial)
    /// </summary>
    public partial class TipoAlarmaPickerPopup : Popup
    {
        public ObservableCollection<TipoAlarma> TiposAlarma { get; set; }
        public TipoAlarma TipoAlarmaSeleccionado { get; set; }
        public ICommand SelectTipoAlarmaCommand { get; }

        public event EventHandler<TipoAlarma> TipoAlarmaSelected;

        private bool _isClosing = false;
        private const int ID_PROMOCION_LOCAL = 13;

        // Flag para indicar si debe usar exactamente la lista pasada (sin usar App.TiposAlarmaDisponibles)
        private bool _usarListaExacta = false;

        /// <summary>
        /// Constructor principal - permite usar App.TiposAlarmaDisponibles si está disponible
        /// </summary>
        public TipoAlarmaPickerPopup(ObservableCollection<TipoAlarma> tiposAlarma, TipoAlarma tipoSeleccionado)
            : this(tiposAlarma, tipoSeleccionado, usarListaExacta: false)
        {
        }

        /// <summary>
        /// Constructor con opción de usar lista exacta - útil cuando se quiere filtrar tipos específicos
        /// </summary>
        /// <param name="tiposAlarma">Lista de tipos de alarma a mostrar</param>
        /// <param name="tipoSeleccionado">Tipo actualmente seleccionado</param>
        /// <param name="usarListaExacta">Si es true, usa solo los tipos de la lista pasada sin consultar App.TiposAlarmaDisponibles</param>
        public TipoAlarmaPickerPopup(ObservableCollection<TipoAlarma> tiposAlarma, TipoAlarma tipoSeleccionado, bool usarListaExacta)
        {
            InitializeComponent();

            TiposAlarma = tiposAlarma;
            TipoAlarmaSeleccionado = tipoSeleccionado;
            _usarListaExacta = usarListaExacta;

            SelectTipoAlarmaCommand = new RelayCommand<TipoAlarma>(OnTipoAlarmaSelected);

            BindingContext = this;

            CargarTipos();
        }

        /// <summary>
        /// Carga y organiza los tipos de alarma en las diferentes secciones del popup
        /// </summary>
        private void CargarTipos()
        {
            try
            {
                var tiposConEstadisticas = new System.Collections.Generic.List<TipoAlarmaConEstadisticas>();

                // Si se solicita usar la lista exacta, respetar el filtrado del llamador (convenio/plataforma)
                // pero ENRIQUECER con estadísticas de App.TiposAlarmaDisponibles si están disponibles,
                // para que los porcentajes se muestren correctamente.
                if (_usarListaExacta)
                {
                    if (TiposAlarma != null && TiposAlarma.Count > 0)
                    {
                        if (App.TiposAlarmaDisponibles != null && App.TiposAlarmaDisponibles.Count > 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"[TipoAlarmaPicker] 🎯 Lista EXACTA ({TiposAlarma.Count} tipos) enriquecida con estadísticas de App.TiposAlarmaDisponibles");
                            tiposConEstadisticas = TiposAlarma
                                .Select(t =>
                                {
                                    // Si ya es TipoAlarmaConEstadisticas (con datos reales), usar directamente
                                    if (t is TipoAlarmaConEstadisticas tce) return tce;
                                    // Buscar en App.TiposAlarmaDisponibles para obtener estadísticas reales
                                    var conEst = App.TiposAlarmaDisponibles
                                        .FirstOrDefault(d => d.TipoalarmaId == t.TipoalarmaId);
                                    return conEst ?? ConvertirATipoConEstadisticas(t);
                                })
                                .ToList();
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[TipoAlarmaPicker] 🎯 Lista EXACTA ({TiposAlarma.Count} tipos) sin estadísticas disponibles");
                            tiposConEstadisticas = TiposAlarma
                                .Select(t => t as TipoAlarmaConEstadisticas ?? ConvertirATipoConEstadisticas(t))
                                .ToList();
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[TipoAlarmaPicker] ❌ Lista exacta solicitada pero está vacía");
                        return;
                    }
                }
                // PRIORIDAD 1: Usar App.TiposAlarmaDisponibles si está disponible (tiene estadísticas)
                else if (App.TiposAlarmaDisponibles != null && App.TiposAlarmaDisponibles.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[TipoAlarmaPicker] ✅ Usando App.TiposAlarmaDisponibles ({App.TiposAlarmaDisponibles.Count} tipos)");
                    tiposConEstadisticas = App.TiposAlarmaDisponibles.ToList();
                }
                // FALLBACK: Si no hay tipos con estadísticas, usar los que vienen del parámetro
                else if (TiposAlarma != null && TiposAlarma.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[TipoAlarmaPicker] ⚠️ Usando TiposAlarma básicos ({TiposAlarma.Count} tipos) - sin estadísticas");
                    tiposConEstadisticas = TiposAlarma
                        .Select(t => t as TipoAlarmaConEstadisticas ?? ConvertirATipoConEstadisticas(t))
                        .ToList();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[TipoAlarmaPicker] ❌ No hay tipos disponibles");
                    return;
                }

                // Filtrar según la plataforma (iOS o Android).
                // EXCEPCIÓN: Si se usa lista exacta (usarListaExacta:true), el llamador ya aplicó
                // el filtro de convenio correctamente en LanzarAlarmaViewModel; no volver a filtrar
                // por VisibleEnAppIos para no bloquear tipos habilitados por convenio (ej. crimen id=2).
                bool esIOS = DeviceInfo.Platform == DevicePlatform.iOS;
                System.Collections.Generic.List<TipoAlarmaConEstadisticas> tiposFiltrados;
                if (_usarListaExacta)
                {
                    tiposFiltrados = tiposConEstadisticas;
                    System.Diagnostics.Debug.WriteLine($"[TipoAlarmaPicker] Lista exacta: sin filtro VisibleEnApp. Tipos: {tiposFiltrados.Count}");
                }
                else
                {
                    tiposFiltrados = tiposConEstadisticas
                        .Where(t => esIOS ? t.VisibleEnAppIos : t.VisibleEnAppAndroid)
                        .ToList();
                    System.Diagnostics.Debug.WriteLine($"[TipoAlarmaPicker] Plataforma: {(esIOS ? "iOS" : "Android")}, Tipos filtrados: {tiposFiltrados.Count}/{tiposConEstadisticas.Count}");
                }

                // Separar en categorías
                var caracteristicos = tiposFiltrados
                    .Where(t => t.EsCaracteristico && t.TipoalarmaId != ID_PROMOCION_LOCAL)
                    .OrderBy(t => t.TipoalarmaId) // Warning(1) primero, luego Crime(2)
                    .ToList();

                var promocion = tiposFiltrados
                    .FirstOrDefault(t => t.TipoalarmaId == ID_PROMOCION_LOCAL);

                var otros = tiposFiltrados
                    .Where(t => !t.EsCaracteristico && t.TipoalarmaId != ID_PROMOCION_LOCAL)
                    .OrderByDescending(t => t.PorcentajeUltimoMes)
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"[TipoAlarmaPicker] Característicos: {caracteristicos.Count}, Otros: {otros.Count}, Promoción: {(promocion != null ? "Sí" : "No")}");

                // Poblar las secciones
                PoblarTiposCaracteristicos(caracteristicos);
                PoblarOtrosTipos(otros);

                if (promocion != null)
                {
                    PoblarPromocion(promocion);
                    StackPromocion.IsVisible = true;
                }
                else
                {
                    StackPromocion.IsVisible = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TipoAlarmaPicker] ❌ Error cargando tipos: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "TipoAlarmaPickerPopup", "CargarTipos");
            }
        }

        /// <summary>
        /// Convierte un TipoAlarma básico a TipoAlarmaConEstadisticas
        /// (para compatibilidad con tipos que no vienen del endpoint con estadísticas)
        /// </summary>
        private TipoAlarmaConEstadisticas ConvertirATipoConEstadisticas(TipoAlarma tipo)
        {
            return new TipoAlarmaConEstadisticas
            {
                TipoalarmaId = tipo.TipoalarmaId,
                Descripciontipoalarma = tipo.Descripciontipoalarma,
                Icono = tipo.Icono,
                ShortAlias = tipo.Descripciontipoalarma, // Usar descripción si no hay short alias
                PorcentajeUltimoMes = 0,
                EsCaracteristico = tipo.TipoalarmaId == 1 || tipo.TipoalarmaId == 2 // Warning y Crime
            };
        }

        /// <summary>
        /// Crea un Frame seleccionable para un tipo de alarma
        /// </summary>
        private Frame CrearTarjetaTipo(TipoAlarmaConEstadisticas tipo, bool esUnicaColumna = false)
        {
            // Frame contenedor
            var frame = new Frame
            {
                CornerRadius = 8,
                Padding = 12,
                BackgroundColor = TipoAlarmaSeleccionado?.TipoalarmaId == tipo.TipoalarmaId
                    ? Color.FromArgb("#E3F2FD")  // Azul claro si está seleccionado
                    : Color.FromArgb("#F8F9FA"), // Gris claro si no está seleccionado
                BorderColor = TipoAlarmaSeleccionado?.TipoalarmaId == tipo.TipoalarmaId
                    ? Color.FromArgb("#1465bb")  // Borde azul si está seleccionado
                    : Colors.LightGray,
                HasShadow = false
            };

            // Agregar TapGestureRecognizer
            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += (s, e) => OnTipoAlarmaSelected(tipo);
            frame.GestureRecognizers.Add(tapGesture);

            // Grid interno
            var gridInterno = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = 36 },
                    new ColumnDefinition { Width = GridLength.Star }
                },
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto }
                },
                ColumnSpacing = 10,
                // Si es una columna única (Promoción), centrar todo el contenido
                HorizontalOptions = esUnicaColumna ? LayoutOptions.Center : LayoutOptions.Fill
            };

            // Icono
            var imagen = new Image
            {
                Source = new ImageResourceConverter().Convert(tipo.IconoPathSinExtension, null, null, null) as ImageSource,
                WidthRequest = 32,
                HeightRequest = 32,
                Aspect = Aspect.AspectFit,
                VerticalOptions = LayoutOptions.Center
            };
            Grid.SetColumn(imagen, 0);
            gridInterno.Children.Add(imagen);

            // StackLayout con Short Alias + Porcentaje
            var stackInfo = new StackLayout
            {
                Spacing = 2,
                VerticalOptions = LayoutOptions.Center
            };

            var labelAlias = new Label
            {
                Text = tipo.ShortAliasTraducido,
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#212529")
            };
            stackInfo.Children.Add(labelAlias);

            var labelPorcentaje = new Label
            {
                Text = tipo.PorcentajeFormateado,
                FontSize = 11,
                TextColor = Color.FromArgb("#6C757D")
            };
            stackInfo.Children.Add(labelPorcentaje);

            Grid.SetColumn(stackInfo, 1);
            gridInterno.Children.Add(stackInfo);

            frame.Content = gridInterno;

            return frame;
        }

        /// <summary>
        /// Pobla la sección de tipos característicos (Warning y Crime)
        /// </summary>
        private void PoblarTiposCaracteristicos(System.Collections.Generic.List<TipoAlarmaConEstadisticas> caracteristicos)
        {
            try
            {
                GridTiposCaracteristicos.Children.Clear();

                for (int i = 0; i < caracteristicos.Count; i++)
                {
                    var tipo = caracteristicos[i];
                    var columna = i % 2; // 0 o 1
                    var frame = CrearTarjetaTipo(tipo);

                    Grid.SetColumn(frame, columna);
                    GridTiposCaracteristicos.Children.Add(frame);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TipoAlarmaPicker] ❌ Error poblando característicos: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "TipoAlarmaPickerPopup", "PoblarTiposCaracteristicos");
            }
        }

        /// <summary>
        /// Pobla la sección de otros tipos (ordenados por porcentaje)
        /// </summary>
        private void PoblarOtrosTipos(System.Collections.Generic.List<TipoAlarmaConEstadisticas> otros)
        {
            try
            {
                GridOtrosTipos.Children.Clear();
                GridOtrosTipos.RowDefinitions.Clear();

                // Calcular número de filas necesarias (2 columnas)
                int numFilas = (int)Math.Ceiling(otros.Count / 2.0);

                // Agregar RowDefinitions dinámicamente
                for (int i = 0; i < numFilas; i++)
                {
                    GridOtrosTipos.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                }

                for (int i = 0; i < otros.Count; i++)
                {
                    var tipo = otros[i];
                    var columna = i % 2; // 0 o 1
                    var fila = i / 2;
                    var frame = CrearTarjetaTipo(tipo);

                    Grid.SetColumn(frame, columna);
                    Grid.SetRow(frame, fila);
                    GridOtrosTipos.Children.Add(frame);
                }

                System.Diagnostics.Debug.WriteLine($"[TipoAlarmaPicker] ✅ Poblados {otros.Count} tipos en {numFilas} filas");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TipoAlarmaPicker] ❌ Error poblando otros tipos: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "TipoAlarmaPickerPopup", "PoblarOtrosTipos");
            }
        }

        /// <summary>
        /// Pobla la sección de Promoción Local (separada al final)
        /// </summary>
        private void PoblarPromocion(TipoAlarmaConEstadisticas promocion)
        {
            try
            {
                GridPromocion.Children.Clear();

                var frame = CrearTarjetaTipo(promocion, esUnicaColumna: true);

                // Centrar la tarjeta de promoción
                frame.HorizontalOptions = LayoutOptions.Center;
                frame.WidthRequest = 350; // Ancho fijo para que se vea centrada

                Grid.SetColumn(frame, 0);
                GridPromocion.Children.Add(frame);

                System.Diagnostics.Debug.WriteLine("[TipoAlarmaPicker] ✅ Promoción Local agregada");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TipoAlarmaPicker] ❌ Error poblando promoción: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "TipoAlarmaPickerPopup", "PoblarPromocion");
            }
        }

        /// <summary>
        /// Maneja la selección de un tipo de alarma
        /// </summary>
        private async void OnTipoAlarmaSelected(TipoAlarma tipoAlarma)
        {
            if (tipoAlarma != null && !_isClosing)
            {
                _isClosing = true;
                TipoAlarmaSeleccionado = tipoAlarma;

                TipoAlarmaSelected?.Invoke(this, tipoAlarma);

                try
                {
                    await CloseAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[TipoAlarmaPicker] Error cerrando popup: {ex.Message}");
                    CrashlyticsHelper.LogError(ex, "TipoAlarmaPickerPopup", "OnTipoAlarmaSelected");
                }
            }
        }

        /// <summary>
        /// Maneja el evento de cancelar
        /// </summary>
        private async void OnCancelTapped(object sender, EventArgs e)
        {
            if (!_isClosing)
            {
                _isClosing = true;
                try
                {
                    await CloseAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[TipoAlarmaPicker] Error cerrando popup: {ex.Message}");
                    CrashlyticsHelper.LogError(ex, "TipoAlarmaPickerPopup", "OnCancelTapped");
                }
            }
        }
    }
}
