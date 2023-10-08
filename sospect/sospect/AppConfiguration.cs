using System;
namespace sospect
{
    public static class AppConfiguration
    {
        public static bool IsDevelopment = false; // Cambia a 'false' para producción o basado en alguna condición

        internal static string ApiHost = IsDevelopment ? "https://www.wescot.com.co:81" : "https://www.wescotcorp.com";

        internal static string Login = "/Personas/LoginUser";
        internal static string RegisterUser = "/Personas/RegisterUser";
        internal static string RegistrarAlarma = "/Alarma/InsertarAlarma";
        internal static string DescribirAlarma = "/Alarma/DescribirAlarma";
        internal static string TraerAlarma = "/Alarma/TraerAlarma";
        internal static string ActualizarDescripcionAlarma = "/Alarma/ActualizarDescripcionAlarma";
        internal static string CalificarDescripcionesAlarma = "/Alarma/CalificarDescripcionesAlarma";
        internal static string CalificarAlarma = "/Alarma/CalificarAlarma";
        internal static string ListarDescripcionesAlarma = "/Alarma/ListarDescripcionesAlarma";
        internal static string ListarTiposAlarma = "/Alarma/ListarTiposAlarma";
        internal static string InsertarUbicacionUsuario = "/Ubicaciones/InsertaUbicacion";
        internal static string ConsultaParametrosUsuario = "/Parametros/ConsultaParametros";
        internal static string AceptacionCondicionesUso = "/Contratos/AceptacionCondiciones";
        internal static string ListarValoresPoderes = "/Transacciones/ListarValoresPoderes";
        internal static string ListarTiposRelaciones = "/Subscripciones/ListarTiposRelaciones";
        internal static string ListarTiposSubscripciones = "/Transacciones/ListarTiposSubscripciones";
        internal static string ObtenerContrato = "/contratos/ObtenerContrato";
        internal static string SolicitarPermisoAProtegido = "/Subscripciones/SolicitarPermisoAProtegido";
        internal static string AprobarPermisoAProtector = "/Subscripciones/AprobarPermisoAProtector";
        internal static string RechazarPermisoAProtector = "/Subscripciones/RechazarPermisoAProtector";
        internal static string SuspenderPermisoAProtector = "/Subscripciones/SuspenderPermisoAProtector";
        internal static string ListarProtegidos = "/Subscripciones/ListarProtegidos";
        internal static string ListarProtectores = "/Subscripciones/ListarProtectores";
        internal static string EliminarProtegido = "/Subscripciones/EliminarProtegido";
        internal static string EliminarProtector = "/Subscripciones/EliminarProtector";
        internal static string ListarMensajes = "/Mensajes/ListarMensajes";
        internal static string LeerMensaje = "/Mensajes/LeerMensaje";
        internal static string SubscripcionProtegido = "/Subscripciones/SubscripcionProtegido";
        internal static string SubscripcionZonaVigilancia = "/Subscripciones/SubscripcionZonaVigilancia";
        internal static string SubscripcionRadioAlarmas = "/Subscripciones/SubscripcionRadioAlarmas";
        internal static string ListarSolicitudesPendientes = "/Subscripciones/ListarSolicitudesPendientes";
        internal static string ListarSolicitudesAprobadas = "/Subscripciones/ListarSolicitudesAprobadas";
        internal static string CompraPoderes = "/Transacciones/CompraPoderes";
        internal static string ListarMisSubscripciones = "/Subscripciones/ListarMisSubscripciones";
        internal static string RenovarSubscripcion = "/Subscripciones/RenovarSubscripcion";
        internal static string CancelarSubscripcion = "/Subscripciones/CancelarSubscripcion";
        internal static string ListarRadiosDisponibles = "/Subscripciones/ListarRadiosDisponibles";
        internal static string ComprobarVersionActiva = "/Versiones/ComprobarVersionActiva";
        internal static string ListarParticipacionTipoAlarma = "/Reportes/ListarParticipacionTipoAlarma";
        internal static string ListaMetricasBasicas = "/Reportes/ListaMetricasBasicas";
        internal static string ObtenerPromedioEfectivoAlarmas = "/Reportes/ObtenerPromedioEfectivoAlarmas";
        internal static string MarcarTodosComoLeidos = "/Mensajes/MarcarTodosComoLeidos";
        internal static string CerrarAlarma = "/Alarma/CerrarAlarma";
        internal static string AsignarAlarma = "/Alarma/AsignarAlarma";

    }
}

