// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using Microsoft.Maui.Handlers;
using sospect.CustomRenderers;
using Google.MobileAds;
using UIKit;

namespace sospect.Platforms.iOS.Handlers
{
    public class AdBannerHandler : ViewHandler<AdBanner, BannerView>
    {
        public static IPropertyMapper<AdBanner, AdBannerHandler> Mapper =
            new PropertyMapper<AdBanner, AdBannerHandler>(ViewMapper);

        public AdBannerHandler() : base(Mapper)
        {
        }

        protected override BannerView CreatePlatformView()
        {
            var adView = new BannerView(AdSizeCons.LargeBanner)
            {
                AdUnitId = "<IOS_BANNER_AD_UNIT_ID>",
                RootViewController = GetVisibleViewController()
            };

#if DEBUG
            adView.AdReceived += (s, args) => {
                System.Diagnostics.Debug.WriteLine("Ad Received");
            };
            adView.ReceiveAdFailed += (s, args) => {
                System.Diagnostics.Debug.WriteLine($"Ad Failed: {args.Error.LocalizedDescription}");
            };
#endif

            adView.LoadRequest(Request.GetDefaultRequest());
            return adView;
        }

        private UIViewController GetVisibleViewController()
        {
            var windows = UIApplication.SharedApplication.Windows;
            foreach (var window in windows)
            {
                if (window.RootViewController != null)
                {
                    return window.RootViewController;
                }
            }
            return null;
        }

        protected override void ConnectHandler(BannerView platformView)
        {
            base.ConnectHandler(platformView);
        }

        protected override void DisconnectHandler(BannerView platformView)
        {
            base.DisconnectHandler(platformView);
        }
    }
}

