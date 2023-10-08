using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using sospect.CustomRenderers;
using sospect.Interfaces;
using sospect.Models;
using sospect.Services;
using sospect.Utils;
using sospect.Views;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using sospect.Helpers;


namespace sospect
{
    public partial class App : Application
    {
        public static Persona persona;
        public static Ubicaciones ubicacionActual { get; set; }
        public static string TokenHubNotification { get; set; }
        public static List<AlarmaCercana>? AlarmasCercanasAMostrar { get; set; }
        public static List<CustomPin> CustomPins { get; set; }
        public static bool justCheckedNotificationPermissions = false;

        public App()
        {
            InitializeComponent();

            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                var exception = args.ExceptionObject as Exception;
                if (exception != null)
                {
                    Microsoft.AppCenter.Crashes.Crashes.TrackError(exception);
                }
            };

            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                Microsoft.AppCenter.Crashes.Crashes.TrackError(args.Exception);
            };

            var page = new MainPage();
            NavigationPage.SetHasNavigationBar(page, false); //Added for remove white header on splash
            MainPage = new NavigationPage(page);
           
            Connectivity.ConnectivityChanged += InternetUtil.Connectivity_ConnectivityChanged;
            ServiceContainer.Resolve<IPushDemoNotificationActionService>().ActionTriggered += NotificationActionTriggered;
        }

        private async Task SetupMainPageAsync()
        {
            try
            {
                string versionCliente = AppInfo.VersionString;
                VersionVerificada IsValidVersion = await ApiService.VerificarVersion(int.Parse(versionCliente));

                if (IsValidVersion.flag_soportada)
                {
                    if (Preferences.Get("User", "") != string.Empty)
                    {
                        App.ubicacionActual = new Ubicaciones();
                        persona = JsonConvert.DeserializeObject<Persona>(Preferences.Get("User", ""));
                        ubicacionActual.p_user_id_thirdparty = persona.user_id_thirdparty;
                        ubicacionActual.Idioma = persona.Idioma;
                        MainPage = new NavigationPage(new SospectTabs()) { BarBackgroundColor = Color.Black };
                    }
                    else
                    {
                        MainPage = new NavigationPage(new LoginPage()) { BarBackgroundColor = Color.Black };
                    }
                }
                else
                {
                    var LabelInformacion = TranslateExtension.Translate("LabelInformacion");
                    var LabelOK = TranslateExtension.Translate("LabelOK");
                    var LabelNuevaVersion = TranslateExtension.Translate("LabelNuevaVersion");
                    // mostrar alerta

                    await MainPage.DisplayAlert(LabelInformacion, LabelNuevaVersion, LabelOK);
                    
                        // abrir la tienda correspondiente
                        if (Device.RuntimePlatform == Device.Android)
                        {
                            await Launcher.TryOpenAsync("https://play.google.com/store/apps/details?id=com.wescotcorp.sospect");
                        }
                        else if (Device.RuntimePlatform == Device.iOS)
                        {
                            await Launcher.TryOpenAsync("itms-apps://itunes.apple.com/us/app/apple-store/com.wescotcorp.sospect");
                        }
                    
                }
            }
            catch (Exception ex)
            {
                // si hay una excepción (posiblemente debido a problemas de red), simplemente continúa
                MainPage = new NavigationPage(new SospectTabs()) { BarBackgroundColor = Color.Black };
                var properties = new Dictionary<string, string> {
                        { "Object", "App" },
                        { "Method", "SetupMainPageAsync" }
                    };
                Microsoft.AppCenter.Crashes.Crashes.TrackError(ex, properties);
            }
        }



        protected async override void OnStart()
        {
            await SetupMainPageAsync();

            if (!string.IsNullOrEmpty(Preferences.Get("alarma_id", "")) && Preferences.Get("alarma_id", "") != "0")
            {
                await App.Current.MainPage.Navigation.PushAsync(new DescribirPage(long.Parse((Preferences.Get("alarma_id", "")))));
            }
        }

        protected override void OnSleep()
        {

        }

        protected async override void OnResume()
        {
            if (!string.IsNullOrEmpty(Preferences.Get("alarma_id", "")) && Preferences.Get("alarma_id", "") != "0")
            {
               await App.Current.MainPage.Navigation.PushAsync(new DescribirPage(long.Parse((Preferences.Get("alarma_id", "")))));
            }

            if (Device.RuntimePlatform == Device.iOS && justCheckedNotificationPermissions)
            {
                var hasNotificationPermission = await HasNotificationPermission();

                // If permissions are still not granted, show the alert to the user
                if (!hasNotificationPermission)
                {
                    var LabelOK = TranslateExtension.Translate("LabelOK");
                    var LblDebesHabilitarNotif = TranslateExtension.Translate("LblDebesHabilitarNotif");

                    // Display the alert
                    await Application.Current.MainPage.DisplayAlert("SOSpect", LblDebesHabilitarNotif, LabelOK);

                    // Open the settings page
                    DependencyService.Get<ISettingsService>().OpenSettings();
                    justCheckedNotificationPermissions = false;
                }
                else
                {
                    DependencyService.Get<ISettingsService>().RegisterDeviceAgain();
                }
            }
        }

        public Task<bool> HasNotificationPermission()
        {
            return DependencyService.Get<IPermissionManager>().CheckNotificationPermission();
        }

        void NotificationActionTriggered(object sender, string e) => ShowActionAlert(e);

        void ShowActionAlert(string alarma_id)
            => MainThread.BeginInvokeOnMainThread(()
                =>
            {
                App.Current.MainPage.Navigation.PushAsync(new DescribirPage(long.Parse(alarma_id)));
            });

    }
}
