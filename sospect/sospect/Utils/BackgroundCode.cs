using System;
using System.Diagnostics;
using System.Timers;
using Xamarin.Forms;

namespace sospect.Utils
{
    public static class BackgroundCode
    {
        public static bool StopRunning { get; set; } = false;

        public static Timer iobj_timer;

        public static bool GetBackgroundData(int pi_SecondsLoop)
        {
            bool lb_ReturnValue = true;


            // Start a timer that runs after 30 seconds.
            Device.StartTimer(TimeSpan.FromSeconds(pi_SecondsLoop), () =>
            {
                // Do the actual request and wait for it to finish.
                //App.GlobalBackgroundData.MyBackgroundData = "Current Date / Time: " + DateTime.Now.ToString();

                //// Don't repeat the timer (we will start a new timer when the request is finished)
                //if (StopRunning)
                //{
                //    lb_ReturnValue = false;
                //    Debug.WriteLine("Stopping Background Process: " + App.GlobalBackgroundData.MyBackgroundData);
                //}
                //else
                //{
                //    Debug.WriteLine("Background Code Processed: " + App.GlobalBackgroundData.MyBackgroundData);
                //}

                return lb_ReturnValue;
            });

            return lb_ReturnValue;
        }
    }
}

