using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace MTCG.Http
{
    internal class HttpServer
    {
        private readonly int _port;
        private readonly StreamTracer _tracer;

        public HttpServer(int port, StreamTracer tracer)
        {
            _port = port;
            _tracer = tracer;
        }

        public async Task Start()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, _port);
            listener.Start();
            _tracer.WriteLine($"HTTP Server started on port {_port}");

            while (true)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                _tracer.WriteLine("Accepted new client connection");

                HandleClient(client);
            }
        }

        private async void HandleClient(TcpClient client)
        {
            using NetworkStream stream = client.GetStream();
            using StreamReader reader = new StreamReader(stream, Encoding.UTF8);
            using StreamWriter writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

            try
            {
                HttpRequest request = HttpRequest.Parse(reader);
                _tracer.WriteLine($"Received request: {request.Method} {request.Path}");

                HttpResponse response;

                if (request.Method == "GET" && request.Path == "/")
                {
                    response = HandleGetRequest();
                }
                else if (request.Method == "POST" && request.Path == "/")
                {
                    response = HandlePostRequest(request);
                }
                else
                {
                    response = new HttpResponse("404 Not Found", "The resource could not be found.");
                }

                response.Send(writer);
                _tracer.WriteLine($"Sent response: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _tracer.WriteLine($"Error: {ex.Message}");
                HttpResponse errorResponse = new HttpResponse("500 Internal Server Error", "An error occurred while processing your request.");
                errorResponse.Send(writer);
            }

            client.Close();
        }

        private HttpResponse HandleGetRequest()
        {
            string responseBody = "<html><body><h1>Welcome to the MTCG Server</h1></body></html>";
            HttpResponse response = new HttpResponse("200 OK", responseBody);
            response.SetHeader("Content-Type", "text/html");
            return response;
        }

        private HttpResponse HandlePostRequest(HttpRequest request)
        {
            _tracer.WriteLine($"POST body: {request.Body}");

            // Simulate user registration (for example)
            string responseBody = "{\"message\":\"User registered successfully\"}";
            HttpResponse response = new HttpResponse("201 Created", responseBody);
            response.SetHeader("Content-Type", "application/json");
            return response;
        }
    }
}
