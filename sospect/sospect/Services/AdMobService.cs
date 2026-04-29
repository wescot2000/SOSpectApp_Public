using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sospect.Services
{
    public class AdMobService
    {
        public void Initialize()
        {
#if ANDROID
            Android.Gms.Ads.MobileAds.Initialize(Platform.CurrentActivity ?? Android.App.Application.Context);
#elif IOS
        Google.MobileAds.MobileAds.SharedInstance.Start(null);
#endif
        }
    }
}
