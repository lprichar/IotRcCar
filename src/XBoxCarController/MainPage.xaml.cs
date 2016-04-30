using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Gaming.Input;
using Windows.UI.Core;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace XBoxCarController
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            Loaded += OnLoaded;
            Gamepad.GamepadAdded += GamepadOnGamepadAdded;
        }

        private async void GamepadOnGamepadAdded(object sender, Gamepad gamepad)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                await DisplayGamepadData(gamepad);
            });
        }

        private async void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            var controller = Gamepad.Gamepads.FirstOrDefault();
            if (controller == null)
            {
                MyTextBlock.Text = "No gamepad detected";
                return;
            }
            await DisplayGamepadData(controller);
        }

        private async Task DisplayGamepadData(Gamepad controller)
        {
            while (true)
            {
                var reading = controller.GetCurrentReading();
                var leftThumbstickX = reading.LeftThumbstickX;
                var leftThumbstickY = reading.LeftThumbstickY;
                MyTextBlock.Text = leftThumbstickX + " " + leftThumbstickY;
                await Task.Delay(100);
            }
        }
    }
}
