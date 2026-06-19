// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
using System.Collections.Generic;
using Foundation;
using sospect.Interfaces;
// CORRECTO: El namespace específico para iOS es diferente
//using Firebase.Crashlytics;

namespace sospect.Platforms.iOS
{
    public class ErrorLoggerIOS : IErrorLogger
    {
        private const string TAG = "SOSpectErrorLogger";

        public void LogError(Exception ex, Dictionary<string, string>? properties = null)
        {
            try
            {
                // Debug local
                System.Diagnostics.Debug.WriteLine($"[{TAG}] iOS Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[{TAG}] StackTrace: {ex.StackTrace}");

                // VERIFICAR: Si Crashlytics no está disponible, usar solo debug
                try
                {
                    //var crashlytics = Crashlytics.SharedInstance;

                    // Configurar información del usuario
                    if (sospect.App.persona != null)
                    {
                        //crashlytics.SetUserId(sospect.App.persona.user_id_thirdparty ?? "unknown");
                        //crashlytics.SetCustomValue(new NSString(sospect.App.persona.Plataforma ?? "unknown"), "user_platform");
                        //crashlytics.SetCustomValue(new NSString(sospect.App.persona.Pais ?? "unknown"), "user_country");
                        // crashlytics.SetCustomValue(new NSString(sospect.App.persona.Idioma ?? "unknown"), "user_language");
                    }

                    // Agregar propiedades personalizadas
                    if (properties != null)
                    {
                        foreach (var kv in properties)
                        {
                            //crashlytics.SetCustomValue(new NSString(kv.Value ?? "null"), kv.Key);
                            System.Diagnostics.Debug.WriteLine($"[{TAG}] Custom Key: {kv.Key} = {kv.Value}");
                        }
                    }

                    // Crear NSError para Crashlytics
                    var userInfo = new NSMutableDictionary
                    {
                        { new NSString("exception_type"), new NSString(ex.GetType().Name) },
                        { new NSString("message"), new NSString(ex.Message) },
                        { new NSString("stackTrace"), new NSString(ex.StackTrace ?? "No stack trace") },
                        { new NSString("source"), new NSString("SOSpect_MAUI") }
                    };

                    var nsError = new NSError(new NSString("SOSpectError"), 500, userInfo);

                    // Enviar error a Crashlytics
                    //crashlytics.RecordError(nsError);
                    //crashlytics.Log($"SOSpect iOS Error: {ex.GetType().Name} - {ex.Message}");

                    System.Diagnostics.Debug.WriteLine($"[{TAG}] Error enviado a Firebase Crashlytics correctamente");
                }
                catch (Exception crashlyticsEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[{TAG}] Crashlytics no disponible en iOS: {crashlyticsEx.Message}");
                    // Continúa con solo debug logging
                }
            }
            catch (Exception logEx)
            {
                // Fallback - solo debug local si falla todo
                System.Diagnostics.Debug.WriteLine($"[{TAG}] ERROR EN LOGGING iOS: {logEx.Message}");
                System.Diagnostics.Debug.WriteLine($"[{TAG}] Error original: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[{TAG}] StackTrace original: {ex.StackTrace}");
            }
        }
    }
}


