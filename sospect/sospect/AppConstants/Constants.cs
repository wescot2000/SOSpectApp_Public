using System;

namespace sospect.AppConstants
{
    public class Constants
    {
        public static string AppName = "sospect";

        // OAuth
        // For Google login, configure at https://console.developers.google.com/
        public static string iOSClientId = "rewqrewqrewqrewq.apps.googleusercontent.com";
        public static string AndroidClientId = "rewqrewqrewqrewqrw.apps.googleusercontent.com";

        // These values do not need changing
        public static string Scope = "https://www.googleapis.com/auth/userinfo.email";
        public static string AuthorizeUrl = "https://accounts.google.com/o/oauth2/auth";
        public static string AccessTokenUrl = "https://www.googleapis.com/oauth2/v4/token";
        public static string UserInfoUrl = "https://www.googleapis.com/oauth2/v2/userinfo";

        // Set these to reversed iOS/Android client ids, with :/oauth2redirect appended
        public static string iOSRedirectUrl = "com.googleusercontent.apps.rewqrewqrewqrewqrw:/oauth2redirect";
        public static string AndroidRedirectUrl = "com.googleusercontent.apps.rewqrewqrewqrewqrew:/oauth2redirect";

        //project id for android gcm
        //Desarrollo
        public const string GoogleConsoleProjectId = "sospecttest-rewqrewq";
        //Produccion
        //public const string GoogleConsoleProjectId = "sospect";

        //Producción
        public const string ListenConnectionString = "Endpoint=sb://Sospect.servicebus.windows.net/;SharedAccessKeyName=DefaultListenSharedAccessSignature;SharedAccessKey=rewqrewqrewqr";
        public const string NotificationHubName = "SospectApp";
    }
}
