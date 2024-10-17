using System.Text;

namespace MTCG.Http
{
    public class HttpResponse
    {
        public StatusCodes StatusCode { get; set; }
        public string Body { get; set; }

        public HttpClient HttpClient
        {
            get => default;
            set
            {
            }
        }

        public override string ToString()
        {
            var response = new StringBuilder();
            response.AppendLine($"HTTP/1.1 {StatusCode}");
            response.AppendLine("Content-Type: application/json; charset=UTF-8");
            response.AppendLine($"Content-Length: {Encoding.UTF8.GetByteCount(Body)}");
            response.AppendLine();
            response.AppendLine(Body);
            return response.ToString();
        }
    }

    public enum StatusCodes
    {
        OK = 200,
        Created = 201,
        NoContent = 204,
        BadRequest = 400,
        Unauthorized = 401,
        Forbidden = 403,
        NotFound = 404,
        MethodNotAllowed = 405,
        Conflict = 409,
        InternalServerError = 500
    }
}
