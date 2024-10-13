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
        private readonly TcpListener _listener;
        private bool listerning = false;

        //Starts the server
        public HttpServer(IPAddress address, int port) {
            _listener = new TcpListener(address, port);
            _listener.Start();
        }

        public void Start()
        {
            _listener.Start();
            listerning = true;

            while (listerning)
            {
                var connection = _listener.AcceptTcpClient();                
            }
        }

    }
}
