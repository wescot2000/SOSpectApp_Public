using sospect.iOS;
using sospect;
using System;
using System.Threading.Tasks;
using UIKit;
using Xamarin.Forms;
using sospect.Interfaces;
using sospect.iOS.Services;
using sospect.Models;
using System.Collections.Generic;

[assembly: Dependency(typeof(BackgroundService))]
namespace sospect.iOS.Services
{
    public class BackgroundService : IBackgroundService
    {
        public async Task RunCodeInBackgroundMode(Func<Ubicaciones, Task<List<AlarmaCercana>>> action, string name = "BackgroundService")
        {
            nint taskId = 0;
            var taskEnded = false;
            taskId = UIApplication.SharedApplication.BeginBackgroundTask(name, () =>
            {
                //when time is up and task has not finished, call this method to finish the task to prevent the app from being terminated
                Console.WriteLine($"Background task '{name}' got killed");
                taskEnded = true;
                UIApplication.SharedApplication.EndBackgroundTask(taskId);
            });
            await Task.Factory.StartNew(async () =>
            {
                //here we run the actual task
                Console.WriteLine($"Background task '{name}' started");
                //await action();
                taskEnded = true;
                UIApplication.SharedApplication.EndBackgroundTask(taskId);
                Console.WriteLine($"Background task '{name}' finished");
            });

            await Task.Factory.StartNew(async () =>
            {
                //Just a method that logs how much time we have remaining. Usually a background task has around 180 seconds to complete. 
                while (!taskEnded)
                {
                    Console.WriteLine($"Background task '{name}' time remaining: {UIApplication.SharedApplication.BackgroundTimeRemaining}");
                    await Task.Delay(1000);
                }
            });
        }
    }
}

