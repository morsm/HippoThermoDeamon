using System;
using LiteDB;
using ColorMine.ColorSpaces;


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

        public override string ToString()
        {
            return String.Format("{0} [{1}]: online={2} on={3} red={4} green={5} blue={6}", Name, Url, Online, On, Red, Green, Blue);
        }

        public void ProcessStateChanges(SetLampDataExtended data)
        {
            if (data.OnChanged) On = Convert.ToBoolean(data.On);
            if (data.ColorChanged)
            {
                if (data.Red >= 0 && data.Red <= 255) Red = Convert.ToByte(data.Red);
                if (data.Green >= 0 && data.Green <= 255) Green = Convert.ToByte(data.Green);
                if (data.Blue >= 0 && data.Blue <= 255) Blue = Convert.ToByte(data.Blue);
            }
            if (data.BrightnessChanged)
            {
                var rgb = new Rgb { R = Red, G = Green, B = Blue };
                var hsb = rgb.To<Hsb>();

                hsb.B = data.Brightness;
                rgb = hsb.To<Rgb>();

                Red = Convert.ToByte(rgb.R);
                Green = Convert.ToByte(rgb.G);
                Blue = Convert.ToByte(rgb.B);
            }
        }
    }

    public class VersionDataObject
    {
        public string version { get; set; }
    }

    public class ConfigDataObject
    {
        public string name { get; set; }
        public int behavior { get; set; }

        public int type { get; set; }
    }

}
