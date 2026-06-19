// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using sospect.Models;
using System;
using System.Collections.Generic;
using System.Threading;

namespace sospect.Views.Popups
{
    /// <summary>
    /// Popup personalizado para seleccionar emprendimiento cuando el usuario tiene más de uno.
    /// Muestra nombre + NIT para distinguir emprendimientos con mismo nombre.
    /// Agregado: 2026-04-10
    /// 2026-04-12: Corregido bug de doble selección — usar Interlocked.CompareExchange para
    ///             garantizar atomicidad del guard _isClosing bajo async/multitap simultáneo.
    ///             También se desactiva InputTransparent en todos los frames al primer tap
    ///             para retroalimentación visual inmediata.
    /// </summary>
    public partial class SeleccionarEmprendimientoPopup : Popup
    {
        // 0 = abierto, 1 = cerrando. Interlocked garantiza atomicidad bajo multitap.
        private int _isClosing = 0;

        // Referencia a todos los frames para deshabilitarlos en bloque al seleccionar
        private readonly List<Frame> _frames = new List<Frame>();

        /// <summary>
        /// Emprendimiento seleccionado por el usuario. Null si canceló.
        /// </summary>
        public EmprendimientoResumenDto EmprendimientoSeleccionado { get; private set; } = null;

        public SeleccionarEmprendimientoPopup(List<EmprendimientoResumenDto> emprendimientos)
        {
            InitializeComponent();
            CargarItems(emprendimientos);
        }

        /// <summary>
        /// Intenta adquirir el lock de cierre. Devuelve true solo para el primer llamador.
        /// Cualquier tap adicional simultáneo obtiene false y es ignorado.
        /// </summary>
        private bool TryAcquireClosingLock()
        {
            return Interlocked.CompareExchange(ref _isClosing, 1, 0) == 0;
        }

        /// <summary>
        /// Deshabilita visualmente todos los frames para dar retroalimentación inmediata
        /// y evitar que el usuario pueda tapear otro ítem mientras el popup se cierra.
        /// </summary>
        private void DeshabilitarTodosLosFrames()
        {
            foreach (var f in _frames)
            {
                f.InputTransparent = true;
                f.Opacity = 0.5;
            }
        }

        private void CargarItems(List<EmprendimientoResumenDto> emprendimientos)
        {
            foreach (var emp in emprendimientos)
            {
                // Frame tappable para cada emprendimiento
                var frame = new Frame
                {
                    Margin = new Thickness(4, 4),
                    Padding = new Thickness(14, 12),
                    CornerRadius = 10,
                    BackgroundColor = Color.FromArgb("#F8F9FA"),
                    BorderColor = Color.FromArgb("#E0E0E0"),
                    HasShadow = false
                };

                var grid = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = GridLength.Star },
                        new ColumnDefinition { Width = GridLength.Auto }
                    }
                };

                var stack = new StackLayout { Spacing = 2 };

                var lblNombre = new Label
                {
                    Text = emp.NombreEmprendimiento,
                    FontSize = 14,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#2C3E50"),
                    LineBreakMode = LineBreakMode.WordWrap
                };
                stack.Children.Add(lblNombre);

                if (!string.IsNullOrEmpty(emp.NitCedulaPropietario))
                {
                    var lblNit = new Label
                    {
                        Text = $"NIT/Cédula: {emp.NitCedulaPropietario}",
                        FontSize = 12,
                        TextColor = Color.FromArgb("#7F8C8D")
                    };
                    stack.Children.Add(lblNit);
                }

                var chevron = new Label
                {
                    Text = "›",
                    FontSize = 20,
                    TextColor = Color.FromArgb("#BDC3C7"),
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.End
                };

                Grid.SetColumn(stack, 0);
                Grid.SetColumn(chevron, 1);
                grid.Children.Add(stack);
                grid.Children.Add(chevron);

                frame.Content = grid;

                // Capturar la variable local para el closure
                var empCapturado = emp;
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += async (s, e) =>
                {
                    // Solo el primer tap concurrente pasa — los demás son ignorados atómicamente
                    if (!TryAcquireClosingLock()) return;

                    Console.WriteLine($"[SelectorEmp] Emprendimiento seleccionado: {empCapturado.NombreEmprendimiento} id={empCapturado.IdEmprendimiento}");

                    // Deshabilitar todos los ítems inmediatamente para feedback visual
                    DeshabilitarTodosLosFrames();

                    EmprendimientoSeleccionado = empCapturado;
                    try
                    {
                        await CloseAsync();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[SelectorEmp] Error cerrando popup: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"[SelectorEmp] Error cerrando popup: {ex.Message}");
                    }
                };
                frame.GestureRecognizers.Add(tapGesture);

                // Registrar frame en la lista para poder deshabilitarlos todos
                _frames.Add(frame);
                ListaEmprendimientos.Children.Add(frame);
            }
        }

        private async void OnCancelarClicked(object sender, EventArgs e)
        {
            if (!TryAcquireClosingLock()) return;
            Console.WriteLine($"[SelectorEmp] Usuario canceló el selector");
            DeshabilitarTodosLosFrames();
            EmprendimientoSeleccionado = null;
            try
            {
                await CloseAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SelectorEmp] Error cerrando popup: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[SelectorEmp] Error cerrando popup: {ex.Message}");
            }
        }
    }
}


