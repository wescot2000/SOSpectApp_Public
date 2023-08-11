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
using Android.Views;
using Android.Widget;
using sospect.CustomRenderers;
using sospect.Droid.Renderers;
using Xamarin.Forms;
using Xamarin.Forms.Maps;
using Xamarin.Forms.Maps.Android;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(MiniMapa), typeof(MiniMapRenderer))]
namespace sospect.Droid.Renderers
{
    public class MiniMapRenderer : MapRenderer, IOnMapReadyCallback
    {
        List<CustomPin> customPins;
        private bool isMapReady;
        private MarkerOptions _markerOptions;

        public MiniMapRenderer(Context context) : base(context)
        {
            customPins = new List<CustomPin>();
        }

        private GoogleMap _map;

        protected override MarkerOptions CreateMarker(Pin pin)
        {
            var marker = new MarkerOptions();
            marker.SetPosition(new LatLng(pin.Position.Latitude, pin.Position.Longitude));
            marker.SetTitle(pin.Label);
            marker.SetSnippet(pin.Address);
            return marker;
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            if (e.PropertyName == nameof(Xamarin.Forms.VisualElement.IsVisible))
            {
                // Se ha cambiado la propiedad IsVisible de la vista
                if (Control != null && Element != null)
                {
                    // Aquí puedes ejecutar el código que necesites
                    Control.Touch += Control_Touch;
                    _map.MapClick -= GoogleMap_MapClick;
                }
            }
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
                var formsMap = (MiniMapa)e.NewElement;
                customPins = formsMap.CustomPins;
                Control.GetMapAsync(this);
            }

            if (Control != null)
                Control.Touch += Control_Touch;

        }


        protected override void OnMapReady(GoogleMap googleMap)
        {
            this._map = googleMap;
            _markerOptions = new MarkerOptions();
            _markerOptions.SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueGreen));

            if (_map != null)
            {
                _map.MapClick += GoogleMap_MapClick;
                ((MapView)Control).Clickable = true; // Añadir esta línea
                //_map.UiSettings.ZoomControlsEnabled = false;
                //_map.UiSettings.RotateGesturesEnabled = false;
                //_map.UiSettings.SetAllGesturesEnabled(false);
                Control.Touch += Control_Touch;
            }

            isMapReady = true;
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

        private void GoogleMap_MapClick(object sender, GoogleMap.MapClickEventArgs e)
        {
            _map.Clear();
            ((MiniMapa)Element).OnTap(new Position(e.Point.Latitude, e.Point.Longitude));
            _markerOptions.SetPosition(new LatLng(e.Point.Latitude, e.Point.Longitude));
            _map.AddMarker(_markerOptions);
        }

        private void Control_Touch(object sender, TouchEventArgs e)
        {
            if (isMapReady)
            {
                switch (e.Event.Action)
                {
                    case MotionEventActions.Down:
                    case MotionEventActions.Move:
                    case MotionEventActions.Up:
                        Parent.RequestDisallowInterceptTouchEvent(true);
                        break;
                }
            }
        }
    }
}

