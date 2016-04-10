using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices;
using Windows.Devices.Gpio;
using Windows.Devices.Pwm;
using Windows.Foundation;
using Windows.System.Threading;
using Windows.UI.ViewManagement;
using Microsoft.IoT.Lightning.Providers;

namespace IotWebServer
{
    public sealed class IotCarDriver
    {
        private GpioController _gpioController;
        private GpioPin _servoPin;
        private PwmController _pwmController;
        private PwmPin _motorPin;
        private const int DelayBetweenPulsesInMs = 5; // the documentation said 25-50ms, but smaller numbers seemed smoother
        const int MinPulseInMicroseconds = 700;
        const int MaxPulseInMicroseconds = 2100;

        public IAsyncAction InitGpioAsync()
        {
            return InitGpio().AsAsyncAction();
        }

        public void SetMotorSpeed(double percent)
        {
            _motorPin.SetActiveDutyCyclePercentage(percent);
        }

        public IAsyncAction ServoGoToAction(double rotationPercent)
        {
            return ServoGoTo(rotationPercent).AsAsyncAction();
        }

        private async Task ServoGoTo(double rotationPercent)
        {
            long microsecondsDelay = RotationToMicrosecondsDelay(rotationPercent);
            if (!Stopwatch.IsHighResolution) throw new Exception("We need high resolution stopwatches to run servo's");
            var ticsInASecond = Stopwatch.Frequency;
            var ticsInAMicrosecond = ticsInASecond / (1000L * 1000L);
            var pulseDurationInTics = microsecondsDelay * ticsInAMicrosecond;

            await ThreadPool.RunAsync(async source =>
            {
                var operationStopwatch = Stopwatch.StartNew();
                var pulseStopwatch = new Stopwatch();
                int pulsesSent = 0;
                Debug.WriteLine($"ServoGoTo R%={rotationPercent} PulseDurationInTics={pulseDurationInTics}");
                while (operationStopwatch.ElapsedMilliseconds < 500)
                {
                    pulseStopwatch.Reset();
                    pulseStopwatch.Start();
                    _servoPin.Write(GpioPinValue.High);
                    while (pulseStopwatch.ElapsedTicks < pulseDurationInTics) { }
                    _servoPin.Write(GpioPinValue.Low);
                    await Task.Delay(DelayBetweenPulsesInMs);
                    pulsesSent++;
                }
                Debug.WriteLine("Pulses Sent: " + pulsesSent);
            }, WorkItemPriority.High);
        }

        private static long RotationToMicrosecondsDelay(double percent)
        {
            var percentAsDouble = percent;
            return (long)(percentAsDouble * (MaxPulseInMicroseconds - MinPulseInMicroseconds)) + MinPulseInMicroseconds;
        }

        private async Task InitGpio()
        {
            if (LightningProvider.IsLightningEnabled)
            {
                // Do something with the Lightning providers
                LowLevelDevicesController.DefaultProvider = LightningProvider.GetAggregateProvider();

                _gpioController = await GpioController.GetDefaultAsync();
                _servoPin = _gpioController.OpenPin(22, GpioSharingMode.Exclusive);
                _servoPin.Write(GpioPinValue.Low);
                _servoPin.SetDriveMode(GpioPinDriveMode.Output);

                _pwmController = (await PwmController.GetControllersAsync(LightningPwmProvider.GetPwmProvider()))[1];
                _motorPin = _pwmController.OpenPin(5);
                _pwmController.SetDesiredFrequency(50);
                _motorPin.SetActiveDutyCyclePercentage(0);
                _motorPin.Start();
            }
            else
            {
                Debug.WriteLine("Please enable lightning providers");
            }

        }

        public void Stop()
        {
            _motorPin.Stop();
        }
    }
}
