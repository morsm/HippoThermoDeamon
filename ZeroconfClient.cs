using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Zeroconf;

namespace Termors.Serivces.HippotronicsLedDaemon
{
    public class ZeroconfClient
    {
        public static readonly string HIPPO_HOST_ID = "HippoLed-"; //"Maarten";
        public static readonly string HIPPO_SVC_ID = "_hippohttp"; //"_ssh";

        public async Task<IList<IZeroconfHost>> EnumerateAllServicesFromAllHosts()
        {
            List<IZeroconfHost> services = new List<IZeroconfHost>();

            ILookup<string, string> domains = await ZeroconfResolver.BrowseDomainsAsync();

            foreach (var domain in domains)
            { 
                var responses = await ZeroconfResolver.ResolveAsync(domain.Key);

                foreach (var resp in responses) services.Add(resp);
            }

            return services;
        }

        public IList<LampClient> FilterHippoHosts(IList<IZeroconfHost> hosts)
        {
            List<LampClient> clients = new List<LampClient>();

            foreach (var host in hosts)
            {
                if (host.DisplayName.Contains(HIPPO_HOST_ID))
                {
                    var services = from s in host.Services where s.Key.Contains(HIPPO_SVC_ID) select s.Value;

                    foreach (var service in services)
                    {
                        // Workaround: if IPAddress is null, look in Id string
                        string ip = host.IPAddress ?? host.Id.Substring(0, host.Id.IndexOf(':'));

                        string url = "http://" + ip + ":" + service.Port;
                        string name = host.DisplayName.Substring(HIPPO_HOST_ID.Length);

                        var newClient = new LampClient(url, name);
                        clients.Add(newClient);

                    }
                }
            }

            return clients;
        }

        public async Task<IList<LampClient>> GetLampClients()
        {
            var hosts = await EnumerateAllServicesFromAllHosts();
            return FilterHippoHosts(hosts);
        }
    }
}
