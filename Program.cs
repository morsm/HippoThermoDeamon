using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Net.Http.Formatting;

using Microsoft.Owin.Hosting;
using Owin;

namespace Termors.Serivces.HippotronicsLedDaemon
{
    class Daemon
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("HippotronicsLedDaemon started");

            // Set up REST services in OWIN web server
            var webapp = WebApp.Start("http://*:9000/", new Action<IAppBuilder>(Configuration));

            Console.CancelKeyPress += (sender, e) =>
            {
                Console.WriteLine("HippotronicsLedDaemon stopped");
                webapp.Dispose();
            };

            // Main program loop, until break signal received,
            // is to refresh the list of clients every minute
            while (true)
            {
                Refresh().Wait();

                Thread.Sleep(60000);
            }
        }

        // This code configures Web API using Owin
        public static void Configuration(IAppBuilder appBuilder)
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

        /// <summary>
        /// Refresh the Zeroconf clients in the database
        /// </summary>
        public static async Task Refresh()
        {
            // Get new lamp clients from ZeroConf
            var lampClients = await GetZeroConf();

            // Get the status of the lamp nodes
            await GetLampStatuses(lampClients);

            // Update the database of current lamps
            UpdateDb(lampClients);
        }

        protected static async Task<IList<LampClient>> GetZeroConf()
        {
            var client = new ZeroconfClient();
            var responses = await client.GetLampClients();

            Console.WriteLine("Refresh cycle executed, {0} services found:", responses.Count);
            foreach (var resp in responses) Console.WriteLine(resp);

            return responses;
        }

        protected static void UpdateDb(IList<LampClient> lamps = null)
        {
            using (var client = new DatabaseClient())
            {

                if (lamps != null)
                {
                    // Add new clients to database
                    foreach (var lamp in lamps) if (lamp.StateSet) client.AddOrUpdate(lamp.Node);
                }

                // Remove old records
                client.PurgeExpired();
            }
        }

        protected static async Task GetLampStatuses(IList<LampClient> lamps = null)
        {
            Task[] requests = new Task[lamps.Count];

            for (int i = 0; i < lamps.Count; i++) 
            {
                // Assume the lamp is online and get its state
                lamps[i].Online = true;
                requests[i] = lamps[i].GetState();
            }

            try
            {
                await Task.WhenAll(requests);
            }
            catch (Exception)
            {
                // One or more lamps gave an exception. They must be offline.
                for (int i = 0; i < requests.Length; i++)
                {
                    if (requests[i].IsFaulted) lamps[i].Online = false;
                }
            }
        }
    }
}
