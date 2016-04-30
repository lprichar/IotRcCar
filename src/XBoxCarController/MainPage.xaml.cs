using System;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.Gaming.Input;
using Windows.UI.Core;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace XBoxCarController
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage
    {
        public MainPage()
        {
            InitializeComponent();
            Gamepad.GamepadAdded += GamepadOnGamepadAdded;
        }

        private async void GamepadOnGamepadAdded(object sender, Gamepad gamepad)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                try
                {
                    await DisplayGamepadData(gamepad);
                }
                catch (Exception ex)
                {
                    MyTextBlock.Text = ex.ToString();
                }
            });
        }

        private async Task DisplayGamepadData(Gamepad controller)
        {
            double lastX = 0;
            double lastY = 0;
            while (true)
            {
                var reading = controller.GetCurrentReading();
                var thumbstickX = reading.LeftThumbstickX;
                var thumbstickY = reading.LeftThumbstickY;

                if (lastX != thumbstickX || lastY != thumbstickY)
                {
                    int throttlePercent = (int)(Math.Max(0, thumbstickY)*100);
                    int servoPercent = (int)(((thumbstickX + 1)/2)*100);
                    var httpClient = new HttpClient();
                    var url = $"http://leesraspi3:8001/Default.html?direction={servoPercent}&motorSpeed={throttlePercent}";
                    await httpClient.GetAsync(url);
                    MyTextBlock.Text = throttlePercent + " " + servoPercent;
                }
                await Task.Delay(500);
                lastX = thumbstickX;
                lastY = thumbstickY;
            }
        }
    }
}
