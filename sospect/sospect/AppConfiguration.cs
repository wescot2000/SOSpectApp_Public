// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
namespace sospect
{
    public static class AppConfiguration
    {
        public static bool IsDevelopment = false; // Cambia a 'false' para producción o basado en alguna condición

        internal static string ApiHost = IsDevelopment ? "https://api.wescot.com.co" : "https://www.wescotcorp.com";
        internal static string WebHost = IsDevelopment ? "https://dev.sospect.com" : "https://www.sospect.com";

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
        internal static string ListaMetricasPersonales = "/Reportes/ListaMetricasPersonales";
        internal static string ObtenerPromedioEfectivoAlarmas = "/Reportes/ObtenerPromedioEfectivoAlarmas";
        internal static string MarcarTodosComoLeidos = "/Mensajes/MarcarTodosComoLeidos";
        internal static string CerrarAlarma = "/Alarma/CerrarAlarma";
        internal static string ProponerCierre = "/Alarma/ProponerCierre";
        internal static string VotarCierre = "/Alarma/VotarCierre";
        internal static string ObtenerSolicitudCierreActiva = "/Alarma/ObtenerSolicitudCierreActiva";
        internal static string AsignarAlarma = "/Alarma/AsignarAlarma";
        internal static string ConsultarConfigNotificaciones = "/Parametros/ConsultaConfNotificaciones";
        internal static string ActualizaConfNotificaciones = "/Parametros/ActualizaConfNotificaciones";
        internal static string WipeUserData = "/Personas/WipeUserData";
        internal static string HistorialAlarmasUbicacion = "/Alarma/HistorialAlarmasUbicacion";
        internal static string LlenarInformacionInicial = "/personas/LlenarInformacionInicial";
        internal static string AgregarUsuarioRedConfianza = "/personas/AgregarUsuarioRedConfianza";
        internal static string ConsultarUsuarioParaRedConfianza = "/personas/ConsultarUsuarioParaRedConfianza";
        internal static string ListarUsuariosRedConfianza = "/personas/ListarUsuariosRedConfianza";
        internal static string SubirArchivoMultimedia = "/Alarma/SubirArchivoMultimedia";
        internal static string PresignedUploadUrl = "/Alarma/PresignedUploadUrl";
        internal static string ObtenerFeedParaTi = "/Ubicaciones/ObtenerFeedParaTi";
        internal static string ObtenerFeedSiguiendo = "/Ubicaciones/ObtenerFeedSiguiendo";

        // SOCIAL (23-02-2026)
        internal static string SeguirUsuario = "/Social/SeguirUsuario";
        internal static string DejarDeSeguirUsuario = "/Social/DejarDeSeguirUsuario";
        internal static string EstasSiguiendo = "/Social/EstasSiguiendo";
        internal static string DarLikeAlarma = "/Social/DarLikeAlarma";
        internal static string QuitarLikeAlarma = "/Social/QuitarLikeAlarma";
        internal static string ReenviarAlarma = "/Social/ReenviarAlarma";
        internal static string QuitarReenvioAlarma = "/Social/QuitarReenvioAlarma";
        internal static string ObtenerPaisesConUsuarios = "/Social/ObtenerPaisesConUsuarios";
        internal static string ObtenerPaises = "/Parametros/ObtenerPaises";             // NUEVO 2026-02-26
        internal static string ObtenerAlarmasPorZona = "/Ubicaciones/ObtenerAlarmasPorZona";
        internal static string PinesMapa = "/Ubicaciones/PinesMapa";              // NUEVO 2026-03-01

        // POLÍTICOS (2026-03-05)
        internal static string ObtenerAutoridadesPorAlarma = "/Politicos/ObtenerAutoridadesPorAlarma";
        internal static string ObtenerReportePolitico = "/Politicos/ObtenerReportePolitico";  // 2026-03-09
        internal static string RegistrarAprobacion = "/Politicos/RegistrarAprobacion";         // 2026-03-28
        internal static string ObtenerAprobacionPersona = "/Politicos/ObtenerAprobacionPersona"; // 2026-03-28
        internal static string ObtenerRankingPoliticos = "/Politicos/ObtenerRankingPoliticos"; // 2026-04-07
    }
}



