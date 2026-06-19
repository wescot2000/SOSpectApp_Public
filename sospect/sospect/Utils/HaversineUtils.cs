// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;

namespace sospect.Utils
{
    public static class HaversineUtils
    {
        private const double EarthRadiusInMeters = 6371000;

        public static double DistanceInMeters(double latitude1, double longitude1, double latitude2, double longitude2)
        {
            var lat1 = latitude1 * (Math.PI / 180);
            var lon1 = longitude1 * (Math.PI / 180);
            var lat2 = latitude2 * (Math.PI / 180);
            var lon2 = longitude2 * (Math.PI / 180);

            var dLat = lat2 - lat1;
            var dLon = lon2 - lon1;

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(lat1) * Math.Cos(lat2) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return EarthRadiusInMeters * c;
        }
    }
}

