// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
using System.Diagnostics;
using System.Timers;
using Microsoft.Maui.Dispatching;

namespace sospect.Utils
{
    public static class BackgroundCode
    {
        public static bool StopRunning { get; set; } = false;

        public static System.Timers.Timer iobj_timer;

        public static bool GetBackgroundData(int pi_SecondsLoop)
        {
            bool lb_ReturnValue = true;

            // Start a timer that runs after the specified seconds.
            Application.Current?.Dispatcher?.StartTimer(TimeSpan.FromSeconds(pi_SecondsLoop), () =>
            {
                // Example background process logic
                // Replace or add your own background data retrieval logic
                if (StopRunning)
                {
                    lb_ReturnValue = false;
                    Debug.WriteLine("Stopping Background Process.");
                }
                else
                {
                    Debug.WriteLine("Background Code Processed at: " + DateTime.Now);
                }

                // Continue or stop the timer
                return lb_ReturnValue;
            });

            return lb_ReturnValue;
        }
    }
}


