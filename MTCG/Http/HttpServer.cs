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
        private TcpListener httpServer;

        public HttpServer()
        {
            httpServer = new TcpListener(IPAddress.Loopback, 10001);
            httpServer.Start();
        }

        public void Run()
        {
            while (true)
            {
                TcpClient clientSocket = httpServer.AcceptTcpClient();
                using var writer = new StreamWriter(clientSocket.GetStream()) { AutoFlush = true };
                using var reader = new StreamReader(clientSocket.GetStream());

                string? line;

                line = reader.ReadLine();
                if (line != null)
                    Console.WriteLine(line);

            }
        }
    }
}
