using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;


namespace Termors.Serivces.HippotronicsLedDaemon
{
    public class LampDataObject
    {
        public bool burn { get; set; }
        public byte red { get; set; }
        public byte green { get; set; }
        public byte blue { get; set; }
    }

    public class LampClient
    {
        protected readonly LampNode _node;
        private bool _online = false;

        public LampClient(string url, string name)
        {
            Url = url;
            Name = name;

            _node = new LampNode();
            StateSet = false;
        }

        public LampClient(LampNode data)
        {
            _node = data;
            StateSet = true;

            Url = data.Url;
            Name = data.Name;
        }

        public string Url { get; set; }
        public string Name { get; set; }
        public bool Online 
        { 
            get
            {
                return _online;
            }
            set
            {
                _online = value;
                if (StateSet) _node.Online = value;
            } 
        }

        public bool StateSet { get; protected set; }

        public LampNode Node
        {
            get
            {
                if (!StateSet) throw new InvalidOperationException("Lamp State not loaded");
                return _node;
            }
        }

        public async Task GetState()
        {
            StateSet = false;
            _node.Url = Url;
            _node.Name = Name;

            var client = new HttpClient();
            var response = await client.GetAsync(Url + "/status.json");
            response.EnsureSuccessStatusCode();     // Throw if HTTP error

            // Read state object
            string json = await response.Content.ReadAsStringAsync();
            var obj = JsonConvert.DeserializeObject<LampDataObject>(json);

            // Store values
            _node.On = obj.burn;
            _node.Red = obj.red;
            _node.Blue = obj.blue;
            _node.Green = obj.green;

            StateSet = true;
            Online = true;
            Node.LastSeen = DateTime.Now;
        }

        public async Task SetState()
        {
            var client = new HttpClient();
            var obj = new LampDataObject
            {
                burn = Node.On,
                red = Node.Red,
                green = Node.Green,
                blue = Node.Blue
            };

            // Post the RGB
            await client.PostAsync<LampDataObject>(Url + "/rgb.json", obj, new JsonMediaTypeFormatter());

            // Switch on or off (this is a separate request)
            string onOffUrl = Url + "/";
            if (Node.On) onOffUrl += "on.html"; else onOffUrl += "off.html";
            await client.GetAsync(onOffUrl);

            // Update last seen
            Node.LastSeen = DateTime.Now;
        }

        public override string ToString()
        {
            return Name + " [" + Url + "]";
        }
    }
}
