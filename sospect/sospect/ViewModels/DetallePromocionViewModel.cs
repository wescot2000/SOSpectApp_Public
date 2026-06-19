// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using sospect.Models;
using sospect.Services;
using sospect.Helpers;

namespace sospect.ViewModels
{
    public class DetallePromocionViewModel : BaseViewModel
    {
        private long _subscripcionId;
        private DetallePromocion _detalle;
        private ObservableCollection<UsuarioNotificadoDisplay> _usuariosNotificados;
        private bool _tieneTextoPush;
        private string _logoTexto = "✗";
        private string _contactoTexto = "✗";
        private string _domicilioTexto = "✗";

        public DetallePromocion Detalle
        {
            get => _detalle;
            set => SetProperty(ref _detalle, value);
        }

        public ObservableCollection<UsuarioNotificadoDisplay> UsuariosNotificados
        {
            get => _usuariosNotificados;
            set => SetProperty(ref _usuariosNotificados, value);
        }

        public bool TieneTextoPush
        {
            get => _tieneTextoPush;
            set => SetProperty(ref _tieneTextoPush, value);
        }

        public string LogoTexto
        {
            get => _logoTexto;
            set => SetProperty(ref _logoTexto, value);
        }

        public string ContactoTexto
        {
            get => _contactoTexto;
            set => SetProperty(ref _contactoTexto, value);
        }

        public string DomicilioTexto
        {
            get => _domicilioTexto;
            set => SetProperty(ref _domicilioTexto, value);
        }

        public DetallePromocionViewModel(long subscripcionId)
        {
            _subscripcionId = subscripcionId;
            UsuariosNotificados = new ObservableCollection<UsuarioNotificadoDisplay>();
            _ = Task.Run(() => CargarDatos());
        }

        private async Task CargarDatos()
        {
            IsRunning = true;
            try
            {
                // Cargar detalle de la promoción
                var detalle = await ApiService.ObtenerDetallePromocion(_subscripcionId);
                if (detalle != null)
                {
                    Detalle = detalle;
                    TieneTextoPush = !string.IsNullOrWhiteSpace(detalle.TextoPushPersonalizado);
                    LogoTexto = detalle.LogoHabilitado ? "✓" : "✗";
                    ContactoTexto = detalle.ContactoHabilitado ? "✓" : "✗";
                    DomicilioTexto = detalle.DomicilioHabilitado ? "✓" : "✗";
                }

                // Cargar lista de usuarios notificados
                var usuarios = await ApiService.ConsultarUsuariosNotificados(_subscripcionId);
                if (usuarios != null)
                {
                    foreach (var usuario in usuarios)
                    {
                        UsuariosNotificados.Add(new UsuarioNotificadoDisplay
                        {
                            UserIdThirdparty = usuario.UserIdThirdparty,
                            UserIdAnonimizado = usuario.UserIdAnonimizado,
                            EnvioAceptadoFirebase = usuario.EnvioAceptadoFirebase,
                            ErrorCode = usuario.ErrorCode,
                            EstadoTexto = GetEstadoTexto(usuario.EnvioAceptadoFirebase)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "DetallePromocionViewModel", "CargarDatos");
            }
            finally
            {
                IsRunning = false;
            }
        }

        private string GetEstadoTexto(bool? envioAceptado)
        {
            if (envioAceptado == null)
                return TranslateExtension.Translate("LblPendiente") ?? "Pendiente";
            if (envioAceptado == true)
                return TranslateExtension.Translate("LblEnvioAceptado") ?? "Aceptado";
            return TranslateExtension.Translate("LblEnvioRechazado") ?? "Rechazado";
        }
    }

    /// <summary>
    /// Clase auxiliar para mostrar usuarios notificados con estado formateado
    /// </summary>
    public class UsuarioNotificadoDisplay
    {
        public string UserIdThirdparty { get; set; }
        public string UserIdAnonimizado { get; set; }
        public bool? EnvioAceptadoFirebase { get; set; }
        public string ErrorCode { get; set; }
        public string EstadoTexto { get; set; }
    }
}


