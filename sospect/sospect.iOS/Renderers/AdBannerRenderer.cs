using System;
using Google.MobileAds;
using sospect.CustomRenderers;
using sospect.iOS.Renderers;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(AdBanner), typeof(AdBannerRenderer))]
namespace sospect.iOS.Renderers
{
    public class AdBannerRenderer : ViewRenderer<AdBanner, BannerView>
    {
        protected override void OnElementChanged(ElementChangedEventArgs<AdBanner> e)
        {
            base.OnElementChanged(e);

            if (Control == null)
            {
                var adView = new BannerView(AdSizeCons.SmartBannerPortrait)
                {
                    //AdUnitId = "ca-app-pub-ytrytre", //anuncio de prueba para verificar buen funcionamiento de Admob, ya verificado
                    AdUnitId = "ca-app-pub-ytreytre",
                    RootViewController = GetVisibleViewController()
                };

            #if DEBUG
                adView.AdReceived += (s, args) => {
                    Console.WriteLine("Ad Received");
                };

                adView.ReceiveAdFailed += (s, args) => {
                    Console.WriteLine($"Ad Failed: {args.Error.LocalizedDescription}");
                };
            #endif


                adView.LoadRequest(Request.GetDefaultRequest());

                SetNativeControl(adView);
            }
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
    }
}