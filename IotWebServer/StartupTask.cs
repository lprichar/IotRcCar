using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace IotWebServer
{
    public sealed class StartupTask : IBackgroundTask
    {
        private BackgroundTaskDeferral serviceDeferral;
        private HttpServer httpServer;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            // Get the deferral object from the task instance
            serviceDeferral = taskInstance.GetDeferral();

            httpServer = new HttpServer(8000);
            httpServer.StartServer();
        }
    }

    public sealed class HttpServer
    {
        private readonly int _port;
        private StreamSocketListener listener;
        private AppServiceConnection appServiceConnection;
        private const uint BufferSize = 8192;

        public HttpServer(int port)
        {
            listener = new StreamSocketListener();
            listener.Control.KeepAlive = true;
            listener.Control.NoDelay = true;

            _port = port;
            listener.ConnectionReceived += async (s, e) => { await ProcessRequestAsync(e.Socket); };
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
                        WriteResponse(requestParts[1], socket);
                    else
                        throw new InvalidDataException("HTTP method not supported: "
                            + requestParts[0]);
                }
            }
        }

        private void WriteResponse(string requestPart, StreamSocket socket)
        {
            WriteResource(socket, "IotWebServer.Default.html");
        }

        private void WriteResource(StreamSocket socket, string resource)
        {
            var assembly = typeof(HttpServer).GetTypeInfo().Assembly;

            using (Stream stream = assembly.GetManifestResourceStream(resource))
            {
               WriteStream(socket, stream);
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
                await listener.BindServiceNameAsync(_port.ToString());

                // Initialize the AppServiceConnection
                appServiceConnection = new AppServiceConnection();
                appServiceConnection.PackageFamilyName = "BlinkyWebService_1w720vyc4ccym";
                appServiceConnection.AppServiceName = "App2AppComService";

                // Send a initialize request 
                var res = await appServiceConnection.OpenAsync();
                if (res != AppServiceConnectionStatus.Success)
                {
                    throw new Exception("Failed to connect to the AppService");
                }
            });
        }
    }
}
