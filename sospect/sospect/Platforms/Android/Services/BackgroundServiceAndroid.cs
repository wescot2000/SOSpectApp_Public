// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using Android.Content;
using Android.OS;
using sospect.Interfaces;
using sospect.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace sospect.Platforms.Android.Services
{
    public class BackgroundServiceAndroid : IBackgroundService
    {
        public Task RunCodeInBackgroundMode(
            Func<Ubicaciones, Task<List<AlarmaCercana>>> action,
            string name = "BackgroundService")
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("========================================");
                System.Diagnostics.Debug.WriteLine($"[BackgroundServiceAndroid] RunCodeInBackgroundMode LLAMADO: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                System.Diagnostics.Debug.WriteLine($"[BackgroundServiceAndroid] Name: {name}");

                var context = global::Android.App.Application.Context;

                var intent = new Intent(context, typeof(LocationForegroundService));

                // Android 8.0+ requiere StartForegroundService
                if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                {
                    System.Diagnostics.Debug.WriteLine($"[BackgroundServiceAndroid] Android {Build.VERSION.SdkInt} - Usando StartForegroundService");
                    context.StartForegroundService(intent);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[BackgroundServiceAndroid] Android {Build.VERSION.SdkInt} - Usando StartService");
                    context.StartService(intent);
                }

                System.Diagnostics.Debug.WriteLine("[BackgroundServiceAndroid] ✓ Servicio iniciado correctamente");
                System.Diagnostics.Debug.WriteLine("========================================");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("========================================");
                System.Diagnostics.Debug.WriteLine("[BackgroundServiceAndroid] ✗ ERROR iniciando servicio");
                System.Diagnostics.Debug.WriteLine($"[BackgroundServiceAndroid] Message: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[BackgroundServiceAndroid] StackTrace: {ex.StackTrace}");
                System.Diagnostics.Debug.WriteLine("========================================");
            }

            return Task.CompletedTask;
        }

        public Task StopBackgroundService()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("========================================");
                System.Diagnostics.Debug.WriteLine($"[BackgroundServiceAndroid] StopBackgroundService LLAMADO: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

                var context = global::Android.App.Application.Context;
                var intent = new Intent(context, typeof(LocationForegroundService));
                context.StopService(intent);

                System.Diagnostics.Debug.WriteLine("[BackgroundServiceAndroid] ✓ Servicio detenido correctamente");
                System.Diagnostics.Debug.WriteLine("========================================");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("========================================");
                System.Diagnostics.Debug.WriteLine("[BackgroundServiceAndroid] ✗ ERROR al detener servicio");
                System.Diagnostics.Debug.WriteLine($"[BackgroundServiceAndroid] Message: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[BackgroundServiceAndroid] StackTrace: {ex.StackTrace}");
                System.Diagnostics.Debug.WriteLine("========================================");
            }

            return Task.CompletedTask;
        }
    }
}


