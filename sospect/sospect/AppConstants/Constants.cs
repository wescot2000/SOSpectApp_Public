// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;

namespace sospect.AppConstants
{
    public class Constants
    {
        public static string AppName = "sospect";

        // OAuth
        // For Google login, configure at https://console.developers.google.com/
        public static string iOSClientId = "<IOS_CLIENT_ID>.apps.googleusercontent.com";
        public static string AndroidClientId = "<ANDROID_CLIENT_ID>.apps.googleusercontent.com";

        // These values do not need changing
        public static string Scope = "https://www.googleapis.com/auth/userinfo.email";
        public static string AuthorizeUrl = "https://accounts.google.com/o/oauth2/auth";
        public static string AccessTokenUrl = "https://www.googleapis.com/oauth2/v4/token";
        public static string UserInfoUrl = "https://www.googleapis.com/oauth2/v2/userinfo";

        // Set these to reversed iOS/Android client ids, with :/oauth2redirect appended
        public static string iOSRedirectUrl = "com.googleusercontent.apps.<IOS_CLIENT_ID>:/oauth2redirect";
        public static string AndroidRedirectUrl = "com.googleusercontent.apps.<ANDROID_CLIENT_ID>:/oauth2redirect";

        //project id for android gcm
        public const string GoogleConsoleProjectId = "<GOOGLE_PROJECT_ID>";

        //Notification Hub - configure via Azure Service Bus
        public const string ListenConnectionString = "Endpoint=sb://<NAMESPACE>.servicebus.windows.net/;SharedAccessKeyName=<KEY_NAME>;SharedAccessKey=<KEY>";
        public const string NotificationHubName = "<HUB_NAME>";
    }
}


