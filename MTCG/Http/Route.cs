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
                },
                new Route
                {
                    Path = "/packages",
                    HttpMethod = "POST",
                    Action = (request) =>
                    {
                        var response = new HttpResponse();

                        if (string.IsNullOrEmpty(request.Token))
                        {
                            response.StatusCode = StatusCodes.Unauthorized;
                            response.Body = "Unauthorized. Token is missing.";
                            return response;
                        }

                        User user = User.GetUserByToken(request.Token);
                        if (user == null || user.Username != "admin")
                        {
                            response.StatusCode = StatusCodes.Forbidden;
                            response.Body = "Provided user is not 'admin'.";
                            return response;
                        }

                        // Deserialize the card data
                        List<Card> cards = JsonConvert.DeserializeObject<List<Card>>(request.Body);
                        if (cards == null || cards.Count != 5)
                        {
                            response.StatusCode = StatusCodes.BadRequest;
                            response.Body = "Invalid package data. A package must contain exactly 5 cards.";
                            return response;
                        }

                        // Handle missing properties by inferring them
                        foreach (var card in cards)
                        {
                            // Infer ElementType from Name if missing
                            if (string.IsNullOrEmpty(card.ElementType))
                            {
                                card.ElementType = InferElementTypeFromName(card.Name);
                            }

                            // Infer CardType from Name if missing
                            if (string.IsNullOrEmpty(card.CardType))
                            {
                                card.CardType = card.Name.ToLower().Contains("spell") ? "spell" : "monster";
                            }
                        }

                        if (Package.CreatePackage(cards))
                        {
                            response.StatusCode = StatusCodes.Created;
                            response.Body = "Package and cards successfully created.";
                        }
                        else
                        {
                            response.StatusCode = StatusCodes.Conflict;
                            response.Body = "At least one card in the package already exists.";
                        }

                        return response;
                    }
                },
                new Route
                {
                    Path = "/transactions/packages",
                    HttpMethod = "POST",
                    Action = (request) =>
                    {
                        var response = new HttpResponse();

                        if (string.IsNullOrEmpty(request.Token))
                        {
                            response.StatusCode = StatusCodes.Unauthorized;
                            response.Body = "Unauthorized. Token is missing.";
                            return response;
                        }

                        User user = User.GetUserByToken(request.Token);
                        if (user == null)
                        {
                            response.StatusCode = StatusCodes.Unauthorized;
                            response.Body = "Invalid token.";
                            return response;
                        }

                        var result = Package.BuyPackage(user);

                        if (result == Package.BuyResult.NoPackageAvailable)
                        {
                            response.StatusCode = StatusCodes.NotFound;
                            response.Body = "No card package available for buying.";
                        }
                        else if (result == Package.BuyResult.NotEnoughCoins)
                        {
                            response.StatusCode = StatusCodes.Forbidden;
                            response.Body = "Not enough money for buying a card package.";
                        }
                        else if (result == Package.BuyResult.Success)
                        {
                            response.StatusCode = StatusCodes.Ok;
                            response.Body = JsonConvert.SerializeObject(user.Cards);
                        }
                        else
                        {
                            response.StatusCode = StatusCodes.InternalServerError;
                            response.Body = "An error occurred while processing your request.";
                        }

                        return response;
                    }
                },
            };
        }

        // Helper method to infer ElementType from the card name
        private static string InferElementTypeFromName(string name)
        {
            if (name.ToLower().Contains("water"))
                return "Water";
            if (name.ToLower().Contains("fire"))
                return "Fire";
            if (name.ToLower().Contains("normal"))
                return "Normal";
            return "Normal"; // Default to Normal if no keyword is found
        }
    }
}
