using System.Text;

namespace MTCG.Http
{
    public class HttpResponse
    {
        public StatusCodes StatusCode { get; set; }
        public string Body { get; set; }

        public override string ToString()
        {
            var response = new StringBuilder();
            response.AppendLine($"HTTP/1.1 {(int)StatusCode} {StatusCode}");
            response.AppendLine("Content-Type: application/json; charset=UTF-8");
            response.AppendLine($"Content-Length: {Encoding.UTF8.GetByteCount(Body)}");
            response.AppendLine();
            response.AppendLine(Body);
            return response.ToString();
        }
    }
}



