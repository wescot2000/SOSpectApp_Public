using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Maui.Views;
using sospect.Helpers;
using sospect.Interfaces;
using sospect.Models;
using sospect.Services;
using sospect.Utils;
using sospect.Views;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.Communication;
using Microsoft.Maui.Controls;

namespace sospect.ViewModels
{
    public class ManualUsuarioViewModel : BaseViewModel
    {
        // Lista actualizada con TODOS los idiomas soportados en el servidor
        private List<string> supportedLanguageCodes = new List<string>
        {
            // Idiomas originales
            "es", "af", "ar", "de", "fr", "hi", "hy", "it", "ja", "ko", "pl", "pt", "ru",
            
            // Nuevos idiomas agregados
            "am",  // Amárico
            "bg",  // Búlgaro
            "bn",  // Bengalí
            "bo",  // Tibetano
            "bs",  // Bosnio
            "ca",  // Catalán
            "cs",  // Checo
            "da",  // Danés
            "el",  // Griego
            "en",  // Inglés
            "et",  // Estonio
            "fa",  // Persa
            "fi",  // Finlandés
            "fj",  // Fiyiano
            "fo",  // Feroés
            "ga",  // Irlandés
            "gl",  // Gallego
            "he",  // Hebreo
            "hr",  // Croata
            "ht",  // Criollo haitiano
            "hu",  // Húngaro
            "id",  // Indonesio
            "is",  // Islandés
            "ka",  // Georgiano
            "kk",  // Kazajo
            "kn",  // Canarés
            "ku",  // Kurdo
            "lo",  // Lao
            "lt",  // Lituano
            "mk",  // Macedonio
            "ml",  // Malayalam
            "mn",  // Mongol
            "mr",  // Maratí
            "ms",  // Malayo
            "ne",  // Nepalí
            "no",  // Noruego
            "pa",  // Panyabí
            "ro",  // Rumano
            "sk",  // Eslovaco
            "sl",  // Esloveno
            "so",  // Somalí
            "sq",  // Albanés
            "sr",  // Serbio
            "sv",  // Sueco
            "te",  // Telugu
            "th",  // Tailandés
            "tk",  // Turcomano
            "tr",  // Turco
            "ty",  // Tahitiano
            "uk",  // Ucraniano
            "ur",  // Urdu
            "uz",  // Uzbeko
            "vi",  // Vietnamita
            "xh",  // Xhosa
            "zh"   // Chino
        };

        public string UrlManualUsuario
        {
            get
            {
                try
                {
                    var deviceLanguageCode = IdiomUtil.ObtenerCodigoDeIdioma();

                    // Si el idioma del dispositivo no está soportado, usar inglés por defecto
                    if (!supportedLanguageCodes.Contains(deviceLanguageCode))
                    {
                        deviceLanguageCode = "en";
                    }

                    return $"https://sospect-s3-data-bucket-prod.s3.us-east-1.amazonaws.com/manuals/{deviceLanguageCode}.pdf";
                }
                catch (Exception ex)
                {
                    // En caso de error, devolver URL con idioma inglés por defecto
                    LogError(ex, "UrlManualUsuario", new Dictionary<string, string> {
                        { "Issue", "Error getting device language code" }
                    });
                    return "https://sospect-s3-data-bucket-prod.s3.us-east-1.amazonaws.com/manuals/en.pdf";
                }
            }
        }

        public ICommand OpenWebCommand => new Command(async () => await OpenWebPage());

        public ICommand OpenWebCommandWescot => new Command(async () => await OpenWebPageWescot());

        public ICommand OpenSospectWebCommand => new Command(async () => await OpenSospectWebPage());

        public ICommand SendEmailCommand => new Command(async () => await SendEmail());

        private void LogError(Exception ex, string method, Dictionary<string, string> additionalProperties = null)
        {
            try
            {
                CrashlyticsHelper.LogError(ex, "ManualUsuarioViewModel", method);
            }
            catch (Exception logEx)
            {
                // Si incluso el logging falla, usar Debug.WriteLine como última opción
                System.Diagnostics.Debug.WriteLine($"Failed to log error in {method}: {logEx.Message}");
                System.Diagnostics.Debug.WriteLine($"Original error: {ex.Message}");
            }
        }

        async Task SendEmail()
        {
            try
            {
                var message = new EmailMessage
                {
                    To = new List<string> { "soporte@wescot.com.co" },
                };

                await Email.Default.ComposeAsync(message);
            }
            catch (FeatureNotSupportedException fbsEx)
            {
                // Email no es compatible en este dispositivo
                var LabelError = await TranslateExtension.TranslateAsync("LabelError");
                var LblErrorCorreoDispositivo = await TranslateExtension.TranslateAsync("LblErrorCorreoDispositivo");
                await ModernAlerts.ShowError(LabelError, LblErrorCorreoDispositivo);
                LogError(fbsEx, "SendEmail-FeatureNotSupportedException");
            }
            catch (Exception ex)
            {
                // Algún otro error ocurrió
                var LabelError = await TranslateExtension.TranslateAsync("LabelError");
                var LblErrorCorreo = await TranslateExtension.TranslateAsync("LblErrorCorreo");
                await ModernAlerts.ShowError(LabelError, $"{LblErrorCorreo}: {ex.Message}");
                LogError(ex, "SendEmail", new Dictionary<string, string> {
                    { "ErrorType", ex.GetType().Name }
                });
            }
        }

        async Task OpenSospectWebPage()
        {
            try
            {
                await Browser.OpenAsync("https://www.wescotcorporation.com/sospect.html", BrowserLaunchMode.SystemPreferred);
            }
            catch (Exception ex)
            {
                var LabelError = await TranslateExtension.TranslateAsync("LabelError");
                var LblErrorNavegador = await TranslateExtension.TranslateAsync("LblErrorNavegador");
                await ModernAlerts.ShowError(LabelError, $"{LblErrorNavegador}: {ex.Message}");
                LogError(ex, "OpenSospectWebPage");
            }
        }

        async Task OpenWebPage()
        {
            try
            {
                await Browser.OpenAsync(UrlManualUsuario, BrowserLaunchMode.SystemPreferred);
            }
            catch (Exception ex)
            {
                var LabelError = await TranslateExtension.TranslateAsync("LabelError");
                var LblErrorManual = await TranslateExtension.TranslateAsync("LblErrorManual");
                await ModernAlerts.ShowError(LabelError, $"{LblErrorManual}: {ex.Message}");
                LogError(ex, "OpenWebPage", new Dictionary<string, string> {
                    { "Url", UrlManualUsuario }
                });
            }
        }

        async Task OpenWebPageWescot()
        {
            try
            {
                await Browser.OpenAsync("http://www.wescotcorporation.com", BrowserLaunchMode.SystemPreferred);
            }
            catch (Exception ex)
            {
                var LabelError = await TranslateExtension.TranslateAsync("LabelError");
                var LblErrorSitioWeb = await TranslateExtension.TranslateAsync("LblErrorSitioWeb");
                await ModernAlerts.ShowError(LabelError, $"{LblErrorSitioWeb}: {ex.Message}");
                LogError(ex, "OpenWebPageWescot");
            }
        }
    }
}