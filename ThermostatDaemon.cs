﻿using System;
using System.Threading.Tasks;


namespace Termors.Serivces.HippotronicsThermoDaemon
{
    public sealed class ThermostatDaemon
    {
        public static double THRESHOLD_CELSIUS = 0.2;
        public static int RELAY_INDEX = 0;

        protected ThermostatDaemon()
        {
            Task.Run(async () =>
            {
                await ReadHeatingOnFromDevice();
                await ReadRoomTemperatureCelsius();

                _state.TargetTemperature = _state.RoomTemperature;      // TODO: perhaps better to store target temp somewhere and read it in case of crash

            });
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

        public async Task<double> ReadRoomTemperatureCelsius()
        {
            var client = new SerialClient();

            double temp = (await client.GetTemperature()).TempCelsius;

            lock (_state)
            {
                _state.RoomTemperature.CelsiusValue = temp;
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
