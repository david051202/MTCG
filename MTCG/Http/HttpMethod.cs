using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCG.Http
{
    public static class HttpMethod
    {
        public static HttpServer HttpServer
        {
            get => default;
            set
            {
            }
        }

        public static HttpMethods GetHttpMethod(string method)
        {
            if (string.IsNullOrWhiteSpace(method))
            {
                throw new ArgumentException("HTTP method cannot be null or empty.");
            }

            // Konvertiere die Methode in Kleinbuchstaben, um Vergleichsprobleme zu vermeiden
            method = method.ToLowerInvariant();

            switch (method)
            {
                case "get":
                    return HttpMethods.GET;

                case "post":
                    return HttpMethods.POST;

                case "put":
                    return HttpMethods.PUT;

                case "delete":
                    return HttpMethods.DELETE;

                case "patch":
                    return HttpMethods.PATCH;

                default:
                    throw new NotSupportedException($"HTTP method '{method}' is not supported.");
            }
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
