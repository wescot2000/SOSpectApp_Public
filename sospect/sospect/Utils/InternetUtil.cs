// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Maui.Networking;
using MauiNetworkAccess = Microsoft.Maui.Networking.NetworkAccess;
using sospect.Interfaces;
using sospect.Helpers;

namespace sospect.Utils
{
    public static class InternetUtil
    {
        private static bool? _isConnected = null;

        public static bool IsConnected
        {
            get
            {
                // CORRECCIÓN: Inicializar en el primer acceso si no se ha establecido
                if (_isConnected == null)
                {
                    CheckConnectivity();
                }
                return _isConnected ?? false;
            }
        }

        private static void CheckConnectivity()
        {
            var current = Connectivity.Current.NetworkAccess;
            _isConnected = current == MauiNetworkAccess.Internet;
            System.Diagnostics.Debug.WriteLine($"InternetUtil.CheckConnectivity: NetworkAccess={current}, IsConnected={_isConnected}");
        }

        internal static async void Connectivity_ConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
        {
            await Task.Delay(1000); // Esperar un segundo para asegurarse de que la conectividad haya cambiado correctamente
            CheckConnectivity(); // Verificar la conectividad actualizada
        }

        public static string GetPublicIpAddress()
        {
            string ipAddress = "";
            try
            {
                string url = "http://checkip.dyndns.org";
                WebRequest req = WebRequest.Create(url);
                WebResponse resp = req.GetResponse();
                System.IO.StreamReader sr = new System.IO.StreamReader(resp.GetResponseStream());
                ipAddress = sr.ReadToEnd().Trim();
                ipAddress = ipAddress.Substring(ipAddress.LastIndexOf(":") + 1).Replace("</body></html>", "").Trim();
                sr.Close();
                resp.Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en InternetUtil.GetPublicIpAddress: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "InternetUtil", "GetPublicIpAddress");
                // Mantener el código existente del catch...
            }
            return ipAddress;
        }
    }
}

