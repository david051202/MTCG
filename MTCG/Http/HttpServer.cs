using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using MTCG.Classes;
using Newtonsoft.Json;

namespace MTCG.Http
{
    public class HttpServer
    {
        private readonly TcpListener _listener;
        private bool listen;
        private readonly List<Route> _routes;

        public HttpServer(int port)
        {
            _listener = new TcpListener(IPAddress.Loopback, port);
            _routes = Route.GetRoutes();
            Console.WriteLine($"[Server] Server initialized on port {port}");
        }

        public async Task StartAsync()
        {
            _listener.Start();
            listen = true;
            Console.WriteLine("[Server] Server started, waiting for connections...");
            Console.WriteLine(new string('#', 50));

            while (listen)
            {
                var connection = await _listener.AcceptTcpClientAsync();
                _ = HandleClientAsync(connection);
            }
        }

        public async Task HandleClientAsync(TcpClient connection)
        {
            if (connection == null)
            {
                Console.WriteLine("[Server] Error: Invalid client.");
                return;
            }

            try
            {
                var client = new HttpClient(connection);
                var request = await client.ReceiveRequestAsync();
                var cleanResourcePath = request.Path.Split('?')[0];

                HttpResponse response = null;

                if (request == null)
                {
                    Console.WriteLine("[Server] Bad request received.");
                    response = new HttpResponse
                    {
                        StatusCode = StatusCodes.BadRequest,
                        Body = "Invalid request..."
                    };
                }
                else
                {
                    foreach (var route in _routes)
                    {
                        if (route.HttpMethod.Equals(request.HttpMethod.ToString(), StringComparison.OrdinalIgnoreCase))
                        {
                            if (route.IsMatch(cleanResourcePath, out var parameters))
                            {
                                response = await route.Action(request, parameters);
                                break;
                            }
                        }
                    }

                    if (response == null)
                    {
                        response = new HttpResponse
                        {
                            StatusCode = StatusCodes.NotFound,
                            Body = "Endpoint not found."
                        };
                    }
                }

                await client.SendResponseAsync(response);
                Console.WriteLine(new string('-', 50));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Server] Error while handling client: {ex.Message}");
            }
        }


        private string ExtractPath(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath))
                return "/";

            int queryStart = fullPath.IndexOf('?');
            return queryStart >= 0 ? fullPath.Substring(0, queryStart) : fullPath;
        }

        public void Stop()
        {
            listen = false;
            _listener.Stop();
            Console.WriteLine("[Server] Server stopped.");
        }
    }
}

