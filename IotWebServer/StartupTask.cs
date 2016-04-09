using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI.ViewManagement;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace IotWebServer
{
    // ReSharper disable once UnusedMember.Global
    public sealed class StartupTask : IBackgroundTask
    {
        private BackgroundTaskDeferral serviceDeferral;
        private HttpServer _httpServer;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            // Get the deferral object from the task instance
            serviceDeferral = taskInstance.GetDeferral();

            _httpServer = new HttpServer(8001);
            _httpServer.StartServer();
        }
    }

    public sealed class HttpServer
    {
        private readonly int _port;
        private readonly StreamSocketListener _listener;
        private AppServiceConnection _appServiceConnection;
        private const uint BufferSize = 8192;

        public HttpServer(int port)
        {
            _listener = new StreamSocketListener();
            _listener.Control.KeepAlive = true;
            _listener.Control.NoDelay = true;

            _port = port;
            _listener.ConnectionReceived += async (s, e) => { await ProcessRequestAsync(e.Socket); };
        }

        private async Task ProcessRequestAsync(StreamSocket socket)
        {
            StringBuilder request = new StringBuilder();
            byte[] data = new byte[BufferSize];
            IBuffer buffer = data.AsBuffer();
            uint dataRead = BufferSize;
            using (IInputStream input = socket.InputStream)
            {
                while (dataRead == BufferSize)
                {
                    await input.ReadAsync(buffer, BufferSize, InputStreamOptions.Partial);
                    request.Append(Encoding.UTF8.GetString(data, 0, data.Length));
                    dataRead = buffer.Length;
                }
            }

            string requestAsString = request.ToString();
            string[] splitRequestAsString = requestAsString.Split('\n');
            if (splitRequestAsString.Length != 0)
            {
                string requestMethod = splitRequestAsString[0];
                string[] requestParts = requestMethod.Split(' ');
                if (requestParts.Length > 1)
                {
                    if (requestParts[0] == "GET")
                        await WriteResponse(requestParts[1], socket);
                    else
                        throw new InvalidDataException("HTTP method not supported: "
                            + requestParts[0]);
                }
            }
        }

        private async Task TryInitializeAppServiceConnection()
        {
            if (_appServiceConnection != null) return;
            // Initialize the AppServiceConnection
            var appServiceConnection = new AppServiceConnection();
            appServiceConnection.PackageFamilyName = "517d6348-bb2f-45b1-89d0-fb7ea8724769_n7wdzm614gaee";
            appServiceConnection.AppServiceName = "IotAppService";

            // Send a initialize request 
            var res = await appServiceConnection.OpenAsync();
            if (res != AppServiceConnectionStatus.Success)
            {
                throw new Exception("Failed to connect to the AppService");
            }
            _appServiceConnection = appServiceConnection;
        }

        private async Task WriteResponse(string requestPart, StreamSocket socket)
        {
            try
            {
                if (requestPart.Contains("motorSpeed="))
                {
                    var motorSpeed = Regex.Match(requestPart, "motorSpeed=([0-9]*)").Groups[1].Value;
                    var updateMessage = new ValueSet();
                    updateMessage.Add("Command", "SetMotorSpeed");
                    updateMessage.Add("Value", motorSpeed);
                    await TryInitializeAppServiceConnection();
                    await _appServiceConnection.SendMessageAsync(updateMessage);
                }
                WriteResource(socket, "IotWebServer.Default.html");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                // throwing an exceptin here will crash the whole service
                WriteString(socket, ex.ToString());
            }
        }

        private void WriteResource(StreamSocket socket, string resource)
        {
            var assembly = typeof(HttpServer).GetTypeInfo().Assembly;

            using (Stream stream = assembly.GetManifestResourceStream(resource))
            {
                WriteStream(socket, stream);
            }
        }

        private static void WriteString(StreamSocket socket, string str)
        {
            byte[] bodyArray = Encoding.UTF8.GetBytes(str);
            // Show the html 
            using (var outputStream = socket.OutputStream)
            using (Stream resp = outputStream.AsStreamForWrite())
            using (MemoryStream stream = new MemoryStream(bodyArray))
            {
                string header = String.Format("HTTP/1.1 200 OK\r\n" +
                                    "Content-Length: {0}\r\n" +
                                    "Connection: close\r\n\r\n",
                                    stream.Length);
                byte[] headerArray = Encoding.UTF8.GetBytes(header);
                resp.Write(headerArray, 0, headerArray.Length);
                stream.CopyTo(resp);
                resp.Flush();
            }
        }

        private static void WriteStream(StreamSocket socket, Stream sourceStream)
        {
            using (var outputStream = socket.OutputStream)
            using (Stream resp = outputStream.AsStreamForWrite())
            {
                string header = String.Format("HTTP/1.1 200 OK\r\n" +
                                              "Content-Length: {0}\r\n" +
                                              "Content-Type: text/html; charset=utf-8\r\n" +
                                              "Connection: close\r\n\r\n",
                    sourceStream.Length);
                byte[] headerArray = Encoding.UTF8.GetBytes(header);
                resp.Write(headerArray, 0, headerArray.Length);
                sourceStream.CopyTo(resp);
                resp.Flush();
            }
        }

        public void StartServer()
        {
            Task.Run(async () =>
            {
                await _listener.BindServiceNameAsync(_port.ToString());
            });
        }
    }
}
