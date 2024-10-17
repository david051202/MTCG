using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
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

        public HttpServer(int port)
        {
            _listener = new TcpListener(IPAddress.Loopback, port);
            Console.WriteLine($"[Server] Server initialized on port {port}");
        }

        public void Start()
        {
            _listener.Start();
            listen = true;
            Console.WriteLine("[Server] Server started, waiting for connections...");

            Task.Run(async () =>
            {
                while (listen)
                {
                    var connection = await _listener.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleClient(connection));
                }
            }).GetAwaiter().GetResult();
        }

        public void HandleClient(TcpClient connection)
        {
            Console.WriteLine("[Server] Handling client...");

            if (connection == null)
            {
                Console.WriteLine("[Server] Error: Invalid client.");
                return;
            }

            using (connection)
            {
                try
                {
                    var client = new HttpClient(connection);
                    Console.WriteLine("[Server] Receiving request...");
                    var request = client.ReceiveRequest();

                    HttpResponse response = new HttpResponse();

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
                        Console.WriteLine("[Server] Valid request received.");
                        if (request.HttpMethod == HttpMethods.POST && request.Path == "/users")
                        {
                            User newUser = JsonConvert.DeserializeObject<User>(request.Body);
                            if (newUser != null && newUser.CreateUser())
                            {
                                response.StatusCode = StatusCodes.OK;
                                response.Body = "User successfully created.";
                            }
                            else
                            {
                                response.StatusCode = StatusCodes.BadRequest;
                                response.Body = "User already exists or an error occurred.";
                            }
                        }
                        else if (request.HttpMethod == HttpMethods.POST && request.Path == "/sessions")
                        {
                            User loginUser = JsonConvert.DeserializeObject<User>(request.Body);
                            if (loginUser != null)
                            {
                                User loggedInUser = User.Login(loginUser.Username, loginUser.Password);
                                if (loggedInUser != null)
                                {
                                    response.StatusCode = StatusCodes.OK;
                                    response.Body = $"Login successful. Token: {loggedInUser.Token}";
                                }
                                else
                                {
                                    response.StatusCode = StatusCodes.Unauthorized;
                                    response.Body = "Invalid username or password.";
                                }
                            }
                            else
                            {
                                response.StatusCode = StatusCodes.BadRequest;
                                response.Body = "Invalid login data.";
                            }
                        }
                        else
                        {
                            response.StatusCode = StatusCodes.NotFound;
                            response.Body = "Endpoint not found.";
                        }
                    }

                    Console.WriteLine("[Server] Sending response...");
                    client.SendResponse(response);
                    Console.WriteLine("[Server] Response sent.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Server] Error while handling client: {ex.Message}");
                }
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