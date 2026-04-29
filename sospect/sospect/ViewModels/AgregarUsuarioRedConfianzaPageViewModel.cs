using System;
using System.Collections.Generic;
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

namespace sospect.ViewModels
{
    public class AgregarUsuarioRedConfianzaPageViewModel : BaseViewModel
    {
        private string _userId;
        public string UserId
        {
            get => _userId;
            set => SetProperty(ref _userId, value);
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

        private bool _isUserDataVisible;
        public bool IsUserDataVisible
        {
            get => _isUserDataVisible;
            set => SetProperty(ref _isUserDataVisible, value);
        }

        private bool _isAdditionalMessageVisible;
        public bool IsAdditionalMessageVisible
        {
            get => _isAdditionalMessageVisible;
            set => SetProperty(ref _isAdditionalMessageVisible, value);
        }

        private bool _isAgregarUsuarioButtonVisible;
        public bool IsAgregarUsuarioButtonVisible
        {
            get => _isAgregarUsuarioButtonVisible;
            set => SetProperty(ref _isAgregarUsuarioButtonVisible, value);
        }

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

        private string _pais;
        public string Pais
        {
            get => _pais;
            set => SetProperty(ref _pais, value);
        }

        private string _nationalId;
        public string NationalId
        {
            get => _nationalId;
            set => SetProperty(ref _nationalId, value);
        }

        private string _nickname;
        public string Nickname
        {
            get => _nickname;
            set => SetProperty(ref _nickname, value);
        }

        private bool _isNicknameErrorVisible;
        public bool IsNicknameErrorVisible
        {
            get => _isNicknameErrorVisible;
            set => SetProperty(ref _isNicknameErrorVisible, value);
        }

        public ICommand ConsultarDisponibilidadCommand { get; set; }
        public ICommand AgregarUsuarioCommand { get; set; }

        public AgregarUsuarioRedConfianzaPageViewModel()
        {
            ConsultarDisponibilidadCommand = new Command(async () => await ConsultarDisponibilidadAsync());
            AgregarUsuarioCommand = new Command(async () => await AgregarUsuarioAsync());
        }

        private async Task ConsultarDisponibilidadAsync()
        {
            var LabelError = await TranslateExtension.TranslateAsync("LabelError");
            var LabelExito = await TranslateExtension.TranslateAsync("LabelExito");
            var LblCompleteCampos = await TranslateExtension.TranslateAsync("LblCompleteCampos");
            var LabelOK = await TranslateExtension.TranslateAsync("LabelOK");
            var LabelInformacion = await TranslateExtension.TranslateAsync("LabelInformacion");
            var MensajeError = await TranslateExtension.TranslateAsync("MensajeError");

            if (string.IsNullOrEmpty(UserId))
            {
                await ModernAlerts.ShowError(LabelError, LblCompleteCampos);
                return;
            }

            var request = new ConsultarUsuarioParaRedConfianzaRequest
            {
                UserIdThirdparty = UserId
            };

            IsRunning = true;
            try
            {
                var response = await ApiService.ConsultarUsuarioParaRedConfianza(request);

                if (response.IsSuccess)
                {
                    if (response.Data != null)
                    {
                        if (response.Data.Result == "failure")
                        {
                            // Remover espacios del mensaje
                            string messageKey = response.Data.Message.Replace(" ", string.Empty);

                            // Intentar obtener la traducción del mensaje
                            var translatedMessage = await TranslateExtension.TranslateAsync(messageKey);

                            // Si no se encuentra la traducción, usar el mensaje original
                            if (string.IsNullOrEmpty(translatedMessage))
                            {
                                translatedMessage = response.Data.Message;
                            }

                            ResultText = translatedMessage;
                            IsResultVisible = true;
                            IsUserDataVisible = false;
                            IsAdditionalMessageVisible = true;
                            IsAgregarUsuarioButtonVisible = false;
                        }
                        else if (response.Data.Result == "success")
                        {
                            // Remover espacios del mensaje
                            string messageKey = response.Data.Message.Replace(" ", string.Empty);

                            // Intentar obtener la traducción del mensaje
                            var translatedMessage = await TranslateExtension.TranslateAsync(messageKey);

                            // Si no se encuentra la traducción, usar el mensaje original
                            if (string.IsNullOrEmpty(translatedMessage))
                            {
                                translatedMessage = response.Data.Message;
                            }

                            Nombres = response.Data.Nombres;
                            Apellidos = response.Data.Apellidos;
                            NumeroMovil = response.Data.NumeroMovil;
                            Email = response.Data.Email;
                            Pais = response.Data.Pais;
                            NationalId = response.Data.NationalId;
                            ResultText = translatedMessage;
                            IsResultVisible = true;
                            IsUserDataVisible = true;
                            IsAdditionalMessageVisible = true;
                            IsAgregarUsuarioButtonVisible = true;
                        }
                    }
                }
                else
                {
                    // Remover espacios del mensaje
                    string messageKey = response.Message.Replace(" ", string.Empty);

                    // Intentar obtener la traducción del mensaje
                    var translatedMessage = await TranslateExtension.TranslateAsync(messageKey);

                    // Si no se encuentra la traducción, usar el mensaje original
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
                CrashlyticsHelper.LogError(ex, "AgregarUsuarioRedConfianzaPageViewModel", "ConsultarDisponibilidadAsync");
            }
            finally
            {
                IsRunning = false;
            }
        }

        private async Task AgregarUsuarioAsync()
        {
            var LabelError = await TranslateExtension.TranslateAsync("LabelError");
            var LabelExito = await TranslateExtension.TranslateAsync("LabelExito");
            var LabelOK = await TranslateExtension.TranslateAsync("LabelOK");
            var LabelInformacion = await TranslateExtension.TranslateAsync("LabelInformacion");
            var MensajeError = await TranslateExtension.TranslateAsync("MensajeError");

            if (string.IsNullOrWhiteSpace(Nickname))
            {
                IsNicknameErrorVisible = true;
                return;
            }
            IsNicknameErrorVisible = false;

            var request = new AgregarUsuarioRedConfianzaRequest
            {
                UserIdThirdpartyLider = App.persona.user_id_thirdparty,
                UserIdThirdpartyNuevo = UserId,
                Nickname = Nickname.Trim()
            };

            IsRunning = true;
            try
            {
                AgregarUsuarioRedConfianzaResponse response = await ApiService.AgregarUsuarioRedConfianza(request);

                if (response.IsSuccess)
                {
                    // Remover espacios del mensaje
                    string messageKey = response.Message.Replace(" ", string.Empty);

                    // Intentar obtener la traducción del mensaje
                    var translatedMessage = await TranslateExtension.TranslateAsync(messageKey);

                    // Si no se encuentra la traducción, usar el mensaje original
                    if (string.IsNullOrEmpty(translatedMessage))
                    {
                        translatedMessage = response.Message;
                    }

                    await ModernAlerts.ShowSuccess(LabelExito, translatedMessage);
                    Application.Current.MainPage = new SospectTabs();
                }
                else
                {

                    // Remover espacios del mensaje
                    string messageKey = response.Message.Replace(" ", string.Empty);

                    // Intentar obtener la traducción del mensaje
                    var translatedMessage = await TranslateExtension.TranslateAsync(messageKey);

                    // Si no se encuentra la traducción, usar el mensaje original
                    if (string.IsNullOrEmpty(translatedMessage))
                    {
                        translatedMessage = response.Message;
                    }

                    await ModernAlerts.ShowError(LabelError, translatedMessage);
                    await Application.Current.MainPage.Navigation.PopAsync();
                }
            }
            catch (Exception ex)
            {
                await ModernAlerts.ShowWarning(LabelInformacion, MensajeError);
                CrashlyticsHelper.LogError(ex, "AgregarUsuarioRedConfianzaPageViewModel", "AgregarUsuarioAsync");
            }
            finally
            {
                IsRunning = false;
            }
        }
    }
}
