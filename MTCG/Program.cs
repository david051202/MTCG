using MTCG.Http;

HttpServer server = new HttpServer(10001);
Console.WriteLine("Server started...");
await server.StartAsync();

