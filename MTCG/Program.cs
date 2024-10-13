using MTCG.Http;
using System.Net;

HttpServer server = new HttpServer(IPAddress.Loopback, 10001);
server.Start();

Console.WriteLine("Server started on http://localhost:10001/");