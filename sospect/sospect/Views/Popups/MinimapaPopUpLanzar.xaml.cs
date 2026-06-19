// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

// MinimapaPopUpLanzar.xaml.cs
// Creado: 2026-03-21 — Minimapa de ruta de escape para el flujo de LANZAMIENTO de alarma.
// Acepta LanzarAlarmaViewModel (o cualquier objeto con CancelarMinimapaCommand /
// HechoDireccionCommand) como BindingContext.
// Lógica de mapa idéntica a MinimapaPopUp.

using CommunityToolkit.Maui.Views;
using sospect.CustomRenderers;
using sospect.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using sospect.Helpers;

namespace sospect.Views.Popups
{
    public partial class MinimapaPopUpLanzar : Popup
    {
        private bool _isMapInitialized = false;

        public MinimapaPopUpLanzar(object bindingContext)
        {
            InitializeComponent();
            System.Diagnostics.Debug.WriteLine("MinimapaPopUpLanzar: Constructor iniciado");

            BindingContext = bindingContext;

            // Suscribirse a los eventos del Popup
            this.Opened += MinimapaPopUpLanzar_Opened;
            this.Closed += MinimapaPopUpLanzar_Closed;

            // Suscribirse al evento MapClicked
            miMiniMapa.MapClicked += OnMapClicked;

            System.Diagnostics.Debug.WriteLine("MinimapaPopUpLanzar: Constructor completado, eventos suscritos");
        }

        private void MinimapaPopUpLanzar_Opened(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("MinimapaPopUpLanzar: Popup abierto");

            if (_isMapInitialized)
            {
                System.Diagnostics.Debug.WriteLine("MinimapaPopUpLanzar: Mapa ya inicializado, omitiendo");
                return;
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    var userLocation = new Location(
                        App.ubicacionActual.latitud,
                        App.ubicacionActual.longitud);

                    miMiniMapa.MoveToRegion(
                        MapSpan.FromCenterAndRadius(
                            userLocation,
                            Distance.FromMeters(100)));

                    _isMapInitialized = true;

                    System.Diagnostics.Debug.WriteLine($"MinimapaPopUpLanzar: Mapa centrado en {userLocation.Latitude}, {userLocation.Longitude}");
                }
                catch (Exception ex)
                {
                    CrashlyticsHelper.LogError(ex, "MinimapaPopUpLanzar", "MinimapaPopUpLanzar_Opened");
                    System.Diagnostics.Debug.WriteLine($"MinimapaPopUpLanzar: Error centrando mapa: {ex.Message}");
                }
            });
        }

        private void OnMapClicked(object sender, MapClickedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"MinimapaPopUpLanzar: MapClicked en {e.Location?.Latitude}, {e.Location?.Longitude}");

                if (e.Location != null)
                {
                    miMiniMapa.OnTap(e.Location);
                    System.Diagnostics.Debug.WriteLine("MinimapaPopUpLanzar: Pin colocado exitosamente");
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "MinimapaPopUpLanzar", "OnMapClicked");
                System.Diagnostics.Debug.WriteLine($"MinimapaPopUpLanzar: Error en OnMapClicked: {ex.Message}");
            }
        }

        private void MinimapaPopUpLanzar_Closed(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("MinimapaPopUpLanzar: Popup cerrado, desuscribiendo eventos");

            if (miMiniMapa != null)
            {
                miMiniMapa.MapClicked -= OnMapClicked;
            }

            this.Opened -= MinimapaPopUpLanzar_Opened;
            this.Closed -= MinimapaPopUpLanzar_Closed;

            System.Diagnostics.Debug.WriteLine("MinimapaPopUpLanzar: Eventos desuscritos correctamente");
        }
    }
}


