using System;
namespace Termors.Serivces.HippotronicsThermoDaemon
{
    public class ThermostatState
    {
        public DateTime TimeStamp { get; set; } = DateTime.Now;

        public Temperature RoomTemperature;
        public Temperature TargetTemperature;

        private double _hum = 0.0;
        public double RelativeHumidity
        {
            get
            {
                return _hum;
            }
            set
            {
                if (value < 0) _hum = 0; else if (value > 100) _hum = 100; else _hum = value;
            }
        }

        public bool HeatingOn;
    }
}
