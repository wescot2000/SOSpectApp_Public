using Firebase.Crashlytics;
using sospect.Interfaces;
using System;
using System.Collections.Generic;
using Android.Util;

namespace sospect.Platforms.Android
{
    public class ErrorLoggerAndroid : IErrorLogger
    {
        private readonly string TAG = "SOSpectErrorLogger";

        public void LogError(Exception ex, Dictionary<string, string> properties = null)
        {
            try
            {
                // Log local para debug
                System.Diagnostics.Debug.WriteLine($"[{TAG}] Error capturado: {ex.Message}");
                Log.Error(TAG, $"Error: {ex.Message}", ex);

                // Firebase Crashlytics
                var crashlytics = FirebaseCrashlytics.Instance;

                // Configurar usuario si está disponible
                if (sospect.App.persona != null)
                {
                    crashlytics.SetUserId(sospect.App.persona.user_id_thirdparty ?? "unknown");
                    crashlytics.SetCustomKey("user_platform", sospect.App.persona.Plataforma ?? "unknown");
                    crashlytics.SetCustomKey("user_country", sospect.App.persona.Pais ?? "unknown");
                    crashlytics.SetCustomKey("user_language", sospect.App.persona.Idioma ?? "unknown");
                }

                // Agregar propiedades personalizadas
                if (properties != null)
                {
                    foreach (var property in properties)
                    {
                        crashlytics.SetCustomKey(property.Key, property.Value ?? "null");
                        System.Diagnostics.Debug.WriteLine($"[{TAG}] Custom Key: {property.Key} = {property.Value}");
                    }
                }

                // Crear Java Throwable para Crashlytics
                var javaThrowable = new Java.Lang.Throwable($"{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");

                // Enviar excepción a Crashlytics
                crashlytics.RecordException(javaThrowable);

                // Log adicional
                crashlytics.Log($"SOSpect Error Logged: {ex.GetType().Name} - {ex.Message}");

                System.Diagnostics.Debug.WriteLine($"[{TAG}] Error enviado a Firebase Crashlytics correctamente");
            }
            catch (Exception logEx)
            {
                // Fallback - solo debug local si falla Crashlytics
                System.Diagnostics.Debug.WriteLine($"[{TAG}] ERROR EN LOGGING: {logEx.Message}");
                System.Diagnostics.Debug.WriteLine($"[{TAG}] Error original: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[{TAG}] StackTrace original: {ex.StackTrace}");

                Log.Error(TAG, $"Error en logging: {logEx.Message}", logEx);
                Log.Error(TAG, $"Error original: {ex.Message}", ex);
            }
        }
    }
}