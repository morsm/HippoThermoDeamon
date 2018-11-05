using System;
using LiteDB;

namespace Termors.Serivces.HippotronicsLedDaemon
{
    public enum NodeType
    {
        Unknown,                // Not determined yet
        LampDimmable,           // One color, dimmable 0-100%
        LampColor1D,            // E.g. cool white to warm white, dimmable 0-100%
        LampColorRGB,           // RGB led
        Switch                  // On/off switch (e.g. relay)
    }

    public class LampNode
    {
        // Configuration
        [BsonId]
        public string Name { get; set; }
        public string Url { get; set; }
        public NodeType NodeType { get; set; }
        public DateTime LastSeen { get; set; }

        // State
        public bool Online { get; set; }
        public bool On { get; set; }
        public byte Red { get; set; }           // Red or dim state
        public byte Green { get; set; }         // Green or 1D color
        public byte Blue{ get; set; }
    }
}
