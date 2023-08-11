using System;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using sospect.Models;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Xamarin.Essentials;
using sospect.AuthHelpers;
using sospect.Utils;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace sospect.Services
{
    public class ApiService
    {
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
                return new ResponseMessage() { IsSuccess = false, Data = "", Message = ex.Message };
            }
        }

        public static async Task<List<AlarmaCercana>> ActualizarUbicacion(Ubicaciones ubicacionUsuario)
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
                    string url = AppConfiguration.ApiHost + AppConfiguration.InsertarUbicacionUsuario;
                    ubicacionUsuario.Idioma = IdiomUtil.ObtenerCodigoDeIdioma();
                    StringContent content = new StringContent(JsonConvert.SerializeObject(ubicacionUsuario), System.Text.Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAPIAsync(url, content);
                    if (response.IsSuccessStatusCode)
                    {
                        List<AlarmaCercana> responseMessage = JsonConvert.DeserializeObject<List<AlarmaCercana>>(JObject.Parse(await response.Content.ReadAsStringAsync())["data"].ToString());
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
                    string url = AppConfiguration.ApiHost + AppConfiguration.ListarDescripcionesAlarma + "?alarma_id=" + descripcionAlarma.alarma_id + "&user_id_thirdparty=" + descripcionAlarma.user_id_thirdparty + "&idioma_dispositivo=" + IdiomUtil.ObtenerCodigoDeIdioma();

                    HttpResponseMessage response = await client.GetAPIAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        List<DetalleDescripcionAlarma> responseMessage = JsonConvert.DeserializeObject<List<DetalleDescripcionAlarma>>(JObject.Parse(await response.Content.ReadAsStringAsync())["data"].ToString());
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
                return null;
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
                return new ResponseMessage() { IsSuccess = false, Data = "", Message = ex.Message };
            }
        }

        public static async Task<List<int>> ObtenerIdsTiposAlarma()
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
                    string url = AppConfiguration.ApiHost + AppConfiguration.ListarTiposAlarma + "?idioma=" + IdiomUtil.ObtenerCodigoDeIdioma();

                    HttpResponseMessage response = await client.GetAPIAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        List<TipoAlarma> tiposAlarma = JsonConvert.DeserializeObject<List<TipoAlarma>>(JObject.Parse(await response.Content.ReadAsStringAsync())["data"].ToString());

                        // Extraer solo los IDs de los tipos de alarma
                        List<int> idsTiposAlarma = tiposAlarma.Select(t => t.TipoalarmaId).ToList();
                        return idsTiposAlarma;
                    }
                    else
                    {
                        List<int> EmptyList = new List<int>();
                        return EmptyList;
                    }
                }
            }
            catch (Exception ex)
            {
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
                    string url = AppConfiguration.ApiHost + AppConfiguration.ListaMetricasBasicas + "?user_id_thirdParty=" + App.persona.user_id_thirdparty + "&idioma_dispositivo=" + IdiomUtil.ObtenerCodigoDeIdioma();

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
                return null;
            }
        }


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
                // Manejar excepciones aquí
                return false;
            }
        }

        public static async Task<List<RelationshipType>> ObtenerTiposRelaciones()
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

                    string url = AppConfiguration.ApiHost + AppConfiguration.ListarTiposRelaciones;

                    HttpResponseMessage response = await client.GetAPIAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        List<RelationshipType> responseMessage = JsonConvert.DeserializeObject<List<RelationshipType>>(JObject.Parse(await response.Content.ReadAsStringAsync())["data"].ToString());
                        return responseMessage;
                    }
                    else
                    {
                        List<RelationshipType> EmptyList = new List<RelationshipType>();
                        return EmptyList;
                    }
                }
            }
            catch (Exception ex)
            {
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
                return new ResponseMessage { IsSuccess = false, Message = $"Error: {ex.Message}" };
            }
        }

        public static async Task<ProtectorResponse> ObtenerProtectores(string userId, string idioma)
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
                    string url = AppConfiguration.ApiHost + AppConfiguration.ListarProtectores + $"?user_id_thirdParty_protegido={userId}&idioma_dispositivo={idioma}";

                    HttpResponseMessage response = await client.GetAPIAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        ProtectorResponse protectorsResponse = JsonConvert.DeserializeObject<ProtectorResponse>(await response.Content.ReadAsStringAsync());
                        return protectorsResponse;
                    }
                    else
                    {
                        return new ProtectorResponse { isSuccess = false, data = null, message = "Error al obtener protectores" };
                    }
                }
            }
            catch (Exception ex)
            {
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
                return new ResponseMessage() { IsSuccess = false, Data = "", Message = ex.Message };
            }
        }
        public static async Task<string> ObtenerParametrosDeUsuario(ParametroUsuarioRequest parametroUsuarioRequest)
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
                    string url = AppConfiguration.ApiHost + AppConfiguration.ConsultaParametrosUsuario;

                    StringContent content = new StringContent(JsonConvert.SerializeObject(parametroUsuarioRequest), System.Text.Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAPIAsync(url, content);
                    if (response.IsSuccessStatusCode)
                    {
                        string responseMessage = JObject.Parse(await response.Content.ReadAsStringAsync())["data"].ToString();
                        return responseMessage;
                    }
                    else
                    {
                        return string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
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
                        List<AlarmaCercana> AlarmasCercanas = JsonConvert.DeserializeObject<List<AlarmaCercana>>(JObject.Parse(await response.Content.ReadAsStringAsync())["data"].ToString());
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
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    //GET Method
                    string url = AppConfiguration.ApiHost + AppConfiguration.ComprobarVersionActiva + "?versioncliente=" + versioncliente;

                    HttpResponseMessage response = await client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        VersionVerificada IsValidVersion = JsonConvert.DeserializeObject<VersionVerificada>(JObject.Parse(await response.Content.ReadAsStringAsync())["data"].ToString());
                        return IsValidVersion;
                    }
                    else
                    {
                        return new VersionVerificada();
                    }
                }
            }
            catch (Exception ex)
            {
                return new VersionVerificada();
            }
        }
    }
}

