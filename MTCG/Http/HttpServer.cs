using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading.Tasks;
using MTCG.BusinessLogic;

namespace MTCG.Http
{
    public class HttpServer
    {
        private readonly TcpListener _listener;
        private readonly UserHandler _userHandler;

        public HttpServer(int port)
        {
            _listener = new TcpListener(IPAddress.Loopback, port);
            _userHandler = new UserHandler();
        }

        public async Task Start()
        {
            _listener.Start();
            Console.WriteLine("Server started...");

            while (true)
            {
                var client = await _listener.AcceptTcpClientAsync();
                _ = HandleClientAsync(client);
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            using var stream = client.GetStream();
            using var reader = new StreamReader(stream);
            using var writer = new StreamWriter(stream) { AutoFlush = true };

            var request = await HttpRequest.Parse(reader);
            if (request == null)
            {
                Console.WriteLine("Received a bad request.");
                await writer.WriteLineAsync(new HttpResponse("400 Bad Request", "{\"error\": \"Invalid request.\"}").ToString());
                return;
            }

            HttpResponse response;

            Console.WriteLine($"Received request: Method = {request.Method}, Path = {request.Path}");

            switch (request.Path)
            {
                case "/sessions":
                    if (request.Method == "POST")
                    {
                        response = await _userHandler.LoginUser(request);
                    }
                    else
                    {
                        response = new HttpResponse("405 Method Not Allowed", "{\"error\": \"Only POST is allowed\"}");
                    }
                    break;

                case "/users":
                    if (request.Method == "POST")
                    {
                        response = await _userHandler.RegisterUser(request);
                    }
                    else
                    {
                        response = new HttpResponse("405 Method Not Allowed", "{\"error\": \"Only POST is allowed\"}");
                    }
                    break;

                default:
                    response = new HttpResponse("404 Not Found", "{\"error\": \"Resource not found\"}");
                    break;
            }

            await writer.WriteLineAsync(response.ToString());
            Console.WriteLine(response.ToString());
        }
    }
}
