using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using MTCG.Classes;

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
            try
            {
                var stream = connection.GetStream();
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    var request = new RequestContext();
                    var firstLine = reader.ReadLine();

                    if (string.IsNullOrEmpty(firstLine))
                    {
                        Console.WriteLine("[Client] Invalid request");
                        return null;
                    }

                    var parts = firstLine.Split(' ');

                    if (parts.Length != 3)
                    {
                        Console.WriteLine("[Client] Malformed request");
                        return null;
                    }

                    request.HttpMethod = HttpMethod.GetHttpMethod(parts[0]);
                    request.Path = parts[1];
                    request.HttpVersion = parts[2];

                    Console.WriteLine($"[Client] {parts[0]} {parts[1]} {parts[2]}");

                    string line;
                    int contentLength = 0;

                    // Headers einlesen
                    while (!string.IsNullOrEmpty(line = reader.ReadLine()))
                    {
                        var headerParts = line.Split(new[] { ": " }, 2, StringSplitOptions.None);
                        if (headerParts.Length == 2)
                        {
                            request.Headers.Add(headerParts[0], headerParts[1]);

                            if (headerParts[0].Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                            {
                                contentLength = int.Parse(headerParts[1]);
                            }
                        }
                    }

                    // Request Body einlesen, falls vorhanden
                    if (request.HttpMethod == HttpMethods.POST || request.HttpMethod == HttpMethods.PUT)
                    {
                        if (contentLength > 0)
                        {
                            char[] buffer = new char[contentLength];
                            reader.Read(buffer, 0, contentLength);
                            request.Body = new string(buffer);
                            Console.WriteLine($"[Client] Body: {request.Body}");
                        }
                    }

                    return request;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"[Client] Error: {e.Message}");
                return null;
            }
        }

        public void SendResponse(HttpResponse response)
        {
            try
            {
                if (!connection.Connected)
                {
                    Console.WriteLine("[Client] Error: Socket is not connected.");
                    return;
                }

                var stream = connection.GetStream();
                using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
                {
                    if (response.StatusCode == 0) 
                    {
                        response.StatusCode = StatusCodes.InternalServerError; 
                    }

                    // Schreibe die Statuszeile
                    writer.Write($"HTTP/1.1 {(int)response.StatusCode} {response.StatusCode}\r\n");
                    Console.WriteLine($"[Client] Status: {(int)response.StatusCode} {response.StatusCode}");

                    // Schreibe die Header und den Body
                    if (!string.IsNullOrEmpty(response.Body))
                    {
                        var payload = Encoding.UTF8.GetBytes(response.Body);
                        writer.Write($"Content-Length: {payload.Length}\r\n");
                        writer.Write("\r\n");
                        writer.Write(response.Body);
                        Console.WriteLine("[Client] Body sent");
                    }
                    else
                    {
                        writer.Write("\r\n");
                        Console.WriteLine("[Client] No body");
                    }

                    writer.Flush();
                    Console.WriteLine("[Client] Response sent");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"[Client] Error: {e.Message}");
            }
        }



        public void Close()
        {
            try
            {
                connection.Close();
                Console.WriteLine("[Client] Closed");
            }
            catch (Exception e)
            {
                Console.WriteLine($"[Client] Error: {e.Message}");
            }
        }
    }
}
