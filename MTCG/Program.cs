using MTCG.Http;
using MTCG.Battle;

var battleManager = BattleManager.Instance;

HttpServer server = new HttpServer(10001);
Console.WriteLine("Server started...");
await server.StartAsync();