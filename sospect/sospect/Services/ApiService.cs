// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using sospect.Models;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Microsoft.Maui.Storage;
using sospect.AuthHelpers;
using sospect.Utils;
using sospect.Helpers;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Net.Security;
using Microsoft.Maui.Controls;
using sospect.Views;
#if ANDROID
using Plugin.FirebasePushNotifications;
#endif
using sospect.Interfaces;
using sospect.DTOs;

namespace sospect.Services
{
    public class ApiService
    {
        /// <summary>
        /// Helper para parsear respuestas JSON de forma segura
        /// </summary>
        private static async Task<string> SafeParseDataField(HttpResponseMessage response)
        {
            if (response == null)
                return string.Empty;

            string content = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(content))
            {
                System.Diagnostics.Debug.WriteLine("SafeParseDataField: Respuesta vacía");
                return string.Empty;
            }

            try
            {
                var data = JObject.Parse(content)["data"];
                return data?.ToString() ?? string.Empty;
            }
            catch (JsonReaderException ex)
            {
                System.Diagnostics.Debug.WriteLine($"SafeParseDataField: Error parseando JSON: {ex.Message}");
                return string.Empty;
            }
        }

        public static async Task<ResponseMessage> RegisterUser(Persona persona)
        {
            try
            {
                using (var client = new HttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        //bypass
                        return true;
                    },
                }
            , false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    //POST Method
                    string url = AppConfiguration.ApiHost + AppConfiguration.RegisterUser;

                    StringContent content = new StringContent(JsonConvert.SerializeObject(persona), System.Text.Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAsync(url, content);
                    var responses = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<ResponseMessage>(responses);
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "RegisterUser");
                return new ResponseMessage() { IsSuccess = false, Data = "", Message = ex.Message };
            }

        }

        public static async Task<ResponseMessage> InsertarAlarma(Alarma alarma)
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        //bypass
                        return true;
                    },
                }
            , false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    //POST Method
                    string url = AppConfiguration.ApiHost + AppConfiguration.RegistrarAlarma;

                    StringContent content = new StringContent(JsonConvert.SerializeObject(alarma), System.Text.Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAPIAsync(url, content);
                    if (response.IsSuccessStatusCode)
                    {
                        ResponseMessage responseMessage = JsonConvert.DeserializeObject<ResponseMessage>(await response.Content.ReadAsStringAsync());
                        return responseMessage;
                    }
                    else
                    {
                        ResponseMessage responseError = JsonConvert.DeserializeObject<ResponseMessage>(await response.Content.ReadAsStringAsync());
                        return responseError;
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "InsertarAlarma");
                return new ResponseMessage() { IsSuccess = false, Data = "", Message = ex.Message };
            }
        }

        public static async Task<List<AlarmaCercana>> ActualizarUbicacion(Ubicaciones ubicacionUsuario)
        {
            try
            {
                // VALIDACIÓN 1: Verificar parámetro de entrada
                if (ubicacionUsuario == null)
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: ubicacionUsuario es null");
                    return new List<AlarmaCercana>();
                }

                // VALIDACIÓN 2: Verificar propiedades críticas
                if (string.IsNullOrEmpty(ubicacionUsuario.p_user_id_thirdparty))
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: p_user_id_thirdparty está vacío");
                    return new List<AlarmaCercana>();
                }

                // VALIDACIÓN 3: Verificar token de acceso ANTES de crear JWTHttpClient
                var token = await SecureStorage.GetAsync("access_token");
                if (string.IsNullOrEmpty(token))
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: Token de acceso está vacío - no se puede crear JWTHttpClient");
                    return new List<AlarmaCercana>();
                }

                System.Diagnostics.Debug.WriteLine($"VALIDACIONES OK - UserID: {ubicacionUsuario.p_user_id_thirdparty}, Token existe: {!string.IsNullOrEmpty(token)}");

                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        //bypass
                        return true;
                    },
                }, false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    //POST Method
                    string url = AppConfiguration.ApiHost + AppConfiguration.InsertarUbicacionUsuario;
                    ubicacionUsuario.Idioma = IdiomUtil.ObtenerCodigoDeIdioma();
                    StringContent content = new StringContent(JsonConvert.SerializeObject(ubicacionUsuario), System.Text.Encoding.UTF8, "application/json");

                    System.Diagnostics.Debug.WriteLine($"Llamando API: {url}");
                    HttpResponseMessage response = await client.PostAPIAsync(url, content);

                    // CORRECCIÓN: Detectar token expirado
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        System.Diagnostics.Debug.WriteLine("ActualizarUbicacion: Token expirado o inválido");
                        return new List<AlarmaCercana>();
                    }

                    if (response.IsSuccessStatusCode)
                    {
                        // DEBUG: Leer el contenido completo de la respuesta
                        string fullResponse = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine($"[ApiService] ===== RAW JSON RESPONSE =====");
                        System.Diagnostics.Debug.WriteLine($"[ApiService] Response length: {fullResponse?.Length ?? 0} caracteres");

                        // Parsear el campo "data" del JSON
                        string dataJson = string.Empty;
                        try
                        {
                            if (!string.IsNullOrWhiteSpace(fullResponse))
                            {
                                var data = JObject.Parse(fullResponse)["data"];
                                dataJson = data?.ToString() ?? string.Empty;
                            }
                        }
                        catch (JsonReaderException ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[ApiService] Error parseando JSON: {ex.Message}");
                        }

                        if (string.IsNullOrWhiteSpace(dataJson))
                        {
                            System.Diagnostics.Debug.WriteLine("[ApiService] ActualizarUbicacion: No se pudo obtener data del response");
                            return new List<AlarmaCercana>();
                        }

                        // DEBUG: Imprimir primeros 500 caracteres del JSON de data
                        System.Diagnostics.Debug.WriteLine($"[ApiService] Data JSON (primeros 500 chars): {(dataJson.Length > 500 ? dataJson.Substring(0, 500) + "..." : dataJson)}");

                        // PATRÓN DTO: Deserializar a DTOs primero (objetos planos, sin INotifyPropertyChanged)
                        List<AlarmaCercanaDto> dtos = JsonConvert.DeserializeObject<List<AlarmaCercanaDto>>(dataJson);

                        // Convertir DTOs a ViewModels solo para uso en UI
                        List<AlarmaCercana> responseMessage = dtos?.Select(dto => new AlarmaCercana(dto)).ToList()
                                                             ?? new List<AlarmaCercana>();

                        // DEBUG CRÍTICO: Imprimir cuántas alarmas se deserializaron
                        System.Diagnostics.Debug.WriteLine($"[ApiService] ===== DESERIALIZACIÓN EXITOSA =====");
                        System.Diagnostics.Debug.WriteLine($"[ApiService] Total alarmas deserializadas: {responseMessage?.Count ?? 0}");

                        if (responseMessage != null && responseMessage.Count > 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"[ApiService] Primera alarma - ID: {responseMessage[0].alarma_id}, Distancia: {responseMessage[0].distancia_en_metros}m");
                            System.Diagnostics.Debug.WriteLine($"[ApiService] Última alarma - ID: {responseMessage[responseMessage.Count - 1].alarma_id}, Distancia: {responseMessage[responseMessage.Count - 1].distancia_en_metros}m");
                        }

                        return responseMessage;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[ApiService] API Error - StatusCode: {response.StatusCode}");

                        // DEBUG: Leer el contenido del error para diagnóstico
                        try
                        {
                            string errorContent = await response.Content.ReadAsStringAsync();
                            System.Diagnostics.Debug.WriteLine($"[ApiService] Error Response: {errorContent}");
                        }
                        catch (Exception readEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"[ApiService] No se pudo leer el contenido del error: {readEx.Message}");
                        }

                        return new List<AlarmaCercana>();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EXCEPCIÓN en ActualizarUbicacion: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");

                CrashlyticsHelper.LogError(ex, "ApiService", "ActualizarUbicacion", new Dictionary<string, string> {
                    { "ErrorType", ex.GetType().Name }
                });

                return new List<AlarmaCercana>();
            }
        }

        /// <summary>
        /// Obtiene el feed "Para ti" desde el endpoint separado.
        /// ORDER BY con decay temporal en el backend. Sin offset (siempre retorna top fresco).
        /// </summary>
        public static async Task<List<AlarmaCercana>> ObtenerFeedParaTi(Ubicaciones ubicacionUsuario, int seed)
        {
            try
            {
                if (ubicacionUsuario == null || string.IsNullOrEmpty(ubicacionUsuario.p_user_id_thirdparty))
                {
                    System.Diagnostics.Debug.WriteLine("[ApiService] ObtenerFeedParaTi: ubicación o user_id es null");
                    return new List<AlarmaCercana>();
                }

                var token = await SecureStorage.GetAsync("access_token");
                if (string.IsNullOrEmpty(token))
                {
                    System.Diagnostics.Debug.WriteLine("[ApiService] ObtenerFeedParaTi: Token vacío");
                    return new List<AlarmaCercana>();
                }

                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true,
                }, false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    string url = AppConfiguration.ApiHost + AppConfiguration.ObtenerFeedParaTi + $"?seed={seed}";
                    ubicacionUsuario.Idioma = IdiomUtil.ObtenerCodigoDeIdioma();
                    StringContent content = new StringContent(
                        JsonConvert.SerializeObject(ubicacionUsuario),
                        System.Text.Encoding.UTF8, "application/json");

                    System.Diagnostics.Debug.WriteLine($"[ApiService] ObtenerFeedParaTi llamando: {url}");
                    HttpResponseMessage response = await client.PostAPIAsync(url, content);

                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        System.Diagnostics.Debug.WriteLine("[ApiService] ObtenerFeedParaTi: Token expirado");
                        return new List<AlarmaCercana>();
                    }

                    if (response.IsSuccessStatusCode)
                    {
                        string fullResponse = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine($"[ApiService] ObtenerFeedParaTi: Response length = {fullResponse?.Length ?? 0}");

                        var data = JObject.Parse(fullResponse)["data"];
                        string dataJson = data?.ToString() ?? string.Empty;

                        if (string.IsNullOrWhiteSpace(dataJson))
                        {
                            System.Diagnostics.Debug.WriteLine("[ApiService] ObtenerFeedParaTi: data vacía");
                            return new List<AlarmaCercana>();
                        }

                        List<AlarmaCercanaDto> dtos = JsonConvert.DeserializeObject<List<AlarmaCercanaDto>>(dataJson);
                        List<AlarmaCercana> result = dtos?.Select(dto => new AlarmaCercana(dto)).ToList()
                                                     ?? new List<AlarmaCercana>();

                        System.Diagnostics.Debug.WriteLine($"[ApiService] ObtenerFeedParaTi: {result.Count} alarmas deserializadas");
                        return result;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[ApiService] ObtenerFeedParaTi: Error StatusCode = {response.StatusCode}");
                        return new List<AlarmaCercana>();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ApiService] EXCEPCIÓN en ObtenerFeedParaTi: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "ApiService", "ObtenerFeedParaTi", new Dictionary<string, string> {
                    { "ErrorType", ex.GetType().Name }
                });
                return new List<AlarmaCercana>();
            }
        }

        /// <summary>
        /// Obtiene el feed "Siguiendo" desde el endpoint dedicado.
        /// Creado: 2026-04-20 — reemplaza la consulta que vivía dentro de InsertarUbicacion.
        /// Desacoplado para evitar contención en vw_alarmas_completa bajo carga concurrente.
        /// </summary>
        public static async Task<List<AlarmaCercana>> ObtenerFeedSiguiendo(Ubicaciones ubicacionUsuario)
        {
            try
            {
                if (ubicacionUsuario == null || string.IsNullOrEmpty(ubicacionUsuario.p_user_id_thirdparty))
                {
                    System.Diagnostics.Debug.WriteLine("[ApiService] ObtenerFeedSiguiendo: ubicación o user_id es null");
                    return new List<AlarmaCercana>();
                }

                var token = await SecureStorage.GetAsync("access_token");
                if (string.IsNullOrEmpty(token))
                {
                    System.Diagnostics.Debug.WriteLine("[ApiService] ObtenerFeedSiguiendo: Token vacío");
                    return new List<AlarmaCercana>();
                }

                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true,
                }, false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.Timeout = TimeSpan.FromSeconds(30);

                    string url = AppConfiguration.ApiHost + AppConfiguration.ObtenerFeedSiguiendo;
                    ubicacionUsuario.Idioma = IdiomUtil.ObtenerCodigoDeIdioma();
                    StringContent content = new StringContent(
                        JsonConvert.SerializeObject(ubicacionUsuario),
                        System.Text.Encoding.UTF8, "application/json");

                    System.Diagnostics.Debug.WriteLine($"[ApiService] ObtenerFeedSiguiendo llamando: {url}");
                    HttpResponseMessage response = await client.PostAPIAsync(url, content);

                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        System.Diagnostics.Debug.WriteLine("[ApiService] ObtenerFeedSiguiendo: Token expirado");
                        return new List<AlarmaCercana>();
                    }

                    if (response.IsSuccessStatusCode)
                    {
                        string fullResponse = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine($"[ApiService] ObtenerFeedSiguiendo: Response length = {fullResponse?.Length ?? 0}");

                        var data = JObject.Parse(fullResponse)["data"];
                        string dataJson = data?.ToString() ?? string.Empty;

                        if (string.IsNullOrWhiteSpace(dataJson))
                        {
                            System.Diagnostics.Debug.WriteLine("[ApiService] ObtenerFeedSiguiendo: data vacía");
                            return new List<AlarmaCercana>();
                        }

                        List<AlarmaCercanaDto> dtos = JsonConvert.DeserializeObject<List<AlarmaCercanaDto>>(dataJson);
                        List<AlarmaCercana> result = dtos?.Select(dto => new AlarmaCercana(dto)).ToList()
                                                     ?? new List<AlarmaCercana>();

                        System.Diagnostics.Debug.WriteLine($"[ApiService] ObtenerFeedSiguiendo: {result.Count} alarmas deserializadas");
                        return result;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[ApiService] ObtenerFeedSiguiendo: Error StatusCode = {response.StatusCode}");
                        return new List<AlarmaCercana>();
                    }
                }
            }
            catch (TaskCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("[ApiService] ObtenerFeedSiguiendo: Timeout (TaskCanceledException)");
                return new List<AlarmaCercana>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ApiService] EXCEPCIÓN en ObtenerFeedSiguiendo: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "ApiService", "ObtenerFeedSiguiendo", new Dictionary<string, string> {
                    { "ErrorType", ex.GetType().Name }
                });
                return new List<AlarmaCercana>();
            }
        }

        /// <summary>
        /// ENDPOINT OPTIMIZADO PARA BACKGROUND SERVICE
        /// Creado: 2026-02-10
        /// Actualiza ubicación en segundo plano y verifica alarmas críticas
        /// Solo retorna COUNT de alarmas (no JSON completo)
        /// Incluye throttling de 10 minutos automático en el backend
        /// </summary>
        public static async Task<(bool success, UbicacionBackgroundResponse response, string error)> ActualizarUbicacionBackground(
            decimal latitud,
            decimal longitud,
            string paisId = "CO",
            string idioma = "es")
        {
            try
            {
                if (string.IsNullOrEmpty(App.persona?.user_id_thirdparty))
                {
                    System.Diagnostics.Debug.WriteLine("[ApiService Background] user_id_thirdparty es null");
                    return (false, null, "Usuario no identificado");
                }

                var token = await SecureStorage.GetAsync("access_token");
                if (string.IsNullOrEmpty(token))
                {
                    System.Diagnostics.Debug.WriteLine("[ApiService Background] Token vacío");
                    return (false, null, "Token no disponible");
                }

                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true,
                }, false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    // Construir URL del nuevo endpoint
                    string url = AppConfiguration.ApiHost + "/ubicaciones/InsertaUbicacionBackground";

                    // Construir body con el modelo Ubicaciones
                    var ubicacion = new Ubicaciones
                    {
                        p_user_id_thirdparty = App.persona.user_id_thirdparty,
                        latitud = (double)latitud,
                        longitud = (double)longitud,
                        Pais = paisId,
                        Idioma = idioma,
                        PantallaOrigen = "BackgroundService"
                    };

                    StringContent content = new StringContent(
                        JsonConvert.SerializeObject(ubicacion),
                        System.Text.Encoding.UTF8,
                        "application/json");

                    System.Diagnostics.Debug.WriteLine($"[ApiService Background] Llamando: {url}");
                    System.Diagnostics.Debug.WriteLine($"[ApiService Background] Lat: {latitud}, Lon: {longitud}");

                    HttpResponseMessage response = await client.PostAPIAsync(url, content);
                    string responseContent = await response.Content.ReadAsStringAsync();

                    // Detectar token expirado
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        System.Diagnostics.Debug.WriteLine("[ApiService Background] Token expirado o inválido");
                        return (false, null, "Token expirado");
                    }

                    if (response.IsSuccessStatusCode)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ApiService Background] Response length: {responseContent?.Length ?? 0}");

                        // Parsear respuesta
                        var apiResponse = JObject.Parse(responseContent);
                        var dataToken = apiResponse["data"];

                        if (dataToken == null)
                        {
                            System.Diagnostics.Debug.WriteLine("[ApiService Background] data token es null");
                            return (false, null, "Respuesta inválida del servidor");
                        }

                        // Deserializar a UbicacionBackgroundResponse
                        var backgroundResponse = JsonConvert.DeserializeObject<UbicacionBackgroundResponse>(dataToken.ToString());

                        if (backgroundResponse != null)
                        {
                            System.Diagnostics.Debug.WriteLine(
                                $"[ApiService Background] Success: {backgroundResponse.CantidadAlarmas} alarmas, " +
                                $"notif sent: {backgroundResponse.NotificacionEnviada}, " +
                                $"mensaje: {backgroundResponse.Mensaje}");
                            return (true, backgroundResponse, null);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("[ApiService Background] Deserialización retornó null");
                            return (false, null, "Error deserializando respuesta");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[ApiService Background] HTTP Error: {response.StatusCode}");
                        System.Diagnostics.Debug.WriteLine($"[ApiService Background] Response: {responseContent}");
                        return (false, null, $"Error HTTP {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ApiService Background] Exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ApiService Background] StackTrace: {ex.StackTrace}");

                CrashlyticsHelper.LogError(ex, "ApiService", "ActualizarUbicacionBackground", new Dictionary<string, string> {
                    { "ErrorType", ex.GetType().Name },
                    { "Latitud", latitud.ToString() },
                    { "Longitud", longitud.ToString() }
                });

                return (false, null, ex.Message);
            }
        }

        public static async Task<List<AlarmaCercana>> ActualizarHistorial(Ubicaciones ubicacionUsuario)
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        //bypass
                        return true;
                    },
                }
            , false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    //POST Method
                    string url = AppConfiguration.ApiHost + AppConfiguration.HistorialAlarmasUbicacion;
                    ubicacionUsuario.Idioma = IdiomUtil.ObtenerCodigoDeIdioma();
                    StringContent content = new StringContent(JsonConvert.SerializeObject(ubicacionUsuario), System.Text.Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAPIAsync(url, content);
                    if (response.IsSuccessStatusCode)
                    {
                        // PATRÓN DTO: Deserializar a DTOs primero
                        string dataJson = JObject.Parse(await response.Content.ReadAsStringAsync())["data"].ToString();
                        List<AlarmaCercanaDto> dtos = JsonConvert.DeserializeObject<List<AlarmaCercanaDto>>(dataJson);

                        // Convertir DTOs a ViewModels
                        List<AlarmaCercana> responseMessage = dtos?.Select(dto => new AlarmaCercana(dto)).ToList()
                                                             ?? new List<AlarmaCercana>();
                        return responseMessage;
                    }
                    else
                    {
                        List<AlarmaCercana> responseError = new List<AlarmaCercana>();
                        return responseError;
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "ActualizarHistorial");
                List<AlarmaCercana> responseError = new List<AlarmaCercana>();
                return responseError;
            }
        }

        public static async Task<ResponseMessage> DescribirAlarma(DescribirAlarma descripcionAlarma)
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        //bypass
                        return true;
                    },
                }
            , false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    //POST Method
                    string url = AppConfiguration.ApiHost + AppConfiguration.DescribirAlarma;

                    StringContent content = new StringContent(JsonConvert.SerializeObject(descripcionAlarma), System.Text.Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAPIAsync(url, content);
                    if (response.IsSuccessStatusCode)
                    {
                        ResponseMessage responseMessage = JsonConvert.DeserializeObject<ResponseMessage>(await response.Content.ReadAsStringAsync());
                        return responseMessage;
                    }
                    else
                    {
                        ResponseMessage responseError = JsonConvert.DeserializeObject<ResponseMessage>(await response.Content.ReadAsStringAsync());
                        return responseError;
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "DescribirAlarma");
                return new ResponseMessage() { IsSuccess = false, Data = "", Message = ex.Message };
            }
        }

        public static async Task<ResponseMessage> ActualizarDescripcionAlarma(DescribirAlarma descripcionAlarma)
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        //bypass
                        return true;
                    },
                }
            , false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    //POST Method
                    string url = AppConfiguration.ApiHost + AppConfiguration.ActualizarDescripcionAlarma;

                    StringContent content = new StringContent(JsonConvert.SerializeObject(descripcionAlarma), System.Text.Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAPIAsync(url, content);
                    if (response.IsSuccessStatusCode)
                    {
                        ResponseMessage responseMessage = JsonConvert.DeserializeObject<ResponseMessage>(await response.Content.ReadAsStringAsync());
                        return responseMessage;
                    }
                    else
                    {
                        ResponseMessage responseError = JsonConvert.DeserializeObject<ResponseMessage>(await response.Content.ReadAsStringAsync());
                        return responseError;
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "ActualizarDescripcionAlarma");
                return new ResponseMessage() { IsSuccess = false, Data = "", Message = ex.Message };
            }
        }

        public static async Task<List<DetalleDescripcionAlarma>> ListarDescripcionesAlarma(AlarmaCercana descripcionAlarma)
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        //bypass
                        return true;
                    },
                }
            , false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    //GET Method
                    string url = AppConfiguration.ApiHost + AppConfiguration.ListarDescripcionesAlarma + "?alarma_id=" + descripcionAlarma.alarma_id + "&user_id_thirdparty=" + App.persona.user_id_thirdparty + "&idioma_dispositivo=" + IdiomUtil.ObtenerCodigoDeIdioma();

                    HttpResponseMessage response = await client.GetAPIAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        string jsonContent = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine($"[API SERVICE] JSON recibido (primeros 500): {jsonContent.Substring(0, Math.Min(500, jsonContent.Length))}...");

                        string dataJson = JObject.Parse(jsonContent)["data"].ToString();
                        System.Diagnostics.Debug.WriteLine($"[API SERVICE] ===== data JSON COMPLETO =====");
                        System.Diagnostics.Debug.WriteLine(dataJson);
                        System.Diagnostics.Debug.WriteLine($"[API SERVICE] ===== FIN data JSON =====");

                        // Configurar settings para capturar errores
                        var settings = new JsonSerializerSettings
                        {
                            Error = (sender, args) =>
                            {
                                System.Diagnostics.Debug.WriteLine($"[API SERVICE ERROR] JSON Error: {args.ErrorContext.Error.Message}");
                                System.Diagnostics.Debug.WriteLine($"[API SERVICE ERROR] Path: {args.ErrorContext.Path}");
                                System.Diagnostics.Debug.WriteLine($"[API SERVICE ERROR] Member: {args.ErrorContext.Member}");
                                args.ErrorContext.Handled = true; // Continuar a pesar del error
                            }
                        };

                        // Prueba: Intentar deserializar solo el array de fotos de la primera descripcion
                        try
                        {
                            var dataArray = JArray.Parse(dataJson);
                            if (dataArray.Count > 0)
                            {
                                var primeraDesc = dataArray[0];
                                var fotosJson = primeraDesc["fotos"]?.ToString();
                                System.Diagnostics.Debug.WriteLine($"[API SERVICE TEST] JSON de fotos: {fotosJson}");

                                if (!string.IsNullOrEmpty(fotosJson))
                                {
                                    var fotosTest = JsonConvert.DeserializeObject<List<FotoDescripcionAlarma>>(fotosJson);
                                    System.Diagnostics.Debug.WriteLine($"[API SERVICE TEST] Fotos deserializadas directamente: {fotosTest?.Count ?? 0}");
                                    if (fotosTest != null && fotosTest.Any())
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[API SERVICE TEST] Primera foto - ID: {fotosTest[0].FotoId}, URL: {fotosTest[0].UrlFoto}");
                                    }
                                }
                            }
                        }
                        catch (Exception exTest)
                        {
                            System.Diagnostics.Debug.WriteLine($"[API SERVICE TEST ERROR] {exTest.Message}");
                        }

                        List<DetalleDescripcionAlarma> responseMessage = JsonConvert.DeserializeObject<List<DetalleDescripcionAlarma>>(dataJson, settings);
                        System.Diagnostics.Debug.WriteLine($"[API SERVICE] Descripciones deserializadas: {responseMessage?.Count ?? 0}");

                        if (responseMessage != null && responseMessage.Any())
                        {
                            foreach (var desc in responseMessage)
                            {
                                System.Diagnostics.Debug.WriteLine($"[API SERVICE] Descripcion {desc.Iddescripcion} - Fotos en objeto: {desc.Fotos?.Count ?? 0}");
                            }
                        }

                        return responseMessage;
                    }
                    else
                    {
                        List<DetalleDescripcionAlarma> EmptyList = new List<DetalleDescripcionAlarma>();
                        return EmptyList;
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "ListarDescripcionesAlarma");
                // CORRECCIÓN: Retornar lista vacía en lugar de null para evitar NullReferenceException
                return new List<DetalleDescripcionAlarma>();
            }
        }

        public static async Task<RespuestaCalificacionDescripcion?> CalificarDescripcionAlarma(CalificacionDescripcionAlarma calificacionDescripcionAlarma)
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        //bypass
                        return true;
                    },
                }
            , false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    //POST Method
                    string url = AppConfiguration.ApiHost + AppConfiguration.CalificarDescripcionesAlarma;

                    StringContent content = new StringContent(JsonConvert.SerializeObject(calificacionDescripcionAlarma), System.Text.Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAPIAsync(url, content);
                    if (response.IsSuccessStatusCode)
                    {
                        RespuestaCalificacionDescripcion responseMessage = JsonConvert.DeserializeObject<RespuestaCalificacionDescripcion>(JObject.Parse(await response.Content.ReadAsStringAsync())["data"].ToString());
                        return responseMessage;
                    }
                    else
                    {
                        RespuestaCalificacionDescripcion? responseError = null;
                        return responseError;
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "CalificarDescripcionAlarma");
                return null;
            }
        }

        public static async Task<ResponseMessage> CalificarAlarma(CalificarAlarma calificacionAlarma)
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        //bypass
                        return true;
                    },
                }
            , false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    //POST Method
                    string url = AppConfiguration.ApiHost + AppConfiguration.CalificarAlarma;

                    StringContent content = new StringContent(JsonConvert.SerializeObject(calificacionAlarma), System.Text.Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAPIAsync(url, content);

                    // Get the response content as string
                    string responseContent = await response.Content.ReadAsStringAsync();

                    // Print or log the response content for debugging
                    Debug.WriteLine(responseContent);

                    if (response.IsSuccessStatusCode)
                    {
                        ResponseMessage responseMessage = JsonConvert.DeserializeObject<ResponseMessage>(await response.Content.ReadAsStringAsync());
                        return responseMessage;
                    }
                    else
                    {
                        ResponseMessage responseError = JsonConvert.DeserializeObject<ResponseMessage>(await response.Content.ReadAsStringAsync());
                        return responseError;
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "CalificarAlarma");
                return new ResponseMessage() { IsSuccess = false, Data = "", Message = ex.Message };
            }
        }

        public static async Task<List<TipoAlarma>> ObtenerTiposAlarmaCompletos()  // Era: ObtenerIdsTiposAlarma
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        //bypass
                        return true;
                    },
                }, false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    string url = AppConfiguration.ApiHost + AppConfiguration.ListarTiposAlarma + "?idioma=" + IdiomUtil.ObtenerCodigoDeIdioma();
                    HttpResponseMessage response = await client.GetAPIAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonString = await response.Content.ReadAsStringAsync();

                        //  LOG PARA DEBUGGING
                        System.Diagnostics.Debug.WriteLine($"=== JSON DE TIPOS ALARMA ===");
                        System.Diagnostics.Debug.WriteLine(jsonString);
                        System.Diagnostics.Debug.WriteLine($"=== FIN JSON ===");

                        List<TipoAlarma> tiposAlarma = JsonConvert.DeserializeObject<List<TipoAlarma>>(
                            JObject.Parse(jsonString)["data"].ToString()
                        );

                        //  LOG: Verificar qué se deserializó
                        foreach (var tipo in tiposAlarma)
                        {
                            System.Diagnostics.Debug.WriteLine($"Deserializado: ID={tipo.TipoalarmaId}, Icono='{tipo.Icono}'");
                        }

                        //  CAMBIO CRÍTICO: Retornar LA LISTA COMPLETA, no solo los IDs
                        return tiposAlarma;
                    }
                    else
                    {
                        return new List<TipoAlarma>();
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "ObtenerTiposAlarmaCompletos");
                return null;
            }
        }

        public static async Task<List<TipoAlarmaReporte>> ObtenerReportBasParticipacionTipoAlarma()
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        //bypass
                        return true;
                    },
                }
                , false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    //GET Method
                    string url = AppConfiguration.ApiHost + AppConfiguration.ListarParticipacionTipoAlarma + "?user_id_thirdParty=" + App.persona.user_id_thirdparty + "&idioma_dispositivo=" + IdiomUtil.ObtenerCodigoDeIdioma();

                    HttpResponseMessage response = await client.GetAPIAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        List<TipoAlarmaReporte> reportesAlarma = JsonConvert.DeserializeObject<List<TipoAlarmaReporte>>(JObject.Parse(await response.Content.ReadAsStringAsync())["data"].ToString());

                        return reportesAlarma;
                    }
                    else
                    {
                        return new List<TipoAlarmaReporte>();
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "ObtenerReportBasParticipacionTipoAlarma");
                return null;
            }
        }

        public static async Task<List<MetricasBasicasReporte>> ObtenerReportBasMetricasSueltas()
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        //bypass
                        return true;
                    },
                }
                , false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    //GET Method
                    string url = AppConfiguration.ApiHost + AppConfiguration.ListaMetricasPersonales + "?user_id_thirdParty=" + App.persona.user_id_thirdparty + "&idioma_dispositivo=" + IdiomUtil.ObtenerCodigoDeIdioma();

                    HttpResponseMessage response = await client.GetAPIAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        List<MetricasBasicasReporte> MetricasBasicas = JsonConvert.DeserializeObject<List<MetricasBasicasReporte>>(JObject.Parse(await response.Content.ReadAsStringAsync())["data"].ToString());

                        return MetricasBasicas;
                    }
                    else
                    {
                        return new List<MetricasBasicasReporte>();
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "ObtenerReportBasMetricasSueltas");
                return null;
            }
        }

        public static async Task<PromEfectivoAlarmasReporteBasResponse> ObtenerPromedioEfectivoAlarmas()
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        //bypass
                        return true;
                    },
                }
                , false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    //GET Method
                    string url = AppConfiguration.ApiHost + AppConfiguration.ObtenerPromedioEfectivoAlarmas + "?user_id_thirdParty=" + App.persona.user_id_thirdparty + "&idioma_dispositivo=" + IdiomUtil.ObtenerCodigoDeIdioma();

                    HttpResponseMessage response = await client.GetAPIAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        PromEfectivoAlarmasReporteBasResponse EfectividadAlarmas = JsonConvert.DeserializeObject<PromEfectivoAlarmasReporteBasResponse>(JObject.Parse(await response.Content.ReadAsStringAsync())["data"].ToString());

                        return EfectividadAlarmas;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "ObtenerPromedioEfectivoAlarmas");
                return null;
            }
        }

        //public static async Task<List<TipoAlarma>> ObtenerTiposAlarma()
        //{
        //    try
        //    {
        //        using (var client = new JWTHttpClient(new HttpClientHandler()
        //        {
        //            ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
        //            {
        //                //bypass
        //                return true;
        //            },
        //        }
        //    , false))
        //        {
        //            client.DefaultRequestHeaders.Accept.Clear();
        //            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        //
        //            //GET Method
        //            string url = AppConfiguration.ApiHost + AppConfiguration.ListarTiposAlarma + "?idioma=" + IdiomUtil.ObtenerCodigoDeIdioma();
        //
        //            HttpResponseMessage response = await client.GetAPIAsync(url);
        //            if (response.IsSuccessStatusCode)
        //            {
        //                List<TipoAlarma> responseMessage = JsonConvert.DeserializeObject<List<TipoAlarma>>(JObject.Parse(await response.Content.ReadAsStringAsync())["data"].ToString());
        //                return responseMessage;
        //            }
        //            else
        //            {
        //                List<TipoAlarma> EmptyList = new List<TipoAlarma>();
        //                return EmptyList;
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return null;
        //    }
        //}

        public static async Task<List<Mensajes>> ObtenerMensajes()
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        //bypass
                        return true;
                    },
                }
            , false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    //GET Method
                    string url = AppConfiguration.ApiHost + AppConfiguration.ListarMensajes + "?user_id_thirdparty=" + App.persona.user_id_thirdparty + "&idioma_dispositivo=" + IdiomUtil.ObtenerCodigoDeIdioma();

                    HttpResponseMessage response = await client.GetAPIAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        List<Mensajes> responseMessage = JsonConvert.DeserializeObject<List<Mensajes>>(JObject.Parse(await response.Content.ReadAsStringAsync())["data"].ToString());
                        return responseMessage;
                    }
                    else
                    {
                        List<Mensajes> EmptyList = new List<Mensajes>();
                        return EmptyList;
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "ObtenerMensajes");
                return null;
            }
        }

        public static async Task<DetalleMensajes> ObtenerDetalleMensajes(DetalleMensajeRequest detalleMensajeRequest)
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        //bypass
                        return true;
                    },
                }
            , false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    //POST Method
                    string url = AppConfiguration.ApiHost + AppConfiguration.LeerMensaje;

                    StringContent content = new StringContent(JsonConvert.SerializeObject(detalleMensajeRequest), System.Text.Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAPIAsync(url, content);
                    if (response.IsSuccessStatusCode)
                    {
                        DetalleMensajes responseMessage = JsonConvert.DeserializeObject<DetalleMensajes>(JObject.Parse(await response.Content.ReadAsStringAsync())["data"].ToString());
                        return responseMessage;
                    }
                    else
                    {
                        return new DetalleMensajes() { };
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "ObtenerDetalleMensajes");
                return new DetalleMensajes() { };
            }
        }

        public static async Task<bool> MarcaTodosLeidos(MarcarMensajesLeidosRequest MarcaLeidosRequest)
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        //bypass
                        return true;
                    },
                }
            , false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    //POST Method
                    string url = AppConfiguration.ApiHost + AppConfiguration.MarcarTodosComoLeidos;

                    StringContent content = new StringContent(JsonConvert.SerializeObject(MarcaLeidosRequest), System.Text.Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAPIAsync(url, content);
                    return response.IsSuccessStatusCode;

                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "MarcaTodosLeidos");
                return false;
            }
        }

        public static async Task<List<SuperPower>> ObtenerValoresPoderes()
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        //bypass
                        return true;
                    },
                }, false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    //GET Method
                    string url = AppConfiguration.ApiHost + AppConfiguration.ListarValoresPoderes;

                    HttpResponseMessage response = await client.GetAPIAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        List<SuperPower> responseMessage = JsonConvert.DeserializeObject<List<SuperPower>>(JObject.Parse(await response.Content.ReadAsStringAsync())["data"].ToString());
                        return responseMessage;
                    }
                    else
                    {
                        List<SuperPower> EmptyList = new List<SuperPower>();
                        return EmptyList;
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "ObtenerValoresPoderes");
                return null;
            }
        }
        public static async Task<List<SubscriptionValue>> ObtenerValoresDeSubscripcion()
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        //bypass
                        return true;
                    },
                }
                , false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    //GET Method
                    string url = AppConfiguration.ApiHost + AppConfiguration.ListarTiposSubscripciones + "?idioma_dispositivo=" + IdiomUtil.ObtenerCodigoDeIdioma();

                    HttpResponseMessage response = await client.GetAPIAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        List<SubscriptionValue> subscriptionValues = JsonConvert.DeserializeObject<List<SubscriptionValue>>(JObject.Parse(await response.Content.ReadAsStringAsync())["data"].ToString());
                        return subscriptionValues;
                    }
                    else
                    {
                        List<SubscriptionValue> EmptyList = new List<SubscriptionValue>();
                        return EmptyList;
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "ObtenerValoresDeSubscripcion");
                return null;
            }
        }

        public static async Task<bool> ComprarSuperPoder(CompraSuperPoderRequest compraRequest)
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        //bypass
                        return true;
                    },
                }, false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    //POST Method
                    string url = AppConfiguration.ApiHost + AppConfiguration.CompraPoderes;
                    StringContent content = new StringContent(JsonConvert.SerializeObject(compraRequest), Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAPIAsync(url, content);
                    return response.IsSuccessStatusCode;
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "ComprarSuperPoder");
                // Manejar excepciones aquí
                return false;
            }
        }

        public static async Task<List<RelationshipType>> ObtenerTiposRelaciones()
        {
            string url = "";
            string rawResponse = "";
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        //bypass
                        return true;
                    },
                }, false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    url = AppConfiguration.ApiHost + AppConfiguration.ListarTiposRelaciones;
                    System.Diagnostics.Debug.WriteLine($"[DIAG-ObtenerTiposRelaciones] URL: {url}");

                    HttpResponseMessage response = await client.GetAPIAsync(url);
                    rawResponse = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"[DIAG-ObtenerTiposRelaciones] StatusCode: {response.StatusCode}");
                    System.Diagnostics.Debug.WriteLine($"[DIAG-ObtenerTiposRelaciones] RawResponse (primeros 1000 chars): {rawResponse?.Substring(0, Math.Min(rawResponse?.Length ?? 0, 1000))}");

                    if (response.IsSuccessStatusCode)
                    {
                        var parsedJson = JObject.Parse(rawResponse);
                        var dataToken = parsedJson["data"];
                        System.Diagnostics.Debug.WriteLine($"[DIAG-ObtenerTiposRelaciones] data token existe: {dataToken != null}");
                        System.Diagnostics.Debug.WriteLine($"[DIAG-ObtenerTiposRelaciones] data token tipo: {dataToken?.Type}");

                        List<RelationshipType> responseMessage = JsonConvert.DeserializeObject<List<RelationshipType>>(dataToken.ToString());
                        System.Diagnostics.Debug.WriteLine($"[DIAG-ObtenerTiposRelaciones] Items deserializados: {responseMessage?.Count ?? 0}");
                        return responseMessage;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[DIAG-ObtenerTiposRelaciones] ERROR - StatusCode no exitoso: {response.StatusCode}");
                        CrashlyticsHelper.LogError(new Exception($"ObtenerTiposRelaciones HTTP Error: {response.StatusCode}, Response: {rawResponse}"), "ApiService", "ObtenerTiposRelaciones");
                        List<RelationshipType> EmptyList = new List<RelationshipType>();
                        return EmptyList;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DIAG-ObtenerTiposRelaciones] EXCEPCION: {ex.GetType().Name}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[DIAG-ObtenerTiposRelaciones] URL: {url}");
                System.Diagnostics.Debug.WriteLine($"[DIAG-ObtenerTiposRelaciones] RawResponse: {rawResponse}");
                System.Diagnostics.Debug.WriteLine($"[DIAG-ObtenerTiposRelaciones] StackTrace: {ex.StackTrace}");
                CrashlyticsHelper.LogError(ex, "ApiService", "ObtenerTiposRelaciones", new Dictionary<string, string> { { "url", url }, { "rawResponse", rawResponse?.Substring(0, Math.Min(rawResponse?.Length ?? 0, 500)) ?? "null" } });
                return null;
            }
        }

        public static async Task<ResponseMessage> RequestPermissionAsync(RequestPermissionModel requestPermission)
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        //bypass
                        return true;
                    },
                }
                , false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    //POST Method
                    string url = AppConfiguration.ApiHost + AppConfiguration.SolicitarPermisoAProtegido;

                    StringContent content = new StringContent(JsonConvert.SerializeObject(requestPermission), System.Text.Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAPIAsync(url, content);
                    if (response.IsSuccessStatusCode)
                    {
                        ResponseMessage responseMessage = JsonConvert.DeserializeObject<ResponseMessage>(await response.Content.ReadAsStringAsync());
                        return responseMessage;
                    }
                    else
                    {
                        ResponseMessage responseError = JsonConvert.DeserializeObject<ResponseMessage>(await response.Content.ReadAsStringAsync());
                        return responseError;
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "RequestPermissionAsync");
                return new ResponseMessage() { IsSuccess = false, Data = "", Message = ex.Message };
            }
        }
        public static async Task<ApiResponse> ObtenerSolicitudesPendientes(string user_id_thirdparty)
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        //bypass
                        return true;
                    },
                }
                , false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    string url = AppConfiguration.ApiHost + AppConfiguration.ListarSolicitudesPendientes + "?user_id_thirdparty=" + user_id_thirdparty;

                    HttpResponseMessage response = await client.GetAPIAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        ApiResponse responseMessage = JsonConvert.DeserializeObject<ApiResponse>(await response.Content.ReadAsStringAsync());
                        return responseMessage;
                    }
                    else
                    {
                        ApiResponse EmptyApiResponse = new ApiResponse { IsSuccess = false, Data = null, Message = "Error al obtener solicitudes pendientes" };
                        return EmptyApiResponse;
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "ObtenerSolicitudesPendientes");
                return null;
            }

        }

        public static async Task<ResponseMessage> AprobarSolicitudAsync(AprobarRechazarSolicitudModel requestModel)
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        //bypass
                        return true;
                    },
                }, false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    string url = AppConfiguration.ApiHost + AppConfiguration.AprobarPermisoAProtector;

                    StringContent content = new StringContent(JsonConvert.SerializeObject(requestModel), System.Text.Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAPIAsync(url, content);
                    if (response.IsSuccessStatusCode)
                    {
                        ResponseMessage responseMessage = JsonConvert.DeserializeObject<ResponseMessage>(await response.Content.ReadAsStringAsync());
                        return responseMessage;
                    }
                    else
                    {
                        ResponseMessage responseError = JsonConvert.DeserializeObject<ResponseMessage>(await response.Content.ReadAsStringAsync());
                        return responseError;
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "AprobarSolicitudAsync");
                return new ResponseMessage() { IsSuccess = false, Data = "", Message = ex.Message };
            }
        }

        public static async Task<ResponseMessage> RechazarSolicitudAsync(AprobarRechazarSolicitudModel requestModel)
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        //bypass
                        return true;
                    },
                }, false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    string url = AppConfiguration.ApiHost + AppConfiguration.RechazarPermisoAProtector;

                    StringContent content = new StringContent(JsonConvert.SerializeObject(requestModel), System.Text.Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAPIAsync(url, content);
                    if (response.IsSuccessStatusCode)
                    {
                        ResponseMessage responseMessage = JsonConvert.DeserializeObject<ResponseMessage>(await response.Content.ReadAsStringAsync());
                        return responseMessage;
                    }
                    else
                    {
                        ResponseMessage responseError = JsonConvert.DeserializeObject<ResponseMessage>(await response.Content.ReadAsStringAsync());
                        return responseError;
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "RechazarSolicitudAsync");
                return new ResponseMessage() { IsSuccess = false, Data = "", Message = ex.Message };
            }
        }

        public static async Task<ApprovedSubscriptionResponse> ObtenerSolicitudesAprobadas()
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        //bypass
                        return true;
                    },
                }
            , false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    //GET Method
                    string url = AppConfiguration.ApiHost + AppConfiguration.ListarSolicitudesAprobadas + "?user_id_thirdparty=" + App.persona.user_id_thirdparty;

                    HttpResponseMessage response = await client.GetAPIAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        ApprovedSubscriptionResponse responseMessage = JsonConvert.DeserializeObject<ApprovedSubscriptionResponse>(await response.Content.ReadAsStringAsync());
                        return responseMessage;
                    }
                    else
                    {
                        return new ApprovedSubscriptionResponse { IsSuccess = false, Data = null, Message = "Error al obtener las solicitudes aprobadas." };
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "ObtenerSolicitudesAprobadas");
                return new ApprovedSubscriptionResponse { IsSuccess = false, Data = null, Message = ex.Message };
            }
        }

        public static async Task<ResponseMessage> CompletarSubscripcion(CompleteSubscriptionRequest requestData)
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        //bypass
                        return true;
                    },
                }
            , false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    //POST Method
                    string url = AppConfiguration.ApiHost + AppConfiguration.SubscripcionProtegido;

                    StringContent content = new StringContent(JsonConvert.SerializeObject(requestData), System.Text.Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAPIAsync(url, content);
                    if (response.IsSuccessStatusCode)
                    {
                        ResponseMessage responseMessage = JsonConvert.DeserializeObject<ResponseMessage>(await response.Content.ReadAsStringAsync());
                        return responseMessage;
                    }
                    else
                    {
                        ResponseMessage responseError = JsonConvert.DeserializeObject<ResponseMessage>(await response.Content.ReadAsStringAsync());
                        return responseError;
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "CompletarSubscripcion");
                return new ResponseMessage() { IsSuccess = false, Data = "", Message = ex.Message };
            }
        }

        public static async Task<List<ProtectedUserData>> GetProtectedUsersAsync()
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        //bypass
                        return true;
                    },
                }
            , false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    //GET Method
                    string url = AppConfiguration.ApiHost + AppConfiguration.ListarProtegidos + "?user_id_thirdParty_protector=" + App.persona.user_id_thirdparty + "&idioma_dispositivo=" + IdiomUtil.ObtenerCodigoDeIdioma();

                    HttpResponseMessage response = await client.GetAPIAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        List<ProtectedUserData> responseMessage = JsonConvert.DeserializeObject<List<ProtectedUserData>>(JObject.Parse(await response.Content.ReadAsStringAsync())["data"].ToString());
                        return responseMessage;
                    }
                    else
                    {
                        List<ProtectedUserData> EmptyList = new List<ProtectedUserData>();
                        return EmptyList;
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "GetProtectedUsersAsync");
                return null;
            }
        }

        public static async Task<ResponseMessage> DeleteProtectedUserAsync(EliminarProtegidoRequest eliminarProtegidoRequest)
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        //bypass
                        return true;
                    },
                }
            , false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    //POST Method
                    string url = AppConfiguration.ApiHost + AppConfiguration.EliminarProtegido;

                    StringContent content = new StringContent(JsonConvert.SerializeObject(eliminarProtegidoRequest), System.Text.Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAPIAsync(url, content);
                    if (response.IsSuccessStatusCode)
                    {
                        ResponseMessage responseMessage = JsonConvert.DeserializeObject<ResponseMessage>(await response.Content.ReadAsStringAsync());
                        return responseMessage;
                    }
                    else
                    {
                        ResponseMessage responseError = JsonConvert.DeserializeObject<ResponseMessage>(await response.Content.ReadAsStringAsync());
                        return responseError;
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "DeleteProtectedUserAsync");
                return new ResponseMessage { IsSuccess = false, Message = $"Error: {ex.Message}" };
            }
        }

        public static async Task<ProtectorResponse> ObtenerProtectores(string userId, string idioma)
        {
            string url = "";
            string rawResponse = "";
            try
            {
                System.Diagnostics.Debug.WriteLine($"[DIAG-ObtenerProtectores] Iniciando - userId: {userId}, idioma: {idioma}");

                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        //bypass
                        return true;
                    },
                }, false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    //GET Method
                    url = AppConfiguration.ApiHost + AppConfiguration.ListarProtectores + $"?user_id_thirdParty_protegido={userId}&idioma_dispositivo={idioma}";
                    System.Diagnostics.Debug.WriteLine($"[DIAG-ObtenerProtectores] URL: {url}");

                    HttpResponseMessage response = await client.GetAPIAsync(url);
                    rawResponse = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"[DIAG-ObtenerProtectores] StatusCode: {response.StatusCode}");
                    System.Diagnostics.Debug.WriteLine($"[DIAG-ObtenerProtectores] RawResponse (primeros 1000 chars): {rawResponse?.Substring(0, Math.Min(rawResponse?.Length ?? 0, 1000))}");

                    if (response.IsSuccessStatusCode)
                    {
                        ProtectorResponse protectorsResponse = JsonConvert.DeserializeObject<ProtectorResponse>(rawResponse);
                        System.Diagnostics.Debug.WriteLine($"[DIAG-ObtenerProtectores] Deserializacion exitosa - isSuccess: {protectorsResponse?.isSuccess}, dataCount: {protectorsResponse?.data?.Count ?? 0}");
                        return protectorsResponse;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[DIAG-ObtenerProtectores] ERROR - StatusCode no exitoso: {response.StatusCode}");
                        CrashlyticsHelper.LogError(new Exception($"ObtenerProtectores HTTP Error: {response.StatusCode}, Response: {rawResponse}"), "ApiService", "ObtenerProtectores");
                        return new ProtectorResponse { isSuccess = false, data = null, message = $"Error al obtener protectores. HTTP {response.StatusCode}" };
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DIAG-ObtenerProtectores] EXCEPCION: {ex.GetType().Name}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[DIAG-ObtenerProtectores] URL: {url}");
                System.Diagnostics.Debug.WriteLine($"[DIAG-ObtenerProtectores] RawResponse: {rawResponse}");
                System.Diagnostics.Debug.WriteLine($"[DIAG-ObtenerProtectores] StackTrace: {ex.StackTrace}");
                CrashlyticsHelper.LogError(ex, "ApiService", "ObtenerProtectores", new Dictionary<string, string> { { "url", url }, { "rawResponse", rawResponse?.Substring(0, Math.Min(rawResponse?.Length ?? 0, 500)) ?? "null" } });
                return new ProtectorResponse { isSuccess = false, data = null, message = ex.Message };
            }
        }

        public static async Task<ResponseMessage> SuspenderPermisoAProtector(SuspenderPermisoRequest suspenderPermisoRequest)
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        //bypass
                        return true;
                    },
                }, false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    //POST Method
                    string url = AppConfiguration.ApiHost + AppConfiguration.SuspenderPermisoAProtector;

                    StringContent content = new StringContent(JsonConvert.SerializeObject(suspenderPermisoRequest), System.Text.Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAPIAsync(url, content);
                    if (response.IsSuccessStatusCode)
                    {
                        ResponseMessage responseMessage = JsonConvert.DeserializeObject<ResponseMessage>(await response.Content.ReadAsStringAsync());
                        return responseMessage;
                    }
                    else
                    {
                        ResponseMessage responseError = JsonConvert.DeserializeObject<ResponseMessage>(await response.Content.ReadAsStringAsync());
                        return responseError;
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "SuspenderPermisoAProtector");
                return new ResponseMessage() { IsSuccess = false, Data = "", Message = ex.Message };
            }
        }

        public static async Task<ResponseMessage> EliminarProtector(EliminarProtectorRequest eliminarProtectorRequest)
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        //bypass
                        return true;
                    },
                }, false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    //POST Method
                    string url = AppConfiguration.ApiHost + AppConfiguration.EliminarProtector;

                    StringContent content = new StringContent(JsonConvert.SerializeObject(eliminarProtectorRequest), System.Text.Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAPIAsync(url, content);
                    if (response.IsSuccessStatusCode)
                    {
                        ResponseMessage responseMessage = JsonConvert.DeserializeObject<ResponseMessage>(await response.Content.ReadAsStringAsync());
                        return responseMessage;
                    }
                    else
                    {
                        ResponseMessage responseError = JsonConvert.DeserializeObject<ResponseMessage>(await response.Content.ReadAsStringAsync());
                        return responseError;
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "EliminarProtector");
                return new ResponseMessage { IsSuccess = false, Message = $"Error: {ex.Message}" };
            }
        }

        public static async Task<List<MisSubscripciones>> ObtenerMisSubscripciones()
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        //bypass
                        return true;
                    },
                }
                , false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    //GET Method
                    string url = AppConfiguration.ApiHost + AppConfiguration.ListarMisSubscripciones + "?user_id_thirdParty_protector=" + App.persona.user_id_thirdparty + "&idioma_dispositivo=" + IdiomUtil.ObtenerCodigoDeIdioma();

                    HttpResponseMessage response = await client.GetAPIAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        List<MisSubscripciones> misSubscripciones = JsonConvert.DeserializeObject<List<MisSubscripciones>>(JObject.Parse(await response.Content.ReadAsStringAsync())["data"].ToString());
                        return misSubscripciones;
                    }
                    else
                    {
                        List<MisSubscripciones> EmptyList = new List<MisSubscripciones>();
                        return EmptyList;
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "ObtenerMisSubscripciones");
                return null;
            }
        }

        public static async Task<ResponseMessage> RenovarSubscripcion(RenovarSubscripcionRequest renovarSubscripcionRequest)
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        //bypass
                        return true;
                    },
                }
            , false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    //POST Method
                    string url = AppConfiguration.ApiHost + AppConfiguration.RenovarSubscripcion;

                    StringContent content = new StringContent(JsonConvert.SerializeObject(renovarSubscripcionRequest), System.Text.Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAPIAsync(url, content);
                    if (response.IsSuccessStatusCode)
                    {
                        ResponseMessage responseMessage = JsonConvert.DeserializeObject<ResponseMessage>(await response.Content.ReadAsStringAsync());
                        return responseMessage;
                    }
                    else
                    {
                        ResponseMessage responseError = JsonConvert.DeserializeObject<ResponseMessage>(await response.Content.ReadAsStringAsync());
                        return responseError;
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "RenovarSubscripcion");
                return new ResponseMessage() { IsSuccess = false, Data = "", Message = ex.Message };
            }
        }

        public static async Task<ResponseMessage> CancelarSubscripcion(CancelarSubscripcionRequest cancelarSubscripcionRequest)
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        //bypass
                        return true;
                    },
                }
            , false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    //POST Method
                    string url = AppConfiguration.ApiHost + AppConfiguration.CancelarSubscripcion;

                    StringContent content = new StringContent(JsonConvert.SerializeObject(cancelarSubscripcionRequest), System.Text.Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAPIAsync(url, content);
                    if (response.IsSuccessStatusCode)
                    {
                        ResponseMessage responseMessage = JsonConvert.DeserializeObject<ResponseMessage>(await response.Content.ReadAsStringAsync());
                        return responseMessage;
                    }
                    else
                    {
                        ResponseMessage responseError = JsonConvert.DeserializeObject<ResponseMessage>(await response.Content.ReadAsStringAsync());
                        return responseError;
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "CancelarSubscripcion");
                return new ResponseMessage() { IsSuccess = false, Data = "", Message = ex.Message };
            }
        }

        public static async Task<ResponseMessage> NuevaZonaVigilancia(NuevaZonaVRequest nuevaZonaVRequest)
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        //bypass
                        return true;
                    },
                }
            , false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    //POST Method
                    string url = AppConfiguration.ApiHost + AppConfiguration.SubscripcionZonaVigilancia;

                    StringContent content = new StringContent(JsonConvert.SerializeObject(nuevaZonaVRequest), System.Text.Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAPIAsync(url, content);
                    if (response.IsSuccessStatusCode)
                    {
                        ResponseMessage responseMessage = JsonConvert.DeserializeObject<ResponseMessage>(await response.Content.ReadAsStringAsync());
                        return responseMessage;
                    }
                    else
                    {
                        ResponseMessage responseError = JsonConvert.DeserializeObject<ResponseMessage>(await response.Content.ReadAsStringAsync());
                        return responseError;
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "NuevaZonaVigilancia");
                return new ResponseMessage() { IsSuccess = false, Data = "", Message = ex.Message };
            }
        }
        public static async Task<List<RadiosDisponiblesResponse>> ObtenerRadiosDisponibles()
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        //bypass
                        return true;
                    },
                }
                , false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    //GET Method
                    string url = AppConfiguration.ApiHost + AppConfiguration.ListarRadiosDisponibles + "?user_id_thirdparty=" + App.persona.user_id_thirdparty;

                    HttpResponseMessage response = await client.GetAPIAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        List<RadiosDisponiblesResponse> misRadios = JsonConvert.DeserializeObject<List<RadiosDisponiblesResponse>>(JObject.Parse(await response.Content.ReadAsStringAsync())["data"].ToString());
                        return misRadios;
                    }
                    else
                    {
                        List<RadiosDisponiblesResponse> EmptyList = new List<RadiosDisponiblesResponse>();
                        return EmptyList;
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "ObtenerRadiosDisponibles");
                return null;
            }
        }

        public static async Task<ResponseMessage> SubscribirNuevoRadio(NuevoRadioRequest subscribirNuevoRadio)
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        //bypass
                        return true;
                    },
                }
            , false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    //POST Method
                    string url = AppConfiguration.ApiHost + AppConfiguration.SubscripcionRadioAlarmas;

                    StringContent content = new StringContent(JsonConvert.SerializeObject(subscribirNuevoRadio), System.Text.Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAPIAsync(url, content);
                    if (response.IsSuccessStatusCode)
                    {
                        ResponseMessage responseMessage = JsonConvert.DeserializeObject<ResponseMessage>(await response.Content.ReadAsStringAsync());
                        return responseMessage;
                    }
                    else
                    {
                        ResponseMessage responseError = JsonConvert.DeserializeObject<ResponseMessage>(await response.Content.ReadAsStringAsync());
                        return responseError;
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "SubscribirNuevoRadio");
                return new ResponseMessage() { IsSuccess = false, Data = "", Message = ex.Message };
            }
        }
        public static async Task<string> ObtenerParametrosDeUsuario(ParametroUsuarioRequest parametroUsuarioRequest)
        {
            var LblSinConexionDetalle = await TranslateExtension.TranslateAsync("LblSinConexionDetalle");
            var LabelOK = await TranslateExtension.TranslateAsync("LabelOK");
            var LblErrorConexion = await TranslateExtension.TranslateAsync("LblErrorConexion");
            var LblCierreSesion = await TranslateExtension.TranslateAsync("LblCierreSesion");
            var LblCierreSesionDetalle = await TranslateExtension.TranslateAsync("LblCierreSesionDetalle");

            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        //bypass
                        return true;
                    },
                }
            , false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    //POST Method
                    string url = AppConfiguration.ApiHost + AppConfiguration.ConsultaParametrosUsuario;

                    Console.WriteLine($"[ApiService] ObtenerParametrosDeUsuario - URL: {url}");
                    Console.WriteLine($"[ApiService] Request - PUserIdThirdparty: {parametroUsuarioRequest.PUserIdThirdparty}");
                    Console.WriteLine($"[ApiService] Request - RegistrationId: {parametroUsuarioRequest.RegistrationId}");

                    StringContent content = new StringContent(JsonConvert.SerializeObject(parametroUsuarioRequest), System.Text.Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAPIAsync(url, content);

                    Console.WriteLine($"[ApiService] Response - StatusCode: {response.StatusCode}");

                    // CORRECCIÓN: Detectar token expirado y redirigir a login
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        Console.WriteLine("[ApiService] Token expirado o inválido - redirigiendo a login");
                        System.Diagnostics.Debug.WriteLine("ObtenerParametrosDeUsuario: Token expirado o inválido, redirigiendo a login");
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            Application.Current.MainPage = new NavigationPage(new LoginPage());
                        });
                        return string.Empty;
                    }

                    if (response.IsSuccessStatusCode)
                    {
                        // CORRECCIÓN: Usar SafeParseDataField para prevenir crashes con JSON vacío
                        string responseMessage = await SafeParseDataField(response);

                        Console.WriteLine($"[ApiService] Response length: {responseMessage?.Length ?? 0}");
                        // TEMPORAL: Mostrar respuesta completa para debugging
                        Console.WriteLine($"[ApiService] Response content: {responseMessage}");

                        if (string.IsNullOrWhiteSpace(responseMessage))
                        {
                            Console.WriteLine("[ApiService] Response vacío o nulo");
                            System.Diagnostics.Debug.WriteLine("ObtenerParametrosDeUsuario: No se pudo obtener data del response");
                            return string.Empty;
                        }

                        if (Connectivity.NetworkAccess == NetworkAccess.Internet)
                        {
                            if (responseMessage.Contains(" \"p_user_id_thirdparty\": null"))
                            {
                                Console.WriteLine("[ApiService] DETECTADO: p_user_id_thirdparty es NULL");

                                // NO cerrar sesión si estamos en proceso de primer login (race condition esperada)
                                if (App.IsFirstLoginInProgress)
                                {
                                    Console.WriteLine("[ApiService] Primer login en progreso - ignorando NULL por race condition");
                                    return string.Empty;
                                }

                                Console.WriteLine("[ApiService] Cerrando sesión por p_user_id_thirdparty NULL");

                                MainThread.BeginInvokeOnMainThread(async () => { await ModernAlerts.ShowWarning(LblCierreSesion, LblCierreSesionDetalle); });
                                await SecureStorage.SetAsync("access_token", "");

                                #if ANDROID
                                await IFirebasePushNotification.Current.UnregisterForPushNotificationsAsync();
                                #endif

                                MainThread.BeginInvokeOnMainThread(() => { App.Current.MainPage = new NavigationPage(new LoginPage()); });
                                return string.Empty;
                            }
                        }
                        else
                        {
                            MainThread.BeginInvokeOnMainThread(async () => { await ModernAlerts.ShowError(LblErrorConexion, LblSinConexionDetalle); });
                        }

                        return responseMessage;
                    }
                    else
                    {
                        Console.WriteLine($"[ApiService] ERROR - StatusCode: {response.StatusCode}");
                        string errorContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"[ApiService] Error content: {errorContent}");
                        System.Diagnostics.Debug.WriteLine($"ObtenerParametrosDeUsuario: Error {response.StatusCode}");
                        return string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] EXCEPTION: {ex.Message}");
                Console.WriteLine($"[ApiService] StackTrace: {ex.StackTrace}");
                CrashlyticsHelper.LogError(ex, "ApiService", "ObtenerParametrosDeUsuario");
                return string.Empty;
            }
        }

        public static async Task<string> ObtenerContratoUsuario()
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        //bypass
                        return true;
                    },
                }
                , false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    //GET Method
                    string url = AppConfiguration.ApiHost + AppConfiguration.ObtenerContrato + "?idioma_dispositivo=" + IdiomUtil.ObtenerCodigoDeIdioma();

                    HttpResponseMessage response = await client.GetAPIAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        string textoContrato = JObject.Parse(await response.Content.ReadAsStringAsync())["data"].ToString();
                        return textoContrato;
                    }
                    else
                    {
                        return string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "ObtenerContratoUsuario");
                return string.Empty;
            }
        }

        public static async Task<ResponseMessage> AceptarContratoDeUsuario(AcceptContractRequest acceptContractRequest)
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        //bypass
                        return true;
                    },
                }
            , false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    //POST Method
                    string url = AppConfiguration.ApiHost + AppConfiguration.AceptacionCondicionesUso;

                    StringContent content = new StringContent(JsonConvert.SerializeObject(acceptContractRequest), System.Text.Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAPIAsync(url, content);
                    if (response.IsSuccessStatusCode)
                    {
                        ResponseMessage responseMessage = JsonConvert.DeserializeObject<ResponseMessage>(await response.Content.ReadAsStringAsync());
                        return responseMessage;
                    }
                    else
                    {
                        return new ResponseMessage() { IsSuccess = false, Data = "", Message = "" }; ;
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "AceptarContratoDeUsuario");
                return new ResponseMessage() { IsSuccess = false, Data = "", Message = ex.Message };
            }
        }

        public static async Task<List<AlarmaCercana>> ObtenerAlarma(long? AlarmaId)
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        //bypass
                        return true;
                    },
                }
                , false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    //GET Method
                    string url = AppConfiguration.ApiHost + AppConfiguration.TraerAlarma + "?alarma_id=" + AlarmaId.ToString() + "&idioma_dispositivo=" + IdiomUtil.ObtenerCodigoDeIdioma() + "&user_id_thirdparty=" + App.persona.user_id_thirdparty;

                    HttpResponseMessage response = await client.GetAPIAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        // PATRÓN DTO: Deserializar a DTOs primero
                        string dataJson = JObject.Parse(await response.Content.ReadAsStringAsync())["data"].ToString();
                        List<AlarmaCercanaDto> dtos = JsonConvert.DeserializeObject<List<AlarmaCercanaDto>>(dataJson);

                        // Convertir DTOs a ViewModels
                        List<AlarmaCercana> AlarmasCercanas = dtos?.Select(dto => new AlarmaCercana(dto)).ToList()
                                                             ?? new List<AlarmaCercana>();
                        return AlarmasCercanas;
                    }
                    else
                    {
                        return new List<AlarmaCercana>();
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "ObtenerAlarma");
                return new List<AlarmaCercana>();
            }
        }

        public static async Task<VersionVerificada> VerificarVersion(int? versioncliente)
        {
            try
            {
                using (var client = new HttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        //bypass
                        return true;
                    },
                }
                , false))
                {
                    // IMPORTANTE: Configurar timeout de 10 segundos
                    client.Timeout = TimeSpan.FromSeconds(10);

                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    //GET Method
                    string url = AppConfiguration.ApiHost + AppConfiguration.ComprobarVersionActiva + "?versioncliente=" + versioncliente;

                    System.Diagnostics.Debug.WriteLine($"[VerificarVersion] URL: {url}");

                    HttpResponseMessage response = await client.GetAsync(url);

                    System.Diagnostics.Debug.WriteLine($"[VerificarVersion] StatusCode: {response.StatusCode}");

                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine($"[VerificarVersion] Response: {responseBody}");

                        VersionVerificada IsValidVersion = JsonConvert.DeserializeObject<VersionVerificada>(JObject.Parse(responseBody)["data"].ToString());

                        System.Diagnostics.Debug.WriteLine($"[VerificarVersion] flag_soportada: {IsValidVersion.flag_soportada}");

                        return IsValidVersion;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[VerificarVersion] Error HTTP - retornando VersionVerificada vacía");
                        return new VersionVerificada();
                    }
                }
            }
            catch (TaskCanceledException ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VerificarVersion] Timeout: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "ApiService", "VerificarVersion-Timeout");
                // En caso de timeout, asumir que la versión es válida para no bloquear al usuario
                return new VersionVerificada { flag_soportada = true };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VerificarVersion] Exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[VerificarVersion] StackTrace: {ex.StackTrace}");
                CrashlyticsHelper.LogError(ex, "ApiService", "VerificarVersion");
                // En caso de error, asumir que la versión es válida para no bloquear al usuario
                return new VersionVerificada { flag_soportada = true };
            }
        }
        public static async Task<ConsultaConfNotifResponse> ConsultarConfiguracionNotificaciones(string p_user_id_thirdparty)
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        return true;
                    },
                }, false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    string url = AppConfiguration.ApiHost + AppConfiguration.ConsultarConfigNotificaciones + "?p_user_id_thirdparty=" + p_user_id_thirdparty;

                    // SOLO ESTE CAMBIO: GetAsync → GetAPIAsync
                    HttpResponseMessage response = await client.GetAPIAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        ConsultaConfNotifResponse currentConfigNotif = JsonConvert.DeserializeObject<ConsultaConfNotifResponse>(
                            JObject.Parse(await response.Content.ReadAsStringAsync())["data"].ToString());
                        return currentConfigNotif;
                    }
                    else
                    {
                        return new ConsultaConfNotifResponse();
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "ConsultarConfiguracionNotificaciones");
                return new ConsultaConfNotifResponse();
            }
        }

        public static async Task<ResponseMessage> ActualizarConfiguracionNotificaciones(ActualizaConfNotifRequest actualizanotificacionesRequest)
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        // bypass
                        return true;
                    },
                }, false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    //POST Method
                    string url = AppConfiguration.ApiHost + AppConfiguration.ActualizaConfNotificaciones;

                    StringContent content = new StringContent(JsonConvert.SerializeObject(actualizanotificacionesRequest), System.Text.Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAPIAsync(url, content);

                    System.Diagnostics.Debug.WriteLine($"=== ApiService.ActualizarConfiguracionNotificaciones ===");
                    System.Diagnostics.Debug.WriteLine($"Status Code: {response.StatusCode}");
                    System.Diagnostics.Debug.WriteLine($"IsSuccessStatusCode: {response.IsSuccessStatusCode}");

                    if (response.IsSuccessStatusCode)
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine($"Response Content: {responseContent}");

                        ResponseMessage responseMessage = JsonConvert.DeserializeObject<ResponseMessage>(responseContent);
                        System.Diagnostics.Debug.WriteLine($"Deserialized IsSuccess: {responseMessage?.IsSuccess}");

                        return responseMessage;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"ERROR: Response not successful");
                        return new ResponseMessage() { IsSuccess = false, Data = "", Message = "" }; ;
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "ActualizarConfiguracionNotificaciones");
                return new ResponseMessage() { IsSuccess = false, Data = "", Message = ex.Message };
            }
        }

        public static async Task<ResponseMessage> CerrarAlarma(CerrarAlarmaRequest cerraralarmaRequest)
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        // bypass
                        return true;
                    },
                }, false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    //POST Method
                    string url = AppConfiguration.ApiHost + AppConfiguration.CerrarAlarma;

                    StringContent content = new StringContent(JsonConvert.SerializeObject(cerraralarmaRequest), System.Text.Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAPIAsync(url, content);
                    if (response.IsSuccessStatusCode)
                    {
                        ResponseMessage responseMessage = JsonConvert.DeserializeObject<ResponseMessage>(await response.Content.ReadAsStringAsync());
                        return responseMessage;
                    }
                    else
                    {
                        // Intentar leer el mensaje de error del body (ej. "already closed")
                        try
                        {
                            var errorBody = await response.Content.ReadAsStringAsync();
                            var errorResponse = JsonConvert.DeserializeObject<ResponseMessage>(errorBody);
                            if (errorResponse != null)
                                return errorResponse;
                        }
                        catch { }
                        return new ResponseMessage() { IsSuccess = false, Data = "", Message = "" };
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "CerrarAlarma");
                return new ResponseMessage() { IsSuccess = false, Data = "", Message = ex.Message };
            }
        }

        public static async Task<ResponseMessage> ProponerCierre(ProponerCierreRequest request)
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        return true;
                    },
                }, false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    string url = AppConfiguration.ApiHost + AppConfiguration.ProponerCierre;
                    StringContent content = new StringContent(JsonConvert.SerializeObject(request), System.Text.Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAPIAsync(url, content);
                    if (response.IsSuccessStatusCode)
                    {
                        ResponseMessage responseMessage = JsonConvert.DeserializeObject<ResponseMessage>(await response.Content.ReadAsStringAsync());
                        return responseMessage;
                    }
                    else
                    {
                        return new ResponseMessage() { IsSuccess = false, Data = "", Message = "" };
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "ProponerCierre");
                return new ResponseMessage() { IsSuccess = false, Data = "", Message = ex.Message };
            }
        }

        public static async Task<ResponseMessage> VotarCierre(VotarCierreRequest request)
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        return true;
                    },
                }, false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    string url = AppConfiguration.ApiHost + AppConfiguration.VotarCierre;
                    StringContent content = new StringContent(JsonConvert.SerializeObject(request), System.Text.Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAPIAsync(url, content);
                    // Leer siempre el body (tanto 200 como 400) para preservar el mensaje de error
                    var bodyStr = await response.Content.ReadAsStringAsync();
                    if (response.IsSuccessStatusCode)
                    {
                        ResponseMessage responseMessage = JsonConvert.DeserializeObject<ResponseMessage>(bodyStr);
                        return responseMessage;
                    }
                    else
                    {
                        // El API retorna BadRequest con ResponseMessage que tiene el mensaje del stored procedure
                        try
                        {
                            var errorResponse = JsonConvert.DeserializeObject<ResponseMessage>(bodyStr);
                            return errorResponse ?? new ResponseMessage() { IsSuccess = false, Data = "", Message = "" };
                        }
                        catch
                        {
                            return new ResponseMessage() { IsSuccess = false, Data = "", Message = bodyStr };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "VotarCierre");
                return new ResponseMessage() { IsSuccess = false, Data = "", Message = ex.Message };
            }
        }

        public static async Task<ResponseMessage> ObtenerSolicitudCierreActiva(long alarmaId, string userId)
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        return true;
                    },
                }, false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    string url = AppConfiguration.ApiHost + AppConfiguration.ObtenerSolicitudCierreActiva;
                    var requestBody = new { alarma_id = alarmaId, user_id_thirdparty = userId };
                    StringContent content = new StringContent(JsonConvert.SerializeObject(requestBody), System.Text.Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAPIAsync(url, content);
                    if (response.IsSuccessStatusCode)
                    {
                        ResponseMessage responseMessage = JsonConvert.DeserializeObject<ResponseMessage>(await response.Content.ReadAsStringAsync());
                        return responseMessage;
                    }
                    else
                    {
                        return new ResponseMessage() { IsSuccess = false, Data = "", Message = "" };
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "ObtenerSolicitudCierreActiva");
                return new ResponseMessage() { IsSuccess = false, Data = "", Message = ex.Message };
            }
        }

        public static async Task<ResponseMessage> AtenderAlarma(AtenderAlarmaRequest atenderalarmaRequest)
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        //bypass 
                        return true;
                    },
                }, false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    //POST Method
                    string url = AppConfiguration.ApiHost + AppConfiguration.AsignarAlarma;

                    StringContent content = new StringContent(JsonConvert.SerializeObject(atenderalarmaRequest), System.Text.Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAPIAsync(url, content);
                    if (response.IsSuccessStatusCode)
                    {
                        ResponseMessage responseMessage = JsonConvert.DeserializeObject<ResponseMessage>(await response.Content.ReadAsStringAsync());
                        return responseMessage;
                    }
                    else
                    {
                        return new ResponseMessage() { IsSuccess = false, Data = "", Message = "" }; ;
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "AtenderAlarma");
                return new ResponseMessage() { IsSuccess = false, Data = "", Message = ex.Message };
            }
        }

        public static async Task<ResponseMessage> WipeUserData(WipeDataRequest wipedataRequest)
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        //bypass 
                        return true;
                    },
                }, false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    //POST Method
                    string url = AppConfiguration.ApiHost + AppConfiguration.WipeUserData;

                    StringContent content = new StringContent(JsonConvert.SerializeObject(wipedataRequest), System.Text.Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAPIAsync(url, content);
                    if (response.IsSuccessStatusCode)
                    {
                        ResponseMessage responseMessage = JsonConvert.DeserializeObject<ResponseMessage>(await response.Content.ReadAsStringAsync());
                        return responseMessage;
                    }
                    else
                    {
                        return new ResponseMessage() { IsSuccess = false, Data = "", Message = "" }; ;
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "WipeUserData");
                return new ResponseMessage() { IsSuccess = false, Data = "", Message = ex.Message };
            }
        }
        public static async Task<ResponseMessage> AgregarDatosBasicos(InformacionInicialModelRequest informacionInicialRequest)
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        //bypass 
                        return true;
                    },
                }, false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    //POST Method
                    string url = AppConfiguration.ApiHost + AppConfiguration.LlenarInformacionInicial;

                    StringContent content = new StringContent(JsonConvert.SerializeObject(informacionInicialRequest), Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAPIAsync(url, content);
                    if (response.IsSuccessStatusCode)
                    {
                        ResponseMessage responseMessage = JsonConvert.DeserializeObject<ResponseMessage>(await response.Content.ReadAsStringAsync());
                        return responseMessage;
                    }
                    else
                    {
                        return new ResponseMessage() { IsSuccess = false, Data = "", Message = "Error en la solicitud." };
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "AgregarDatosBasicos");
                return new ResponseMessage() { IsSuccess = false, Data = "", Message = ex.Message };
            }
        }
        public static async Task<ConsultarUsuarioParaRedConfianzaResponse> ConsultarUsuarioParaRedConfianza(ConsultarUsuarioParaRedConfianzaRequest request)
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true,
                }, false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    //POST Method
                    string url = AppConfiguration.ApiHost + AppConfiguration.ConsultarUsuarioParaRedConfianza;

                    StringContent content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAPIAsync(url, content);
                    if (response.IsSuccessStatusCode)
                    {
                        ConsultarUsuarioParaRedConfianzaResponse responseMessage = JsonConvert.DeserializeObject<ConsultarUsuarioParaRedConfianzaResponse>(await response.Content.ReadAsStringAsync());
                        return responseMessage;
                    }
                    else
                    {
                        return new ConsultarUsuarioParaRedConfianzaResponse() { IsSuccess = false, Message = "Error in API response." };
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "ConsultarUsuarioParaRedConfianza");
                return new ConsultarUsuarioParaRedConfianzaResponse() { IsSuccess = false, Message = ex.Message };
            }
        }
        public static async Task<AgregarUsuarioRedConfianzaResponse> AgregarUsuarioRedConfianza(AgregarUsuarioRedConfianzaRequest request)
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        // Bypass 
                        return true;
                    },
                }, false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    // POST Method
                    string url = AppConfiguration.ApiHost + AppConfiguration.AgregarUsuarioRedConfianza;

                    StringContent content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAPIAsync(url, content);
                    if (response.IsSuccessStatusCode)
                    {
                        var jsonResponse = await response.Content.ReadAsStringAsync();
                        AgregarUsuarioRedConfianzaResponse responseMessage = JsonConvert.DeserializeObject<AgregarUsuarioRedConfianzaResponse>(jsonResponse);
                        return responseMessage;
                    }
                    else
                    {
                        return new AgregarUsuarioRedConfianzaResponse() { IsSuccess = false, Data = null, Message = "Error en la respuesta del servidor" };
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "AgregarUsuarioRedConfianza");
                return new AgregarUsuarioRedConfianzaResponse() { IsSuccess = false, Data = null, Message = ex.Message };
            }
        }
        public static async Task<List<UsuarioRedConfianza>> ListarUsuariosRedConfianza(ListarUsuariosRedConfianzaRequest request)
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        // Bypass
                        return true;
                    },
                }
                , false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    // POST Method
                    string url = AppConfiguration.ApiHost + AppConfiguration.ListarUsuariosRedConfianza;
                    StringContent content = new StringContent(JsonConvert.SerializeObject(request), System.Text.Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAPIAsync(url, content);
                    if (response.IsSuccessStatusCode)
                    {
                        List<UsuarioRedConfianza> responseMessage = JsonConvert.DeserializeObject<List<UsuarioRedConfianza>>(JObject.Parse(await response.Content.ReadAsStringAsync())["data"].ToString());
                        return responseMessage;
                    }
                    else
                    {
                        return new List<UsuarioRedConfianza>();
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "ListarUsuariosRedConfianza");
                return new List<UsuarioRedConfianza>();
            }
        }
        public static async Task<string> SubirArchivoMultimedia(string filePath)
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        // Bypass
                        return true;
                    },
                }, false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    // POST Method
                    string url = AppConfiguration.ApiHost + AppConfiguration.SubirArchivoMultimedia;
                    using (var multipartContent = new MultipartFormDataContent())
                    {
                        var fileContent = new ByteArrayContent(File.ReadAllBytes(filePath));
                        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
                        multipartContent.Add(fileContent, "file", Path.GetFileName(filePath));
                        HttpResponseMessage response = await client.PostAPIAsync(url, multipartContent);
                        if (response.IsSuccessStatusCode)
                        {
                            string remoteUrl = JObject.Parse(await response.Content.ReadAsStringAsync())["data"].ToString();
                            return remoteUrl;
                        }
                        else
                        {
                            return string.Empty;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "SubirArchivoMultimedia");
                return string.Empty;
            }
        }

        public static async Task<PresignedUrlResponse> SolicitarPresignedUrlAsync(string mimeType, string extension)
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true,
                }, false))
                {
                    var safeExt = extension.StartsWith(".") ? extension : $".{extension}";
                    string url = AppConfiguration.ApiHost + AppConfiguration.PresignedUploadUrl
                        + $"?mimeType={Uri.EscapeDataString(mimeType)}&extension={Uri.EscapeDataString(safeExt)}";

                    HttpResponseMessage response = await client.GetAPIAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        var json = JObject.Parse(await response.Content.ReadAsStringAsync());
                        return new PresignedUrlResponse
                        {
                            PresignedUrl = json["presignedUrl"]?.ToString(),
                            S3Key = json["s3Key"]?.ToString()
                        };
                    }

                    System.Diagnostics.Debug.WriteLine($"[SolicitarPresignedUrl] Error HTTP {response.StatusCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "SolicitarPresignedUrlAsync");
                return null;
            }
        }

        public static async Task SubirArchivoAPresignedUrlAsync(string presignedUrl, string filePath, string mimeType)
        {
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromMinutes(5);

                using (var fileStream = File.OpenRead(filePath))
                using (var content = new StreamContent(fileStream))
                {
                    content.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
                    var response = await client.PutAsync(presignedUrl, content);

                    if (!response.IsSuccessStatusCode)
                    {
                        var body = await response.Content.ReadAsStringAsync();
                        throw new Exception($"Upload a S3 falló: {response.StatusCode} — {body}");
                    }
                }
            }
        }

        /// <summary>
        /// Obtiene tipos de alarma con estadísticas de los últimos 30 días
        /// Usado para el filtro de alarmas en MAUI
        /// Fecha: 2025-12-20
        /// </summary>
        public static async Task<List<TipoAlarmaConEstadisticas>> ObtenerTiposAlarmaConEstadisticas()
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        // Bypass certificate validation
                        return true;
                    },
                }, false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    // GET Method
                    string url = AppConfiguration.ApiHost + "/Alarma/ListarTiposAlarmaConEstadisticas";

                    System.Diagnostics.Debug.WriteLine($"[TiposConEstadisticas] Llamando a: {url}");

                    HttpResponseMessage response = await client.GetAPIAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonString = await response.Content.ReadAsStringAsync();

                        System.Diagnostics.Debug.WriteLine($"=== JSON TIPOS CON ESTADISTICAS ===");
                        System.Diagnostics.Debug.WriteLine(jsonString);
                        System.Diagnostics.Debug.WriteLine($"=== FIN JSON ===");

                        var tiposConEstadisticas = JsonConvert.DeserializeObject<List<TipoAlarmaConEstadisticas>>(
                            JObject.Parse(jsonString)["data"].ToString()
                        );

                        // Log para verificar deserialización
                        foreach (var tipo in tiposConEstadisticas)
                        {
                            System.Diagnostics.Debug.WriteLine(
                                $"Tipo: ID={tipo.TipoalarmaId}, " +
                                $"ShortAlias='{tipo.ShortAlias}', " +
                                $"Porcentaje={tipo.PorcentajeUltimoMes}%, " +
                                $"Cantidad={tipo.CantidadUltimoMes}"
                            );
                        }

                        return tiposConEstadisticas;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[TiposConEstadisticas] Error HTTP: {response.StatusCode}");
                        return new List<TipoAlarmaConEstadisticas>();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TiposConEstadisticas] Exception: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "ApiService", "ObtenerTiposAlarmaConEstadisticas");
                return new List<TipoAlarmaConEstadisticas>();
            }
        }

        /// <summary>
        /// Obtiene los conteos de usuarios disponibles por intervalos de radio
        /// para calcular el alcance de una promoción local
        /// </summary>
        public static async Task<ObtenerConteosPorIntervalosResponse> ObtenerConteosPorIntervalos(ObtenerConteosPorIntervalosRequest request)
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        //bypass
                        return true;
                    },
                }
            , false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    //POST Method
                    string url = AppConfiguration.ApiHost + "/AlarmaPromocional/ObtenerConteosPorIntervalos";

                    StringContent content = new StringContent(JsonConvert.SerializeObject(request), System.Text.Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAPIAsync(url, content);
                    if (response.IsSuccessStatusCode)
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();
                        var responseObj = JObject.Parse(responseContent);

                        if (responseObj["isSuccess"]?.ToObject<bool>() == true)
                        {
                            var dataObj = responseObj["data"];
                            var conteosArray = dataObj["conteos"];

                            var conteos = new List<ConteoUsuariosPorRadio>();
                            if (conteosArray != null)
                            {
                                foreach (var conteo in conteosArray)
                                {
                                    conteos.Add(new ConteoUsuariosPorRadio
                                    {
                                        RadioMetros = conteo["radioMetros"]?.ToObject<int>() ?? 0,
                                        CantidadUsuarios = conteo["cantidadUsuarios"]?.ToObject<int>() ?? 0
                                    });
                                }
                            }

                            return new ObtenerConteosPorIntervalosResponse
                            {
                                IsSuccess = true,
                                Conteos = conteos,
                                Message = responseObj["message"]?.ToString() ?? ""
                            };
                        }
                        else
                        {
                            return new ObtenerConteosPorIntervalosResponse
                            {
                                IsSuccess = false,
                                Conteos = new List<ConteoUsuariosPorRadio>(),
                                Message = responseObj["message"]?.ToString() ?? "Error al obtener conteos"
                            };
                        }
                    }
                    else
                    {
                        return new ObtenerConteosPorIntervalosResponse
                        {
                            IsSuccess = false,
                            Conteos = new List<ConteoUsuariosPorRadio>(),
                            Message = $"Error HTTP: {response.StatusCode}"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "ObtenerConteosPorIntervalos");
                return new ObtenerConteosPorIntervalosResponse
                {
                    IsSuccess = false,
                    Conteos = new List<ConteoUsuariosPorRadio>(),
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// Consulta si un emprendimiento existe por NIT y determina si el usuario es propietario
        /// </summary>
        public static async Task<ConsultarEmprendimientoResponse> ConsultarEmprendimientoPorNIT(ConsultarEmprendimientoRequest request)
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        //bypass
                        return true;
                    },
                }
            , false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    //POST Method
                    string url = AppConfiguration.ApiHost + "/Emprendimientos/ConsultarPorNIT";

                    StringContent content = new StringContent(JsonConvert.SerializeObject(request), System.Text.Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAPIAsync(url, content);
                    if (response.IsSuccessStatusCode)
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();
                        var responseObj = JObject.Parse(responseContent);

                        if (responseObj["isSuccess"]?.ToObject<bool>() == true)
                        {
                            var dataObj = responseObj["data"];

                            return new ConsultarEmprendimientoResponse
                            {
                                IsSuccess = true,
                                existe = dataObj["existe"]?.ToObject<bool>() ?? false,
                                es_propietario = dataObj["es_propietario"]?.ToObject<bool?>(),
                                emprendimiento = dataObj["emprendimiento"]?.ToObject<EmprendimientoDto>(),
                                Message = responseObj["message"]?.ToString() ?? "Consulta exitosa"
                            };
                        }
                        else
                        {
                            return new ConsultarEmprendimientoResponse
                            {
                                IsSuccess = false,
                                existe = false,
                                es_propietario = null,
                                emprendimiento = null,
                                Message = responseObj["message"]?.ToString() ?? "Error al consultar emprendimiento"
                            };
                        }
                    }
                    else
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();
                        var responseObj = JObject.Parse(responseContent);

                        return new ConsultarEmprendimientoResponse
                        {
                            IsSuccess = false,
                            existe = false,
                            es_propietario = null,
                            emprendimiento = null,
                            Message = responseObj["message"]?.ToString() ?? $"Error HTTP: {response.StatusCode}"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "ConsultarEmprendimientoPorNIT");
                return new ConsultarEmprendimientoResponse
                {
                    IsSuccess = false,
                    existe = false,
                    es_propietario = null,
                    emprendimiento = null,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// Crea una nueva alarma promocional (Promoción Local)
        /// </summary>
        public static async Task<CrearAlarmaPromocionalResponse> CrearAlarmaPromocional(CrearAlarmaPromocionalRequest request)
        {
            try
            {
                // DEBUG: Verificar datos del logo antes de enviar
                System.Diagnostics.Debug.WriteLine($"[ApiService.CrearAlarmaPromocional] === ENVIANDO REQUEST ===");
                System.Diagnostics.Debug.WriteLine($"[ApiService.CrearAlarmaPromocional] logo_habilitado: {request.logo_habilitado}");
                System.Diagnostics.Debug.WriteLine($"[ApiService.CrearAlarmaPromocional] url_logo: {request.url_logo ?? "NULL"}");
                System.Diagnostics.Debug.WriteLine($"[ApiService.CrearAlarmaPromocional] LogoBase64 presente: {!string.IsNullOrEmpty(request.LogoBase64)}");
                if (!string.IsNullOrEmpty(request.LogoBase64))
                {
                    System.Diagnostics.Debug.WriteLine($"[ApiService.CrearAlarmaPromocional] LogoBase64 longitud: {request.LogoBase64.Length} caracteres");
                }
                System.Diagnostics.Debug.WriteLine($"[ApiService.CrearAlarmaPromocional] Fotos count: {request.Fotos?.Count ?? 0}");

                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        //bypass
                        return true;
                    },
                }
            , false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    //POST Method
                    string url = AppConfiguration.ApiHost + "/AlarmaPromocional/CrearAlarmaPromocional";

                    StringContent content = new StringContent(JsonConvert.SerializeObject(request), System.Text.Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAPIAsync(url, content);
                    if (response.IsSuccessStatusCode)
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();
                        var responseObj = JObject.Parse(responseContent);

                        if (responseObj["isSuccess"]?.ToObject<bool>() == true)
                        {
                            var dataObj = responseObj["data"];

                            return new CrearAlarmaPromocionalResponse
                            {
                                IsSuccess = true,
                                AlarmaId = dataObj["alarmaId"]?.ToObject<long>() ?? 0,
                                SaldoResultante = dataObj["saldoResultante"]?.ToObject<int>() ?? 0,
                                Message = responseObj["message"]?.ToString() ?? "Promoción creada exitosamente"
                            };
                        }
                        else
                        {
                            return new CrearAlarmaPromocionalResponse
                            {
                                IsSuccess = false,
                                AlarmaId = 0,
                                SaldoResultante = 0,
                                Message = responseObj["message"]?.ToString() ?? "Error al crear promoción"
                            };
                        }
                    }
                    else
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();
                        var responseObj = JObject.Parse(responseContent);

                        return new CrearAlarmaPromocionalResponse
                        {
                            IsSuccess = false,
                            AlarmaId = 0,
                            SaldoResultante = 0,
                            Message = responseObj["message"]?.ToString() ?? $"Error HTTP: {response.StatusCode}"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "CrearAlarmaPromocional");
                return new CrearAlarmaPromocionalResponse
                {
                    IsSuccess = false,
                    AlarmaId = 0,
                    SaldoResultante = 0,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// Verifica si el usuario ya aceptó términos para un chat de publicidad específico
        /// </summary>
        public static async Task<VerificarAceptacionTerminosResponse> VerificarAceptacionTerminos(long alarmaId, string userIdThirdparty)
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                }, false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    string url = AppConfiguration.ApiHost + "/ChatPublicidad/VerificarAceptacionTerminos";

                    var requestData = new
                    {
                        alarma_id = alarmaId,
                        user_id_thirdparty = userIdThirdparty
                    };

                    StringContent content = new StringContent(
                        JsonConvert.SerializeObject(requestData),
                        System.Text.Encoding.UTF8,
                        "application/json"
                    );

                    HttpResponseMessage response = await client.PostAPIAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();
                        var responseObj = JObject.Parse(responseContent);

                        if (responseObj["isSuccess"]?.ToObject<bool>() == true)
                        {
                            var data = responseObj["data"];
                            return new VerificarAceptacionTerminosResponse
                            {
                                IsSuccess = true,
                                ChatId = data["chatId"]?.ToObject<long?>(),
                                YaAceptoTerminos = data["yaAceptoTerminos"]?.ToObject<bool>() ?? false,
                                EstadoChat = data["estadoChat"]?.ToString() ?? "no_existe",
                                EsProveedor = data["esProveedor"]?.ToObject<bool>() ?? false,
                                Message = responseObj["message"]?.ToString()
                            };
                        }
                        else
                        {
                            return new VerificarAceptacionTerminosResponse
                            {
                                IsSuccess = false,
                                YaAceptoTerminos = false,
                                Message = responseObj["message"]?.ToString() ?? "Error verificando términos"
                            };
                        }
                    }
                    else
                    {
                        string errorContent = await response.Content.ReadAsStringAsync();
                        return new VerificarAceptacionTerminosResponse
                        {
                            IsSuccess = false,
                            YaAceptoTerminos = false,
                            Message = $"Error HTTP {response.StatusCode}: {errorContent}"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "VerificarAceptacionTerminos");
                return new VerificarAceptacionTerminosResponse
                {
                    IsSuccess = false,
                    YaAceptoTerminos = false,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// Lista todos los chats de una alarma promocional (para el creador/proveedor)
        /// Agregado: 02-02-2026
        /// </summary>
        public static async Task<List<ChatResumenModel>> ListarChatsPorAlarma(long alarmaId)
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                }, false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    // 2026-04-11: Pasar idioma_dispositivo para que la API traduzca ultimo_mensaje
                    string idiomaDispositivo = App.persona?.Idioma ?? "es";
                    string url = $"{AppConfiguration.ApiHost}/ChatPublicidad/ListarChatsPorAlarma?alarma_id={alarmaId}&user_id_thirdparty={App.persona?.user_id_thirdparty ?? ""}&idioma_dispositivo={idiomaDispositivo}";

                    HttpResponseMessage response = await client.GetAPIAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();
                        var responseObj = JObject.Parse(responseContent);

                        if (responseObj["isSuccess"]?.ToObject<bool>() == true)
                        {
                            var data = responseObj["data"];
                            var chatsJson = data?["chats"];
                            if (chatsJson != null)
                            {
                                return chatsJson.ToObject<List<ChatResumenModel>>() ?? new List<ChatResumenModel>();
                            }
                        }
                    }
                    return new List<ChatResumenModel>();
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "ListarChatsPorAlarma");
                return new List<ChatResumenModel>();
            }
        }

        /// <summary>
        /// Obtiene el detalle completo de una suscripción promocional
        /// </summary>
        public static async Task<DetallePromocion> ObtenerDetallePromocion(long subscripcionId)
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                }, false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    string url = AppConfiguration.ApiHost + "/Subscripciones/ObtenerDetallePromocion/" + subscripcionId;

                    HttpResponseMessage response = await client.GetAPIAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();
                        var responseObj = JObject.Parse(responseContent);

                        if (responseObj["isSuccess"]?.ToObject<bool>() == true)
                        {
                            var data = responseObj["data"];
                            return JsonConvert.DeserializeObject<DetallePromocion>(data.ToString());
                        }
                    }
                    return null;
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "ObtenerDetallePromocion");
                return null;
            }
        }

        /// <summary>
        /// Consulta los usuarios notificados para una suscripción promocional
        /// </summary>
        // ─── MÉTODOS SOCIALES (23-02-2026) ─────────────────────────────────────────

        public static async Task<ResponseMessage> SeguirUsuario(string userIdThirdparty, string userIdASeguir)
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                }, false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    string url = AppConfiguration.ApiHost + AppConfiguration.SeguirUsuario;
                    var body = new { user_id_thirdparty = userIdThirdparty, user_id_a_seguir = userIdASeguir };
                    StringContent content = new StringContent(JsonConvert.SerializeObject(body), System.Text.Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await client.PostAPIAsync(url, content);
                    return JsonConvert.DeserializeObject<ResponseMessage>(await response.Content.ReadAsStringAsync());
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "SeguirUsuario");
                return new ResponseMessage() { IsSuccess = false, Data = "", Message = ex.Message };
            }
        }

        public static async Task<ResponseMessage> DejarDeSeguirUsuario(string userIdThirdparty, string userIdADejar)
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                }, false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    string url = AppConfiguration.ApiHost + AppConfiguration.DejarDeSeguirUsuario;
                    var body = new { user_id_thirdparty = userIdThirdparty, user_id_a_dejar = userIdADejar };
                    StringContent content = new StringContent(JsonConvert.SerializeObject(body), System.Text.Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await client.PostAPIAsync(url, content);
                    return JsonConvert.DeserializeObject<ResponseMessage>(await response.Content.ReadAsStringAsync());
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "DejarDeSeguirUsuario");
                return new ResponseMessage() { IsSuccess = false, Data = "", Message = ex.Message };
            }
        }

        public static async Task<bool> EstasSiguiendoAsync(string userIdThirdparty, string userIdObjetivo)
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                }, false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    string url = AppConfiguration.ApiHost + AppConfiguration.EstasSiguiendo
                        + "?user_id_thirdparty=" + Uri.EscapeDataString(userIdThirdparty)
                        + "&user_id_objetivo=" + Uri.EscapeDataString(userIdObjetivo);
                    HttpResponseMessage response = await client.GetAPIAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        var rm = JsonConvert.DeserializeObject<ResponseMessage>(await response.Content.ReadAsStringAsync());
                        return rm?.Data != null && Convert.ToBoolean(rm.Data);
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "EstasSiguiendoAsync");
                return false;
            }
        }

        public static async Task<ResponseMessage> DarLikeAlarma(string userIdThirdparty, long alarmaId)
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                }, false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    string url = AppConfiguration.ApiHost + AppConfiguration.DarLikeAlarma;
                    var body = new { user_id_thirdparty = userIdThirdparty, alarma_id = alarmaId };
                    StringContent content = new StringContent(JsonConvert.SerializeObject(body), System.Text.Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await client.PostAPIAsync(url, content);
                    return JsonConvert.DeserializeObject<ResponseMessage>(await response.Content.ReadAsStringAsync());
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "DarLikeAlarma");
                return new ResponseMessage() { IsSuccess = false, Data = "", Message = ex.Message };
            }
        }

        public static async Task<ResponseMessage> QuitarLikeAlarma(string userIdThirdparty, long alarmaId)
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                }, false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    string url = AppConfiguration.ApiHost + AppConfiguration.QuitarLikeAlarma;
                    var body = new { user_id_thirdparty = userIdThirdparty, alarma_id = alarmaId };
                    StringContent content = new StringContent(JsonConvert.SerializeObject(body), System.Text.Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await client.PostAPIAsync(url, content);
                    return JsonConvert.DeserializeObject<ResponseMessage>(await response.Content.ReadAsStringAsync());
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "QuitarLikeAlarma");
                return new ResponseMessage() { IsSuccess = false, Data = "", Message = ex.Message };
            }
        }

        public static async Task<ResponseMessage> ReenviarAlarma(string userIdThirdparty, long alarmaId)
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                }, false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    string url = AppConfiguration.ApiHost + AppConfiguration.ReenviarAlarma;
                    var body = new { user_id_thirdparty = userIdThirdparty, alarma_id = alarmaId };
                    StringContent content = new StringContent(JsonConvert.SerializeObject(body), System.Text.Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await client.PostAPIAsync(url, content);
                    return JsonConvert.DeserializeObject<ResponseMessage>(await response.Content.ReadAsStringAsync());
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "ReenviarAlarma");
                return new ResponseMessage() { IsSuccess = false, Data = "", Message = ex.Message };
            }
        }

        public static async Task<ResponseMessage> QuitarReenvioAlarma(string userIdThirdparty, long alarmaId)
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                }, false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    string url = AppConfiguration.ApiHost + AppConfiguration.QuitarReenvioAlarma;
                    var body = new { user_id_thirdparty = userIdThirdparty, alarma_id = alarmaId };
                    StringContent content = new StringContent(JsonConvert.SerializeObject(body), System.Text.Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await client.PostAPIAsync(url, content);
                    return JsonConvert.DeserializeObject<ResponseMessage>(await response.Content.ReadAsStringAsync());
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "QuitarReenvioAlarma");
                return new ResponseMessage() { IsSuccess = false, Data = "", Message = ex.Message };
            }
        }

        public static async Task<List<sospect.Models.PaisUsuarios>> ObtenerPaisesConUsuarios()
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                }, false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    string url = AppConfiguration.ApiHost + AppConfiguration.ObtenerPaisesConUsuarios;
                    HttpResponseMessage response = await client.GetAPIAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        // FIX 2026-02-28: Usar JObject.Parse en lugar de DeserializeObject<ResponseMessage>
                        // porque rm.Data.ToString() sobre un JArray produce un string malformado que falla
                        // al deserializar. Error original: "Unexpected character encountered while parsing value: ["
                        var json = await response.Content.ReadAsStringAsync();
                        var jObj = Newtonsoft.Json.Linq.JObject.Parse(json);
                        if (jObj["isSuccess"]?.Value<bool>() == true && jObj["data"] != null)
                            return JsonConvert.DeserializeObject<List<sospect.Models.PaisUsuarios>>(jObj["data"].ToString())
                                   ?? new List<sospect.Models.PaisUsuarios>();
                    }
                    return new List<sospect.Models.PaisUsuarios>();
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "ObtenerPaisesConUsuarios");
                return new List<sospect.Models.PaisUsuarios>();
            }
        }

        // NUEVO 2026-02-26: Catálogo maestro de países (ISO alpha-2) para Picker en AgregarDatosBasicosPage
        public static async Task<List<sospect.Models.PaisItem>> ObtenerPaises()
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                }, false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    string url = AppConfiguration.ApiHost + AppConfiguration.ObtenerPaises;
                    HttpResponseMessage response = await client.GetAPIAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        // FIX 2026-04-02: Usar JObject.Parse porque ResponseMessage.Data es string?
                        // y Newtonsoft no puede deserializar un JSON array directamente en string.
                        // Mismo patrón que ObtenerPaisesConUsuarios (fix 2026-02-28).
                        var json = await response.Content.ReadAsStringAsync();
                        var jObj = Newtonsoft.Json.Linq.JObject.Parse(json);
                        if (jObj["isSuccess"]?.Value<bool>() == true && jObj["data"] != null)
                            return JsonConvert.DeserializeObject<List<sospect.Models.PaisItem>>(jObj["data"].ToString())
                                   ?? new List<sospect.Models.PaisItem>();
                    }
                    return new List<sospect.Models.PaisItem>();
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "ObtenerPaises");
                return new List<sospect.Models.PaisItem>();
            }
        }

        public static async Task<List<sospect.Models.AlarmaCuadranteDto>> ObtenerAlarmasPorZona(decimal latMin, decimal latMax, decimal lngMin, decimal lngMax)
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                }, false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    string url = AppConfiguration.ApiHost + AppConfiguration.ObtenerAlarmasPorZona;
                    var body = new { lat_min = latMin, lat_max = latMax, lng_min = lngMin, lng_max = lngMax };
                    StringContent content = new StringContent(JsonConvert.SerializeObject(body), System.Text.Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await client.PostAPIAsync(url, content);
                    if (response.IsSuccessStatusCode)
                    {
                        var rm = JsonConvert.DeserializeObject<ResponseMessage>(await response.Content.ReadAsStringAsync());
                        if (rm?.IsSuccess == true && rm.Data != null)
                            return JsonConvert.DeserializeObject<List<sospect.Models.AlarmaCuadranteDto>>(rm.Data.ToString());
                    }
                    return new List<sospect.Models.AlarmaCuadranteDto>();
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "ObtenerAlarmasPorZona");
                return new List<sospect.Models.AlarmaCuadranteDto>();
            }
        }

        /// <summary>
        /// Obtiene pines o clusters del mapa para el viewport visible.
        /// Creado: 2026-03-01 — Endpoint GET /Ubicaciones/PinesMapa (viewport-driven).
        /// zoom >= 15 → pines individuales (PinMapaDto).
        /// zoom &lt;= 14 → clusters (ClusterMapaDto).
        /// No requiere JWT (mapa es visible públicamente).
        /// </summary>
        public static async Task<PinesMapaResponse?> ObtenerPinesMapa(
            decimal minLat, decimal maxLat, decimal minLon, decimal maxLon, int zoom,
            decimal userLat = 0, decimal userLon = 0)  // Coordenadas del usuario para calcular distancia
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                }, false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    string url = AppConfiguration.ApiHost + AppConfiguration.PinesMapa
                        + $"?minLat={minLat.ToString(System.Globalization.CultureInfo.InvariantCulture)}"
                        + $"&maxLat={maxLat.ToString(System.Globalization.CultureInfo.InvariantCulture)}"
                        + $"&minLon={minLon.ToString(System.Globalization.CultureInfo.InvariantCulture)}"
                        + $"&maxLon={maxLon.ToString(System.Globalization.CultureInfo.InvariantCulture)}"
                        + $"&zoom={zoom}"
                        + $"&userLat={userLat.ToString(System.Globalization.CultureInfo.InvariantCulture)}"
                        + $"&userLon={userLon.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

                    System.Diagnostics.Debug.WriteLine($"[ApiService] ObtenerPinesMapa: {url}");

                    HttpResponseMessage response = await client.GetAPIAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        string fullResponse = await response.Content.ReadAsStringAsync();
                        // ResponseMessage.Data es string? pero el API devuelve un objeto JSON
                        // en "data", no una cadena → usar JObject para evitar crash de deserialización
                        var jRoot = JObject.Parse(fullResponse);
                        bool isSuccess = jRoot["isSuccess"]?.ToObject<bool>() == true;
                        if (isSuccess && jRoot["data"] != null)
                        {
                            string dataJson = jRoot["data"]!.ToString();
                            var resultado = JsonConvert.DeserializeObject<PinesMapaResponse>(dataJson);
                            System.Diagnostics.Debug.WriteLine($"[ApiService] ObtenerPinesMapa: tipo={resultado?.tipo}, zoom={zoom}");
                            return resultado;
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[ApiService] ObtenerPinesMapa: Error StatusCode = {response.StatusCode}");
                    }
                    return null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ApiService] EXCEPCIÓN en ObtenerPinesMapa: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "ApiService", "ObtenerPinesMapa", new Dictionary<string, string> {
                    { "ErrorType", ex.GetType().Name },
                    { "zoom", zoom.ToString() }
                });
                return null;
            }
        }

        public static async Task<List<UsuarioNotificado>> ConsultarUsuariosNotificados(long subscripcionId)
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                }, false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    string url = AppConfiguration.ApiHost + "/Subscripciones/ConsultarUsuariosNotificados/" + subscripcionId;

                    HttpResponseMessage response = await client.GetAPIAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();
                        var responseObj = JObject.Parse(responseContent);

                        if (responseObj["isSuccess"]?.ToObject<bool>() == true)
                        {
                            var data = responseObj["data"];
                            var usuarios = data["usuarios"];
                            return JsonConvert.DeserializeObject<List<UsuarioNotificado>>(usuarios.ToString());
                        }
                    }
                    return new List<UsuarioNotificado>();
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "ConsultarUsuariosNotificados");
                return new List<UsuarioNotificado>();
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // POLÍTICOS (2026-03-05)
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Dado un alarma_id, devuelve la cadena de autoridades políticas vigentes:
        /// Edil → Alcalde → Gobernador → Presidente.
        /// </summary>
        public static async Task<List<AutoridadResponsableDto>> ObtenerAutoridadesPorAlarma(long alarmaId)
        {
            try
            {
                // Verificar token de acceso ANTES de crear JWTHttpClient
                var token = await SecureStorage.GetAsync("access_token");
                if (string.IsNullOrEmpty(token))
                {
                    System.Diagnostics.Debug.WriteLine("[ApiService] ObtenerAutoridadesPorAlarma: Token de acceso está vacío");
                    return new List<AutoridadResponsableDto>();
                }

                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        //bypass
                        return true;
                    },
                }, false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    //GET Method
                    string url = AppConfiguration.ApiHost + AppConfiguration.ObtenerAutoridadesPorAlarma + $"/{alarmaId}";
                    System.Diagnostics.Debug.WriteLine($"[ApiService] ObtenerAutoridadesPorAlarma: {url}");

                    HttpResponseMessage response = await client.GetAPIAsync(url);

                    // Detectar token expirado
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        System.Diagnostics.Debug.WriteLine("[ApiService] ObtenerAutoridadesPorAlarma: Token expirado o inválido");
                        return new List<AutoridadResponsableDto>();
                    }

                    if (response.IsSuccessStatusCode)
                    {
                        string jsonContent = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine($"[ApiService] ObtenerAutoridadesPorAlarma: Response length: {jsonContent?.Length ?? 0} chars");

                        string dataJson = JObject.Parse(jsonContent)["data"]?.ToString() ?? string.Empty;

                        if (string.IsNullOrWhiteSpace(dataJson))
                        {
                            System.Diagnostics.Debug.WriteLine("[ApiService] ObtenerAutoridadesPorAlarma: data field vacío");
                            return new List<AutoridadResponsableDto>();
                        }

                        List<AutoridadResponsableDto> result = JsonConvert.DeserializeObject<List<AutoridadResponsableDto>>(dataJson)
                                                               ?? new List<AutoridadResponsableDto>();

                        System.Diagnostics.Debug.WriteLine($"[ApiService] ObtenerAutoridadesPorAlarma: {result.Count} autoridades deserializadas");
                        return result;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[ApiService] ObtenerAutoridadesPorAlarma: HTTP {response.StatusCode}");
                        return new List<AutoridadResponsableDto>();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ApiService] EXCEPCIÓN en ObtenerAutoridadesPorAlarma: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "ApiService", "ObtenerAutoridadesPorAlarma", new Dictionary<string, string> {
                    { "ErrorType", ex.GetType().Name },
                    { "AlarmaId", alarmaId.ToString() }
                });
                return new List<AutoridadResponsableDto>();
            }
        }

        /// <summary>
        /// Obtiene las métricas de desempeño de un político:
        /// tasa de resolución, alarmas abiertas/cerradas, likes, reenvíos, tiempo promedio
        /// y distribución por tipo de alarma (para el gráfico pastel).
        /// </summary>
        public static async Task<ReportePoliticoDto?> ObtenerReportePolitico(int politicoId)
        {
            try
            {
                var token = await SecureStorage.GetAsync("access_token");
                if (string.IsNullOrEmpty(token))
                {
                    System.Diagnostics.Debug.WriteLine("[ApiService] ObtenerReportePolitico: Token de acceso está vacío");
                    return null;
                }

                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        return true;
                    },
                }, false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    string url = AppConfiguration.ApiHost + AppConfiguration.ObtenerReportePolitico + $"/{politicoId}";
                    System.Diagnostics.Debug.WriteLine($"[ApiService] ObtenerReportePolitico: {url}");

                    HttpResponseMessage response = await client.GetAPIAsync(url);

                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        System.Diagnostics.Debug.WriteLine("[ApiService] ObtenerReportePolitico: Token expirado o inválido");
                        return null;
                    }

                    if (response.IsSuccessStatusCode)
                    {
                        string jsonContent = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine($"[ApiService] ObtenerReportePolitico: Response length: {jsonContent?.Length ?? 0} chars");

                        string dataJson = JObject.Parse(jsonContent)["data"]?.ToString() ?? string.Empty;

                        if (string.IsNullOrWhiteSpace(dataJson))
                        {
                            System.Diagnostics.Debug.WriteLine("[ApiService] ObtenerReportePolitico: data field vacío (político sin métricas aún)");
                            return null;
                        }

                        ReportePoliticoDto? result = JsonConvert.DeserializeObject<ReportePoliticoDto>(dataJson);
                        System.Diagnostics.Debug.WriteLine($"[ApiService] ObtenerReportePolitico: PctResolucion={result?.PctResolucion}, TiposAlarma={result?.TiposAlarma?.Count ?? 0}");
                        return result;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[ApiService] ObtenerReportePolitico: HTTP {response.StatusCode}");
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ApiService] EXCEPCIÓN en ObtenerReportePolitico: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "ApiService", "ObtenerReportePolitico", new Dictionary<string, string> {
                    { "ErrorType", ex.GetType().Name },
                    { "PoliticoId", politicoId.ToString() }
                });
                return null;
            }
        }

        /// <summary>
        /// Registra o actualiza la calificación ciudadana (1-5 estrellas) del usuario
        /// autenticado para un político. Si ya existe una calificación previa, se actualiza.
        /// Retorna la calificación guardada, o -1 en caso de error.
        /// </summary>
        public static async Task<int> RegistrarAprobacion(int politicoId, int calificacion)
        {
            try
            {
                var token = await SecureStorage.GetAsync("access_token");
                if (string.IsNullOrEmpty(token))
                {
                    System.Diagnostics.Debug.WriteLine("[ApiService] RegistrarAprobacion: Token de acceso está vacío");
                    return -1;
                }

                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true,
                }, false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    string url = AppConfiguration.ApiHost + AppConfiguration.RegistrarAprobacion;
                    System.Diagnostics.Debug.WriteLine($"[ApiService] RegistrarAprobacion: politicoId={politicoId} calificacion={calificacion}");

                    var body = new { politicoId, calificacion };
                    var content = new StringContent(
                        JsonConvert.SerializeObject(body),
                        System.Text.Encoding.UTF8,
                        "application/json");

                    HttpResponseMessage response = await client.PostAPIAsync(url, content);

                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        System.Diagnostics.Debug.WriteLine("[ApiService] RegistrarAprobacion: Token expirado o inválido");
                        return -1;
                    }

                    if (response.IsSuccessStatusCode)
                    {
                        string jsonContent = await response.Content.ReadAsStringAsync();
                        var dataObj = JObject.Parse(jsonContent)["data"];
                        int savedCalificacion = dataObj?["calificacion"]?.Value<int>() ?? calificacion;
                        System.Diagnostics.Debug.WriteLine($"[ApiService] RegistrarAprobacion: guardada={savedCalificacion}");
                        return savedCalificacion;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[ApiService] RegistrarAprobacion: HTTP {response.StatusCode}");
                        return -1;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ApiService] EXCEPCIÓN en RegistrarAprobacion: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "ApiService", "RegistrarAprobacion", new Dictionary<string, string> {
                    { "ErrorType", ex.GetType().Name },
                    { "PoliticoId", politicoId.ToString() }
                });
                return -1;
            }
        }

        /// <summary>
        /// Obtiene la calificación previa del usuario autenticado para un político.
        /// Retorna 0 si el usuario aún no ha calificado a este político, o -1 en caso de error.
        /// </summary>
        public static async Task<int> ObtenerAprobacionPersona(int politicoId)
        {
            try
            {
                var token = await SecureStorage.GetAsync("access_token");
                if (string.IsNullOrEmpty(token))
                {
                    System.Diagnostics.Debug.WriteLine("[ApiService] ObtenerAprobacionPersona: Token de acceso está vacío");
                    return 0;
                }

                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true,
                }, false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    string url = AppConfiguration.ApiHost + AppConfiguration.ObtenerAprobacionPersona + $"/{politicoId}";
                    System.Diagnostics.Debug.WriteLine($"[ApiService] ObtenerAprobacionPersona: {url}");

                    HttpResponseMessage response = await client.GetAPIAsync(url);

                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        System.Diagnostics.Debug.WriteLine("[ApiService] ObtenerAprobacionPersona: Token expirado o inválido");
                        return 0;
                    }

                    if (response.IsSuccessStatusCode)
                    {
                        string jsonContent = await response.Content.ReadAsStringAsync();
                        var dataObj = JObject.Parse(jsonContent)["data"];
                        int calificacion = dataObj?["calificacion"]?.Value<int>() ?? 0;
                        System.Diagnostics.Debug.WriteLine($"[ApiService] ObtenerAprobacionPersona: calificacion={calificacion}");
                        return calificacion;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[ApiService] ObtenerAprobacionPersona: HTTP {response.StatusCode}");
                        return 0;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ApiService] EXCEPCIÓN en ObtenerAprobacionPersona: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "ApiService", "ObtenerAprobacionPersona", new Dictionary<string, string> {
                    { "ErrorType", ex.GetType().Name },
                    { "PoliticoId", politicoId.ToString() }
                });
                return 0;
            }
        }

        /// <summary>
        /// Obtiene el ranking de políticos locales para una métrica dada.
        /// Devuelve top 5 mejores y tail 5 peores.
        /// GET /Politicos/ObtenerRankingPoliticos?metrica={metrica}
        /// </summary>
        public static async Task<RankingPoliticosDto> ObtenerRankingPoliticos(string metrica)
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        //bypass
                        return true;
                    },
                }
                , false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    string url = AppConfiguration.ApiHost + AppConfiguration.ObtenerRankingPoliticos + $"?metrica={metrica}";
                    Console.WriteLine($"[ApiService] ObtenerRankingPoliticos: url={url}");

                    HttpResponseMessage response = await client.GetAPIAsync(url);
                    Console.WriteLine($"[ApiService] ObtenerRankingPoliticos: statusCode={response.StatusCode}");

                    if (response.IsSuccessStatusCode)
                    {
                        string body = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"[ApiService] ObtenerRankingPoliticos: body={body}");
                        RankingPoliticosDto resultado = JsonConvert.DeserializeObject<RankingPoliticosDto>(JObject.Parse(body)["data"].ToString());
                        Console.WriteLine($"[ApiService] ObtenerRankingPoliticos: top5={resultado?.Top5?.Count} tail5={resultado?.Tail5?.Count}");
                        return resultado;
                    }
                    else
                    {
                        Console.WriteLine($"[ApiService] ObtenerRankingPoliticos: HTTP {response.StatusCode}");
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] EXCEPCION ObtenerRankingPoliticos: {ex.GetType().Name} - {ex.Message}");
                CrashlyticsHelper.LogError(ex, "ApiService", "ObtenerRankingPoliticos");
                return null;
            }
        }

        /// <summary>
        /// Obtiene el dashboard de gamificación del emprendedor.
        /// Endpoint: GET /Emprendimientos/ObtenerDashboardEmprendedor
        /// Si el usuario no tiene emprendimiento activo, retorna TieneEmprendimiento=false.
        /// Agregado: 2026-04-09
        /// </summary>
        public static async Task<DashboardEmprendedorResponse> ObtenerDashboardEmprendedor(string userIdThirdparty, long idEmprendimiento = 0)
        {
            try
            {
                Console.WriteLine($"[Dashboard] ObtenerDashboardEmprendedor START - userId={userIdThirdparty}, idEmprendimiento={idEmprendimiento}");
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                }, false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    string url = AppConfiguration.ApiHost + $"/Emprendimientos/ObtenerDashboardEmprendedor?user_id_thirdparty={Uri.EscapeDataString(userIdThirdparty)}";
                    if (idEmprendimiento > 0)
                        url += $"&id_emprendimiento={idEmprendimiento}";
                    Console.WriteLine($"[Dashboard] URL={url}");

                    HttpResponseMessage response = await client.GetAPIAsync(url);
                    Console.WriteLine($"[Dashboard] HTTP StatusCode={response.StatusCode}");

                    if (response.IsSuccessStatusCode)
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"[Dashboard] Response body={responseContent}");
                        var responseObj = JObject.Parse(responseContent);

                        if (responseObj["isSuccess"]?.ToObject<bool>() == true)
                        {
                            var data = responseObj["data"];
                            bool tieneEmprendimiento = data["tieneEmprendimiento"]?.ToObject<bool>() ?? false;
                            Console.WriteLine($"[Dashboard] isSuccess=true, tieneEmprendimiento={tieneEmprendimiento}");

                            if (!tieneEmprendimiento)
                            {
                                Console.WriteLine($"[Dashboard] Usuario sin emprendimiento → retornando TieneEmprendimiento=false");
                                return new DashboardEmprendedorResponse
                                {
                                    IsSuccess = true,
                                    TieneEmprendimiento = false
                                };
                            }

                            Console.WriteLine($"[Dashboard] Emprendimiento encontrado, mapeando respuesta");
                            return new DashboardEmprendedorResponse
                            {
                                IsSuccess = true,
                                TieneEmprendimiento = true,
                                Pais = data["pais"]?.ToString(),
                                TotalEmprendedoresEnPais = data["totalEmprendedoresEnPais"]?.ToObject<int>() ?? 0,
                                MiPuesto = data["miPuesto"]?.ToObject<int>() ?? 0,
                                MisMetricas = data["misMetricas"]?.ToObject<MetricasEmprendedorDto>(),
                                Mejor = data["mejor"]?.ToObject<EmprendedorResumenDto>(),
                                Peor = data["peor"]?.ToObject<EmprendedorResumenDto>()
                            };
                        }
                        else
                        {
                            Console.WriteLine($"[Dashboard] isSuccess=false, message={responseObj["message"]}");
                            return new DashboardEmprendedorResponse
                            {
                                IsSuccess = false,
                                TieneEmprendimiento = false,
                                Message = responseObj["message"]?.ToString() ?? "Error obteniendo dashboard"
                            };
                        }
                    }
                    else
                    {
                        string errorContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"[Dashboard] HTTP Error {response.StatusCode}: {errorContent}");
                        return new DashboardEmprendedorResponse
                        {
                            IsSuccess = false,
                            TieneEmprendimiento = false,
                            Message = $"Error HTTP {response.StatusCode}: {errorContent}"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Dashboard] EXCEPCION {ex.GetType().Name}: {ex.Message}");
                CrashlyticsHelper.LogError(ex, "ApiService", "ObtenerDashboardEmprendedor");
                return new DashboardEmprendedorResponse
                {
                    IsSuccess = false,
                    TieneEmprendimiento = false,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// Obtiene la lista de emprendimientos activos del usuario.
        /// Endpoint: GET /Emprendimientos/ObtenerEmprendimientosDelUsuario
        /// Agregado: 2026-04-10
        /// </summary>
        public static async Task<ObtenerEmprendimientosResponse> ObtenerEmprendimientosDelUsuario(string userIdThirdparty)
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                }, false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    string url = AppConfiguration.ApiHost + $"/Emprendimientos/ObtenerEmprendimientosDelUsuario?user_id_thirdparty={Uri.EscapeDataString(userIdThirdparty)}";

                    HttpResponseMessage response = await client.GetAPIAsync(url);
                    string responseContent = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var responseObj = JsonConvert.DeserializeObject<JObject>(responseContent);
                        bool isSuccess = responseObj?["isSuccess"]?.ToObject<bool>() ?? false;

                        if (isSuccess)
                        {
                            var data = responseObj["data"];
                            var lista = data?["emprendimientos"]?.ToObject<List<EmprendimientoResumenDto>>() ?? new List<EmprendimientoResumenDto>();
                            return new ObtenerEmprendimientosResponse
                            {
                                IsSuccess = true,
                                Message = responseObj["message"]?.ToString() ?? "",
                                Emprendimientos = lista
                            };
                        }
                        else
                        {
                            return new ObtenerEmprendimientosResponse
                            {
                                IsSuccess = false,
                                Message = responseObj?["message"]?.ToString() ?? "Error desconocido"
                            };
                        }
                    }
                    else
                    {
                        return new ObtenerEmprendimientosResponse
                        {
                            IsSuccess = false,
                            Message = $"Error HTTP {response.StatusCode}"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "ObtenerEmprendimientosDelUsuario");
                return new ObtenerEmprendimientosResponse { IsSuccess = false, Message = ex.Message };
            }
        }

        /// <summary>
        /// Obtiene los datos de la última promoción lanzada por el usuario para un emprendimiento.
        /// Endpoint: GET /Emprendimientos/ObtenerUltimaPromocion
        /// Agregado: 2026-04-10
        /// </summary>
        public static async Task<UltimaPromocionResponse> ObtenerUltimaPromocion(string userIdThirdparty, long idEmprendimiento)
        {
            try
            {
                using (var client = new JWTHttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                }, false))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    string url = AppConfiguration.ApiHost + $"/Emprendimientos/ObtenerUltimaPromocion?user_id_thirdparty={Uri.EscapeDataString(userIdThirdparty)}&id_emprendimiento={idEmprendimiento}";

                    HttpResponseMessage response = await client.GetAPIAsync(url);
                    string responseContent = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var responseObj = JsonConvert.DeserializeObject<JObject>(responseContent);
                        bool isSuccess = responseObj?["isSuccess"]?.ToObject<bool>() ?? false;

                        if (isSuccess)
                        {
                            var data = responseObj["data"];
                            bool tienePrevia = data?["tienePromocionPrevia"]?.ToObject<bool>() ?? false;

                            if (!tienePrevia)
                                return new UltimaPromocionResponse { IsSuccess = true, TienePromocionPrevia = false };

                            var fotos = data?["fotos"]?.ToObject<List<FotoUltimaPromocionDto>>() ?? new List<FotoUltimaPromocionDto>();

                            return new UltimaPromocionResponse
                            {
                                IsSuccess = true,
                                TienePromocionPrevia = true,
                                SubscripcionId = data?["subscripcionId"]?.ToObject<long?>(),
                                AlarmaId = data?["alarmaId"]?.ToObject<long?>(),
                                DescripcionAlarma = data?["descripcionAlarma"]?.ToString() ?? "",
                                LogoHabilitado = data?["logoHabilitado"]?.ToObject<bool>() ?? false,
                                DomicilioHabilitado = data?["domicilioHabilitado"]?.ToObject<bool>() ?? false,
                                ContactoHabilitado = data?["contactoHabilitado"]?.ToObject<bool>() ?? false,
                                ProveedorAceptoTerminosChat = data?["proveedorAceptoTerminosChat"]?.ToObject<bool>() ?? false,
                                RadioMetros = data?["radioMetros"]?.ToObject<int>() ?? 500,
                                DuracionDias = data?["duracionDias"]?.ToObject<int>() ?? 7,
                                TextoPushPersonalizado = data?["textoPushPersonalizado"]?.ToString() ?? "",
                                CantidadMediaAdjunta = data?["cantidadMediaAdjunta"]?.ToObject<int>() ?? 1,
                                CantidadUsuariosPush = data?["usuariosPushNotificados"]?.ToObject<int>() ?? 0,
                                UrlLogo = data?["urlLogo"]?.ToString() ?? "",
                                Fotos = fotos
                            };
                        }
                        else
                        {
                            return new UltimaPromocionResponse
                            {
                                IsSuccess = false,
                                Message = responseObj?["message"]?.ToString() ?? "Error desconocido"
                            };
                        }
                    }
                    else
                    {
                        return new UltimaPromocionResponse
                        {
                            IsSuccess = false,
                            Message = $"Error HTTP {response.StatusCode}"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "ApiService", "ObtenerUltimaPromocion");
                return new UltimaPromocionResponse { IsSuccess = false, Message = ex.Message };
            }
        }
    }
}


