using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;

namespace IotHelloWorldReceiver
{
    public sealed class AppService : IBackgroundTask
    {
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            // Get the deferral object from the task instance
            serviceDeferral = taskInstance.GetDeferral();

            var appService = taskInstance.TriggerDetails as AppServiceTriggerDetails;
            if (appService != null &&
                appService.Name == "IotAppService")
            {
                appServiceConnection = appService.AppServiceConnection;
                appServiceConnection.RequestReceived += OnRequestReceived;
            }
        }

        private void OnRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var messageDefferal = args.GetDeferral();
            var message = args.Request.Message;
            string command = message["Command"] as string;
            string value = message["Value"] as string;

            messageDefferal.Complete();

            switch (command)
            {
                case "SetMotorSpeed":
                    var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                    localSettings.Values["MotorSpeed"] = value;
                    Windows.Storage.ApplicationData.Current.SignalDataChanged();
                    break;
                case "Quit":
                    //Service was asked to quit. Give us service deferral
                    //so platform can terminate the background task
                    serviceDeferral.Complete();
                    break;
            }
        }

        BackgroundTaskDeferral serviceDeferral;
        AppServiceConnection appServiceConnection;
    }
}
