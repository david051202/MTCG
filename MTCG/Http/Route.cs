using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MTCG.Classes;
using Newtonsoft.Json;

namespace MTCG.Http
{
    public class Route
    {
        public string Path { get; set; }
        public string HttpMethod { get; set; }
        public Func<RequestContext, HttpResponse> Action { get; set; }

        public static List<Route> GetRoutes()
        {
            return new List<Route>
            {
                new Route
                {
                    Path = "/users",
                    HttpMethod = "POST", 
                    Action = (request) =>
                    {
                        var response = new HttpResponse();
                        User newUser = JsonConvert.DeserializeObject<User>(request.Body);
                        if (newUser != null && newUser.CreateUser())
                        {
                            response.StatusCode = StatusCodes.Created;
                            response.Body = "User successfully created.";
                        }
                        else
                        {
                            response.StatusCode = StatusCodes.BadRequest;
                            response.Body = "User already exists or an error occurred.";
                        }
                        return response;
                    }
                },
                new Route
                {
                    Path = "/sessions",
                    HttpMethod = "POST", 
                    Action = (request) =>
                    {
                        var response = new HttpResponse();
                        User loginUser = JsonConvert.DeserializeObject<User>(request.Body);
                        if (loginUser != null)
                        {
                            User loggedInUser = User.Login(loginUser.Username, loginUser.Password);
                            if (loggedInUser != null)
                            {
                                response.StatusCode = StatusCodes.Ok;
                                response.Body = $"Login successful. Token: {loggedInUser.Token}";
                            }
                            else
                            {
                                response.StatusCode = StatusCodes.Unauthorized;
                                response.Body = "Invalid username or password.";
                            }
                        }
                        else
                        {
                            response.StatusCode = StatusCodes.BadRequest;
                            response.Body = "Invalid login data.";
                        }
                        return response;
                    }
                }
            };
        }
    }
}
