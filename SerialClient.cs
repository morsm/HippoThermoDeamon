using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using Newtonsoft.Json;


namespace Termors.Serivces.HippotronicsThermoDaemon
{
    /// <summary>
    /// Client proxy to Arduino Serial daemon
    /// </summary>
    public class SerialClient
    {
        protected string _baseUrl = "";

        public SerialClient(string url)
        {
            _baseUrl = url;
        }

        protected async Task<string> GetRequest(string uri)
        {
            var client = new HttpClient() ;
            var response = await client.GetAsync(_baseUrl + uri);
            response.EnsureSuccessStatusCode();     // Throw if HTTP error

            // Read state object
            string json = await response.Content.ReadAsStringAsync();
            return json;
        }

        public async Task<SerialTemp> GetTemperature()
        {
            var json = await GetRequest("temp");
            return JsonConvert.DeserializeObject<SerialTemp>(json);
        }

        public async Task<int[]> GetRelayStatus()
        {
            var json = await GetRequest("relaystatus");
            return JsonConvert.DeserializeObject<int[]>(json);
        }

        public async Task SetRelay(int index, bool state)
        {
            var uri = String.Format("setrelay/{0}/{1}", index, state ? 1 : 0);
            await GetRequest(uri);
        }
    }

    public struct SerialTemp
    {
        public double TempCelsius { get; set; }

        public double RelHumidity { get; set; }
    }
}
