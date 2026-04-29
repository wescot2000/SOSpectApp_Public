using Android.App;
using Android.OS;
using Android.Runtime;
using Firebase;
using Firebase.Crashlytics;
using Android.Util;
using sospect.Helpers;

namespace sospect.Platforms.Android;

[Application]
public class MainApplication : MauiApplication
{
    private readonly string TAG = "SOSpectApp";

    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    public override void OnCreate()
    {
        try
        {
            Log.Info(TAG, "=== MainApplication.OnCreate START ===");

            base.OnCreate();

            Log.Info(TAG, " base.OnCreate completado");

            //  PASO 1: Inicializar Firebase APP
            try
            {
                var firebaseApp = FirebaseApp.InitializeApp(this);
                if (firebaseApp != null)
                {
                    Log.Info(TAG, $" Firebase inicializado: {firebaseApp.Name}");
                }
                else
                {
                    Log.Warn(TAG, " FirebaseApp.InitializeApp retornó null");
                }
            }
            catch (System.Exception ex)
            {
                Log.Error(TAG, $" Error inicializando Firebase: {ex.Message}", ex);
                CrashlyticsHelper.LogError(ex, "MainApplication", "OnCreate-Firebase");
                // NO lanzar excepción - intentar continuar
            }

            //  PASO 2: Inicializar Crashlytics
            try
            {
                var crashlytics = FirebaseCrashlytics.Instance;
                crashlytics.SetCrashlyticsCollectionEnabled(Java.Lang.Boolean.True);
                crashlytics.Log("SOSpect App iniciada - MainApplication.OnCreate");
                Log.Info(TAG, " Firebase Crashlytics inicializado");
            }
            catch (System.Exception ex)
            {
                Log.Error(TAG, $" Error inicializando Crashlytics: {ex.Message}", ex);
                CrashlyticsHelper.LogError(ex, "MainApplication", "OnCreate-Crashlytics");
            }

            //  PASO 3: Push Notifications
            // Plugin.FirebasePushNotifications v4.x se configura en MauiProgram.cs
            // via .UseFirebasePushNotifications() - ya no requiere FirebasePushNotificationManager
            Log.Info(TAG, " Firebase Push Notifications v4.x se inicializa via MauiProgram");

            Log.Info(TAG, "=== MainApplication.OnCreate END ===");
        }
        catch (System.Exception ex)
        {
            Log.Error(TAG, $" ERROR CRÍTICO en MainApplication.OnCreate: {ex.Message}", ex);
            System.Diagnostics.Debug.WriteLine($" CRASH EN MAINAPPLICATION: {ex}");

            //  Intentar log a Crashlytics si está disponible
            try
            {
                var crashlytics = FirebaseCrashlytics.Instance;
                var javaThrowable = new Java.Lang.Throwable($"MainApplication.OnCreate: {ex.Message}\n{ex.StackTrace}");
                crashlytics.RecordException(javaThrowable);
            }
            catch { }

            throw; // Re-lanzar para que Android lo maneje
        }
    }
}