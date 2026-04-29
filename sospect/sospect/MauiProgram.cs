using Camera.MAUI;
using CommunityToolkit.Maui;
using Microcharts.Maui;
using Microsoft.Extensions.Logging;
using Plugin.AdMob;
#if ANDROID
using Plugin.FirebasePushNotifications;
#endif
using sospect.CustomRenderers;
using sospect.Handlers;
using sospect.Interfaces;
using sospect.Services;
using SospectIPopupService = sospect.Services.IPopupService;
using SospectPopupService = sospect.Services.PopupService;

#if ANDROID
using sospect.Platforms.Android.Handlers;
#endif

namespace sospect;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseMauiCommunityToolkitCamera()
            .UseMauiCameraView()
            .UseMauiMaps()
            // FIREBASE: Inicializar Firebase Push Notifications v4.x
            // TEMPORAL: Solo Android - iOS usa APNs directamente (bindings Firebase iOS incompatibles con .NET 10)
            #if ANDROID
            .UseFirebasePushNotifications(o =>
            {
                o.Android.DefaultNotificationImportance = global::Android.App.NotificationImportance.Max;
                o.Android.DefaultIconResource = sospect.Resource.Drawable.ic_notificacionespush;
                o.Android.DefaultColor = global::Android.Graphics.Color.ParseColor("#FF6F00");
                o.Android.NotificationChannels = new Plugin.FirebasePushNotifications.Platforms.Channels.NotificationChannelRequest[]
                {
                    new Plugin.FirebasePushNotifications.Platforms.Channels.NotificationChannelRequest
                    {
                        ChannelId = "FirebasePushNotificationChannel",
                        ChannelName = "General",
                        IsDefault = true,
                        Importance = global::Android.App.NotificationImportance.Max
                    }
                };
            })
            #endif
            // AdMob: Activo en Android e iOS.
            // En iOS el SDK nativo se inicializa desde AppDelegate (MobileAds.SharedInstance.Start).
            // NOTA: En iOS Debug se usa el Ad Unit ID de prueba de Google porque los IDs de
            // producción solo sirven ads cuando la app está publicada en la App Store.
            // Android siempre usa el ID de producción (funciona en Debug y Release).
            #if ANDROID || IOS
            .UseAdMob(
                androidDefaultBannerAdUnitId: "<ANDROID_BANNER_AD_UNIT_ID>",
#if IOS && DEBUG
                iosDefaultBannerAdUnitId: "ca-app-pub-3940256099942544/2934735716"
#else
                iosDefaultBannerAdUnitId: "<IOS_BANNER_AD_UNIT_ID>"
#endif
            )
            #endif
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("icomoon.ttf", "IcoMoonFamily");
            })
            .UseMicrocharts();

        // Autenticación
        builder.Services.AddSingleton<Microsoft.Maui.Authentication.IWebAuthenticator>
            (Microsoft.Maui.Authentication.WebAuthenticator.Default);

        // Servicios
        builder.Services.AddSingleton<SospectIPopupService, SospectPopupService>();

#if ANDROID
        builder.Services.AddSingleton<AdMobService>();
        builder.Services.AddSingleton<IErrorLogger, sospect.Platforms.Android.ErrorLoggerAndroid>();
        builder.Services.AddSingleton<INotification, sospect.Platforms.Android.Services.LocalNotificationService>();
        builder.Services.AddSingleton<IBackgroundService, sospect.Platforms.Android.Services.BackgroundServiceAndroid>();
#elif IOS
        builder.Services.AddSingleton<AdMobService>();
        builder.Services.AddSingleton<IBackgroundService, sospect.Platforms.iOS.Services.BackgroundServiceIOS>();
#endif

        // REGISTRAR TODOS LOS HANDLERS EN UN SOLO LUGAR
#if ANDROID
        builder.ConfigureMauiHandlers(handlers =>
        {
            // AdBanner handler
            handlers.AddHandler<sospect.CustomRenderers.AdBanner, sospect.Platforms.Android.Handlers.AdBannerHandler>();

            // CustomMap handler
            handlers.AddHandler<sospect.CustomRenderers.CustomMap, sospect.Platforms.Android.Handlers.CustomMapHandler>();

            // MiniMapa handler
            handlers.AddHandler<MiniMapa, MiniMapaHandler>();

            //  CustomImage handler - UNA SOLA VEZ con namespace completo
            handlers.AddHandler<Image, sospect.Platforms.Android.Handlers.CustomImageHandler>();
        });
#elif IOS
        builder.ConfigureMauiHandlers(handlers =>
        {
            // AdBanner handler para iOS
            handlers.AddHandler<sospect.CustomRenderers.AdBanner, sospect.Platforms.iOS.Handlers.AdBannerHandler>();

            // CustomMap handler para iOS (pines de usuario, alarmas y tap en mapa)
            handlers.AddHandler<sospect.CustomRenderers.CustomMap, sospect.Platforms.iOS.Handlers.CustomMapHandler>();

            // MiniMapa handler para iOS (pin personalizado en ZoneSubscriptionPage)
            handlers.AddHandler<MiniMapa, MiniMapaHandler>();
        });
#endif

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
