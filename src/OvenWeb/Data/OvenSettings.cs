using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OvenWeb.Data
{
    public class OvenSettings
    {
        public const int Celsius = 0;
        public const int Fahrenheit = 1;
        public const int Kelvin = 2;

        public int TemperatureIn { get; set; }
        public int HeatPhase2 { get; set; } = 30;
        public int HeatDuration { get; set; } = 2;
        public int HeatWait { get; set; } = 4;
        public int[] RelayPins { get; set; } = new int[] { 4, 17 };
        public System.Device.Spi.SpiConnectionSettings SpiConnectionSettings { get; set; } = 
            new System.Device.Spi.SpiConnectionSettings(0, 0) 
            { 
                ClockFrequency = Max6675.SpiClockFrequency, 
                Mode = System.Device.Spi.SpiMode.Mode0 
            };
    }
}
