using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MTCG.Http
{
    public class HttpClient
    {
        private readonly TcpClient connection;
        public HttpClient(TcpClient connection)
        {
            this.connection = connection;
        }

        public RequestContext ReceiveRequest()
        {
            var stream = connection.GetStream();
            var reader = new StreamReader(stream);
            var request = new RequestContext();
            var firstLine = reader.ReadLine();

            var parts = firstLine.Split(' ');

            request.HttpMethod = HttpMethod.GetHttpMethod(parts[0]);
            request.Path = parts[1];
            request.HttpVersion = parts[2];

            try
            {
                string line;

                while (!string.IsNullOrEmpty(line = reader.ReadLine()))
                {
                    var headerParts = line.Split(": ");
                    request.Headers.Add(headerParts[0], headerParts[1]);
                }

                if (request.Headers.ContainsKey("Authorization"))
                {
                    request.Token = request.Headers["Authorization"];
                }

                if (request.HttpMethod == HttpMethods.POST || request.HttpMethod == HttpMethods.PUT)
                {
                    var body = new StringBuilder();
                    while (reader.Peek() != -1)
                    {
                        body.Append((char)reader.Read());
                    }
                    request.Body = body.ToString();
                }
                return request;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }

        public void SendResponse(HttpResponse response)
        {
            var stream = connection.GetStream();
            var writer = new StreamWriter(stream) { AutoFlush = true };

            writer.Write($"HTTP/1.1 {(int)response.StatusCode} {response.StatusCode}\r\n");

            if (!string.IsNullOrEmpty(response.Body))
            {
                var payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response.Body));
                writer.Write($"Content-Length: {payload.Length}\r\n");
                writer.Write("\r\n");
                writer.Write(Encoding.UTF8.GetString(payload));
                writer.Close();
                ;
            }
            else
            {
                writer.Write("\r\n");
                writer.Close();
            }
        }

        public void Close()
        {
            connection.Close();
        }
    }
}
