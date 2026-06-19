// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using sospect.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Controls.Xaml;

namespace sospect.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ZoneSubscriptionPageAndroid : ContentPage
    {
        public ZoneSubscriptionViewModel ViewModel { get; }

        public ZoneSubscriptionPageAndroid()
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
                miMiniMapa.OnTap(e.Location);

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


