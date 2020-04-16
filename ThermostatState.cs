using System;
namespace Termors.Serivces.HippotronicsThermoDaemon
{
    public class ThermostatState
    {
        public DateTime TimeStamp { get; set; } = DateTime.Now;

        public Temperature RoomTemperature;
        public Temperature TargetTemperature;

        public bool HeatingOn;
    }
}
