using Android.Content;
using Android.Gms.Ads;
using sospect.CustomRenderers;
using sospect.Droid.Renderers;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(AdBanner), typeof(AdBannerRenderer))]
namespace sospect.Droid.Renderers
{
    public class AdBannerRenderer : ViewRenderer<AdBanner, AdView>
    {
        public AdBannerRenderer(Context context) : base(context) { }

        protected override void OnElementChanged(ElementChangedEventArgs<AdBanner> e)
        {
            base.OnElementChanged(e);

            if (Control == null)
            {
                var adView = new AdView(Context)
                {
                    AdSize = AdSize.SmartBanner,
                    AdUnitId = "ca-app-pub-fdsafdsafds"
                };

                adView.LoadAd(new AdRequest.Builder().Build());

                SetNativeControl(adView);
            }
        }
    }
}