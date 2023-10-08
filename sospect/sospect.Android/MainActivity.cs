using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.OS;
using Android.Content;
using Android;
using Android.Media;
using sospect.Services;
using sospect.Droid.Services;
using Firebase.Iid;
using sospect.Interfaces;
using Xamarin.Forms;
using Plugin.InAppBilling;
using Android.Widget;
using Firebase;
using Firebase.Messaging;
using System.Threading.Tasks;
using Android.Gms.Extensions;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;

namespace sospect.Droid
{
    [Activity(Label = "SOSpect",
        Icon = "@mipmap/icon",
        Theme = "@style/splashscreen",
        MainLauncher = true,
        LaunchMode = LaunchMode.SingleTop,
        ScreenOrientation = ScreenOrientation.Portrait,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity, Android.Gms.Tasks.IOnSuccessListener
    {
        const int RequestLocationId = 0;
        internal static readonly string CHANNEL_ID = "SOSPectChannel";
        internal static readonly int NOTIFICATION_ID = 100;

        #region Nueva implementacion Notificaciones push

        IPushDemoNotificationActionService _notificationActionService;
        IDeviceInstallationService _deviceInstallationService;

        IPushDemoNotificationActionService NotificationActionService
            => _notificationActionService ??
                (_notificationActionService =
                ServiceContainer.Resolve<IPushDemoNotificationActionService>());

        IDeviceInstallationService DeviceInstallationService
            => _deviceInstallationService ??
                (_deviceInstallationService =
                ServiceContainer.Resolve<IDeviceInstallationService>());

        public void OnSuccess(Java.Lang.Object result)
        => DeviceInstallationService.Token =
            result.Class.GetMethod("getToken").Invoke(result).ToString();

        void ProcessNotificationActions(Intent intent)
        {
            try
            {
                if (intent?.HasExtra("alarma_id") == true)
                {
                    var action = intent.GetStringExtra("alarma_id");

                    if (!string.IsNullOrEmpty(action))
                        NotificationActionService.TriggerAction(action);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        //public static void StartLocationService(Context context)
        //{
        //    Intent intent = new Intent(context, typeof(BackgroundService));
        //    context.StartService(intent);
        //}

        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);
            ProcessNotificationActions(intent);
        }

        #endregion

        readonly string[] LocationPermissions =
        {
            Manifest.Permission.AccessCoarseLocation,
            Manifest.Permission.AccessFineLocation
        };

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);


            AppCenter.Start("9af016bc-f990-45dc-9ba6-1b05c60d2695",
                   typeof(Analytics), typeof(Crashes));

            // Iniciar la conexión con Google Play Billing
            Plugin.InAppBilling.CrossInAppBilling.Current.ConnectAsync();
            Bootstrap.Begin(() => new DeviceInstallationService());

            DependencyService.Register<IBackgroundService, BackgroundService>();
            if (DeviceInstallationService.NotificationsSupported)
            {

                Task.Run(async () =>
                {
                    try
                    {
                        var token = await FirebaseMessaging.Instance.GetToken();
                        // Aquí puedes hacer algo con el token, como enviarlo a tu servidor.
                    }
                    catch (Exception ex)
                    {
                        await App.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
                    }
                });


            }

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.SetFlags("FastRenderers_Experimental");
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
            Xamarin.FormsMaps.Init(this, savedInstanceState);
            Rg.Plugins.Popup.Popup.Init(this);
            CreateNotificationChannel();

            LoadApplication(new App());
            ProcessNotificationActions(Intent);
        }


        void CreateNotificationChannel()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            {
                // Notification channels are new in API 26 (and not a part of the
                // support library). There is no need to create a notification
                // channel on older versions of Android.
                return;
            }

            var channel = new NotificationChannel(CHANNEL_ID,
                                                  "SOSPectChannel",
                                                  NotificationImportance.High)
            {

                Description = "Firebase Cloud Messages appear in this channel"
            };

            var notificationManager = (NotificationManager)GetSystemService(Android.Content.Context.NotificationService);
            notificationManager.CreateNotificationChannel(channel);
        }

        protected override void OnStart()
        {
            base.OnStart();

            if ((int)Build.VERSION.SdkInt >= 23)
            {
                if (CheckSelfPermission(Manifest.Permission.AccessFineLocation) != Permission.Granted)
                {
                    RequestPermissions(LocationPermissions, RequestLocationId);
                }
                else
                {
                    // Permissions already granted - display a message.
                }
            }
        }

        public override void OnBackPressed()
        {
            Rg.Plugins.Popup.Popup.SendBackPressed(base.OnBackPressed);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            if (requestCode == RequestLocationId)
            {
                if ((grantResults.Length == 1) && (grantResults[0] == (int)Permission.Granted))
                {
                    // Permissions granted - display a message.
                }
                else
                {
                    // Permissions denied - display a message.
                }
            }
            else
            {
                Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
                base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            }
        }
    }

    [Activity(NoHistory = true, Exported = true, LaunchMode = LaunchMode.SingleTop)]
    [IntentFilter(new[] { Intent.ActionView }, Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable }, DataScheme = "sospect")]
    public class WebAuthenticationCallbackActivity : Xamarin.Essentials.WebAuthenticatorCallbackActivity
    {
    }
}
