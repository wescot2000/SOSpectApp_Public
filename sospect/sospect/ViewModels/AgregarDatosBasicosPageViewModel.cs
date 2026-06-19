// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Newtonsoft.Json;
using sospect.Helpers;
using sospect.Interfaces;
using sospect.Models;
using sospect.Services;
using sospect.Views;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace sospect.ViewModels
{
    public class AgregarDatosBasicosPageViewModel : BaseViewModel
    {
        private string _nombres;
        public string Nombres
        {
            get => _nombres;
            set => SetProperty(ref _nombres, value);
        }

        private string _apellidos;
        public string Apellidos
        {
            get => _apellidos;
            set => SetProperty(ref _apellidos, value);
        }

        private string _numeroMovil;
        public string NumeroMovil
        {
            get => _numeroMovil;
            set => SetProperty(ref _numeroMovil, value);
        }

        private string _email;
        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        // ── País: Picker sobre catálogo maestro de países (ISO alpha-2) ─────────
        // MODIFICADO 2026-02-26: reemplaza la propiedad Pais (string libre)
        private ObservableCollection<PaisItem> _paisesDisponibles = new();
        public ObservableCollection<PaisItem> PaisesDisponibles
        {
            get => _paisesDisponibles;
            set => SetProperty(ref _paisesDisponibles, value);
        }

        private PaisItem? _paisSeleccionado;
        public PaisItem? PaisSeleccionado
        {
            get => _paisSeleccionado;
            set => SetProperty(ref _paisSeleccionado, value);
        }
        // ────────────────────────────────────────────────────────────────────────

        private string _nationalId;
        public string NationalId
        {
            get => _nationalId;
            set => SetProperty(ref _nationalId, value);
        }

        private string _resultText;
        public string ResultText
        {
            get => _resultText;
            set => SetProperty(ref _resultText, value);
        }

        private bool _isResultVisible;
        public bool IsResultVisible
        {
            get => _isResultVisible;
            set => SetProperty(ref _isResultVisible, value);
        }

        public ICommand SaveBasicDataCommand { get; set; }

        public AgregarDatosBasicosPageViewModel()
        {
            SaveBasicDataCommand = new Command(async () => await SaveBasicDataAsync());
            _ = LoadUserData();
        }

        private async Task LoadUserData()
        {
            IsRunning = true;
            try
            {
                // 1. Cargar catálogo de países (GET /Parametros/ObtenerPaises)
                var paises = await ApiService.ObtenerPaises();
                PaisesDisponibles = new ObservableCollection<PaisItem>(paises);

                // 2. Cargar datos actuales del usuario
                var request = new ConsultarUsuarioParaRedConfianzaRequest
                {
                    UserIdThirdparty = App.persona.user_id_thirdparty
                };

                var response = await ApiService.ConsultarUsuarioParaRedConfianza(request);

                if (response.IsSuccess && response.Data != null)
                {
                    Nombres    = response.Data.Nombres;
                    Apellidos  = response.Data.Apellidos;
                    NumeroMovil = response.Data.NumeroMovil;
                    Email      = response.Data.Email;
                    NationalId = response.Data.NationalId;

                    // Preseleccionar el país del usuario en el Picker (por ISO alpha-2)
                    // response.Data.Pais ahora contiene ISO alpha-2 ("CO", "MX") gracias al JOIN con paises
                    if (!string.IsNullOrWhiteSpace(response.Data.Pais))
                    {
                        PaisSeleccionado = PaisesDisponibles
                            .FirstOrDefault(p => string.Equals(p.PaisId, response.Data.Pais,
                                StringComparison.OrdinalIgnoreCase));
                    }
                }
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "AgregarDatosBasicosPageViewModel", "LoadUserData");
            }
            finally
            {
                IsRunning = false;
            }
        }

        private async Task SaveBasicDataAsync()
        {
            var LabelError             = await TranslateExtension.TranslateAsync("LabelError");
            var LabelExito             = await TranslateExtension.TranslateAsync("LabelExito");
            var LblCompleteCampos      = await TranslateExtension.TranslateAsync("LblCompleteCampos");
            var LabelOK                = await TranslateExtension.TranslateAsync("LabelOK");
            var LabelInformacion       = await TranslateExtension.TranslateAsync("LabelInformacion");
            var MensajeError           = await TranslateExtension.TranslateAsync("MensajeError");
            var LabelGuardadoRedConfianza = await TranslateExtension.TranslateAsync("LabelGuardadoRedConfianza");

            if (string.IsNullOrEmpty(Nombres) || string.IsNullOrEmpty(Apellidos)
                || string.IsNullOrEmpty(NumeroMovil) || string.IsNullOrEmpty(Email)
                || PaisSeleccionado == null
                || string.IsNullOrEmpty(NationalId))
            {
                await ModernAlerts.ShowError(LabelError, LblCompleteCampos);
                return;
            }

            var informacionInicialRequest = new InformacionInicialModelRequest
            {
                UserIdThirdparty = App.persona.user_id_thirdparty,
                Nombres     = Nombres,
                Apellidos   = Apellidos,
                NumeroMovil = NumeroMovil,
                Email       = Email,
                Pais        = PaisSeleccionado.PaisId,  // ISO alpha-2: "CO", "MX", "US"
                NationalId  = NationalId
            };

            IsRunning = true;
            try
            {
                ResponseMessage response = await ApiService.AgregarDatosBasicos(informacionInicialRequest);

                if (response.IsSuccess)
                {
                    await ModernAlerts.ShowSuccess(LabelExito, LabelGuardadoRedConfianza);
                    Application.Current.MainPage = new SospectTabs();
                }
                else
                {
                    string messageKey = response.Message.Replace(" ", string.Empty);
                    var translatedMessage = await TranslateExtension.TranslateAsync(messageKey);

                    if (string.IsNullOrEmpty(translatedMessage))
                    {
                        translatedMessage = response.Message;
                    }

                    await ModernAlerts.ShowError(LabelError, translatedMessage);
                }
            }
            catch (Exception ex)
            {
                await ModernAlerts.ShowWarning(LabelInformacion, MensajeError);
                CrashlyticsHelper.LogError(ex, "AgregarDatosBasicosPageViewModel", "SaveBasicDataAsync");
            }
            finally
            {
                IsRunning = false;
            }
        }
    }
}


