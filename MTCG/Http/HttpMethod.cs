using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCG.Http
{
    public static class HttpMethod
    {
        public static HttpMethods GetHttpMethod(string method)
        {
            return method.ToLowerInvariant() switch
            {
                "get" => HttpMethods.GET,
                "post" => HttpMethods.POST,
                "put" => HttpMethods.PUT,
                "delete" => HttpMethods.DELETE,
                "patch" => HttpMethods.PATCH
            };
        }
    }

    public enum HttpMethods
    {
        GET,
        POST,
        PUT,
        DELETE,
        PATCH
    }
}
