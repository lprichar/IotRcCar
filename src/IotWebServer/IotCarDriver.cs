using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices;
using Windows.Devices.Gpio;
using Windows.Devices.Pwm;
using Windows.Foundation;
using Microsoft.IoT.Lightning.Providers;

namespace IotWebServer
{
    public sealed class IotCarDriver
    {
        private GpioController _gpioController;
        private PwmPin _servoPin;
        private PwmController _pwmController;
        private PwmPin _motorPin;
        private int _servoPinNum = 26;
        private int _motorPinNumber = 21;

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
            var maxPwmPercent = .4;

            var percentToSet = rotationPercent * maxPwmPercent;

            _servoPin.SetActiveDutyCyclePercentage(percentToSet);
            await Task.Delay(500);
            _servoPin.SetActiveDutyCyclePercentage(0);
        }

        private async Task InitGpio()
        {
            if (LightningProvider.IsLightningEnabled)
            {
                // Do something with the Lightning providers
                LowLevelDevicesController.DefaultProvider = LightningProvider.GetAggregateProvider();

                _pwmController = (await PwmController.GetControllersAsync(LightningPwmProvider.GetPwmProvider()))[1];
                _servoPin = _pwmController.OpenPin(_servoPinNum);
                _servoPin.SetActiveDutyCyclePercentage(0);
                _servoPin.Start();

                _motorPin = _pwmController.OpenPin(_motorPinNumber);
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
