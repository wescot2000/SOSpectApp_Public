using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Input;
using Newtonsoft.Json;
using sospect.Helpers;
using sospect.Interfaces;
using sospect.Models;
using sospect.Services;
using sospect.Utils;
using sospect.Views;
using sospect.Views.Popups;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Controls;
using CommunityToolkit.Maui.Views;

namespace sospect.ViewModels
{
    public class TermsAndConditionsViewModel : BaseViewModel
    {
        private string _termsAndConditionsText;
        public string TermsAndConditionsText
        {
            get => _termsAndConditionsText;
            set => SetProperty(ref _termsAndConditionsText, value);
        }

        private bool _flagUsuarioDebeFirmar;
        public bool FlagUsuarioDebeFirmar
        {
            get => this._flagUsuarioDebeFirmar;
            set => this.SetValue(ref this._flagUsuarioDebeFirmar, value);
        }

        public ICommand AcceptTermsCommand { get; }
        public ICommand DeclineTermsCommand { get; }

        public TermsAndConditionsViewModel()
        {
            ParametrosUsuario parametros = JsonConvert.DeserializeObject<ParametrosUsuario>(Preferences.Get("ParametrosUsuario", ""));

            FlagUsuarioDebeFirmar = parametros.FlagUsuarioDebeFirmarCto;
            AcceptTermsCommand = new Command(AcceptTerms);
            DeclineTermsCommand = new Command(DeclineTerms);
            Task.Run(() => LoadTermsAndConditionsAsync());
        }

        private async void LoadTermsAndConditionsAsync()
        {
            IsRunning = true;
            try
            {
                TermsAndConditionsText = await ApiService.ObtenerContratoUsuario();
            }
            catch (Exception ex)
            {
                CrashlyticsHelper.LogError(ex, "TermsAndConditionsViewModel", "LoadTermsAndConditionsAsync");
            }

            IsRunning = false;
        }

        private async void AcceptTerms()
        {
            IsRunning = true;

            string LblExitoEnAceptacion = await TranslateExtension.TranslateAsync("LblExitoEnAceptacion");
            string LblFalloEnAceptacion = await TranslateExtension.TranslateAsync("LblFalloEnAceptacion");
            string LabelInformacion = await TranslateExtension.TranslateAsync("LabelInformacion");
            string LabelError = await TranslateExtension.TranslateAsync("LabelError");
            string LabelOK = await TranslateExtension.TranslateAsync("LabelOK");
            string MensajeError = await TranslateExtension.TranslateAsync("MensajeError");

            try
            {
                // LOG: Antes de llamar al API
                System.Diagnostics.Debug.WriteLine("[TermsViewModel] Llamando AceptarContratoDeUsuario...");

                var response = await ApiService.AceptarContratoDeUsuario(new AcceptContractRequest()
                {
                    PIpAceptacion = InternetUtil.GetPublicIpAddress(),
                    PUserIdThirdparty = App.persona.user_id_thirdparty
                });

                // LOG: Respuesta del API
                System.Diagnostics.Debug.WriteLine($"[TermsViewModel] Response.IsSuccess: {response.IsSuccess}");
                System.Diagnostics.Debug.WriteLine($"[TermsViewModel] Response.Message: {response.Message}");

                if (response.IsSuccess)
                {
                    // CRÍTICO: Actualizar cache local para que HomePage no vuelva a mostrar el contrato
                    try
                    {
                        var parametrosString = Preferences.Get("ParametrosUsuario", "");
                        if (!string.IsNullOrEmpty(parametrosString))
                        {
                            var parametros = JsonConvert.DeserializeObject<ParametrosUsuario>(parametrosString);
                            parametros.FlagUsuarioDebeFirmarCto = false;
                            Preferences.Set("ParametrosUsuario", JsonConvert.SerializeObject(parametros));
                            System.Diagnostics.Debug.WriteLine("[TermsViewModel] Cache local actualizado: FlagUsuarioDebeFirmarCto = false");
                        }
                    }
                    catch (Exception cacheEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[TermsViewModel] Error actualizando cache: {cacheEx.Message}");
                    }

                    // LOG: Mostrando popup de éxito
                    System.Diagnostics.Debug.WriteLine("[TermsViewModel] Mostrando popup de éxito...");

                    // MODERNO: Usar popup personalizado en lugar de DisplayAlert
                    await ModernAlerts.ShowSuccess(LabelInformacion, LblExitoEnAceptacion);

                    // LOG: Popup cerrado - esperando antes de navegar
                    System.Diagnostics.Debug.WriteLine("[TermsViewModel] Popup cerrado - esperando 300ms...");

                    // CRÍTICO: Delay para garantizar que el popup se cerró completamente en iOS
                    await Task.Delay(300);

                    // LOG: Navegando a SospectTabs
                    System.Diagnostics.Debug.WriteLine("[TermsViewModel] Cambiando MainPage a SospectTabs...");

                    // CAMBIO: Usar InvokeOnMainThreadAsync en lugar de BeginInvokeOnMainThread
                    // Navegar después de cerrar el popup
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        // NOTA: SospectTabs (TabbedPage) ya tiene NavigationPages internos.
                        // NO envolver en otro NavigationPage para evitar doble barra de navegación.
                        App.Current.MainPage = new SospectTabs();
                    });

                    // LOG: Navegación completada
                    System.Diagnostics.Debug.WriteLine("[TermsViewModel] MainPage cambiado exitosamente");
                }
                else
                {
                    // LOG: Error en la respuesta
                    System.Diagnostics.Debug.WriteLine($"[TermsViewModel] Error en respuesta: {response.Message}");

                    // MODERNO: Error también con popup personalizado
                    await ModernAlerts.ShowError(LabelError, LblFalloEnAceptacion);
                }
            }
            catch (Exception ex)
            {
                // LOG: Excepción capturada
                System.Diagnostics.Debug.WriteLine($"[TermsViewModel] EXCEPTION: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[TermsViewModel] StackTrace: {ex.StackTrace}");

                // MODERNO: Error de conexión con popup personalizado
                await ModernAlerts.ShowError(LabelError, MensajeError);
                CrashlyticsHelper.LogError(ex, "TermsAndConditionsViewModel", "AcceptTerms");
            }
            finally
            {
                // LOG: Finally block
                System.Diagnostics.Debug.WriteLine("[TermsViewModel] Finally: IsRunning = false");
                IsRunning = false;
            }
        }

        private async void DeclineTerms()
        {
            var LblDeclinacionAceptacion = await TranslateExtension.TranslateAsync("LblDeclinacionAceptacion");
            var LabelInformacion = await TranslateExtension.TranslateAsync("LabelInformacion");

            // MODERNO: Warning popup en lugar de DisplayAlert básico
            await ModernAlerts.ShowWarning(LabelInformacion, LblDeclinacionAceptacion);

            // Cerrar aplicación después del popup
            System.Diagnostics.Process.GetCurrentProcess().CloseMainWindow();
        }

        // ELIMINAR: Ya no necesitamos este método porque usamos ModernAlerts
        // private async Task ShowModernAlert(ModernAlertConfig config, Action onClosed = null) { ... }
    }
}