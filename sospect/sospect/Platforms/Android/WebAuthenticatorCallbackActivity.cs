// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

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

