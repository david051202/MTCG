using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCG.Http
{
    internal class HttpRequest
    {
        public string Method { get; private set; }
        public string Path { get; private set; }
        public string HttpVersion { get; private set; }
        public Dictionary<string, string> Headers { get; private set; }
        public string Body { get; private set; }

        public HttpRequest(string method, string path, string httpVersion)
        {
            Method = method;
            Path = path;
            HttpVersion = httpVersion;
            Headers = new Dictionary<string, string>();
        }

        public static HttpRequest Parse(StreamReader reader)
        {
            // Read the request line
            string requestLine = reader.ReadLine();
            if (string.IsNullOrEmpty(requestLine))
            {
                throw new InvalidOperationException("Empty request");
            }

            string[] tokens = requestLine.Split(' ');
            if (tokens.Length != 3)
            {
                throw new InvalidOperationException("Invalid request line");
            }

            string method = tokens[0];
            string path = tokens[1];
            string httpVersion = tokens[2];

            HttpRequest request = new HttpRequest(method, path, httpVersion);

            // Read headers
            string headerLine;
            while (!string.IsNullOrEmpty(headerLine = reader.ReadLine()))
            {
                string[] headerTokens = headerLine.Split(new[] { ": " }, StringSplitOptions.None);
                if (headerTokens.Length == 2)
                {
                    request.Headers[headerTokens[0]] = headerTokens[1];
                }
            }

            // Read body if POST
            if (request.Method == "POST" && request.Headers.ContainsKey("Content-Length"))
            {
                int contentLength = int.Parse(request.Headers["Content-Length"]);
                char[] buffer = new char[contentLength];
                reader.Read(buffer, 0, contentLength);
                request.Body = new string(buffer);
            }

            return request;
        }
    }
}
