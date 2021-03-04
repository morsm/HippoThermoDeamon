using System;
using System.Diagnostics;
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
            Logger.Log("Initializing to serial service at {0}", url);
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

        public void SetMedianBehavior(bool on, uint count)
        {
            InternalState.RoomTemperatureQueue = on ? new MedianTempStore(count) : new MedianTempStore(1);
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
                    TargetTemperature = _state.TargetTemperature,
                    RelativeHumidity = _state.RelativeHumidity
                };

                copy.RoomTemperatureQueue.Add(_state.RoomTemperatureQueue.Average);

                return copy;
            }
        }

        public async Task<double> ReadRoomTemperatureCelsius()
        {
            var client = new SerialClient(SerialUrl);

            var result = await client.GetTemperature();
            double temp = result.TempCelsius;
            double hum = result.RelHumidity;

            Debug.WriteLine("Temp before: average={0}, median={1} ; read value {2}", _state.RoomTemperatureQueue.Average.CelsiusValue, _state.RoomTemperatureQueue.Median.CelsiusValue, temp);

            lock (_state)
            {
                _state.RoomTemperatureQueue.Add(new Temperature() { CelsiusValue = temp }, Daemon.Config.Smoothing);
                _state.RelativeHumidity = hum;
            }

            Debug.WriteLine("Temp after: average={0}, median={1}", _state.RoomTemperatureQueue.Average.CelsiusValue, _state.RoomTemperatureQueue.Median.CelsiusValue);

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
                if (_state.RoomTemperatureQueue.Median.CelsiusValue < _state.TargetTemperature.CelsiusValue - THRESHOLD_CELSIUS)
                {
                    // Switch heating on if it's off, getting too cold
                    if (!currentlyOn)
                    {
                        Logger.Log("Switching heating on, median temp is {0} compared to target {1}, temperature values: {2}", _state.RoomTemperatureQueue.Median, _state.TargetTemperature, _state.RoomTemperatureQueue);

                        Task.Run(async () => await SwitchHeating(true));
                    }
                }
                else if (_state.RoomTemperatureQueue.Median.CelsiusValue > _state.TargetTemperature.CelsiusValue + THRESHOLD_CELSIUS)
                {
                    // Switch heating off if it's on, getting too hot
                    if (currentlyOn)
                    {
                        Logger.Log("Switching heating off, median temp is {0} compared to target {1}, temperature values: {2}", _state.RoomTemperatureQueue.Median, _state.TargetTemperature, _state.RoomTemperatureQueue);

                        Task.Run(async () => await SwitchHeating(false));
                    }
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
            var temp = new Temperature() { CelsiusValue = Daemon.Config.DefaultTemperature };

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
                Logger.Log("Can't read stored target temperature, taking default as target instead.");
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
