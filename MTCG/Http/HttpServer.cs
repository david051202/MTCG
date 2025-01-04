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

                HttpResponse response = new HttpResponse();

                if (request == null)
                {
                    Console.WriteLine("[Server] Bad request received.");
                    response.StatusCode = StatusCodes.BadRequest;
                    response.Body = "Invalid request...";
                }
                else
                {
                    var route = _routes.FirstOrDefault(r => r.HttpMethod == request.HttpMethod.ToString() && r.Path == request.Path);
                    if (route != null)
                    {
                        response = route.Action(request);
                    }
                    else
                    {
                        response.StatusCode = StatusCodes.NotFound;
                        response.Body = "Endpoint not found.";
                    }
                }

                if (response.StatusCode == 0)
                {
                    response.StatusCode = StatusCodes.InternalServerError;
                }

                await client.SendResponseAsync(response);
                Console.WriteLine(new string('-', 50));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Server] Error while handling client: {ex.Message}");
            }
        }

        public void Stop()
        {
            listen = false;
            _listener.Stop();
            Console.WriteLine("[Server] Server stopped.");
        }
    }
}
