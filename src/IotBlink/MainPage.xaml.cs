using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Devices.Gpio;
using Windows.Devices.Pwm;
using Windows.UI.Core;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.IoT.Lightning.Providers;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace IotBlink
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            try
            {
                if (LightningProvider.IsLightningEnabled)
                {
                    LowLevelDevicesController.DefaultProvider = LightningProvider.GetAggregateProvider();
                }

                var pwmControllers = await PwmController.GetControllersAsync(LightningPwmProvider.GetPwmProvider());
                var pwmController = pwmControllers[1]; // the on-device controller
                var pin = pwmController.OpenPin(22);
                pwmController.SetDesiredFrequency(50); // try to match 50Hz
                pin.SetActiveDutyCyclePercentage(0);
                pin.Start();
                await Pulse(pin);
                await StartRealtimeConnection();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private static async Task Pulse(PwmPin pin)
        {
            for (double percent = 0; percent <= 1; percent += .01)
            {
                pin.SetActiveDutyCyclePercentage(percent);
                await Task.Delay(20);
            }
            for (double percent = 1; percent >= 0; percent -= .01)
            {
                pin.SetActiveDutyCyclePercentage(percent);
                await Task.Delay(20);
            }
        }

        private HubConnection _connection;
        private IHubProxy _proxy;

        private async Task StartRealtimeConnection()
        {
            try
            {
                _connection = new HubConnection("http://sirenofshame.com");
                _connection.Error += Connection_Error;
                _proxy = _connection.CreateHubProxy("SosHub");
                _proxy.On<string>("addNotification", OnAddNotification);
                await _connection.Start();
            }
            catch (Exception ex)
            {
                Message.Text = ex.Message;
            }
        }

        private void Connection_Error(Exception obj)
        {
            Message.Text = obj.Message;
        }

        private async void OnAddNotification(string message)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Message.Text = message;
            });
        }
    }
}
