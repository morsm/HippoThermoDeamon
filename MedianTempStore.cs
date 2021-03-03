using System;
using System.Collections.Generic;
using System.Text;

namespace Termors.Serivces.HippotronicsThermoDaemon
{
    /// <summary>
    /// Storage for a number of temperatures, so we can get a median or average
    /// to prevent sensor fluctuation driving the thermostat nuts
    /// </summary>
    public class MedianTempStore
    {
        protected Queue<Temperature> _temperatures;

        public MedianTempStore(uint count = 5)
        {
            Count = count;
            _temperatures = new Queue<Temperature>((int) Count);
        }

        public uint Count { get; protected set; }

        public void Add(Temperature t)
        {
            lock (_temperatures)
            {
                while (_temperatures.Count >= Count) _temperatures.Dequeue();
                _temperatures.Enqueue(t);
            }
        }

        public Temperature[] ToArray()
        {
            lock (_temperatures)
            {
                return _temperatures.ToArray();
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            var arr = ToArray();

            // Append all values
            foreach (var val in arr) sb.Append(val).Append(" ");

            // Remove trailing space
            if (sb.Length > 1) sb.Remove(sb.Length - 2, 1);

            return sb.ToString();
        }

        public Temperature Average
        {
            get
            {
                if (_temperatures.Count == 0) return new Temperature();

                var arr = ToArray();
                double celsiusSum = 0.0;

                foreach (var val in arr) celsiusSum += val.CelsiusValue;

                return new Temperature() { CelsiusValue = celsiusSum / arr.Length };
            }
        }

        public Temperature Median
        {
            get
            {
                if (_temperatures.Count == 0) return new Temperature();

                var arr = ToArray();
                Array.Sort<Temperature>(arr);
                int middleIdx = arr.Length / 2;
                return arr[middleIdx];
            }
        }
    }
}
