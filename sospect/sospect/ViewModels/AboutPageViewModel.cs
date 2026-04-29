using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.ApplicationModel.Communication;
using CommunityToolkit.Maui.Views;
using sospect.Helpers;
using sospect.Interfaces;
using sospect.Models;
using sospect.Services;
using sospect.Utils;
using sospect.Views;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace sospect.ViewModels
{
    public class AboutPageViewModel : BaseViewModel
    {
        public string AppVersion { get; } = $"SOSpect App Version: {AppInfo.VersionString}";

        public ICommand OpenWebCommandWescot => new Command(async () => await OpenWebPageWescot());
        public ICommand OpenSospectWebCommand => new Command(async () => await OpenSospectWebPage());
        public ICommand OpenWebCommandIconfinder => new Command(async () => await OpenWebPageIconfinder());
        public ICommand OpenWebCommandDibuRome => new Command(async () => await OpenWebPageDibuRome());
        public ICommand SendEmailCommand => new Command(async () => await SendEmail());

        private void LogError(Exception ex, string method, Dictionary<string, string> additionalProperties = null)
        {
            try
            {
                CrashlyticsHelper.LogError(ex, "AboutPageViewModel", method);
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
                LogError(ex, "OpenSospectWebPage", new Dictionary<string, string> {
                    { "Url", "https://www.wescotcorporation.com/sospect.html" }
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
                LogError(ex, "OpenWebPageWescot", new Dictionary<string, string> {
                    { "Url", "http://www.wescotcorporation.com" }
                });
            }
        }

        async Task OpenWebPageIconfinder()
        {
            try
            {
                await Browser.OpenAsync("https://www.iconfinder.com/iconsets/streamline-emoji-1", BrowserLaunchMode.SystemPreferred);
            }
            catch (Exception ex)
            {
                var LabelError = await TranslateExtension.TranslateAsync("LabelError");
                var LblErrorNavegador = await TranslateExtension.TranslateAsync("LblErrorNavegador");
                await ModernAlerts.ShowError(LabelError, $"{LblErrorNavegador}: {ex.Message}");
                LogError(ex, "OpenWebPageIconfinder", new Dictionary<string, string> {
                    { "Url", "https://www.iconfinder.com/iconsets/streamline-emoji-1" }
                });
            }
        }

        async Task OpenWebPageDibuRome()
        {
            try
            {
                await Browser.OpenAsync("https://web.facebook.com/diburomemusic/?_rdc=1&_rdr", BrowserLaunchMode.SystemPreferred);
            }
            catch (Exception ex)
            {
                var LabelError = await TranslateExtension.TranslateAsync("LabelError");
                var LblErrorNavegador = await TranslateExtension.TranslateAsync("LblErrorNavegador");
                await ModernAlerts.ShowError(LabelError, $"{LblErrorNavegador}: {ex.Message}");
                LogError(ex, "OpenWebPageDibuRome", new Dictionary<string, string> {
                    { "Url", "https://web.facebook.com/diburomemusic/?_rdc=1&_rdr" }
                });
            }
        }
    }
}