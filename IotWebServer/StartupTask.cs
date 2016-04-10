using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace IotWebServer
{
    // ReSharper disable once UnusedMember.Global
    public sealed class StartupTask : IBackgroundTask
    {
        private BackgroundTaskDeferral _serviceDeferral;
        private HttpServer _httpServer;
        private IotCarDriver _iotCarDriver;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            // Get the deferral object from the task instance
            _serviceDeferral = taskInstance.GetDeferral();

            _iotCarDriver = new IotCarDriver();
            _httpServer = new HttpServer(8001);

            await _iotCarDriver.InitGpioAsync();

            _httpServer.StartServer();
            _httpServer.OnMotorSpeedChange += HttpServerOnOnMotorSpeedChange;
            _httpServer.OnDirectionChange += HttpServerOnOnDirectionChange;
        }

        ~StartupTask()
        {
            _iotCarDriver.Stop();
        }

        private async void HttpServerOnOnDirectionChange(object sender, OnDirectionChangeArgs args)
        {
            await _iotCarDriver.ServoGoToAction(args.NewDirection);
        }

        private void HttpServerOnOnMotorSpeedChange(object sender, OnMotorSpeedChangeArgs args)
        {
            _iotCarDriver.SetMotorSpeed(args.NewMotorSpeed);
        }
    }
}
