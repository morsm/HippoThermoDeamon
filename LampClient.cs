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

            // Is the lamp type known yet?
            if (_node.NodeType == NodeType.Unknown) await GetNodeType();

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

        protected async Task GetNodeType()
        {
            try
            {
                // Get the node version
                var client = new HttpClient();
                var versionResponse = await client.GetAsync(Url + "/version.json");

                // If the node version is not implemented, it is really old and it is a RGB lamp
                if (!versionResponse.IsSuccessStatusCode) throw new NotSupportedException();

                // Deserialize version info
                string versionJson = await versionResponse.Content.ReadAsStringAsync();
                var versionObj = JsonConvert.DeserializeObject<VersionDataObject>(versionJson);

                string[] versionMajorMinor = versionObj.version.Split('.');
                int major = Convert.ToInt32(versionMajorMinor[0]);
                int minor = Convert.ToInt32(versionMajorMinor[1]);

                // Type supported as of 3.3
                bool supported = major > 3 || (major == 3 && minor >= 3);
                if (!supported) throw new NotSupportedException(String.Format("Version is {0}.{1} and needs to be at least 3.3", major, minor));

                // Get the type
                var typeResponse = await client.GetAsync(Url + "/config.json");
                // Deserialize type info
                string typeJson = await typeResponse.Content.ReadAsStringAsync();
                var typeObj = JsonConvert.DeserializeObject<ConfigDataObject>(typeJson);

                _node.NodeType = (NodeType)typeObj.type;

            }
            catch (Exception e)
            {
                // If the node version is not implemented, it is really old and it is a RGB lamp
                _node.NodeType = NodeType.LampColorRGB;

                Console.WriteLine("Assuming {0} is an RGB lamp, because of problem: {1} message {2}", this.Name, e.GetType().Name, e.Message);
            }
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
