using System;
using Android.App;
using Android.Content;
using Firebase.Messaging;
using sospect.Services;
using System.Linq;
using Android;
using Android.Graphics;
using Android.Media;
using Android.OS;
using Android.Util;
using WindowsAzure.Messaging;
using sospect.Interfaces;
using sospect.Droid.Services;
using AndroidX.Core.App;
using sospect.Views;
using Java.Util.Prefs;

[assembly: Xamarin.Forms.Dependency(typeof(PushNotificationFirebaseMessagingService))]
namespace sospect.Droid.Services
{
    [Service(Exported = false)]
    [IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
    public class PushNotificationFirebaseMessagingService : FirebaseMessagingService, INotification
    {
        IPushDemoNotificationActionService _notificationActionService;
        INotificationRegistrationService _notificationRegistrationService;
        IDeviceInstallationService _deviceInstallationService;
        int notificationId = 1;


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

        public override void OnNewToken(string token)
        {
            try
            {
                Log.Info("MyApp", "New FCM token retrieved: " + token);
                DeviceInstallationService.Token = token;
                App.TokenHubNotification = token;
                NotificationRegistrationService.RefreshRegistrationAsync()
                    .ContinueWith((task) => { if (task.IsFaulted) throw task.Exception; });
            }
            catch (Exception ex)
            {
                Log.Error("MyApp", "Error setting FCM token: " + ex.Message, ex);
            }
        }


        public override void OnMessageReceived(RemoteMessage message)
        {
            if (message.Data.TryGetValue("message", out var messageAction))
            {
                var data = message.Data;

                if (data != null)
                {
                    var alarma_id = data["alarma_id"];
                    Xamarin.Essentials.Preferences.Set("alarma_id", alarma_id);
                    SendNotification(messageAction, alarma_id);
                }
            }
        }

        public void SendNotification(string messageBody, string alarma_id)
        {
            var pendingIntentFlags = PendingIntentFlags.Immutable;

            var intent = new Intent(this, typeof(MainActivity));
            intent.AddFlags(ActivityFlags.ClearTop);
            intent.PutExtra("alarma_id", alarma_id);
            var pendingIntent = PendingIntent.GetActivity(this, 0, intent, pendingIntentFlags);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                // Instantiate the builder and set notification elements:
                Notification.Builder builder = new Notification.Builder(ApplicationContext, "SOSPectChannel")
                    .SetContentTitle("SOSpect")
                    .SetContentText(messageBody)
                    .SetStyle(new Notification.BigTextStyle().BigText(messageBody))
                    .SetAutoCancel(true)
                    .SetContentIntent(pendingIntent)
                    ;
                builder.SetSmallIcon(Resource.Drawable.ic_notificacionespush);
                builder.SetColor(unchecked((int)0xFFFFA500));

                builder.SetGroup("Notificaciones");
                builder.SetGroupSummary(true);
                // Build the notification:
                Notification notification = builder.Build();

                // Get the notification manager:
                NotificationManager notificationManager = NotificationManager.FromContext(this);

                // Publish the notification:


                var channelName = "Notificaciones";
                var channelDescription = "Notificaciones de Sospect";
                var channel = new NotificationChannel("SospectChannel", channelName, NotificationImportance.High)
                {
                    Description = channelDescription,
                    LightColor = Color.Blue
                };
                channel.SetShowBadge(true);
                notificationManager.CreateNotificationChannel(channel);
                notificationManager.Notify(notificationId, notification);
                notificationId++;
            }
            else
            {
                var notificationBuilder = new NotificationCompat.Builder(this, "SOSPectChannel")
                        .SetContentTitle("SOSpect")
                        .SetContentText(messageBody)
                        .SetPriority((int)NotificationPriority.Max)
                        .SetAutoCancel(true)
                        .SetVibrate(new long[] { 500, 0, 500 })
                        .SetContentIntent(pendingIntent)
                        .SetStyle(new NotificationCompat.BigTextStyle().BigText(messageBody))
                        .SetSound(RingtoneManager.GetDefaultUri(RingtoneType.Notification))
                        .SetGroup("Notificaciones");

                var notificationManager = NotificationManager.FromContext(this);


                if (Android.OS.Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
                {
                    notificationBuilder.SetSmallIcon(Resource.Drawable.ic_notificacionespush);
                    notificationBuilder.SetColor(unchecked((int)0xFFFFA500));
                }
                else
                {
                    notificationBuilder.SetSmallIcon(Resource.Drawable.ic_notificacionespush);
                }

                notificationManager.Notify(notificationId, notificationBuilder.Build());
                notificationId++;
            }

        }
    }
}

