using System;
namespace Termors.Serivces.HippotronicsLedDaemon
{
    public class SetLampData
    {
        public SetLampData()
        {
            Red = Green = Blue = -1;    // Indicating not set
        }

        public string On { get; set; }
        public int Red { get; set; }
        public int Green { get; set; }
        public int Blue { get; set; }

        public override string ToString()
        {
            return String.Format("on={0} red={1} green={2} blue={3}", On, Red, Green, Blue);
        }
    }

    public class SetLampDataExtended : SetLampData
    {

        public SetLampDataExtended()
        {
            OnChanged = ColorChanged = BrightnessChanged = false;
        }

        public SetLampDataExtended(SetLampData data)
        {
            On = data.On;
            Red = data.Red;
            Green = data.Green;
            Blue = data.Blue;

            OnChanged = ColorChanged = true;
        }

        public bool OnChanged { get; set; }
        public bool ColorChanged { get; set; }

        public double Brightness { get; set; }
        public bool BrightnessChanged { get; set; }
    }
}
