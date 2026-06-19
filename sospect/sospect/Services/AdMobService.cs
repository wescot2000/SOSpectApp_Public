// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

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


