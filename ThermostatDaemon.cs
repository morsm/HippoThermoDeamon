using System;
using System.Threading.Tasks;


namespace Termors.Serivces.HippotronicsThermoDaemon
{

    public class NotInitializedException : Exception
    {
    }

    public sealed class ThermostatDaemon
    {
        public static double THRESHOLD_CELSIUS = 0.2;
        public static int RELAY_INDEX = 0;

        protected ThermostatDaemon()
        {
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

        internal async static Task Initialize()
        {
            Instance = new ThermostatDaemon();

            await Instance.InitializeInstance();
        }

        internal async Task InitializeInstance()
        {
            await ReadHeatingOnFromDevice();
            await ReadRoomTemperatureCelsius();

            _state.TargetTemperature = _state.RoomTemperature;      // TODO: perhaps better to store target temp somewhere and read it in case of crash
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
            var client = new SerialClient();

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
            var client = new SerialClient();

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
            var client = new SerialClient();

            await client.SetRelay(RELAY_INDEX, onOff);

            lock (_state)
            {
                _state.HeatingOn = onOff;
            }
        }
    }
}
