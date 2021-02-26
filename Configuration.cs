using System;
namespace Termors.Serivces.HippotronicsThermoDaemon
{
    public class Configuration
    {
        public ushort Port { get; set; }
        public string SerialService { get; set; }
        public string DbService { get; set; }
        public double DefaultTemperature { get; set; }
        public bool EnableDb { get; set; }
    }
}
