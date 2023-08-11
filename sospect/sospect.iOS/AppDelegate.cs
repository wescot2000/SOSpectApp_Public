using System;
using System.Collections.Generic;
using System.Linq;
using AudioToolbox;
using Foundation;
using sospect.AppConstants;
using sospect.Utils;
using UIKit;
using UserNotifications;
using WindowsAzure.Messaging;
using System.Diagnostics;
using System.Threading.Tasks;
using sospect.iOS.Extensions;
using sospect.iOS.Services;
using sospect.Services;
using Xamarin.Essentials;
using System.Globalization;
using sospect.Interfaces;
using Xamarin.Forms;
using Xamarin.Essentials;
using ObjCRuntime;
using CoreData;
using sospect.Views;
using sospect.iOS.Delegates;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;

namespace sospect.iOS
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the 
    // User Interface of the application, as well as listening (and optionally responding) to 
    // application events from iOS.
    [Register("AppDelegate")]
    public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
    {
        IPushDemoNotificationActionService _notificationActionService;
        INotificationRegistrationService _notificationRegistrationService;
        IDeviceInstallationService _deviceInstallationService;

        IPushDemoNotificationActionService NotificationActionService
            => _notificationActionService ??
                (_notificationActionService =
                ServiceContainer.Resolve<IPushDemoNotificationActionService>());

        INotificationRegistrationService NotificationRegistrationService
            => _notificationRegistrationService ??
                (_notificationRegistrationService =
                ServiceContainer.Resolve<INotificationRegistrationService>());

        IDeviceInstallationService DeviceInstallationService
            => _deviceInstallationService ??
                (_deviceInstallationService =
                ServiceContainer.Resolve<IDeviceInstallationService>());

        //
        // This method is invoked when the application has loaded and is ready to run. In this 
        // method you should instantiate the window, load the UI into it and then make the window
        // visible.
        //
        // You have 17 seconds to return from this method, or iOS will terminate your application.
        //

        private SBNotificationHub Hub { get; set; }

        public override void OnActivated(UIApplication uiApplication)
        {
            base.OnActivated(uiApplication);

            // Clear the badge
            UIApplication.SharedApplication.ApplicationIconBadgeNumber = 0;
        }


        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            Xamarin.Forms.DependencyService.Register<ISettingsService, SettingsService>();
            global::Xamarin.Forms.Forms.Init();
            UNUserNotificationCenter.Current.Delegate = new UserNotificationCenterDelegate();
            Bootstrap.Begin(() => new DeviceInstallationService());
            DependencyService.Register<IBackgroundService, BackgroundService>();
            AppCenter.Start("trewtrewtrewtrewtrewt",
                   typeof(Analytics), typeof(Crashes));

            if (DeviceInstallationService.NotificationsSupported)
            {
                UNUserNotificationCenter.Current.RequestAuthorization(
                        UNAuthorizationOptions.Alert |
                        UNAuthorizationOptions.Badge |
                        UNAuthorizationOptions.Sound,
                        (approvalGranted, error) =>
                        {
                            if (approvalGranted && error == null)
                                RegisterForRemoteNotifications();
                        });
               
            }

            Xamarin.FormsMaps.Init();
            Rg.Plugins.Popup.Popup.Init();
            LoadApplication(new App());

            using (var userInfo = options?.ObjectForKey(
            UIApplication.LaunchOptionsRemoteNotificationKey) as NSDictionary)
                ProcessNotificationActions(userInfo);

            return base.FinishedLaunching(app, options);
        }

        public static void RegisterForRemoteNotifications()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var pushSettings = UIUserNotificationSettings.GetSettingsForTypes(
                    UIUserNotificationType.Alert |
                    UIUserNotificationType.Badge |
                    UIUserNotificationType.Sound,
                    new NSSet());

                UIApplication.SharedApplication.RegisterUserNotificationSettings(pushSettings);
                UIApplication.SharedApplication.RegisterForRemoteNotifications();
            });
        }

        //public override void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
        //{

        //    Hub = new SBNotificationHub(Constants.ListenConnectionString, Constants.NotificationHubName);

        //    NSSet tags = null;
        //    if (!string.IsNullOrEmpty(App.persona.user_id_thirdparty))
        //    {
        //        tags = new NSSet(App.persona.user_id_thirdparty);
        //        Hub.RegisterNativeAsync(deviceToken, tags);
        //    }
        //}

        //public override void DidRegisterUserNotificationSettings(UIApplication application, UIUserNotificationSettings notificationSettings)
        //{
        //    //base.DidRegisterUserNotificationSettings(application, notificationSettings);
        //}

        //Método para recibir mensajes de notificaciones push
        public override void ReceivedRemoteNotification(UIApplication application, NSDictionary userInfo)
        {
            ProcessNotification(userInfo, false);
        }

        public override void DidReceiveRemoteNotification(UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
        {
            ProcessNotification(userInfo, false);
            completionHandler(UIBackgroundFetchResult.NewData);
        }

        void ProcessNotification(NSDictionary options, bool fromFinishedLaunching)
        {
            // Check to see if the dictionary has the aps key.  This is the notification payload you would have sent
            if (null != options && (options.ContainsKey(new NSString("message")) || options.ContainsKey(new NSString("alarma_id"))))
            {
                //Get the aps dictionary

                //NSDictionary message = options.ObjectForKey(new NSString("message")) as NSDictionary;
                var message = options.ObjectForKey(new NSString("message")) as NSString;
                var alarma_id = options.ObjectForKey(new NSString("alarma_id")) as NSString;

                Preferences.Set("alarma_id", alarma_id.Description);
                //NSMutableDictionary aps = options.ObjectForKey(new NSString("aps")) as NSMutableDictionary;

                //If this came from the ReceivedRemoteNotification while the app was running,
                // we of course need to manually process things like the sound, badge, and alert.
                if (!fromFinishedLaunching && message != null)
                {
                    UIAlertView avAlert = new UIAlertView("Notificación", message.Description, null, "OK", null);
                    avAlert.Show();

                    string NotificationSoundPath = @"/System/Library/Audio/UISounds/sms-received1.caf";

                    SystemSound notificationSound = SystemSound.FromFile(NotificationSoundPath);
                    notificationSound.AddSystemSoundCompletion(SystemSound.Vibrate.PlaySystemSound);
                    notificationSound.PlaySystemSound();             
                }

                if (string.IsNullOrEmpty(alarma_id.Description))
                {
                    App.Current.MainPage.Navigation.PushAsync(new DescribirPage(long.Parse(alarma_id)));
                }
            }
        }

        public void RefrescarTokenPushNotification()
        {
            // register for remote notifications based on system version
            if (UIDevice.CurrentDevice.CheckSystemVersion(10, 0))
            {
                UNUserNotificationCenter.Current.RequestAuthorization(UNAuthorizationOptions.Alert |
                    UNAuthorizationOptions.Sound |
                    UNAuthorizationOptions.Sound,
                    (granted, error) =>
                    {
                        if (granted)
                            InvokeOnMainThread(UIApplication.SharedApplication.RegisterForRemoteNotifications);
                    });
            }
            else if (UIDevice.CurrentDevice.CheckSystemVersion(8, 0))
            {
                var pushSettings = UIUserNotificationSettings.GetSettingsForTypes(
                UIUserNotificationType.Alert | UIUserNotificationType.Badge | UIUserNotificationType.Sound,
                new NSSet());

                UIApplication.SharedApplication.RegisterUserNotificationSettings(pushSettings);
                UIApplication.SharedApplication.RegisterForRemoteNotifications();
            }
            else
            {
                UIRemoteNotificationType notificationTypes = UIRemoteNotificationType.Alert | UIRemoteNotificationType.Badge | UIRemoteNotificationType.Sound;
                UIApplication.SharedApplication.RegisterForRemoteNotificationTypes(notificationTypes);
            }
        }

        public override void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error)
        {
            UIAlertView avAlert = new UIAlertView("Notification", error.Description, null, "OK", null);
            avAlert.Show();
        }

        public override bool OpenUrl(UIApplication app, NSUrl url, NSDictionary options)
        {
            return Xamarin.Essentials.Platform.OpenUrl(app, url, options);
        }

        public override bool ContinueUserActivity(UIApplication application, NSUserActivity userActivity, UIApplicationRestorationHandler completionHandler)
        {
            if (Xamarin.Essentials.Platform.ContinueUserActivity(application, userActivity, completionHandler))
                return true;
            return base.ContinueUserActivity(application, userActivity, completionHandler);
        }

        Task CompleteRegistrationAsync(NSData deviceToken)
        {
            DeviceInstallationService.Token = deviceToken.ToHexString();
            return NotificationRegistrationService.RefreshRegistrationAsync();
        }

        void ProcessNotificationActions(NSDictionary userInfo)
        {
            if (userInfo == null)
                return;

            try
            {
                var actionValue = userInfo.ObjectForKey(new NSString("action")) as NSString;

                if (!string.IsNullOrWhiteSpace(actionValue?.Description))
                    NotificationActionService.TriggerAction(actionValue.Description);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public override void RegisteredForRemoteNotifications(
            UIApplication application,
            NSData deviceToken)
            => CompleteRegistrationAsync(deviceToken).ContinueWith((task)
                =>
            { if (task.IsFaulted) throw task.Exception; });

        //public override void ReceivedRemoteNotification(
        //    UIApplication application,
        //    NSDictionary userInfo)
        //    => ProcessNotificationActions(userInfo);

        //public override void FailedToRegisterForRemoteNotifications(
        //    UIApplication application,
        //    NSError error)
        //    => Debug.WriteLine(error.Description);
    }
}
