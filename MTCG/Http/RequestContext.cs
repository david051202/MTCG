using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public RequestContext()
        {
            HttpMethod = HttpMethods.GET;
            Path = "";
            HttpVersion = "HTTP/1.1";
            Token = null;
            Body = null;
            Headers = new Dictionary<string, string>();
        }
    }
}
