using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Maps.Handlers;
using sospect.Extensions;
using sospect.Helpers;
using sospect.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace sospect.CustomRenderers
{
    public class MiniMapa : Microsoft.Maui.Controls.Maps.Map
    {
        public static readonly BindableProperty CustomPinsProperty =
            BindableProperty.Create(nameof(CustomPins), typeof(List<CustomPin>), typeof(MiniMapa),
                defaultValue: new List<CustomPin>());

        public List<CustomPin> CustomPins
        {
            get { return (List<CustomPin>)GetValue(CustomPinsProperty); }
            set { SetValue(CustomPinsProperty, value); }
        }

        public Location CurrentMapPosition { get; set; }

        public MiniMapa()
        {
            CustomPins = new List<CustomPin>();

            // CR�TICO: Configuraci�n EXACTA como CustomMap
            IsZoomEnabled = true;
            IsScrollEnabled = true;
            IsShowingUser = false;

            Console.WriteLine("[MINIMAP-DIAG] MiniMapa: Constructor inicializado");
        }

        public event EventHandler<MapTapEventArgs> TapOnMap;

        /// <summary>
        /// M�todo llamado cuando el usuario toca el mapa
        /// Coloca un pin personalizado en la ubicaci�n tocada
        /// </summary>
        public void OnTap(Location coordinates)
        {
            try
            {
                Console.WriteLine($"[MINIMAP-DIAG] OnTap INICIO: Lat={coordinates.Latitude} Lng={coordinates.Longitude}");

                var eventArgs = new MapTapEventArgs { Position = coordinates };
                OnTap(eventArgs);
                CurrentMapPosition = coordinates;

                var LabelRutaEscape = TranslateExtension.Translate("LabelRutaEscape");
                var LabelEscapoEnEstaDireccion = TranslateExtension.Translate("LabelEscapoEnEstaDireccion");

                Console.WriteLine($"[MINIMAP-DIAG] Labels traducidos: RutaEscape='{LabelRutaEscape}'");

                Dispatcher.Dispatch(() =>
                {
                    var pinsToRemove = this.Pins.Where(p => p.Label == LabelRutaEscape).ToList();
                    Console.WriteLine($"[MINIMAP-DIAG] Dispatcher: Pins antes={this.Pins.Count} PinsARemover={pinsToRemove.Count}");

                    foreach (var pin in pinsToRemove)
                        this.Pins.Remove(pin);

                    CustomPins.Clear();

                    var customPin = new CustomPin()
                    {
                        MarkerId = "rutaEscape",
                        Label = LabelRutaEscape,
                        Type = PinType.Place,
                        Address = "",
                        Location = CurrentMapPosition,
                        ImageSource = "direccionescapesospechoso"
                    };

                    Console.WriteLine($"[MINIMAP-DIAG] CustomPin creado: Label='{customPin.Label}' Address='{customPin.Address}' ImageSource='{customPin.ImageSource}'");

                    CustomPins.Add(customPin);
                    this.Pins.Add(customPin);

                    Console.WriteLine($"[MINIMAP-DIAG] Pin agregado. TotalPins={this.Pins.Count} Handler={this.Handler?.GetType()?.Name ?? "null"}");
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MINIMAP-DIAG] ERROR en OnTap: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "MiniMapa", "OnTap");
            }
        }

        protected virtual void OnTap(MapTapEventArgs e)
        {
            TapOnMap?.Invoke(this, e);
        }
    }
}
