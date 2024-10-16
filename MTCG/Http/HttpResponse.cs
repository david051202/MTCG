using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCG.Http
{
    internal class HttpResponse
    {
        public string StatusCode { get; private set; }
        public Dictionary<string, string> Headers { get; private set; }
        public string Body { get; private set; }

        public HttpResponse(string statusCode, string body = "")
        {
            StatusCode = statusCode;
            Headers = new Dictionary<string, string>
            {
                { "Content-Type", "text/plain" },
                { "Content-Length", body.Length.ToString() }
            };
            Body = body;
        }

        public void SetHeader(string key, string value)
        {
            Headers[key] = value;
        }

        public void Send(StreamWriter writer)
        {
            // Write status line
            writer.WriteLine($"HTTP/1.1 {StatusCode}");

            // Write headers
            foreach (var header in Headers)
            {
                writer.WriteLine($"{header.Key}: {header.Value}");
            }

            writer.WriteLine(); // End of headers

            // Write body
            writer.WriteLine(Body);
        }
    }
}
