using System;
using System.Collections.Generic;
using System.Device.Spi;
using System.Linq;
using System.Threading.Tasks;

namespace OvenWeb.Data
{
    public class Max6675 : IDisposable
    {
        public const int SpiClockFrequency = 4000000;

        private SpiDevice _spiDevice;
        private readonly byte[] _raw;

        public Max6675(SpiDevice spiDevice)
        {
            _spiDevice = spiDevice;
            _raw = new byte[2];
        }

        public double Celsius()
        {
            return Read() * 0.25;
        }

        public double Farenheit() => (Celsius() * 1.8) + 32;
        public double Kelvin() => Celsius() + 273.15;

        private int Read()
        {
            Span<byte> raw = new Span<byte>(_raw);
            _spiDevice.Read(raw);

            var sv = (raw[0] << 8) + raw[1];
            return sv >>= 3;
        }

        public void Dispose()
        {
            if (_spiDevice != null)
            {
                _spiDevice.Dispose();
                _spiDevice = null;
            }
        }
    }
}
