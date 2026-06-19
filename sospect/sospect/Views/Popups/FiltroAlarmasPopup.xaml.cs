// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using CommunityToolkit.Maui.Views;
using sospect.Models;
using sospect.Converters;
using System.Linq;
using System.Diagnostics;
using Newtonsoft.Json;
using Microsoft.Maui.Controls;

namespace sospect.Views.Popups
{
    /// <summary>
    /// Popup para filtrar tipos de alarma en Mapa y "Para ti"
    /// NO afecta a "Siguiendo / En tu área" (feed de prioridad crítica)
    /// Fecha: 2025-12-20
    /// </summary>
    public partial class FiltroAlarmasPopup : Popup
    {
        private bool _filtrosAplicados = false;

        // Copia local de preferencias para no modificar App.TiposAlarmaDisponibles directamente
        private Dictionary<int, bool> _estadoLocalFiltros = new Dictionary<int, bool>();

        public bool FiltrosAplicados => _filtrosAplicados;

        public FiltroAlarmasPopup()
        {
            InitializeComponent();
            // Cargar estado inicial SINCRÓNICAMENTE desde SecureStorage
            // Usamos .GetAwaiter().GetResult() para evitar que el popup se muestre vacío
            try
            {
                CargarPreferenciasSync();
                CargarTipos();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FiltroPopup] Error inicializando: {ex.Message}");
            }
        }

        /// <summary>
        /// Carga las preferencias de forma síncrona para evitar que el popup se muestre vacío
        /// </summary>
        private void CargarPreferenciasSync()
        {
            try
            {
                if (App.TiposAlarmaDisponibles == null || App.TiposAlarmaDisponibles.Count == 0)
                    return;

                // Cargar desde SecureStorage de forma síncrona
                var json = SecureStorage.Default.GetAsync("tipos_alarma_deshabilitados").GetAwaiter().GetResult() ?? "[]";
                var deshabilitados = JsonConvert.DeserializeObject<List<int>>(json) ?? new List<int>();

                Debug.WriteLine($"[FiltroPopup] Cargando preferencias sync: {deshabilitados.Count} tipos deshabilitados");

                // Crear copia local del estado
                _estadoLocalFiltros.Clear();
                foreach (var tipo in App.TiposAlarmaDisponibles)
                {
                    bool estaHabilitado = !deshabilitados.Contains(tipo.TipoalarmaId);
                    _estadoLocalFiltros[tipo.TipoalarmaId] = estaHabilitado;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FiltroPopup] Error cargando preferencias sync: {ex.Message}");
                // En caso de error, todos habilitados por defecto
                if (App.TiposAlarmaDisponibles != null)
                {
                    foreach (var tipo in App.TiposAlarmaDisponibles)
                    {
                        _estadoLocalFiltros[tipo.TipoalarmaId] = true;
                    }
                }
            }
        }

        private void CargarTipos()
        {
            try
            {
                if (App.TiposAlarmaDisponibles == null || App.TiposAlarmaDisponibles.Count == 0)
                {
                    Debug.WriteLine("[FiltroPopup] No hay tipos disponibles");
                    return;
                }

                // Separar característicos (Crimen y Advertencia) del resto
                var caracteristicos = App.TiposAlarmaDisponibles
                    .Where(t => t.EsCaracteristico)
                    .OrderBy(t => t.TipoalarmaId) // Advertencia(1) primero, luego Crimen(2)
                    .ToList();

                var otros = App.TiposAlarmaDisponibles
                    .Where(t => !t.EsCaracteristico)
                    .OrderByDescending(t => t.PorcentajeUltimoMes)
                    .ToList();

                Debug.WriteLine($"[FiltroPopup] Característicos: {caracteristicos.Count}, Otros: {otros.Count}");

                // Poblar tipos característicos en Grid (2 columnas)
                PoblarTiposCaracteristicos(caracteristicos);

                // Poblar otros tipos en Grid (2 columnas)
                PoblarOtrosTipos(otros);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FiltroPopup] Error cargando tipos: {ex.Message}");
            }
        }

        private void PoblarTiposCaracteristicos(List<TipoAlarmaConEstadisticas> caracteristicos)
        {
            try
            {
                GridTiposCaracteristicos.Children.Clear();

                for (int i = 0; i < caracteristicos.Count; i++)
                {
                    var tipo = caracteristicos[i];
                    var columna = i % 2; // 0 o 1

                    // Crear Frame contenedor
                    var frame = new Frame
                    {
                        CornerRadius = 8,
                        Padding = 10,
                        BackgroundColor = Color.FromArgb("#F8F9FA"),
                        BorderColor = Colors.LightGray,
                        HasShadow = false
                    };

                    // Grid interno
                    var gridInterno = new Grid
                    {
                        ColumnDefinitions =
                        {
                            new ColumnDefinition { Width = 32 },
                            new ColumnDefinition { Width = GridLength.Star },
                            new ColumnDefinition { Width = GridLength.Auto }
                        },
                        RowDefinitions =
                        {
                            new RowDefinition { Height = GridLength.Auto }
                        },
                        ColumnSpacing = 8
                    };

                    // Icono
                    var imagen = new Image
                    {
                        Source = new ImageResourceConverter().Convert(tipo.IconoPathSinExtension, null, null, null) as ImageSource,
                        WidthRequest = 28,
                        HeightRequest = 28,
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
                        FontSize = 13,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Color.FromArgb("#212529")
                    };
                    stackInfo.Children.Add(labelAlias);

                    var labelPorcentaje = new Label
                    {
                        Text = tipo.PorcentajeFormateado,
                        FontSize = 10,
                        TextColor = Color.FromArgb("#6C757D")
                    };
                    stackInfo.Children.Add(labelPorcentaje);

                    Grid.SetColumn(stackInfo, 1);
                    gridInterno.Children.Add(stackInfo);

                    // Switch - usa el estado local, no el de App.TiposAlarmaDisponibles
                    var tipoId = tipo.TipoalarmaId; // Capturar para el closure
                    var switchControl = new Microsoft.Maui.Controls.Switch
                    {
                        IsToggled = _estadoLocalFiltros.ContainsKey(tipoId) ? _estadoLocalFiltros[tipoId] : true,
                        OnColor = Color.FromArgb("#1465bb"),
                        ThumbColor = Colors.White,
                        VerticalOptions = LayoutOptions.Center
                    };

                    // Binding al cambio - modifica solo la copia local
                    switchControl.Toggled += (s, e) =>
                    {
                        _estadoLocalFiltros[tipoId] = e.Value;
                        Debug.WriteLine($"[FiltroPopup] {tipo.ShortAlias}: {(e.Value ? "Habilitado" : "Deshabilitado")}");
                    };

                    Grid.SetColumn(switchControl, 2);
                    gridInterno.Children.Add(switchControl);

                    frame.Content = gridInterno;

                    Grid.SetColumn(frame, columna);
                    GridTiposCaracteristicos.Children.Add(frame);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FiltroPopup] Error poblando caracteristicos: {ex.Message}");
            }
        }

        private void PoblarOtrosTipos(List<TipoAlarmaConEstadisticas> otros)
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

                    // Crear Frame contenedor (igual que los característicos)
                    var frame = new Frame
                    {
                        CornerRadius = 8,
                        Padding = 10,
                        BackgroundColor = Color.FromArgb("#F8F9FA"),
                        BorderColor = Colors.LightGray,
                        HasShadow = false
                    };

                    // Grid interno
                    var gridInterno = new Grid
                    {
                        ColumnDefinitions =
                        {
                            new ColumnDefinition { Width = 32 },
                            new ColumnDefinition { Width = GridLength.Star },
                            new ColumnDefinition { Width = GridLength.Auto }
                        },
                        RowDefinitions =
                        {
                            new RowDefinition { Height = GridLength.Auto }
                        },
                        ColumnSpacing = 8
                    };

                    // Icono
                    var imagen = new Image
                    {
                        Source = new ImageResourceConverter().Convert(tipo.IconoPathSinExtension, null, null, null) as ImageSource,
                        WidthRequest = 28,
                        HeightRequest = 28,
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
                        FontSize = 13,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Color.FromArgb("#212529")
                    };
                    stackInfo.Children.Add(labelAlias);

                    var labelPorcentaje = new Label
                    {
                        Text = tipo.PorcentajeFormateado,
                        FontSize = 10,
                        TextColor = Color.FromArgb("#6C757D")
                    };
                    stackInfo.Children.Add(labelPorcentaje);

                    Grid.SetColumn(stackInfo, 1);
                    gridInterno.Children.Add(stackInfo);

                    // Switch - usa el estado local, no el de App.TiposAlarmaDisponibles
                    var tipoId = tipo.TipoalarmaId; // Capturar para el closure
                    var switchControl = new Microsoft.Maui.Controls.Switch
                    {
                        IsToggled = _estadoLocalFiltros.ContainsKey(tipoId) ? _estadoLocalFiltros[tipoId] : true,
                        OnColor = Color.FromArgb("#1465bb"),
                        ThumbColor = Colors.White,
                        VerticalOptions = LayoutOptions.Center
                    };

                    // Binding al cambio - modifica solo la copia local
                    switchControl.Toggled += (s, e) =>
                    {
                        _estadoLocalFiltros[tipoId] = e.Value;
                        Debug.WriteLine($"[FiltroPopup] {tipo.ShortAlias}: {(e.Value ? "Habilitado" : "Deshabilitado")}");
                    };

                    Grid.SetColumn(switchControl, 2);
                    gridInterno.Children.Add(switchControl);

                    frame.Content = gridInterno;

                    Grid.SetColumn(frame, columna);
                    Grid.SetRow(frame, fila);
                    GridOtrosTipos.Children.Add(frame);
                }

                Debug.WriteLine($"[FiltroPopup] Poblados {otros.Count} tipos en {numFilas} filas (2 columnas)");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FiltroPopup] Error poblando otros tipos: {ex.Message}");
            }
        }

        private async void OnAplicarClicked(object sender, EventArgs e)
        {
            try
            {
                Debug.WriteLine("[FiltroPopup] Aplicando filtros...");

                // Guardar preferencias
                await GuardarPreferencias();

                // Marcar que se aplicaron filtros
                _filtrosAplicados = true;

                // Cerrar popup
                await CloseAsync();

                Debug.WriteLine("[FiltroPopup] Filtros aplicados y guardados");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FiltroPopup] Error aplicando filtros: {ex.Message}");
            }
        }

        private async void OnCancelarClicked(object sender, EventArgs e)
        {
            try
            {
                Debug.WriteLine("[FiltroPopup] Cancelado - No se guardan cambios");

                // No necesitamos restaurar nada porque usamos copia local
                // App.TiposAlarmaDisponibles nunca fue modificado

                // Marcar que NO se aplicaron filtros
                _filtrosAplicados = false;

                // Cerrar popup
                await CloseAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FiltroPopup] Error cancelando: {ex.Message}");
            }
        }

        private async Task GuardarPreferencias()
        {
            try
            {
                if (App.TiposAlarmaDisponibles == null || App.TiposAlarmaDisponibles.Count == 0)
                    return;

                // Obtener IDs deshabilitados desde la copia local
                var deshabilitados = _estadoLocalFiltros
                    .Where(kvp => !kvp.Value)
                    .Select(kvp => kvp.Key)
                    .ToList();

                // 1. Guardar en SecureStorage
                var json = JsonConvert.SerializeObject(deshabilitados);
                await SecureStorage.Default.SetAsync("tipos_alarma_deshabilitados", json);

                Debug.WriteLine($"[FiltroPopup] Guardados en SecureStorage {deshabilitados.Count} tipos deshabilitados: [{string.Join(", ", deshabilitados)}]");

                // 2. Aplicar cambios a App.TiposAlarmaDisponibles (SOLO al guardar)
                foreach (var tipo in App.TiposAlarmaDisponibles)
                {
                    if (_estadoLocalFiltros.ContainsKey(tipo.TipoalarmaId))
                    {
                        tipo.EstaHabilitado = _estadoLocalFiltros[tipo.TipoalarmaId];
                    }
                }

                var habilitados = _estadoLocalFiltros.Count(kvp => kvp.Value);
                Debug.WriteLine($"[FiltroPopup] {habilitados} tipos habilitados, {deshabilitados.Count} deshabilitados");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FiltroPopup] Error guardando preferencias: {ex.Message}");
            }
        }

    }
}


