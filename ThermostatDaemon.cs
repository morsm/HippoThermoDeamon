using System;

namespace Termors.Serivces.HippotronicsThermoDaemon
{
    public sealed class ThermostatDaemon
    {
        public static double THRESHOLD_CELSIUS = 0.2;

        protected ThermostatDaemon()
        {
            ReadHeatingOnFromDevice();
            ReadRoomTemperatureCelsius();

            _state.TargetTemperature = _state.RoomTemperature;      // TODO: perhaps better to store target temp somewhere and read it in case of crash
        }

        public static ThermostatDaemon Instance = new ThermostatDaemon();

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
                    TargetTemperature = _state.TargetTemperature
                };

                return copy;
            }
        }

        public double ReadRoomTemperatureCelsius()
        {
            // TODO: get room temperature from serial daemon
            double temp = 20.0 + new Random().NextDouble();

            lock (_state)
            {
                _state.RoomTemperature.CelsiusValue = temp;
            }

            return temp;
        }

        public bool ReadHeatingOnFromDevice()
        {
            // TODO: get relay status from serial daemon
            var onOff = false;

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
                    if (! currentlyOn) SwitchHeating(true);
                }
                else if (_state.RoomTemperature.CelsiusValue > _state.TargetTemperature.CelsiusValue + THRESHOLD_CELSIUS)
                {
                    // Switch heating off if it's on, getting too hot
                    if (currentlyOn) SwitchHeating(false);
                }

                return _state.HeatingOn != currentlyOn;
            }
        }

        private void SwitchHeating(bool onOff)
        {
            // TODO: switch heating on or off using daemon

            _state.HeatingOn = onOff;
        }
    }
}
