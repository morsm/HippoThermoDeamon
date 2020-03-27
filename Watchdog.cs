using System;
using System.Threading;
using System.Threading.Tasks;

namespace Termors.Serivces.HippotronicsLedDaemon
{
    /// <summary>
    /// Kills the process in case communication with one of the LED devices hangs up the main loop
    /// </summary>
    public class Watchdog
    {
        private ushort _minutes = 2;

        protected Watchdog()
        {
        }

        public static Watchdog Dog = new Watchdog();

        public ushort WatchdogMinutes
        {
            get
            {
                return _minutes;
            }
            set
            {
            }
        }

        public DateTime LastWokenUp { get; protected set; } = DateTime.Now;

        public void Wake()
        {
            LastWokenUp = DateTime.Now;
        }

        public void ScheduleDog()
        {
            Task.Run(() => CheckDog());
        }

        protected void CheckDog()
        {
            TimeSpan elapsed = DateTime.Now.Subtract(LastWokenUp);

            if (elapsed.Minutes >= WatchdogMinutes)
            {
                ExitDog();
            }

            // Still ok. Wait another minute and schedule the next check
            Thread.Sleep(60000);
            ScheduleDog();
        }

        protected void ExitDog()
        {
            var message = "Watchdog triggered - exiting process";

            Logger.LogError(message);
            Environment.FailFast(message);
        }
    }
}
