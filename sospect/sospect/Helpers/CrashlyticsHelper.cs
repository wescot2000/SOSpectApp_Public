using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using sospect.Interfaces;

namespace sospect.Helpers
{
    /// <summary>
    /// Helper class para el manejo consistente de errores con Firebase Crashlytics
    /// </summary>
    public static class CrashlyticsHelper
    {
        // ITER3: Contador para limitar RecordException (cuota Firebase: 8 non-fatal/sesión)
        private static int _diagCallCount = 0;
        private const int MAX_DIAG_EXCEPTIONS = 3;
        /// <summary>
        /// Registra un error en Firebase Crashlytics con propiedades consistentes
        /// </summary>
        /// <param name="ex">La excepci�n a registrar</param>
        /// <param name="className">Nombre de la clase donde ocurri� el error</param>
        /// <param name="methodName">Nombre del m�todo donde ocurri� el error</param>
        /// <param name="additionalProperties">Propiedades adicionales opcionales</param>
        public static void LogError(Exception ex, string className, string methodName, Dictionary<string, string> additionalProperties = null)
        {
            try
            {
                // Validaci�n de par�metros
                if (ex == null || string.IsNullOrWhiteSpace(className) || string.IsNullOrWhiteSpace(methodName))
                {
                    System.Diagnostics.Debug.WriteLine("CrashlyticsHelper: Par�metros inv�lidos para LogError");
                    return;
                }

                var properties = new Dictionary<string, string>
                {
                    { "Object", className },
                    { "Method", methodName },
                    { "Timestamp", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC") }
                };

                // Agregar propiedades adicionales si existen
                if (additionalProperties != null)
                {
                    foreach (var prop in additionalProperties)
                    {
                        if (!properties.ContainsKey(prop.Key))
                            properties[prop.Key] = prop.Value;
                    }
                }

                var errorLogger = Microsoft.Maui.IPlatformApplication.Current?.Services?.GetService<IErrorLogger>();
                if (errorLogger == null)
                {
                    System.Diagnostics.Debug.WriteLine("CrashlyticsHelper: ErrorLogger service no disponible");
                    return;
                }

                errorLogger.LogError(ex, properties);
            }
            catch (Exception logEx)
            {
                System.Diagnostics.Debug.WriteLine($"CrashlyticsHelper: Error al loggear a Crashlytics: {logEx.Message}");
            }
        }

        /// <summary>
        /// Registra un error en Firebase Crashlytics con mensaje personalizado
        /// </summary>
        /// <param name="ex">La excepci�n a registrar</param>
        /// <param name="className">Nombre de la clase donde ocurri� el error</param>
        /// <param name="methodName">Nombre del m�todo donde ocurri� el error</param>
        /// <param name="customMessage">Mensaje personalizado para el error</param>
        public static void LogError(Exception ex, string className, string methodName, string customMessage)
        {
            if (string.IsNullOrWhiteSpace(customMessage))
            {
                LogError(ex, className, methodName);
                return;
            }

            var additionalProperties = new Dictionary<string, string>
            {
                { "CustomMessage", customMessage }
            };

            LogError(ex, className, methodName, additionalProperties);
        }

        /// <summary>
        /// Registra un error personalizado con informaci�n m�nima
        /// </summary>
        /// <param name="message">Mensaje del error</param>
        /// <param name="className">Nombre de la clase</param>
        /// <param name="methodName">Nombre del m�todo</param>
        public static void LogCustomError(string message, string className, string methodName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(message))
                {
                    System.Diagnostics.Debug.WriteLine("CrashlyticsHelper: Mensaje vac�o para LogCustomError");
                    return;
                }

                var customException = new Exception(message);
                LogError(customException, className, methodName);
            }
            catch (Exception logEx)
            {
                System.Diagnostics.Debug.WriteLine($"CrashlyticsHelper: Error al loggear error personalizado a Crashlytics: {logEx.Message}");
            }
        }

        /// <summary>
        /// TEMPORAL: Registra un mensaje de diagnostico GPS en Crashlytics como excepcion no-fatal.
        /// Permite depurar problemas de ubicacion en campo sin Visual Studio conectado.
        /// Remover una vez confirmado que el fix de GPS funciona correctamente.
        /// </summary>
        /// <param name="tag">Etiqueta corta del punto de diagnostico</param>
        /// <param name="message">Mensaje descriptivo</param>
        /// <param name="data">Datos adicionales opcionales (lat, lng, fuente, etc.)</param>
        public static void LogDiagnostico(string tag, string message, Dictionary<string, string> data = null)
        {
            try
            {
                _diagCallCount++;

#if ANDROID
                // ITER3+4: Log ilimitado via FirebaseCrashlytics.Log() — envuelto en try/catch
                // porque Firebase puede no estar inicializado en ciertos contextos (timer, threads)
                try
                {
                    Firebase.Crashlytics.FirebaseCrashlytics.Instance.Log($"[DIAG-{_diagCallCount}] {tag}: {message}");
                    if (data != null)
                    {
                        foreach (var kvp in data)
                            Firebase.Crashlytics.FirebaseCrashlytics.Instance.SetCustomKey($"D_{kvp.Key}", kvp.Value);
                    }
                }
                catch (Exception firebaseEx)
                {
                    System.Diagnostics.Debug.WriteLine($"CrashlyticsHelper: Firebase.Log falló: {firebaseEx.Message}");
                }
#endif

                // RecordException solo para las primeras N llamadas (cuota limitada a 8 non-fatal/sesión)
                if (_diagCallCount <= MAX_DIAG_EXCEPTIONS)
                {
                    var properties = new Dictionary<string, string>
                    {
                        { "DiagTag", tag },
                        { "DiagMessage", message },
                        { "Timestamp", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC") }
                    };

                    if (data != null)
                    {
                        foreach (var prop in data)
                        {
                            if (!properties.ContainsKey(prop.Key))
                                properties[prop.Key] = prop.Value;
                        }
                    }

                    var diagException = new Exception($"[DIAG-GPS] {tag}: {message}");
                    var errorLogger = Microsoft.Maui.IPlatformApplication.Current?.Services?.GetService<IErrorLogger>();
                    errorLogger?.LogError(diagException, properties);
                }

                System.Diagnostics.Debug.WriteLine($"[DIAG-GPS] {tag}: {message}");
            }
            catch (Exception logEx)
            {
                System.Diagnostics.Debug.WriteLine($"CrashlyticsHelper: Error en LogDiagnostico: {logEx.Message}");
            }
        }
    }
}