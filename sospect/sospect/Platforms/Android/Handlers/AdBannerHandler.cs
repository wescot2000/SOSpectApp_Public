using Microsoft.Maui.Handlers;
using sospect.CustomRenderers;
using Android.Gms.Ads;
using AndroidView = Android.Views.View;
using AndroidApp = Android.App.Application;

namespace sospect.Platforms.Android.Handlers
{
    public class AdBannerHandler : ViewHandler<AdBanner, AdView>
    {
        public static IPropertyMapper<AdBanner, AdBannerHandler> Mapper =
            new PropertyMapper<AdBanner, AdBannerHandler>(ViewMapper);

        public AdBannerHandler() : base(Mapper)
        {
        }

        protected override AdView CreatePlatformView()
        {
            var context = MauiContext?.Context ??
                         Microsoft.Maui.ApplicationModel.Platform.CurrentActivity ??
                         AndroidApp.Context;

            if (context == null)
            {
                throw new InvalidOperationException("No se pudo obtener el contexto de Android");
            }

            // Crear AdView real
            var adView = new AdView(context)
            {
                AdSize = AdSize.LargeBanner,
                AdUnitId = "<ANDROID_BANNER_AD_UNIT_ID>"
            };

            // Cargar el anuncio
            var adRequest = new AdRequest.Builder().Build();
            adView.LoadAd(adRequest);

            return adView;
        }

        protected override void ConnectHandler(AdView platformView)
        {
            base.ConnectHandler(platformView);
        }

        protected override void DisconnectHandler(AdView platformView)
        {
            platformView?.Destroy();
            base.DisconnectHandler(platformView);
        }
    }
}