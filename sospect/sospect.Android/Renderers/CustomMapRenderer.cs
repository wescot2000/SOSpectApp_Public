using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Service.Notification;
using Android.Views;
using Android.Widget;
using sospect.CustomRenderers;
using sospect.Droid.Renderers;
using sospect.Popups;
using sospect.Views;
using Xamarin;
using Xamarin.Forms;
using Xamarin.Forms.Maps;
using Xamarin.Forms.Maps.Android;
using Xamarin.Forms.Platform.Android;
using static Android.Gms.Maps.GoogleMap;

[assembly: ExportRenderer(typeof(CustomMap), typeof(CustomMapRenderer))]
namespace sospect.Droid.Renderers
{
    public class CustomMapRenderer : MapRenderer, IOnMapReadyCallback, IInfoWindowAdapter
    {
        List<CustomPin> customPins;

        public CustomMapRenderer(Context context) : base(context)
        {
            customPins = new List<CustomPin>();
        }

        protected override void OnElementChanged(ElementChangedEventArgs<Map> e)
        {
            /***************************************************************************/
            // El codigo para el Tap sobre el mapa
            /***************************************************************************/
            if (_map != null)
                _map.MapClick -= GoogleMap_MapClick;

            base.OnElementChanged(e);

            if (e.OldElement != null)
            {

            }

            if (e.NewElement != null)
            {
                var formsMap = (CustomMap)e.NewElement;
                customPins = formsMap.CustomPins;
                Control.GetMapAsync(this);
            }

            if (Control != null)
                ((MapView)Control).GetMapAsync(this);

        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            if (e.PropertyName == nameof(CustomMap.Pins))
            {
                if (sender is sospect.CustomRenderers.CustomMap map)
                {
                    if (map.Pins.Any())
                    {
                        customPins = (List<CustomPin>)map.Pins;
                    }
                }
            }
        }

        public Android.Views.View GetInfoContents(Marker marker)
        {
            //throw new NotImplementedException();
            return null;

        }

        protected override MarkerOptions CreateMarker(Pin pin)
        {
            var marker = new MarkerOptions();
            marker.SetPosition(new LatLng(pin.Position.Latitude, pin.Position.Longitude));
            marker.SetTitle(pin.Label);
            marker.SetSnippet(pin.Address);
            CustomPin cpin = (CustomPin)pin;
            switch (cpin.TipoAlarma)
            {
                case 1:
                    marker.SetIcon(BitmapDescriptorFactory.FromResource(Resource.Drawable.WarningPin));
                    break;
                case 2:
                    marker.SetIcon(BitmapDescriptorFactory.FromResource(Resource.Drawable.DangerPin));
                    break;
                case 3:
                    marker.SetIcon(BitmapDescriptorFactory.FromResource(Resource.Drawable.AmarilloPleitoCallejero));
                    break;
                case 4:
                    marker.SetIcon(BitmapDescriptorFactory.FromResource(Resource.Drawable.VerdeAzulMascotaPerdida));
                    break;
                case 5:
                    marker.SetIcon(BitmapDescriptorFactory.FromResource(Resource.Drawable.AzulPersonaPerdida));
                    break;
                case 6:
                    marker.SetIcon(BitmapDescriptorFactory.FromResource(Resource.Drawable.NegroDisturbiosProtestas));
                    break;
                case 7:
                    marker.SetIcon(BitmapDescriptorFactory.FromResource(Resource.Drawable.VerdeAcompanamientoVisual));
                    break;
                case 8:
                    marker.SetIcon(BitmapDescriptorFactory.FromResource(Resource.Drawable.PurpuraViolenciaIntrafamiliar));
                    break;
                case 9:
                    marker.SetIcon(BitmapDescriptorFactory.FromResource(Resource.Drawable.DireccionEscapeSospechoso));
                    break;

                default:
                    marker.SetIcon(BitmapDescriptorFactory.FromResource(Resource.Drawable.user_location));
                    break;
            }

            return marker;
        }

        CustomPin GetCustomPin(Marker annotation)
        {
            var position = new Position(annotation.Position.Latitude, annotation.Position.Longitude);
            if (!(customPins is null))
            {
                foreach (var pin in customPins)
                {
                    if (pin.Position == position)
                    {
                        return pin;
                    }
                }
            }

            return null;
        }

        /*********************************************************************/
        // Codigo para el manejo del Tap sobre el mapa en Android
        /*********************************************************************/
        // uso Google map nativo en Android
        private GoogleMap _map;

        protected override void OnMapReady(GoogleMap googleMap)
        {
            _map = googleMap;

            if (_map != null)
            {
                _map.MapClick += GoogleMap_MapClick;
                _map.InfoWindowClick += OnInfoWindowClick;
                _map.SetInfoWindowAdapter(this);
                //_map.UiSettings.ZoomControlsEnabled = false;
                //_map.UiSettings.RotateGesturesEnabled = false;
                //_map.UiSettings.SetAllGesturesEnabled(false);
            }

        }

        private void OnInfoWindowClick(object sender, GoogleMap.InfoWindowClickEventArgs eventArg)
        {
            if (eventArg.Marker.Title == "User")
            {
                return;
            }

            string idAlarma = eventArg.Marker.Title.Split(' ')[1];
            App.Current.MainPage.Navigation.PushAsync(new DescribirPage(long.Parse(idAlarma)));
        }

        public Android.Views.View GetInfoWindow(Marker marker)
        {

            return null;
        }

        private void GoogleMap_MapClick(object sender, GoogleMap.MapClickEventArgs e)
        {
            ((CustomMap)Element).OnTap(new Position(e.Point.Latitude, e.Point.Longitude));
        }
    }
}

