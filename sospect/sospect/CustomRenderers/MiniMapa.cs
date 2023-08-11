using System;
using System.Collections.Generic;
using Rg.Plugins.Popup.Services;
using sospect.Extensions;
using sospect.Helpers;
using Xamarin.Forms;
using Xamarin.Forms.Maps;

namespace sospect.CustomRenderers
{
    public class MiniMapa : Map
    {
        public static readonly BindableProperty CustomPinsProperty = BindableProperty.Create(nameof(CustomPins), typeof(List<CustomPin>), typeof(CustomMap));

        public List<CustomPin> CustomPins
        {
            get { return (List<CustomPin>)GetValue(CustomPinsProperty); }
            set { SetValue(CustomPinsProperty, value); }
        }

        public Position CurrentMapPosition { get; set; }

        public MiniMapa()
        {
            CustomPins = new List<CustomPin>();
        }

        public event EventHandler<MapTapEventArgs> TapOnMap;

        public void OnTap(Position coordinates)
        {
            OnTap(new MapTapEventArgs { Position = coordinates });

            CurrentMapPosition = coordinates;
            var LabelRutaEscape = TranslateExtension.Translate("LabelRutaEscape");
            var LabelEscapoEnEstaDireccion = TranslateExtension.Translate("LabelEscapoEnEstaDireccion");
            CustomPin rutaEscapePin = new CustomPin()
            {
                MarkerId = "rutaEscape",
                Label = LabelRutaEscape,
                Type = PinType.Generic,
                Address = LabelEscapoEnEstaDireccion,
                Position = CurrentMapPosition
            };

            Device.BeginInvokeOnMainThread(() =>
            {
                CustomPins.Add(rutaEscapePin);
            });
        }

        protected virtual void OnTap(MapTapEventArgs e)
        {
            TapOnMap?.Invoke(this, e);
        }
    }
}

