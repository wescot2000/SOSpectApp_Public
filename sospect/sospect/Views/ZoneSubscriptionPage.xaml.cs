using sospect.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Maps;
using Xamarin.Forms.Xaml;

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
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            Device.BeginInvokeOnMainThread(() =>
            {
                miMiniMapa.MoveToRegion(MapSpan.FromCenterAndRadius(
                    new Position(
                        App.ubicacionActual.latitud,
                        App.ubicacionActual.longitud),
                    Distance.FromMeters(200)));
            });
        }
    }
}
