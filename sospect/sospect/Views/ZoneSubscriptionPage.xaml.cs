using sospect.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Controls.Xaml;

namespace sospect.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ZoneSubscriptionPage : ContentPage
    {
        public ZoneSubscriptionViewModel ViewModel { get; }

        public ZoneSubscriptionPage()
        {
            InitializeComponent();
            ViewModel = new ZoneSubscriptionViewModel();
            BindingContext = ViewModel;

            // Suscribirse al evento de tap del mapa
            miMiniMapa.MapClicked += OnMapClicked;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Centrar el mapa en la ubicación actual del usuario
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var userLocation = new Location(
                    App.ubicacionActual.latitud,
                    App.ubicacionActual.longitud);

                miMiniMapa.MoveToRegion(
                    MapSpan.FromCenterAndRadius(
                        userLocation,
                        Distance.FromMeters(200)));
            });
        }

        private void OnMapClicked(object sender, MapClickedEventArgs e)
        {
            if (e.Location != null)
            {
                System.Diagnostics.Debug.WriteLine($"[ZONE-TAP] OnMapClicked disparado. Lat={e.Location.Latitude:F6} Lng={e.Location.Longitude:F6}");
                Console.WriteLine($"[ZONE-TAP] Console.WriteLine - Lat={e.Location.Latitude:F6} Lng={e.Location.Longitude:F6}");

                miMiniMapa.OnTap(e.Location);

                System.Diagnostics.Debug.WriteLine($"[ZONE-TAP] Despues de OnTap. Handler={miMiniMapa.Handler?.GetType()?.Name ?? "null"} Pins={miMiniMapa.Pins.Count}");

                // Actualizar las etiquetas de coordenadas
                LatitudeLabel.Text = $"Latitud: {e.Location.Latitude:F6}";
                LongitudeLabel.Text = $"Longitud: {e.Location.Longitude:F6}";
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            // Desuscribirse del evento
            miMiniMapa.MapClicked -= OnMapClicked;
        }
    }
}