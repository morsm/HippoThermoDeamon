using System;

using Newtonsoft.Json;

namespace Termors.Serivces.HippotronicsThermoDaemon
{
    public enum TemperatureScale
    {
        CELSIUS,
        FAHRENHEIT
    }

    public struct Temperature
    {
        [JsonProperty(propertyName:"value")]
        public double Temp { get; set; } 

        [JsonIgnore]
        public TemperatureScale Scale { get; set; }

        [JsonProperty(propertyName: "scale")]
        public string ScaleName
        {
            get
            {
                return Scale.ToString();
            }
            set
            {
                Scale = (TemperatureScale) Enum.Parse(typeof(TemperatureScale), value);
            }
        }

        public static double C2F(double celsiusValue)
        {
            return 9.0 * celsiusValue / 5.0 + 32.0;
        }

        public static double F2C(double fahrenheitValue)
        {
            return (fahrenheitValue - 32.0) * 5.0 / 9.0;
        }

        [JsonIgnore]
        public double CelsiusValue
        {
            get
            {
                if (Scale == TemperatureScale.CELSIUS) return Temp;
                else return F2C(Temp);
            }

            set
            {
                Temp = value;
                Scale = TemperatureScale.CELSIUS;
            }
        }

        [JsonIgnore]
        public double FahrenheitValue
        {
            get
            {
                if (Scale == TemperatureScale.FAHRENHEIT) return Temp;
                else return C2F(Temp);
            }

            set
            {
                Temp = value;
                Scale = TemperatureScale.FAHRENHEIT;
            }
        }

        public override string ToString()
        {
            return String.Format("{0:0.0} {1}", Temp, Scale == TemperatureScale.CELSIUS ? '\u2103' : '\u2109');
        }
    }
}
