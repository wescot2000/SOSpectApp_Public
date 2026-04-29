using CommunityToolkit.Maui.Views;
using sospect.CustomRenderers;
using sospect.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using sospect.Helpers;

namespace sospect.Views.Popups
{
    public partial class MinimapaPopUp : Popup
    {
        private bool _isMapInitialized = false;
        private DescribirAlarmaPopUpViewModel _viewModel;

        public MinimapaPopUp(object bindingContext)
        {
            InitializeComponent();
            System.Diagnostics.Debug.WriteLine("MinimapaPopUp: Constructor iniciado");

            // Guardar referencia al ViewModel
            if (bindingContext is DescribirAlarmaPopUpViewModel viewModel)
            {
                _viewModel = viewModel;
                System.Diagnostics.Debug.WriteLine("MinimapaPopUp: ViewModel capturado");
            }

            BindingContext = bindingContext;

            // Suscribirse a los eventos del Popup
            this.Opened += MinimapaPopUp_Opened;
            this.Closed += MinimapaPopUp_Closed;

            // Suscribirse al evento MapClicked
            miMiniMapa.MapClicked += OnMapClicked;

            System.Diagnostics.Debug.WriteLine("MinimapaPopUp: Constructor completado, eventos suscritos");
        }

        private void MinimapaPopUp_Opened(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("MinimapaPopUp: Popup abierto");

            if (_isMapInitialized)
            {
                System.Diagnostics.Debug.WriteLine("MinimapaPopUp: Mapa ya inicializado, omitiendo");
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

                    System.Diagnostics.Debug.WriteLine($"MinimapaPopUp: Mapa centrado en {userLocation.Latitude}, {userLocation.Longitude}");
                }
                catch (Exception ex)
                {
                    CrashlyticsHelper.LogError(ex, "MinimapaPopUp", "MinimapaPopUp_Opened");
                    System.Diagnostics.Debug.WriteLine($"MinimapaPopUp: Error centrando mapa: {ex.Message}");
                }
            });
        }

        private void OnMapClicked(object sender, MapClickedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"MinimapaPopUp: MapClicked en {e.Location?.Latitude}, {e.Location?.Longitude}");

                if (e.Location != null)
                {
                    miMiniMapa.OnTap(e.Location);
                    System.Diagnostics.Debug.WriteLine("MinimapaPopUp: Pin colocado exitosamente");
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "MinimapaPopUp", "OnMapClicked");
                System.Diagnostics.Debug.WriteLine($"MinimapaPopUp: Error en OnMapClicked: {ex.Message}");
            }
        }

        private void MinimapaPopUp_Closed(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("MinimapaPopUp: Popup cerrado, desuscribiendo eventos");

            if (miMiniMapa != null)
            {
                miMiniMapa.MapClicked -= OnMapClicked;
            }

            this.Opened -= MinimapaPopUp_Opened;
            this.Closed -= MinimapaPopUp_Closed;

            System.Diagnostics.Debug.WriteLine("MinimapaPopUp: Eventos desuscritos correctamente");
        }

        // NUEVO: M�todo p�blico para cerrar el popup desde afuera
        public async Task CerrarPopupAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("MinimapaPopUp: CerrarPopupAsync llamado");
                await this.CloseAsync();
                System.Diagnostics.Debug.WriteLine("MinimapaPopUp: Popup cerrado exitosamente");
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "MinimapaPopUp", "CerrarPopupAsync");
                System.Diagnostics.Debug.WriteLine($"MinimapaPopUp: Error cerrando popup: {ex.Message}");
            }
        }
    }
}