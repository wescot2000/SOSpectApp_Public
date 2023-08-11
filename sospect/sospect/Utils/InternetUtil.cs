using System;
using System.Net;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace sospect.Utils
{
    public static class InternetUtil
    {
        private static bool isConnected;

        public static bool IsConnected
        {
            get { return isConnected; }
        }

        private static void CheckConnectivity()
        {
            var current = Connectivity.NetworkAccess;
            isConnected = current == NetworkAccess.Internet;
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
                // Manejar excepción
            }
            return ipAddress;
        }
    }
}

