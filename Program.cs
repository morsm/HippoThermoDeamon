using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Web.Http;
using System.Net.Http.Formatting;

using Newtonsoft.Json;
using Microsoft.Owin.Hosting;
using Owin;


namespace Termors.Serivces.HippotronicsThermoDaemon
{
    class Daemon
    {
        private ManualResetEvent _endEvent = new ManualResetEvent(false);
        private DateTime _lastDbWrite = new DateTime(1977, 7, 6);           // Date in the past, make sure to update DB first time

        public static async Task Main(string[] args)
        {
            Config = ReadConfig();

            await new Daemon().Run(args);
        }

        public static Configuration Config { get; private set; }

        public async Task Run(string[] args)
        {
            string assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            int lastPoint = assemblyVersion.LastIndexOf('.');
            assemblyVersion = assemblyVersion.Substring(0, lastPoint);
            Logger.Log("HippotronicsThermoDaemon version {0} started", assemblyVersion);

            // Set up REST services in OWIN web server
            var url = String.Format("http://*:{0}/", Config.Port);
            var webapp = WebApp.Start(url, new Action<IAppBuilder>(Configuration));

            // Start the Thermostat Daemon. This will throw if there is a problem and the app will exit
            await ThermostatDaemon.Initialize(Config.SerialService);

            // See if we need to use some averaging
            ThermostatDaemon.Instance.SetMedianBehavior(Config.UseMedian, Config.MedianSamples);

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

        private static Configuration ReadConfig()
        {
            using (StreamReader rea = new StreamReader("config.json"))
            {
                string json = rea.ReadToEnd();
                return JsonConvert.DeserializeObject<Configuration>(json);
            }
        }

        private async Task ScheduleNextUpdate()
        {
            await Task.Run(async () => await UpdateTemperature());
        }

        private async Task UpdateTemperature()
        {
            // Wait some seconds before updating the temperature
            bool quit = _endEvent.WaitOne((int) Config.Pollingloop * 1000);
            if (quit) return;

            try
            {
                var daemon = ThermostatDaemon.Instance;

                // Get current temperature
                await daemon.ReadRoomTemperatureCelsius();

                // Determine if we need to switch the heating on or off
                var switched = daemon.DetermineHeatingOn();

                // Trigger Watchdog so it doesn't kick us out.
                // If this loop hangs, the watchdog will exit the process
                // after five minutes, so that systemd (or similar)
                // can restart it
                Watchdog.Dog.Wake();

                if (switched)
                {
                    var state = daemon.InternalState;

                    Logger.Log("Heating switched {0}, room temperature {1}, target temperature {2}", state.HeatingOn ? "On" : "Off", state.RoomTemperatureQueue, state.TargetTemperature);
                }

                if (Config.EnableDb && DateTime.Now.Subtract(_lastDbWrite).TotalSeconds >= 300)
                {
                    // Send update to database every five minutes.
                    // Doesn't matter if this fails, it's optional
                    //
                    // Also, store the target temperature to disk in case of daemon crash.
                    try
                    {
                        DatabaseClient client = new DatabaseClient { Url = Config.DbService };

                        await client.PushState(daemon.InternalState);

                        daemon.StoreTargetTemperature();

                        _lastDbWrite = DateTime.Now;
                    }
                    catch (Exception exOptional)
                    {
                        Logger.LogError("Couldn't push state to database, but continuing. Exception {0}: {1}", exOptional.GetType().Name, exOptional.Message);
                    }
                }

                await ScheduleNextUpdate();


            }
            catch (Exception ex)
            {
                Logger.LogError("Error in temperature update loop: {0}, {1}. Daemon quitting.", ex.GetType().Name, ex.Message);
                _endEvent.Set();
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
