// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using sospect.Models;
using Microsoft.Maui.Controls.Maps;
using Location = Microsoft.Maui.Devices.Sensors.Location;

namespace sospect.CustomRenderers
{
    public class CustomPin : Pin
    {
        //  CORRECCI�N CR�TICA: Usar field como en Xamarin, NO property
        public string Id;

        //  MANTENER: Propiedades adicionales para MAUI
        public string MarkerId { get; set; }
        public string IdAlarma { get; set; }
        public long TipoAlarma { get; set; }
        public bool FlagPropietarioAlarma { get; set; }
        public AlarmaCercana AlarmaCercana { get; set; }

        // NUEVO: Propiedad para imagen personalizada del pin
        public string ImageSource { get; set; }

        // FLECHA DIRECCIÓN: Bearing en grados (0=Norte, 90=Este) para el marcador de flecha
        // Solo relevante cuando TipoAlarma == -1 (marcador sintético de dirección de escape)
        public float ArrowBearing { get; set; }

        //  CONSTRUCTOR MEJORADO: Inicializar Id como field
        public CustomPin()
        {
            Id = Guid.NewGuid().ToString(); // Field assignment
            MarkerId = Id; // Sincronizar MarkerId
        }

        //  PROPIEDAD AUXILIAR: Para compatibilidad con c�digo legacy que espera Position
        public Position Position
        {
            get => new Position(Location?.Latitude ?? 0, Location?.Longitude ?? 0);
            set => Location = new Location(value.Latitude, value.Longitude);
        }
    }

    //  ESTRUCTURA AUXILIAR: Para mantener compatibilidad total con Xamarin
    public struct Position
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public Position(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }

        // Operadores de igualdad para comparaciones
        public static bool operator ==(Position left, Position right)
        {
            return Math.Abs(left.Latitude - right.Latitude) < 0.000001 &&
                   Math.Abs(left.Longitude - right.Longitude) < 0.000001;
        }

        public static bool operator !=(Position left, Position right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            if (obj is Position other)
                return this == other;
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Latitude, Longitude);
        }
    }
}

