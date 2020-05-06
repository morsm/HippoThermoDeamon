using System;
using System.Net.Http;
using System.Threading.Tasks;

using Newtonsoft.Json;


namespace Termors.Serivces.HippotronicsThermoDaemon
{
    internal class DatabaseMessage
    {
        public double RoomTemperature { get; set; }
        public double TargetTemperature { get; set; }
        public double RelativeHumidity { get; set; }
        public bool HeatingOn { get; set; }
    }

    /// <summary>
    /// Class to log current temperature via node.js daemon to Amazon DynamoDB
    /// </summary>
    public class DatabaseClient
    {
        public string Url { get; set; }

        public DatabaseClient()
        {
        }

        public async Task PushState(ThermostatState state)
        {
            var client = new HttpClient();

            var message = new DatabaseMessage
            {
                RoomTemperature = state.RoomTemperature.CelsiusValue,
                TargetTemperature = state.TargetTemperature.CelsiusValue,
                RelativeHumidity = state.RelativeHumidity,
                HeatingOn = state.HeatingOn
            };

            var json = JsonConvert.SerializeObject(message);
            var content = new StringContent(json);
            content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/json");

            await client.PostAsync(Url, content);
        }
    }
}
