using System;
using Xamarin.Forms;
using System.Collections.Generic;
using Xamarin.Forms.Maps;
using Rg.Plugins.Popup.Services;
using sospect.Extensions;

namespace sospect.CustomRenderers
{
    /// <summary>
    /// Clase que extiende el control Map para poder tener la lista de los pines a pintar 
    /// en el mapa. Para tener un evento ap sobre el map y obtener la ubicacion al hacer Tap
    /// del point y llevarlo a una posicion relativa dentro del mapa
    /// </summary>
    public class CustomMap : Map
    {
        public static readonly BindableProperty CustomPinsProperty = BindableProperty.Create(nameof(CustomPins), typeof(List<CustomPin>), typeof(CustomMap));

        public List<CustomPin> CustomPins
        {
            get { return (List<CustomPin>)GetValue(CustomPinsProperty); }
            set { SetValue(CustomPinsProperty, value); }
        }

        public Position CurrentMapPosition { get; set; }


        public event EventHandler<MapTapEventArgs> TapOnMap;

        #region Constructors
        public CustomMap()
        {
            //CurrentMapPosition = new Position();
        }

        /// <summary>
        /// Constructor que selecciona una region
        /// </summary>
        /// <param name="region">La region seleccionada en el mapa</param>
        public CustomMap(MapSpan region) : base(region)
        {

        }
        #endregion

        public async void OnTap(Position coordinates)
        {
            OnTap(new MapTapEventArgs { Position = coordinates });

            // Aqui ya tengo la coordenada en coordinates, ahora es pasarla al Popup de confirmacion de lanzar alarma
            CurrentMapPosition = coordinates;
            await PopupNavigation.Instance.PushAsync(new Popups.ConfirmarLanzarAlarma(coordinates.Latitude.Trim(6), coordinates.Longitude.Trim(6)));
        }

        protected virtual void OnTap(MapTapEventArgs e)
        {
            TapOnMap?.Invoke(this, e);
        }
    }

    /// <summary>
    /// Uso de EventArgs en el mapa, cuando el usuario hacer Tap sobre el mapa
    /// </summary>
    public class MapTapEventArgs : EventArgs
    {
        public Position Position { get; set; }
    }
}

