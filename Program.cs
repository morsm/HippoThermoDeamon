using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Web.Http;
using System.Net.Http.Formatting;

using Microsoft.Owin.Hosting;
using Owin;


namespace Termors.Serivces.HippotronicsThermoDaemon
{
    class Daemon
    {
        private ManualResetEvent _endEvent = new ManualResetEvent(false);

        public static async Task Main(string[] args)
        {
            await new Daemon().Run(args);
        }

        public async Task Run(string[] args)
        { 
            Logger.Log("HippotronicsThermoDaemon started");

            // Set up REST services in OWIN web server
            var webapp = WebApp.Start("http://*:9002/", new Action<IAppBuilder>(Configuration));

            Console.CancelKeyPress += (sender, e) =>
            {
                Logger.Log("HippotronicsThermoDaemon stopped");
                webapp.Dispose();
            };

            Logger.Log("HippotronicsThermoDaemon running");

            // Schedule purge of records that have not been updated
            await ScheduleNextUpdate();
            

            // Run until Ctrl+C
            Console.CancelKeyPress += (sender, e) =>
            {
                _endEvent.Set();
            };

            // Start watchdog
            Watchdog.Dog.ScheduleDog();

            // Wait for normal termination
            _endEvent.WaitOne();

            Logger.Log("HippotronicsThermoDaemon ending");
            Environment.Exit(0);        // Normal exit
        }


        private async Task ScheduleNextUpdate()
        {
            await Task.Run(async () => await UpdateTemperature());
        }

        private async Task UpdateTemperature()
        {
            // Wait one minute before updating the temperature
            bool quit = _endEvent.WaitOne(5000);
            if (quit) return;

            // Trigger Watchdog so it doesn't kick us out.
            // If this loop hangs, the watchdog will exit the process
            // after five minutes, so that systemd (or similar)
            // can restart it
            Watchdog.Dog.Wake();

            var daemon = ThermostatDaemon.Instance;

            // Get current temperature
            daemon.ReadRoomTemperatureCelsius();

            // Determine if we need to switch the heating on or off
            var switched = daemon.DetermineHeatingOn();

            if (switched)
            {
                var state = daemon.InternalState;

                Logger.Log("Heating switched {0}, room temperature {1}, target temperature {2}", state.HeatingOn ? "On" : "Off", state.RoomTemperature, state.TargetTemperature );
            }

            // TODO: Log to database?

            if (!quit)
            {
                await ScheduleNextUpdate();
            }
        }


        // This code configures Web API using Owin
        public void Configuration(IAppBuilder appBuilder)
        {
            // Configure Web API for self-host. 
            HttpConfiguration config = new HttpConfiguration();

            // Format to JSON by default
            config.Formatters.Clear();
            config.Formatters.Add(new JsonMediaTypeFormatter());

            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            config.EnsureInitialized();

            appBuilder.UseWebApi(config);
        }

    }
}
