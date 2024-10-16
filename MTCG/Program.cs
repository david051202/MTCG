using MTCG.Http;
using System;
using System.Threading.Tasks;

namespace MTCG
{
    class Program
    {
        static async Task Main(string[] args)
        {
            int port = 10001; // Port, auf dem der Server laufen soll

            var server = new HttpServer(port);

            try
            {
                await server.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting server: {ex.Message}");
            }
        }
    }
}
