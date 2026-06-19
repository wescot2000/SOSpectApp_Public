// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

// ConfirmarUbicacionPromoPage.xaml.cs
// Creado: 2026-02-28  |  Fix: 2026-03-01
// Propósito: Página de confirmación de ubicación para PromocionLocalPage (flujo estilo Uber).
//
// FIX 2026-03-01: Cambiado de PushModalAsync/PopModalAsync a PushAsync/PopAsync.
// Causa: PromocionLocalPage se abre con PushAsync en el stack Tab. Al usar PushModalAsync
// desde ahí, se crea un stack modal independiente; al hacer PopModalAsync se regresaba a
// SospectTabs (la raíz) en vez de a PromocionLocalPage.
// Solución: Esta página se empuja con PushAsync (mismo stack Tab) y comunica el resultado
// de vuelta a PromocionLocalPage mediante un TaskCompletionSource pasado en el constructor.

using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using sospect.CustomRenderers;

namespace sospect.Views
{
    public partial class ConfirmarUbicacionPromoPage : ContentPage
    {
        private double _latitud;
        private double _longitud;
        private readonly TaskCompletionSource<(bool confirmado, double lat, double lon)> _tcs;
        private const string PinMarkerId = "promoConfirm";

        public double LatitudConfirmada { get; private set; }
        public double LongitudConfirmada { get; private set; }

        public ConfirmarUbicacionPromoPage(double latitud, double longitud,
            TaskCompletionSource<(bool confirmado, double lat, double lon)> tcs)
        {
            InitializeComponent();
            _latitud = latitud;
            _longitud = longitud;
            _tcs = tcs;
            // Inicializar con la ubicación original por si el usuario no mueve el pin
            LatitudConfirmada = latitud;
            LongitudConfirmada = longitud;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            miMapa.MapClicked += OnMapClicked;

            // Centrar el mapa y colocar el pin en la ubicación inicial
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var loc = new Location(_latitud, _longitud);
                miMapa.MoveToRegion(MapSpan.FromCenterAndRadius(loc, Distance.FromMeters(200)));
                ColocarPin(loc);
            });
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            miMapa.MapClicked -= OnMapClicked;
        }

        private void OnMapClicked(object sender, MapClickedEventArgs e)
        {
            if (e.Location != null)
            {
                LatitudConfirmada = e.Location.Latitude;
                LongitudConfirmada = e.Location.Longitude;
                ColocarPin(e.Location);
                System.Diagnostics.Debug.WriteLine(
                    $"[ConfirmarUbicacionPromoPage] Pin movido a: {LatitudConfirmada:F6}, {LongitudConfirmada:F6}");
            }
        }

        private void ColocarPin(Location ubicacion)
        {
            // Borrar pin anterior si existe
            var toRemove = miMapa.Pins.Where(p => p.Label == PinMarkerId).ToList();
            foreach (var p in toRemove)
                miMapa.Pins.Remove(p);
            miMapa.CustomPins?.Clear();

            // Crear el pin con la imagen de promoción local
            var pin = new CustomPin
            {
                MarkerId = PinMarkerId,
                Label = PinMarkerId,
                Type = PinType.Place,
                Address = string.Empty,
                Location = ubicacion,
                ImageSource = "promocionlocal"
            };
            miMapa.CustomPins?.Add(pin);
            miMapa.Pins.Add(pin);
        }

        private async void OnConfirmarClicked(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[ConfirmarUbicacionPromoPage] Ubicación confirmada: {LatitudConfirmada:F6}, {LongitudConfirmada:F6}");
            // Resolver el TCS ANTES del Pop para que PromocionLocalPage reciba el resultado
            _tcs.TrySetResult((true, LatitudConfirmada, LongitudConfirmada));
            await Navigation.PopAsync();
        }

        private async void OnCancelarClicked(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[ConfirmarUbicacionPromoPage] Usuario canceló la confirmación de ubicación");
            _tcs.TrySetResult((false, _latitud, _longitud));
            await Navigation.PopAsync();
        }
    }
}


