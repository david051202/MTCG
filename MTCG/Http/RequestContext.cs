using System;
using System.Collections.Generic;

namespace MTCG.Http
{
    public class RequestContext
    {
        public HttpMethods HttpMethod { get; set; }
        public string Path { get; set; }
        public string HttpVersion { get; set; }
        public string Token { get; set; }
        public string Body { get; set; }
        public Dictionary<string, string> Headers { get; set; }

        // Vorhandener Standardkonstruktor
        public RequestContext()
        {
            HttpMethod = HttpMethods.GET;
            Path = "";
            HttpVersion = "HTTP/1.1";
            Token = null;
            Body = null;
            Headers = new Dictionary<string, string>();
        }

        // Neuer Konstruktor zum Parsen der rohen Anfrage
        public RequestContext(string rawRequest)
        {
            Headers = new Dictionary<string, string>();

            // Zerlegen der Anfrage in einzelne Zeilen
            var lines = rawRequest.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            if (lines.Length == 0)
                throw new ArgumentException("Leere Anfrage.");

            // Parse die Anfragezeile (z.B. "GET /deck?format=plain HTTP/1.1")
            var requestLine = lines[0];
            var requestLineParts = requestLine.Split(' ');

            if (requestLineParts.Length != 3)
                throw new ArgumentException("Ungültige Anfragezeile.");

            // Setze die HTTP-Methode
            if (!Enum.TryParse<HttpMethods>(requestLineParts[0], true, out var method))
                throw new ArgumentException($"Ungültige HTTP-Methode: {requestLineParts[0]}");

            HttpMethod = method;

            // Setze den Pfad (inklusive Query-String)
            Path = requestLineParts[1];

            // Setze die HTTP-Version
            HttpVersion = requestLineParts[2];

            // Parse die Header
            int lineIndex = 1;
            while (lineIndex < lines.Length && !string.IsNullOrWhiteSpace(lines[lineIndex]))
            {
                var headerParts = lines[lineIndex].Split(new[] { ':' }, 2);
                if (headerParts.Length == 2)
                {
                    Headers[headerParts[0].Trim()] = headerParts[1].Trim();
                }
                lineIndex++;
            }

            // Parse den Body, falls vorhanden
            lineIndex++; // Überspringe die leere Zeile zwischen Headern und Body
            if (lineIndex < lines.Length)
            {
                Body = string.Join("\n", lines, lineIndex, lines.Length - lineIndex);
            }

            // Extrahiere das Token aus den Headern
            ExtractToken();
        }

        public void ExtractToken()
        {
            if (Headers.TryGetValue("Authorization", out var authHeader))
            {
                if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    Token = authHeader.Substring("Bearer ".Length).Trim();
                }
            }
        }
    }
}
