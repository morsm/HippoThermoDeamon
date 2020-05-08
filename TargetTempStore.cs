using System;

namespace Termors.Serivces.HippotronicsThermoDaemon
{
    /// <summary>
    /// Small serializable class to store the target temperature, for daemon recovery
    /// </summary>
    public class TargetTempStore
    {
        public TargetTempStore()
        {
        }

        public DateTime Timestamp { get; set; }

        public Temperature Target { get; set; }
    }
}
