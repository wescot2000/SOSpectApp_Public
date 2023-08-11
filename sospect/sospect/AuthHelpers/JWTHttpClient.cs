using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Threading.Tasks;
using sospect.Services;
using sospect.Utils;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace sospect.AuthHelpers
{
    public class JWTHttpClient : HttpClient
    {
        public static string Token
        {
            get => Xamarin.Essentials.Preferences.Get("access_token", "");
            set => Xamarin.Essentials.Preferences.Set("access_token", value);
        }

        public JWTHttpClient()
        {
            // Set your base address
            //BaseAddress = new Uri(AppConfiguration.ApiHost);
            if (!string.IsNullOrEmpty(Token))
            {
                DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", JWTHttpClient.Token);
            }
        }

        public JWTHttpClient(HttpMessageHandler handler, bool disposeHandler) : base(handler, disposeHandler)
        {
            if (!string.IsNullOrEmpty(Token))
            {
                DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", JWTHttpClient.Token);
            }
        }

        /// <summary>
        /// Refreshes the token if necessary, if we were unable to and needed to return false
        /// </summary>
        /// <returns>
        /// True is successful or unnecessary
        /// False is unsuccessful or a failure
        /// </returns>
        public bool CheckRefresh()
        {
            // Determine if your token is expired, if so refresh
            var handler = new JwtSecurityTokenHandler();
            var Readedtoken = handler.ReadJwtToken(Token);
            DateTime expdate = Readedtoken.ValidTo;

            if (expdate < DateTime.UtcNow)
                return true;
            return false;
        }

        public async Task<HttpResponseMessage> GetAPIAsync(string path)
        {
            // Check our Token using CheckRefresh
            try
            {
                if (InternetUtil.IsConnected)
                {
                    return new HttpResponseMessage() { Content = new StringContent("") };
                }
                //Si es true se debe refrescar el token para realizar la petición
                if (CheckRefresh())
                {
                    App.Current.MainPage = new NavigationPage(new Views.LoginPage());
                    return new HttpResponseMessage() { Content = new StringContent("") };
                }
                else
                {
                    return await GetAsync(path);
                }
            }
            catch (Exception ex)
            {
                throw new TokenException("Error refreshing token", ex);
            }
        }

        public async Task<HttpResponseMessage> PostAPIAsync(string path, HttpContent content)
        {
            //Si es true se debe refrescar el token para realizar la petición
            if (InternetUtil.IsConnected)
            {
                return new HttpResponseMessage() { Content = new StringContent("") };
            }
            if (CheckRefresh())
            {
                App.Current.MainPage = new NavigationPage(new Views.LoginPage());
                return new HttpResponseMessage() { Content = new StringContent("") };
            }
            else
            {
                return await PostAsync(path, content);
            }
        }

        public class TokenException : Exception
        {
            public TokenException() : base()
            {

            }
            public TokenException(string msg) : base(msg)
            {

            }
            public TokenException(string msg, Exception inner) : base(msg, inner)
            {

            }
        }
    }
}

