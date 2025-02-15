﻿using Newtonsoft.Json;
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

        public HttpServer HttpServer
        {
            get => default;
            set
            {
            }
        }

        public async Task<RequestContext> ReceiveRequestAsync()
        {
            try
            {
                var stream = connection.GetStream();
                using (var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true))
                {
                    var request = new RequestContext();
                    var firstLine = await reader.ReadLineAsync();

                    if (string.IsNullOrEmpty(firstLine))
                    {
                        Console.WriteLine("[Client] Invalid request: No first line.");
                        return null;
                    }

                    var parts = firstLine.Split(' ');

                    if (parts.Length != 3)
                    {
                        Console.WriteLine("[Client] Malformed request: Incorrect number of parts.");
                        return null;
                    }

                    request.HttpMethod = Enum.Parse<HttpMethods>(parts[0], true);
                    request.Path = parts[1];
                    request.HttpVersion = parts[2];

                    Console.WriteLine($"[Client] {parts[0]} {parts[1]} {parts[2]}");

                    string line;
                    int contentLength = 0;

                    // Headers einlesen
                    while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync()))
                    {
                        var headerParts = line.Split(new[] { ": " }, 2, StringSplitOptions.None);
                        if (headerParts.Length == 2)
                        {
                            request.Headers.Add(headerParts[0], headerParts[1]);

                            if (headerParts[0].Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                            {
                                if (int.TryParse(headerParts[1], out contentLength))
                                {
                                    // erfolgreich geparst
                                }
                                else
                                {
                                    Console.WriteLine("[Client] Invalid Content-Length header.");
                                }
                            }
                        }
                    }

                    // Token extrahieren
                    request.ExtractToken();

                    // Request Body einlesen, falls vorhanden
                    if ((request.HttpMethod == HttpMethods.POST || request.HttpMethod == HttpMethods.PUT) && contentLength > 0)
                    {
                        char[] buffer = new char[contentLength];
                        await reader.ReadAsync(buffer, 0, contentLength);
                        request.Body = new string(buffer);
                        Console.WriteLine($"[Client] Body: {request.Body}");
                    }

                    return request;
                }
            }
            catch (FormatException ex)
            {
                Console.WriteLine($"[Client] Format error: {ex.Message}");
                return null;
            }
            catch (IOException ex)
            {
                Console.WriteLine($"[Client] IO error: {ex.Message}");
                return null;
            }
            catch (Exception e)
            {
                Console.WriteLine($"[Client] Error: {e.Message}");
                return null;
            }
        }

        public async Task SendResponseAsync(HttpResponse response)
        {
            try
            {
                if (!connection.Connected)
                {
                    Console.WriteLine("[Client] Error: Socket is not connected.");
                    return;
                }

                var stream = connection.GetStream();
                using (var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true))
                {
                    if (response.StatusCode == 0)
                    {
                        response.StatusCode = StatusCodes.InternalServerError;
                    }

                    // Schreibe die Statuszeile
                    await writer.WriteAsync($"HTTP/1.1 {(int)response.StatusCode} {response.StatusCode}\r\n");
                    Console.WriteLine($"\n[Client] Status: {(int)response.StatusCode} {response.StatusCode}");

                    // Schreibe die Header und den Body
                    if (!string.IsNullOrEmpty(response.Body))
                    {
                        var payload = Encoding.UTF8.GetBytes(response.Body);
                        await writer.WriteAsync($"Content-Length: {payload.Length}\r\n");
                        await writer.WriteAsync("Content-Type: application/json; charset=UTF-8\r\n");
                        await writer.WriteAsync("\r\n");
                        await writer.WriteAsync(response.Body);
                    }
                    else
                    {
                        await writer.WriteAsync("\r\n");
                    }

                    await writer.FlushAsync();
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
