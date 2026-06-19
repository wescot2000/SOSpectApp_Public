// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using Newtonsoft.Json;
using System.Collections.Generic;

namespace sospect.Models
{
    /// <summary>
    /// Modelo para representar un usuario notificado en una promoción
    /// </summary>
    public class UsuarioNotificado
    {
        [JsonProperty("userIdThirdparty")]
        public string UserIdThirdparty { get; set; }

        [JsonProperty("userIdAnonimizado")]
        public string UserIdAnonimizado { get; set; }

        [JsonProperty("envioAceptadoFirebase")]
        public bool? EnvioAceptadoFirebase { get; set; }

        [JsonProperty("errorCode")]
        public string ErrorCode { get; set; }
    }

    /// <summary>
    /// Respuesta del endpoint de usuarios notificados
    /// </summary>
    public class ConsultarUsuariosNotificadosResponse
    {
        [JsonProperty("subscripcionId")]
        public long SubscripcionId { get; set; }

        [JsonProperty("totalUsuarios")]
        public int TotalUsuarios { get; set; }

        [JsonProperty("enviosAceptados")]
        public int EnviosAceptados { get; set; }

        [JsonProperty("enviosRechazados")]
        public int EnviosRechazados { get; set; }

        [JsonProperty("enviosPendientes")]
        public int EnviosPendientes { get; set; }

        [JsonProperty("usuarios")]
        public List<UsuarioNotificado> Usuarios { get; set; } = new List<UsuarioNotificado>();
    }
}


