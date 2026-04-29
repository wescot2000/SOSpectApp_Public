using Android.App;
using Android.Content;
using Android.Content.PM;

namespace sospect.Platforms.Android
{
    [Activity(NoHistory = true,
              LaunchMode = LaunchMode.SingleTop,
              Exported = true)]
    [IntentFilter(new[] { Intent.ActionView },
                  Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
                  DataScheme = "sospect")]
    public class WebAuthenticatorCallbackActivity : Microsoft.Maui.Authentication.WebAuthenticatorCallbackActivity
    {
        // Esta clase maneja los callbacks de autenticación OAuth
        // para Google, Apple, Facebook, etc.
    }
}