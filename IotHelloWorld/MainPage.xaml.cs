using System;
using System.Globalization;
using System.Threading.Tasks;
using Windows.Devices;
using Windows.Devices.Gpio;
using Windows.Devices.Pwm;
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
        private GpioPin pin;
        private int LED_PIN = 21;
        private GpioPinValue pinValue;
        private PwmController pwmController;
        private PwmPin motorPin;
        double RestingPulseLegnth = 0;

        public MainPage()
        {
            InitializeComponent();

            //timer = new DispatcherTimer();
            //timer.Interval = TimeSpan.FromMilliseconds(500);
            //timer.Tick += TimerOnTick;
            this.Loaded += OnLoaded;

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

                pwmController = (await PwmController.GetControllersAsync(LightningPwmProvider.GetPwmProvider()))[1];
                motorPin = pwmController.OpenPin(5);
                pwmController.SetDesiredFrequency(50);
                motorPin.SetActiveDutyCyclePercentage(RestingPulseLegnth);
                motorPin.Start();
                //motorPin.SetActiveDutyCyclePercentage(.5);
            }
            else
            {
                StatusMessage.Text = "Please enable lightning providers";
                //InitializeGpioDefaultProvider();
            }

        }

        private void InitializeGpioDefaultProvider()
        {
            var gpio = GpioController.GetDefault();

            // Show an error if there is no GPIO controller
            if (gpio == null)
            {
                pin = null;
                StatusMessage.Text = "There is no GPIO controller on this device.";
                return;
            }

            pin = gpio.OpenPin(LED_PIN, GpioSharingMode.Exclusive);
            pin.SetDriveMode(GpioPinDriveMode.Output);
            pinValue = GpioPinValue.High;
            pin.Write(pinValue);

            StatusMessage.Text = "GPIO initialized successfully";
        }

        private void TimerOnTick(object sender, object o)
        {
            
        }

        private void ClickMe_Click(object sender, RoutedEventArgs e)
        {
            StatusMessage.Text = "Hello, Windows IoT Core!";
        }

        private void Slider_OnValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            StatusMessage.Text = slider.Value.ToString(CultureInfo.InvariantCulture);

            motorPin.SetActiveDutyCyclePercentage(slider.Value * .01);
        }
    }
}
