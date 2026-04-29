using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using sospect.Interfaces;
using Microsoft.Maui.ApplicationModel;
using AndroidApp = Android.App.Application;

namespace sospect.Platforms.Android.Services
{
    public class LocalNotificationService : INotification
    {
        private const string CHANNEL_ID = "FirebasePushNotificationChannel";
        private const int NOTIFICATION_ID_BASE = 1000;

        public void SendNotification(string title, string message, string alarmaId, string imageUrl = null, string logoUrl = null, string chatId = "0")
        {
            try
            {
                var context = Platform.CurrentActivity ?? AndroidApp.Context;

                // Crear intent para abrir la app cuando se toque la notificación
                var intent = new Intent(context, typeof(MainActivity));
                intent.SetFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);
                intent.PutExtra("alarma_id", alarmaId);
                // 2026-04-11: Pasar chat_id en el intent para que NotificationOpened pueda leerlo
                intent.PutExtra("chat_id", chatId);
                System.Diagnostics.Debug.WriteLine($"[LocalNotification] Intent: alarma_id={alarmaId}, chat_id={chatId}");

                var pendingIntent = PendingIntent.GetActivity(
                    context,
                    Convert.ToInt32(alarmaId),
                    intent,
                    PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable
                );

                // Crear notificación base
                var notificationBuilder = new NotificationCompat.Builder(context, CHANNEL_ID)
                    .SetContentTitle(title)
                    .SetContentText(message)
                    .SetSmallIcon(Resource.Drawable.ic_notificacionespush)
                    .SetColor(global::Android.Graphics.Color.ParseColor("#FF6F00"))
                    .SetPriority(NotificationCompat.PriorityMax)
                    .SetCategory(NotificationCompat.CategoryAlarm)
                    .SetAutoCancel(true)
                    .SetContentIntent(pendingIntent)
                    .SetDefaults(NotificationCompat.DefaultAll)
                    .SetVisibility(NotificationCompat.VisibilityPublic);

                // Descargar imágenes si están disponibles
                global::Android.Graphics.Bitmap promoBitmap = null;
                global::Android.Graphics.Bitmap logoBitmap = null;

                if (!string.IsNullOrEmpty(imageUrl))
                {
                    promoBitmap = DownloadImageFromUrl(imageUrl);
                }

                if (!string.IsNullOrEmpty(logoUrl))
                {
                    logoBitmap = DownloadImageFromUrl(logoUrl);
                }

                // CASO 1: Tenemos ambas imágenes (logo y promo)
                // → Logo cuando está colapsada, imagen promo cuando se expande
                if (promoBitmap != null && logoBitmap != null)
                {
                    try
                    {
                        var bigPictureStyle = new NotificationCompat.BigPictureStyle()
                            .BigPicture(promoBitmap) // Imagen grande cuando se expande
                            .BigLargeIcon(logoBitmap) // Mantener logo visible cuando se expande
                            .SetSummaryText(message);

                        notificationBuilder
                            .SetStyle(bigPictureStyle)
                            .SetLargeIcon(logoBitmap); // Logo como thumbnail cuando está colapsada

                        System.Diagnostics.Debug.WriteLine($"[LocalNotification] Dual-image: Logo={logoUrl}, Promo={imageUrl}");
                    }
                    catch (Exception imgEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[LocalNotification] Error configurando dual-image: {imgEx.Message}");
                        notificationBuilder.SetStyle(new NotificationCompat.BigTextStyle().BigText(message));
                    }
                }
                // CASO 2: Solo tenemos imagen promo (sin logo)
                // → Imagen promo como thumbnail y grande
                else if (promoBitmap != null)
                {
                    try
                    {
                        var bigPictureStyle = new NotificationCompat.BigPictureStyle()
                            .BigPicture(promoBitmap)
                            .BigLargeIcon((global::Android.Graphics.Bitmap)null) // Ocultar large icon cuando se expande
                            .SetSummaryText(message);

                        notificationBuilder
                            .SetStyle(bigPictureStyle)
                            .SetLargeIcon(promoBitmap); // Mostrar como thumbnail cuando está colapsada

                        System.Diagnostics.Debug.WriteLine($"[LocalNotification] Solo promo image: {imageUrl}");
                    }
                    catch (Exception imgEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[LocalNotification] Error cargando promo image: {imgEx.Message}");
                        notificationBuilder.SetStyle(new NotificationCompat.BigTextStyle().BigText(message));
                    }
                }
                // CASO 3: Solo tenemos logo (sin imagen promo)
                // → Logo como thumbnail, sin BigPictureStyle
                else if (logoBitmap != null)
                {
                    try
                    {
                        notificationBuilder
                            .SetLargeIcon(logoBitmap)
                            .SetStyle(new NotificationCompat.BigTextStyle().BigText(message));

                        System.Diagnostics.Debug.WriteLine($"[LocalNotification] Solo logo: {logoUrl}");
                    }
                    catch (Exception imgEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[LocalNotification] Error cargando logo: {imgEx.Message}");
                        notificationBuilder.SetStyle(new NotificationCompat.BigTextStyle().BigText(message));
                    }
                }
                // CASO 4: No hay imágenes
                // → Solo texto
                else
                {
                    notificationBuilder.SetStyle(new NotificationCompat.BigTextStyle().BigText(message));
                    System.Diagnostics.Debug.WriteLine($"[LocalNotification] Sin imágenes, usando BigTextStyle");
                }

                // Obtener NotificationManager
                var notificationManager = context.GetSystemService(Context.NotificationService) as NotificationManager;

                // Crear canal si es Android 8.0+
                if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                {
                    var channel = new NotificationChannel(
                        CHANNEL_ID,
                        "SOSpect Alerts",
                        NotificationImportance.High
                    )
                    {
                        Description = "Notificaciones de alarmas cercanas",
                        LockscreenVisibility = NotificationVisibility.Public
                    };
                    channel.EnableVibration(true);
                    channel.EnableLights(true);
                    channel.SetSound(
                        global::Android.Media.RingtoneManager.GetDefaultUri(global::Android.Media.RingtoneType.Notification),
                        new global::Android.Media.AudioAttributes.Builder()
                            .SetUsage(global::Android.Media.AudioUsageKind.Notification)
                            .SetContentType(global::Android.Media.AudioContentType.Sonification)
                            .Build()
                    );

                    notificationManager?.CreateNotificationChannel(channel);
                }

                // Mostrar notificación con ID único basado en alarma_id
                int notificationId = NOTIFICATION_ID_BASE + Convert.ToInt32(alarmaId);
                var notification = notificationBuilder.Build();
                notificationManager?.Notify(notificationId, notification);

                System.Diagnostics.Debug.WriteLine($"[LocalNotification] Notificación mostrada - ID: {notificationId}, Alarma: {alarmaId}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LocalNotification] Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[LocalNotification] StackTrace: {ex.StackTrace}");
            }
        }

        // Sobrecarga para compatibilidad con la interfaz existente
        public void SendNotification(string message, string alarmaId)
        {
            SendNotification("⚠️ SOSpect Alert", message, alarmaId, null, null);
        }

        /// <summary>
        /// Descarga una imagen desde URL y la convierte a Bitmap para usar como LargeIcon.
        /// FIX 2026-02-27: Usar Task.Run para descargar en thread pool y evitar deadlock
        /// cuando se llama desde el hilo de UI (MainThread.BeginInvokeOnMainThread).
        /// </summary>
        private global::Android.Graphics.Bitmap DownloadImageFromUrl(string url)
        {
            try
            {
                // Task.Run ejecuta en thread pool, GetAwaiter().GetResult() espera sin
                // capturar el SynchronizationContext del hilo de UI → evita deadlock.
                return Task.Run(async () =>
                {
                    using (var httpClient = new System.Net.Http.HttpClient())
                    {
                        httpClient.Timeout = TimeSpan.FromSeconds(10);
                        var imageBytes = await httpClient.GetByteArrayAsync(url).ConfigureAwait(false);
                        return global::Android.Graphics.BitmapFactory.DecodeByteArray(imageBytes, 0, imageBytes.Length);
                    }
                }).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LocalNotification] Error descargando imagen: {ex.Message}");
                return null;
            }
        }
    }
}
