using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Web.Http;
using System.Net.Http.Formatting;

using Microsoft.Owin.Hosting;
using Owin;

using Termors.Nuget.MDNSServiceDirectory;


namespace Termors.Serivces.HippotronicsLedDaemon
{
    class Daemon
    {
        public static readonly string HIPPO_HOST_ID = "HippoLed-";
        public static readonly string HIPPO_SVC_ID = "._hippohttp";

        private static ServiceDirectory _serviceDirectory = new ServiceDirectory();
        private static ManualResetEvent _endEvent = new ManualResetEvent(false);


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

            // Set up ServiceDirectory to look for MDNS lamps on the network
            _serviceDirectory.FilterFunction = (arg) => { return arg.ToLowerInvariant().Contains(HIPPO_SVC_ID); };
            _serviceDirectory.KeepaliveCheckInterval = 10;
            _serviceDirectory.KeepalivePing = _serviceDirectory.KeepaliveTcp = true;

            _serviceDirectory.HostDiscovered += (dir, entry) => { HostDiscoveredOrUpdated(dir, entry).Wait(); };
            _serviceDirectory.HostUpdated += (dir, entry) => { HostDiscoveredOrUpdated(dir, entry).Wait(); };
            _serviceDirectory.HostRemoved += HostRemoved;

            _serviceDirectory.Init();

            Console.WriteLine("HippotronicsLedDaemon running");

            // Schedule purge of records that have not been updated
            Task.Run(() => UpdateLampDatabase());

            // Run until Ctrl+C
            Console.CancelKeyPress += (sender, e) =>
            {
                _endEvent.Set();
            };

            _endEvent.WaitOne();

        }

        private static void UpdateLampDatabase()
        {
            lock (DatabaseClient.Synchronization)
            {
                using (var client = new DatabaseClient())
                {
                    // Update the status for all the lamps that are still in there
                    var lamps = client.GetAll();
                    foreach (var lamp in lamps)
                    {
                        UpdateSingleLamp(client, lamp);
                    }

                    // Remove old records
                    client.PurgeExpired();
                }
            }

            bool quit = _endEvent.WaitOne(60000);
            if (! quit) Task.Run(() => UpdateLampDatabase());
        }

        private static void UpdateSingleLamp(DatabaseClient db, LampNode lamp)
        {
            var lampClient = new LampClient(lamp);

            try
            {
                lampClient.GetState().Wait();

                lock (DatabaseClient.Synchronization)
                {
                    using (var client = new DatabaseClient())
                    {
                        client.AddOrUpdate(lampClient.Node);
                    }
                }
            }
            catch
            {
                // Error is ok; if the lamp is not seen, it will be removed
            }
        }

        private static void HostRemoved(ServiceDirectory directory, ServiceEntry entry)
        {
            lock (DatabaseClient.Synchronization)
            {
                using (var client = new DatabaseClient())
                {
                    client.RemoveByName(ServiceEntryToName(entry));
                }
            }
        }

        private static async Task HostDiscoveredOrUpdated(ServiceDirectory directory, ServiceEntry entry)
        {
            LampClient newClient = new LampClient(ServiceEntryToUrl(entry), ServiceEntryToName(entry));

            await GetLampStatus(newClient);
            UpdateDb(newClient);
        }

        private static string ServiceEntryToName(ServiceEntry entry)
        {
            int idxTrailer = entry.Service.IndexOf(HIPPO_SVC_ID);
            int lenHeader = HIPPO_HOST_ID.Length;
            string name = entry.Service.Substring(lenHeader, idxTrailer - lenHeader);

            return name;
        }

        private static string ServiceEntryToUrl(ServiceEntry entry)
        {
            StringBuilder sb = new StringBuilder("http://");

            if (entry.IPAddresses.Count > 0) sb.Append(entry.IPAddresses[0]); else sb.Append(entry.Host);
            sb.Append(":").Append(entry.Port);

            return sb.ToString();
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

        protected static void UpdateDb(LampClient lamp)
        {
            lock (DatabaseClient.Synchronization)
            {
                using (var client = new DatabaseClient())
                {

                    // Add new client to database
                    if (lamp.StateSet) client.AddOrUpdate(lamp.Node);
                }
            }
        }

        protected static async Task GetLampStatus(LampClient lamp)
        {
            // Assume the lamp is online and get its state
            lamp.Online = true;

            try
            {
                await lamp.GetState();
            }
            catch (Exception)
            {
                lamp.Online = false;
            }
        }
    }
}
