using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Device.Spi;
using System.Linq;
using System.Threading.Tasks;

namespace OvenWeb.Data
{
    public class OvenService : IDisposable
    {
        public double CurrentTemperature { get; set; }
        public OvenProgram CurrentOvenProgram { get; set; }

        public string Time 
        { 
            get 
            {
                return $"{_elapsed.Hours:00}:{_elapsed.Minutes:00}:{_elapsed.Seconds:00}";
            } 
        }
        public bool IsWorked { get; set; }

        private int _tempLoopDelay = 250;
        private bool _measureTemperature = true;
        private readonly OvenSettingsService _ovenSettings;
        private DateTime _dtStart = DateTime.Now;
        private TimeSpan _elapsed = TimeSpan.Zero;

        private readonly Max6675 _max6675;
        private readonly GpioController _gpioController;
        
        private System.Threading.CancellationTokenSource _cts;

        public OvenService(GpioController gpioController, OvenSettingsService ovenSettingsService)
        {
            _ovenSettings = ovenSettingsService;

            var spi = SpiDevice.Create(_ovenSettings.OvenSettings.SpiConnectionSettings);
            _max6675 = new Max6675(spi);

            _gpioController = gpioController;

            foreach (var item in _ovenSettings.OvenSettings.RelayPins)
            {
                _gpioController.OpenPin(item, PinMode.Output);
                _gpioController.Write(item, PinValue.High);
            }

            Task.Run(() => TemperatureLoop());
        }

        private async void TemperatureLoop()
        {
            while (_measureTemperature)
            {
                if (_ovenSettings.OvenSettings.TemperatureIn == OvenSettings.Celsius)
                    CurrentTemperature = _max6675.Celsius();
                else if (_ovenSettings.OvenSettings.TemperatureIn == OvenSettings.Fahrenheit)
                    CurrentTemperature = _max6675.Farenheit();
                else if (_ovenSettings.OvenSettings.TemperatureIn == OvenSettings.Kelvin)
                    CurrentTemperature = _max6675.Kelvin();

                _elapsed = DateTime.Now - _dtStart;
                await Task.Delay(_tempLoopDelay);
            }
        }

        public void Start(int temperature)
        {
            var p = new OvenProgram() 
            { 
                Items = new List<OvenProgramItem>() 
                { 
                    new OvenProgramItem() { Time = new DateTime(), Temperature = temperature, Relays = _ovenSettings.OvenSettings.RelayPins.Length } 
                }, 
                Name = "Temperature" 
            };
            StartProgram(p);
        }

        public void StartProgram(OvenProgram op)
        {
            if (_cts != null)
                _cts.Dispose();

            _cts = new System.Threading.CancellationTokenSource();

            Task.Run(() => ProgramLoop(op, _cts.Token));
        }
        public void Stop() => _cts.Cancel();

        private async void ProgramLoop(OvenProgram op, System.Threading.CancellationToken ct)
        {
            _dtStart = DateTime.Now;
            var stopwatch = new System.Diagnostics.Stopwatch();
            
            CurrentOvenProgram = op;
            IsWorked = true;

            foreach (var item in op.Items)
            {
                if (ct.IsCancellationRequested || item.Temperature == -1)
                    break;

                stopwatch.Reset();
                bool startStopwatch = true;
                op.CurrentItemId = item.Id;

                while (stopwatch.Elapsed.TotalSeconds <= item.Time.TimeOfDay.TotalSeconds || item.Time.TimeOfDay == TimeSpan.Zero)
                {
                    if (ct.IsCancellationRequested)
                        break;

                    if (CurrentTemperature < item.Temperature)
                    {
                        //Phase 1
                        if (CurrentTemperature < item.Temperature - _ovenSettings.OvenSettings.HeatPhase2)
                        {
                            for (int i = 0; i < item.Relays; i++)
                                _gpioController.Write(_ovenSettings.OvenSettings.RelayPins[i], PinValue.Low);

                            while (CurrentTemperature < item.Temperature)
                            {
                                if (ct.IsCancellationRequested || CurrentTemperature > item.Temperature - _ovenSettings.OvenSettings.HeatPhase2)
                                    break;

                                await Task.Delay(_tempLoopDelay);
                            }

                            for (int i = 0; i < item.Relays; i++)
                                _gpioController.Write(_ovenSettings.OvenSettings.RelayPins[i], PinValue.High);
                        }
                        else //Phase 2
                        {
                            while (CurrentTemperature < item.Temperature)
                            {
                                if (ct.IsCancellationRequested)
                                    break;

                                for (int i = 0; i < item.Relays; i++)
                                    _gpioController.Write(_ovenSettings.OvenSettings.RelayPins[i], PinValue.Low);

                                await Task.Delay(_ovenSettings.OvenSettings.HeatDuration * 1000);

                                for (int i = 0; i < item.Relays; i++)
                                    _gpioController.Write(_ovenSettings.OvenSettings.RelayPins[i], PinValue.High);

                                if (ct.IsCancellationRequested)
                                    break;

                                await Task.Delay(_ovenSettings.OvenSettings.HeatWait * 1000);
                            }
                        }
                    }
                    else
                    {
                        if (startStopwatch)
                        {
                            startStopwatch = false;
                            stopwatch.Start();
                        }

                        await Task.Delay(_tempLoopDelay);
                    }
                }
            }

            stopwatch.Stop();
            IsWorked = false;
        }

        public void StartSecondsProgram(int seconds)
        {
            if (_cts != null)
                _cts.Dispose();

            _cts = new System.Threading.CancellationTokenSource();

            Task.Run(() => SecondLoop(_cts.Token, seconds));
        }

        private async void SecondLoop(System.Threading.CancellationToken ct, int seconds)
        {
            int d = seconds * 1000;

            CurrentOvenProgram = new OvenProgram()
            {
                Items = new List<OvenProgramItem>()
                {
                    new OvenProgramItem() { Time = new DateTime(), Temperature = 0, Relays = _ovenSettings.OvenSettings.RelayPins.Length }
                },
                Name = "Loop"
            };

            _dtStart = DateTime.Now;
            IsWorked = true;

            while (ct.IsCancellationRequested == false)
            {
                foreach (var item in _ovenSettings.OvenSettings.RelayPins)
                    _gpioController.Write(item, PinValue.Low);

                await Task.Delay(d);

                foreach (var item in _ovenSettings.OvenSettings.RelayPins)
                    _gpioController.Write(item, PinValue.High);

                await Task.Delay(d);
            }

            IsWorked = false;
        }

        public void Dispose()
        {
            _measureTemperature = false;

            if (IsWorked)
            {
                Stop();
                while (IsWorked)
                    Task.Delay(100).GetAwaiter().GetResult();
            }

            if (_cts != null)
                _cts.Dispose();

            foreach (var item in _ovenSettings.OvenSettings.RelayPins)
            {
                _gpioController.SetPinMode(item, PinMode.Input);
                _gpioController.ClosePin(item);
            }

            if (_max6675 != null)
                _max6675.Dispose();
        }
    }
}