using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Threading.Tasks;
using sospect.Interfaces;
using sospect.Services;
using sospect.Utils;
using sospect.Views;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using static System.Net.WebRequestMethods;
using sospect.Helpers;

namespace sospect.AuthHelpers
{
    public class JWTHttpClient : HttpClient
    {
        public static async Task<string> GetTokenAsync()
        {
            return await SecureStorage.GetAsync("access_token");
        }

        public static async Task SetTokenAsync(string value)
        {
            await SecureStorage.SetAsync("access_token", value);
        }

        public JWTHttpClient()
        {
            InitializeClient().ConfigureAwait(false);
        }

        public JWTHttpClient(HttpMessageHandler handler, bool disposeHandler) : base(handler, disposeHandler)
        {
            InitializeClient().ConfigureAwait(false);
        }

        private async Task InitializeClient()
        {
            var token = await GetTokenAsync();
            Console.WriteLine($"[JWTHttpClient] InitializeClient - Token presente: {!string.IsNullOrEmpty(token)}");
            if (!string.IsNullOrEmpty(token))
            {
                DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                Console.WriteLine($"[JWTHttpClient] Authorization header configurado");
            }
            else
            {
                Console.WriteLine($"[JWTHttpClient] WARNING: Token vacío, no se configuró Authorization header");
            }
        }

        /// <summary>
        /// Refreshes the token if necessary, if we were unable to and needed to return false
        /// </summary>
        /// <returns>
        /// True is successful or unnecessary
        /// False is unsuccessful or a failure
        /// </returns>
        public async Task<bool> CheckRefresh()
        {
            var token = await GetTokenAsync();

            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("[JWTHttpClient] CheckRefresh: Token vacío");
                // Si el token no existe, considerar que necesita refresco
                return true;
            }

            try
            {
                // Determinar si el token está expirado, si es así, refrescar
                var handler = new JwtSecurityTokenHandler();
                var readedToken = handler.ReadJwtToken(token);
                DateTime expdate = readedToken.ValidTo;

                Console.WriteLine($"[JWTHttpClient] CheckRefresh: Token expira {expdate:yyyy-MM-dd HH:mm:ss} UTC, Ahora: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");

                if (expdate < DateTime.UtcNow)
                {
                    Console.WriteLine("[JWTHttpClient] CheckRefresh: Token EXPIRADO");
                    return true;
                }

                Console.WriteLine("[JWTHttpClient] CheckRefresh: Token VÁLIDO");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[JWTHttpClient] CheckRefresh ERROR: {ex.Message}");
                return true;
            }
        }

        public async Task<HttpResponseMessage> GetAPIAsync(string path)
        {
            try
            {
                // CORRECCIÓN: Verificar que SÍ hay conexión (no negar la condición)
                if (InternetUtil.IsConnected)
                {
                    var token = await GetTokenAsync();

                    // Si el token está vacío o expirado, retornar Unauthorized
                    if (string.IsNullOrEmpty(token) || await CheckRefresh())
                    {
                        System.Diagnostics.Debug.WriteLine($"GetAPIAsync: Token inválido o expirado para {path}");
                        return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized)
                        {
                            Content = new StringContent("")
                        };
                    }

                    // Asegurar que el header de autorización esté configurado
                    DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                    var response = await GetAsync(path);

                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        System.Diagnostics.Debug.WriteLine($"GetAPIAsync: Respuesta Unauthorized del servidor para {path}");
                    }

                    return response;
                }

                // Si NO hay conexión, retornar error de red
                System.Diagnostics.Debug.WriteLine($"GetAPIAsync: Sin conexión para {path}");
                return new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable)
                {
                    Content = new StringContent("")
                };
            }
            catch (HttpRequestException httpEx)
            {
                CrashlyticsHelper.LogError(httpEx, "JWTHttpClient", "GetAPIAsync");
                throw;
            }
            catch (TaskCanceledException tcEx)
            {
                CrashlyticsHelper.LogError(tcEx, "JWTHttpClient", "GetAPIAsync");
                throw;
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "JWTHttpClient", "GetAPIAsync");
                throw new TokenException("Error refreshing token", ex);
            }
        }

        public async Task<HttpResponseMessage> PostAPIAsync(string path, HttpContent content)
        {
            try
            {
                // CORRECCIÓN: Verificar que SÍ hay conexión (no negar la condición)
                if (InternetUtil.IsConnected)
                {
                    var token = await GetTokenAsync();

                    // Si el token está vacío o expirado, retornar Unauthorized
                    if (string.IsNullOrEmpty(token) || await CheckRefresh())
                    {
                        Console.WriteLine($"[JWTHttpClient] PostAPIAsync: Token inválido o expirado para {path}");
                        return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized)
                        {
                            Content = new StringContent("")
                        };
                    }

                    // Asegurar que el header de autorización esté configurado
                    DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    Console.WriteLine($"[JWTHttpClient] PostAPIAsync: Authorization header configurado para {path}");

                    var response = await PostAsync(path, content);

                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        System.Diagnostics.Debug.WriteLine($"PostAPIAsync: Respuesta Unauthorized del servidor para {path}");
                    }

                    return response;
                }

                // Si NO hay conexión, retornar error de red
                System.Diagnostics.Debug.WriteLine($"PostAPIAsync: Sin conexión para {path}");
                return new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable)
                {
                    Content = new StringContent("")
                };
            }
            catch (HttpRequestException httpEx)
            {
                CrashlyticsHelper.LogError(httpEx, "JWTHttpClient", "PostAPIAsync");
                throw;
            }
            catch (TaskCanceledException tcEx)
            {
                CrashlyticsHelper.LogError(tcEx, "JWTHttpClient", "PostAPIAsync");
                throw;
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "JWTHttpClient", "PostAPIAsync");
                throw new TokenException("Error refreshing token", ex);
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
