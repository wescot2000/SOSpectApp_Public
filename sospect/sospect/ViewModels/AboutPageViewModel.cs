using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Rg.Plugins.Popup.Services;
using sospect.Helpers;
using sospect.Models;
using sospect.Services;
using sospect.Utils;
using sospect.Views;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace sospect.ViewModels
{
    public class AboutPageViewModel : BaseViewModel
    {
        public ICommand OpenWebCommandWescot => new Command(async () => await OpenWebPageWescot());

        public ICommand OpenSospectWebCommand => new Command(async () => await OpenSospectWebPage());
        public ICommand OpenWebCommandIconfinder => new Command(async () => await OpenWebPageIconfinder());
        public ICommand OpenWebCommandDibuRome => new Command(async () => await OpenWebPageDibuRome());

        public ICommand SendEmailCommand => new Command(async () => await SendEmail());

        async Task SendEmail()
        {
            var LabelError = TranslateExtension.Translate("LabelError");
            var LabelOK = TranslateExtension.Translate("LabelOK");
            var LblErrorCorreoDispositivo = TranslateExtension.Translate("LblErrorCorreoDispositivo");
            var LblErrorCorreo = TranslateExtension.Translate("LblErrorCorreo");

            try
            {
                var message = new EmailMessage
                {
                    To = new List<string> { "soporte@wescot.com.co" },
                };

                await Email.ComposeAsync(message);
            }
            catch (FeatureNotSupportedException fbsEx)
            {
                // Email no es compatible en este dispositivo
                await Application.Current.MainPage.DisplayAlert(LabelError, LblErrorCorreoDispositivo, LabelOK);
                var properties = new Dictionary<string, string> {
                        { "Object", "AboutPageViewModel" },
                        { "Method", "SendEmail-FeatureNotSupportedException" }
                    };
                Microsoft.AppCenter.Crashes.Crashes.TrackError(fbsEx, properties);
            }
            catch (Exception ex)
            {
                // Algun otro error ocurrió
                await Application.Current.MainPage.DisplayAlert(LabelError, LblErrorCorreo + ex.Message, LabelOK);
                var properties = new Dictionary<string, string> {
                        { "Object", "AboutPageViewModel" },
                        { "Method", "SendEmail" }
                    };
                Microsoft.AppCenter.Crashes.Crashes.TrackError(ex, properties);
            }
        }



        async Task OpenSospectWebPage()
        {
            await Xamarin.Essentials.Browser.OpenAsync("https://www.wescotcorporation.com/sospect.html", Xamarin.Essentials.BrowserLaunchMode.SystemPreferred);
        }

        async Task OpenWebPageWescot()
        {
            await Xamarin.Essentials.Browser.OpenAsync("http://www.wescotcorporation.com", Xamarin.Essentials.BrowserLaunchMode.SystemPreferred);
        }

        async Task OpenWebPageIconfinder()
        {
            await Xamarin.Essentials.Browser.OpenAsync("https://www.iconfinder.com/iconsets/streamline-emoji-1", Xamarin.Essentials.BrowserLaunchMode.SystemPreferred);
        }

        async Task OpenWebPageDibuRome()
        {
            await Xamarin.Essentials.Browser.OpenAsync("https://web.facebook.com/diburomemusic/?_rdc=1&_rdr", Xamarin.Essentials.BrowserLaunchMode.SystemPreferred);
        }
    }
}

