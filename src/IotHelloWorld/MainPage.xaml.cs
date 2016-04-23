using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using Windows.Devices;
using Windows.Devices.Gpio;
using Windows.Devices.Pwm;
using Windows.Storage;
using Windows.System.Threading;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Microsoft.IoT.Lightning.Providers;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace IotHelloWorld
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private DispatcherTimer timer;
        private int LED_PIN = 21;
        private GpioPinValue pinValue;
        private PwmPin motorPin;
        private GpioPin _pin;
        private PwmPin _pin27;
        double RestingPulseLegnth = 0;
        private const int DelayBetweenPulsesInMs = 5; // the documentation said 25-50ms, but smaller numbers seemed smoother
        const int MinPulseInMicroseconds = 700;
        const int MaxPulseInMicroseconds = 2100;

        public MainPage()
        {
            InitializeComponent();

            Loaded += OnLoaded;
            ApplicationData.Current.DataChanged += async (d, a) => await HandleDataChangedEvent(d, a);
        }

        private async Task HandleDataChangedEvent(ApplicationData sender, object args)
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            if (!localSettings.Values.ContainsKey("MotorSpeed"))
            {
                return;
            }

            string motorSpeed = localSettings.Values["MotorSpeed"] as string;
            int motorSpeedInt = int.Parse(motorSpeed);
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                SetMotorSpeed(motorSpeedInt * .01);
                StatusMessage.Text = "Motor Speed Changed to: " + motorSpeed;
            });
        }

        private async void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            await InitGPIO();
        }

        private async Task InitGPIO()
        {
            if (LightningProvider.IsLightningEnabled)
            {
                // Do something with the Lightning providers
                LowLevelDevicesController.DefaultProvider = LightningProvider.GetAggregateProvider();

                var pwmControllers = await PwmController.GetControllersAsync(LightningPwmProvider.GetPwmProvider());
                var pwmController = pwmControllers[1]; // the on-device controller
                pwmController.SetDesiredFrequency(50); // try to match 50Hz

                _pin27 = pwmController.OpenPin(22);
                _pin27.SetActiveDutyCyclePercentage(.25);
                _pin27.Start();

                motorPin = pwmController.OpenPin(5);
                motorPin.SetActiveDutyCyclePercentage(RestingPulseLegnth);
                motorPin.Start();


            }
            else
            {
                StatusMessage.Text = "Please enable lightning providers";
            }

            var gpioController = await GpioController.GetDefaultAsync();
            if (gpioController == null)
            {
                StatusMessage.Text = "There is no GPIO controller on this device.";
                return;
            }
            _pin = gpioController.OpenPin(22);
            _pin.SetDriveMode(GpioPinDriveMode.Output);
            _pin.Write(GpioPinValue.High);
            await Task.Delay(500);
            _pin.Write(GpioPinValue.Low);
        }

        private void LedBrightness_OnValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            _pin27.SetActiveDutyCyclePercentage(ledBrightness.Value * .01);
            ledPercent.Text = ledBrightness.Value + "%";
        }
        private async void ClickMe_Click(object sender, RoutedEventArgs e)
        {
            _pin.Write(GpioPinValue.High);

            for (double percent = 0; percent <= 1; percent += .01)
            {
                _pin27.SetActiveDutyCyclePercentage(percent);
                await Task.Delay(20);
            }
            for (double percent = 1; percent >= 0; percent -= .01)
            {
                _pin27.SetActiveDutyCyclePercentage(percent);
                await Task.Delay(20);
            }
        }

        private void MotorSpeed_OnValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            StatusMessage.Text = motorSpeed.Value.ToString(CultureInfo.InvariantCulture);

            SetMotorSpeed(motorSpeed.Value * .01);
        }

        private void SetMotorSpeed(double percent)
        {
            motorPin.SetActiveDutyCyclePercentage(percent);
        }

        private async Task ServoGoTo(double rotationPercent)
        {
            long microsecondsDelay = RotationToMicrosecondsDelay(rotationPercent);
            var ticsInASecond = Stopwatch.Frequency;
            var ticsInAMicrosecond = ticsInASecond / (1000L * 1000L);
            var pulseDurationInTics = microsecondsDelay * ticsInAMicrosecond;

            await ThreadPool.RunAsync(async source =>
            {
                var operationStopwatch = Stopwatch.StartNew();
                var pulseStopwatch = new Stopwatch();
                while (operationStopwatch.ElapsedMilliseconds < 500)
                {
                    pulseStopwatch.Reset();
                    pulseStopwatch.Start();
                    _pin.Write(GpioPinValue.High);
                    while (pulseStopwatch.ElapsedTicks < pulseDurationInTics) { }
                    _pin.Write(GpioPinValue.Low);
                    await Task.Delay(DelayBetweenPulsesInMs);
                }
            }, WorkItemPriority.High);
        }

        private static long RotationToMicrosecondsDelay(double percent)
        {
            var percentAsDouble = percent * .01;
            return (long)(percentAsDouble * (MaxPulseInMicroseconds - MinPulseInMicroseconds)) + MinPulseInMicroseconds;
        }

        private async void ServoRotation_OnValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            var percent = servoRotation.Value;
            await ServoGoTo(percent);
        }
    }
}
