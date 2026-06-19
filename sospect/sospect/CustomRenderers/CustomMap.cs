// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using sospect.Extensions;
using System;
using System.Collections.Generic;
using Location = Microsoft.Maui.Devices.Sensors.Location;
using MauiMap = Microsoft.Maui.Controls.Maps.Map;
using sospect.Helpers;

namespace sospect.CustomRenderers
{
    /// <summary>
    /// Clase que extiende el control Map para poder tener la lista de los pines a pintar 
    /// en el mapa. Para tener un evento tap sobre el mapa y obtener la ubicación al hacer Tap
    /// del point y llevarlo a una posición relativa dentro del mapa
    /// </summary>
    public class CustomMap : MauiMap
    {
        public static readonly BindableProperty CustomPinsProperty = BindableProperty.Create(
            nameof(CustomPins), typeof(List<CustomPin>), typeof(CustomMap));
        public List<CustomPin> CustomPins
        {
            get { return (List<CustomPin>)GetValue(CustomPinsProperty); }
            set { SetValue(CustomPinsProperty, value); }
        }
        // AGREGADAS: Propiedades que faltaban para gestos
        public static readonly BindableProperty HasScrollEnabledProperty = BindableProperty.Create(
            nameof(HasScrollEnabled), typeof(bool), typeof(CustomMap), true);
        public bool HasScrollEnabled
        {
            get { return (bool)GetValue(HasScrollEnabledProperty); }
            set { SetValue(HasScrollEnabledProperty, value); }
        }
        public static readonly BindableProperty HasZoomEnabledProperty = BindableProperty.Create(
            nameof(HasZoomEnabled), typeof(bool), typeof(CustomMap), true);
        public bool HasZoomEnabled
        {
            get { return (bool)GetValue(HasZoomEnabledProperty); }
            set { SetValue(HasZoomEnabledProperty, value); }
        }
        public static readonly BindableProperty HasRotationEnabledProperty = BindableProperty.Create(
            nameof(HasRotationEnabled), typeof(bool), typeof(CustomMap), false);
        public bool HasRotationEnabled
        {
            get { return (bool)GetValue(HasRotationEnabledProperty); }
            set { SetValue(HasRotationEnabledProperty, value); }
        }
        public Location CurrentMapPosition { get; set; }
        public event EventHandler<MapTapEventArgs> TapOnMap;

        // NUEVO: Evento para detectar cambios en la región visible (para clustering zoom-aware)
        public event EventHandler<MapSpanChangedEventArgs> VisibleRegionChanged;

        // NUEVO: MapSpan actual (cache para optimización)
        private MapSpan _lastVisibleRegion;

        #region Constructors
        public CustomMap()
        {
            //CurrentMapPosition = new Location();
        }
        /// <summary>
        /// Constructor que selecciona una región
        /// </summary>
        /// <param name="region">La región seleccionada en el mapa</param>
        public CustomMap(MapSpan region) : base(region)
        {
        }
        #endregion

        public void OnTap(Location coordinates)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"CustomMap: OnTap en {coordinates.Latitude}, {coordinates.Longitude}");

                // Guardar la posición actual
                CurrentMapPosition = coordinates;

                // Disparar el evento TapOnMap (para cualquier suscriptor nativo)
                OnTap(new MapTapEventArgs { Position = coordinates });

                // Notificar a HomePage via MessagingCenter (canal único y correcto)
                MessagingCenter.Send<CustomMap, Location>(this, "MapTapped", coordinates);
                System.Diagnostics.Debug.WriteLine("CustomMap: Mensaje 'MapTapped' enviado");
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "CustomMap", "OnTap");
                System.Diagnostics.Debug.WriteLine($"CustomMap: Error en OnTap: {ex.Message}");
            }
        }

        protected virtual void OnTap(MapTapEventArgs e)
        {
            TapOnMap?.Invoke(this, e);
        }

        // NUEVO: Método para disparar el evento de cambio de región visible
        public void OnVisibleRegionChanged(MapSpan newRegion)
        {
            if (newRegion == null)
                return;

            // Solo disparar si la región cambió significativamente
            if (_lastVisibleRegion == null || HasSignificantChange(_lastVisibleRegion, newRegion))
            {
                _lastVisibleRegion = newRegion;
                VisibleRegionChanged?.Invoke(this, new MapSpanChangedEventArgs { NewRegion = newRegion });
                System.Diagnostics.Debug.WriteLine($"CustomMap: VisibleRegionChanged disparado - LatDelta: {newRegion.LatitudeDegrees:F6}");
            }
        }

        // Helper para detectar cambios significativos en la región (evita eventos excesivos)
        private bool HasSignificantChange(MapSpan oldRegion, MapSpan newRegion)
        {
            const double threshold = 0.0001; // ~11 metros

            var latDiff = Math.Abs(oldRegion.Center.Latitude - newRegion.Center.Latitude);
            var lngDiff = Math.Abs(oldRegion.Center.Longitude - newRegion.Center.Longitude);
            var latDeltaDiff = Math.Abs(oldRegion.LatitudeDegrees - newRegion.LatitudeDegrees);
            var lngDeltaDiff = Math.Abs(oldRegion.LongitudeDegrees - newRegion.LongitudeDegrees);

            return latDiff > threshold || lngDiff > threshold ||
                   latDeltaDiff > threshold || lngDeltaDiff > threshold;
        }
    }

    /// <summary>
    /// Uso de EventArgs en el mapa, cuando el usuario hace Tap sobre el mapa
    /// </summary>
    public class MapTapEventArgs : EventArgs
    {
        public Location Position { get; set; }
    }

    /// <summary>
    /// EventArgs para cambios en la región visible del mapa (zoom/pan)
    /// </summary>
    public class MapSpanChangedEventArgs : EventArgs
    {
        public MapSpan NewRegion { get; set; }
    }
}

