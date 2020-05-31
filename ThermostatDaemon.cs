using System;
using System.IO;
using System.Threading.Tasks;

using Newtonsoft.Json;


namespace Termors.Serivces.HippotronicsThermoDaemon
{

    public class NotInitializedException : Exception
    {
    }

    public sealed class ThermostatDaemon
    {
        public static double THRESHOLD_CELSIUS = 0.2;
        public static int RELAY_INDEX = 0;
        public static string SerialUrl { get; set; }

        protected ThermostatDaemon(string serialUrl)
        {
            SerialUrl = serialUrl;
        }

        private static ThermostatDaemon _instance = null;
        public static ThermostatDaemon Instance
        {
            get
            {
                if (_instance == null) throw new NotInitializedException();
                return _instance;
            }

            private set
            {
                _instance = value;
            }
        }

        internal async static Task Initialize(string url = "http://localhost:9003/webapi/")
        {
            Instance = new ThermostatDaemon(url);

            await Instance.InitializeInstance();
        }

        internal async Task InitializeInstance()
        {
            await ReadHeatingOnFromDevice();
            await ReadRoomTemperatureCelsius();

            _state.TargetTemperature = ReadStoredTargetTemperature();
            Logger.Log("Taking initial target temperature of {0}", _state.TargetTemperature);

            DetermineHeatingOn();
        }

        private ThermostatState _state = new ThermostatState();
        public ThermostatState InternalState
        {
            get
            {
                // Make a copy so that the internal state cannot be altered
                var copy = new ThermostatState
                {
                    HeatingOn = _state.HeatingOn,
                    RoomTemperature = _state.RoomTemperature,
                    TargetTemperature = _state.TargetTemperature,
                    RelativeHumidity = _state.RelativeHumidity
                };

                return copy;
            }
        }

        public async Task<double> ReadRoomTemperatureCelsius()
        {
            var client = new SerialClient(SerialUrl);

            var result = await client.GetTemperature();
            double temp = result.TempCelsius;
            double hum = result.RelHumidity;

            lock (_state)
            {
                _state.RoomTemperature.CelsiusValue = temp;
                _state.RelativeHumidity = hum;
            }

            return temp;
        }

        public async Task<bool> ReadHeatingOnFromDevice()
        {
            var client = new SerialClient(SerialUrl);

            int[] states = await client.GetRelayStatus();

            var onOff = states[RELAY_INDEX] == 1;

            lock (_state)
            {
                _state.HeatingOn = onOff;
            }

            return onOff;
        }

        public void SetTargetTemperature(Temperature temperature)
        {
            lock (_state)
            {
                _state.TargetTemperature = temperature;
            }

            StoreTargetTemperature();

            DetermineHeatingOn();
        }

        // Returns true if the state of the heating was changed. InternalState will show if it's on or off
        public bool DetermineHeatingOn()
        {
            lock(_state)
            {
                var currentlyOn = _state.HeatingOn;

                // Temperature below target?
                if (_state.RoomTemperature.CelsiusValue < _state.TargetTemperature.CelsiusValue - THRESHOLD_CELSIUS)
                {
                    // Switch heating on if it's off, getting too cold
                    if (! currentlyOn) Task.Run(async() => await SwitchHeating(true));
                }
                else if (_state.RoomTemperature.CelsiusValue > _state.TargetTemperature.CelsiusValue + THRESHOLD_CELSIUS)
                {
                    // Switch heating off if it's on, getting too hot
                    if (currentlyOn) Task.Run(async () => await SwitchHeating(false));
                }

                return _state.HeatingOn != currentlyOn;
            }
        }

        private async Task SwitchHeating(bool onOff)
        {
            var client = new SerialClient(SerialUrl);

            await client.SetRelay(RELAY_INDEX, onOff);

            lock (_state)
            {
                _state.HeatingOn = onOff;
            }
        }

        // Functions to read and write stored temperature
        private string TargetTempStorePath
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "stored_target_temp.json");
            }
        }


        private Temperature ReadStoredTargetTemperature()
        {
            var temp = _state.RoomTemperature;

            try
            {
                using (StreamReader rea = new StreamReader(TargetTempStorePath))
                {
                    string json = rea.ReadToEnd();
                    var stored = JsonConvert.DeserializeObject<TargetTempStore>(json);

                    // If the data is younger than 24 hours, accept it
                    // Otherwise, just take current room temperature instead and maintain that
                    var age = DateTime.Now - stored.Timestamp;
                    if (age.TotalHours < 24)
                        temp = stored.Target;
                    else
                        Logger.Log("Stored target temperature too old ({0} minutes)", age.TotalMinutes);
                }

            }
            catch
            {
                Logger.Log("Can't read stored target temperature, taking room temperature as target instead.");
            }

            return temp;
        }

        public void StoreTargetTemperature()
        {
            TargetTempStore storeObj = new TargetTempStore { Target = _state.TargetTemperature, Timestamp = DateTime.Now };

            using (StreamWriter wri = new StreamWriter(TargetTempStorePath))
            {
                wri.Write(JsonConvert.SerializeObject(storeObj));
            }
        }
    }
}
