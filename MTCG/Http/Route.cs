﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MTCG.Classes;
using MTCG.Battle;
using MTCG.Models;
using Newtonsoft.Json;

namespace MTCG.Http
{
    public class Route
    {
        public string Path { get; set; }
        public string HttpMethod { get; set; }
        public Func<RequestContext, Dictionary<string, string>, Task<HttpResponse>> Action { get; set; }

        public HttpServer HttpServer
        {
            get => default;
            set
            {
            }
        }

        public bool IsMatch(string requestPath, out Dictionary<string, string> parameters)
        {
            parameters = new Dictionary<string, string>();

            var routeSegments = Path.Trim('/').Split('/');
            var requestSegments = requestPath.Trim('/').Split('/');

            if (routeSegments.Length != requestSegments.Length)
                return false;

            for (int i = 0; i < routeSegments.Length; i++)
            {
                if (routeSegments[i].StartsWith("{") && routeSegments[i].EndsWith("}"))
                {
                    var paramName = routeSegments[i].Trim('{', '}');
                    parameters[paramName] = requestSegments[i];
                }
                else if (!routeSegments[i].Equals(requestSegments[i], StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
            return true;
        }

        public static List<Route> GetRoutes()
        {
            var routes = new List<Route>
            {
                new Route
                {
                    Path = "/users",
                    HttpMethod = "POST",
                    Action = async (request, parameters) =>
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
                    Action = async (request, parameters) =>
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
,
                new Route
                {
                    Path = "/packages",
                    HttpMethod = "POST",
                    Action = async (request, parameters) =>
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
                    Action = async (request, parameters) =>
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
                new Route
                {
                    Path = "/cards",
                    HttpMethod = "GET",
                    Action = async (request, parameters) =>
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

                        if (user.Cards == null || user.Cards.Count == 0)
                        {
                            response.StatusCode = StatusCodes.NoContent;
                            response.Body = "User has no cards.";
                            return response;
                        }

                        // Output cards to the console
                        Console.WriteLine($"[Server] User {user.Username} has the following cards:");
                        foreach (var card in user.Cards)
                        {
                            Console.WriteLine($"- {card.Name} (ID: {card.Id}, Damage: {card.Damage}, Element: {card.ElementType}, Type: {card.CardType})");
                        }

                        response.StatusCode = StatusCodes.Ok;
                        response.Body = JsonConvert.SerializeObject(user.Cards);
                        return response;
                    }
                },
                new Route
                {
                    Path = "/deck",
                    HttpMethod = "GET",
                    Action = async (request, parameters) =>
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

                        // Retrieve user's deck
                        Deck deck = Deck.GetDeckByUserId(user.UserId);

                        if (deck == null || deck.Cards == null || deck.Cards.Count == 0)
                        {
                            response.StatusCode = StatusCodes.Ok;
                            response.Body = "User has no cards in the deck.";
                            return response;
                        }

                        Console.WriteLine($"[Server] User {user.Username} has the following cards in their deck:\n");

                        string format = "json";
                        if (TryGetQueryParameter(request.Path, "format", out string formatValue))
                        {
                            format = formatValue.ToLower().Trim();
                        }

                        if (format == "plain")
                        {
                            var deckDescription = new StringBuilder();
                            for (int i = 0; i < deck.Cards.Count; i++)
                            {
                                var card = deck.Cards[i];
                                deckDescription.AppendLine($"{i + 1}. {card.Name} ({card.Damage} Damage, {card.ElementType}, {card.CardType})");
                            }
                            Console.WriteLine(deckDescription);
                            response.StatusCode = StatusCodes.Ok;
                            response.Body = deckDescription.ToString().TrimEnd();
                        }
                        else
                        {
                            response.StatusCode = StatusCodes.Ok;
                            response.Body = JsonConvert.SerializeObject(deck.Cards, Formatting.Indented);
                            Console.WriteLine(response.Body);
                        }

                        return response;
                    }
                },
                new Route
                {
                    Path = "/deck",
                    HttpMethod = "PUT",
                    Action = async (request, parameters) =>
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

                        List<Guid> cardIds = JsonConvert.DeserializeObject<List<Guid>>(request.Body);
                        if (cardIds == null || cardIds.Count != 4)
                        {
                            response.StatusCode = StatusCodes.BadRequest;
                            response.Body = "Invalid deck data. A deck must contain exactly 4 unique card IDs.";
                            return response;
                        }

                        var userCardIds = new HashSet<Guid>(user.Cards.ConvertAll(card => card.Id));
                        foreach (var cardId in cardIds)
                        {
                            if (!userCardIds.Contains(cardId))
                            {
                                response.StatusCode = StatusCodes.Forbidden;
                                response.Body = "At least one of the provided cards does not belong to the user or is not available.";
                                return response;
                            }
                        }

                        Deck deck = new Deck { UserId = user.UserId };
                        if (deck.ConfigureDeck(cardIds))
                        {
                            response.StatusCode = StatusCodes.Ok;
                            response.Body = "The deck has been successfully configured.";
                        }
                        else
                        {
                            response.StatusCode = StatusCodes.InternalServerError;
                            response.Body = "An error occurred while configuring the deck.";
                        }

                        return response;
                    }
                },
                new Route
                {
                    Path = "/users/{username}",
                    HttpMethod = "GET",
                    Action = async (request, parameters) =>
                    {
                        var response = new HttpResponse();

                        if (string.IsNullOrEmpty(request.Token))
                        {
                            response.StatusCode = StatusCodes.Unauthorized;
                            response.Body = "Unauthorized. Token is missing.";
                            return response;
                        }

                        User requestingUser = User.GetUserByToken(request.Token);
                        if (requestingUser == null)
                        {
                            response.StatusCode = StatusCodes.Unauthorized;
                            response.Body = "Invalid token.";
                            return response;
                        }

                        string username = parameters["username"];

                        User user = User.GetUserByUsername(username);
                        if (user == null)
                        {
                            response.StatusCode = StatusCodes.NotFound;
                            response.Body = "User not found.";
                            return response;
                        }

                        if (requestingUser.Username != "admin" && requestingUser.Username != username)
                        {
                            response.StatusCode = StatusCodes.Forbidden;
                            response.Body = "You are not authorized to view this user's data.";
                            return response;
                        }

                        UserStats stats = user.GetStats();
                        if (stats != null)
                        {
                            Console.WriteLine($"[Server] User Stats for {user.Username}: Elo={stats.Elo}, Wins={stats.Wins}, Losses={stats.Losses}, Draws={stats.Draws}");
                        }

                        var userData = new
                        {
                            User = user,
                            Stats = stats
                        };

                        response.StatusCode = StatusCodes.Ok;
                        response.Body = JsonConvert.SerializeObject(userData, Formatting.Indented);
                        return response;
                    }
                },
                new Route
                {
                    Path = "/users/{username}",
                    HttpMethod = "PUT",
                    Action = async (request, parameters) =>
                    {
                        var response = new HttpResponse();

                        if (string.IsNullOrEmpty(request.Token))
                        {
                            response.StatusCode = StatusCodes.Unauthorized;
                            response.Body = "Unauthorized. Token is missing.";
                            return response;
                        }

                        User requestingUser = User.GetUserByToken(request.Token);
                        if (requestingUser == null)
                        {
                            response.StatusCode = StatusCodes.Unauthorized;
                            response.Body = "Invalid token.";
                            return response;
                        }

                        string username = parameters["username"];
                        if (requestingUser.Username != "admin" && requestingUser.Username != username)
                        {
                            response.StatusCode = StatusCodes.Forbidden;
                            response.Body = "You are not authorized to update this user's data.";
                            return response;
                        }

                        User user = User.GetUserByUsername(username);
                        if (user == null)
                        {
                            response.StatusCode = StatusCodes.NotFound;
                            response.Body = "User not found.";
                            return response;
                        }

                        var updatedData = JsonConvert.DeserializeObject<User>(request.Body);
                        if (updatedData == null)
                        {
                            response.StatusCode = StatusCodes.BadRequest;
                            response.Body = "Invalid user data.";
                            return response;
                        }

                        if (user.UpdateUserData(updatedData.Name, updatedData.Bio, updatedData.Image))
                        {
                            response.StatusCode = StatusCodes.Ok;
                            response.Body = "User successfully updated.";
                        }
                        else
                        {
                            response.StatusCode = StatusCodes.InternalServerError;
                            response.Body = "An error occurred while updating the user data.";
                        }

                        return response;
                    }
                },
                new Route
                {
                    Path = "/stats",
                    HttpMethod = "GET",
                    Action = async (request, parameters) =>
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

                        UserStats stats = user.GetStats();

                        if (stats != null)
                        {
                            Console.WriteLine($"[Server] Retrieved stats for user: {user.Username}\n");

                            Console.WriteLine("{0,-10} {1,-10}", "Attribute", "Value");
                            Console.WriteLine(new string('-', 20));

                            Console.WriteLine("{0,-10} {1,-10}", "Elo", stats.Elo);
                            Console.WriteLine("{0,-10} {1,-10}", "Wins", stats.Wins);
                            Console.WriteLine("{0,-10} {1,-10}", "Losses", stats.Losses);
                            Console.WriteLine("{0,-10} {1,-10}", "Draws", stats.Draws);

                            response.StatusCode = StatusCodes.Ok;
                            response.Body = JsonConvert.SerializeObject(stats, Formatting.Indented);
                        }
                        else
                        {
                            response.StatusCode = StatusCodes.InternalServerError;
                            response.Body = "An error occurred while retrieving user stats.";
                        }

                        return response;
                    }
                },
                new Route
                {
                    Path = "/scoreboard",
                    HttpMethod = "GET",
                    Action = async (request, parameters) =>
                    {
                        var response = new HttpResponse();

                        if (string.IsNullOrEmpty(request.Token))
                        {
                            response.StatusCode = StatusCodes.Unauthorized;
                            response.Body = "Unauthorized. Token is missing.";
                            return response;
                        }

                        User requestingUser = User.GetUserByToken(request.Token);
                        if (requestingUser == null)
                        {
                            response.StatusCode = StatusCodes.Unauthorized;
                            response.Body = "Invalid token.";
                            return response;
                        }

                        List<ScoreboardEntry> scoreboard = UserStats.GetAllOrderedByElo();

                        if (scoreboard != null)
                        {
                            Console.WriteLine($"[Server] Retrieved scoreboard with {scoreboard.Count} entries.\n");

                            Console.WriteLine("{0,-20} {1,-5} {2,-5} {3,-7} {4,-5}", "Username", "Elo", "Wins", "Losses", "Draws");
                            Console.WriteLine(new string('-', 45));

                            foreach (var entry in scoreboard)
                            {
                                Console.WriteLine("{0,-20} {1,-5} {2,-5} {3,-7} {4,-5}",
                                    entry.Username, entry.Elo, entry.Wins, entry.Losses, entry.Draws);
                            }

                            response.StatusCode = StatusCodes.Ok;
                            response.Body = JsonConvert.SerializeObject(scoreboard, Formatting.Indented);
                        }
                        else
                        {
                            response.StatusCode = StatusCodes.InternalServerError;
                            response.Body = "An error occurred while retrieving the scoreboard.";
                        }

                        return response;
                    }
                },
                new Route
                {
                    Path = "/battles",
                    HttpMethod = "POST",
                    Action = async (request, parameters) =>
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

                        var deck = Deck.GetDeckByUserId(user.UserId);
                        if (deck == null || deck.Cards.Count < 4)
                        {
                            response.StatusCode = StatusCodes.BadRequest;
                            response.Body = "Deck is not properly configured. Please configure a deck with at least 4 cards.";
                            return response;
                        }

                        BattleManager.Instance.AddUserToLobby(user);

                        response.StatusCode = StatusCodes.Accepted;
                        response.Body = "You have been added to the battle queue.";
                        return response;
                    }
                },
                new Route
                {
                    Path = "/battles/results",
                    HttpMethod = "GET",
                    Action = async (request, parameters) =>
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

                        string battleResult = BattleManager.Instance.GetBattleResult(user.UserId);

                        if (!string.IsNullOrEmpty(battleResult))
                        {
                            response.StatusCode = StatusCodes.Ok;
                            response.Body = battleResult;
                        }
                        else
                        {
                            response.StatusCode = StatusCodes.NoContent;
                            response.Body = "No battle result available yet.";
                        }

                        return response;
                    }
                },
                new Route
                {
                    Path = "/tradings",
                    HttpMethod = "GET",
                    Action = async (request, parameters) =>
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

                        var tradingDeals = TradingDeal.GetAllTradingDeals();
                        if (tradingDeals.Count == 0)
                        {
                            response.StatusCode = StatusCodes.NoContent;
                        }
                        else
                        {
                            response.StatusCode = StatusCodes.Ok;
                            response.Body = JsonConvert.SerializeObject(tradingDeals);
                        }

                        return response;
                    }
                },
                new Route
                {
                    Path = "/tradings",
                    HttpMethod = "POST",
                    Action = async (request, parameters) =>
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

                        var tradingDeal = JsonConvert.DeserializeObject<TradingDeal>(request.Body);
                        if (tradingDeal == null)
                        {
                            response.StatusCode = StatusCodes.BadRequest;
                            response.Body = "Invalid trading deal data.";
                            return response;
                        }

                        if (!user.OwnsCard(tradingDeal.CardToTrade))
                        {
                            response.StatusCode = StatusCodes.Forbidden;
                            response.Body = "The deal contains a card that is not owned by the user or locked in the deck.";
                            return response;
                        }

                        tradingDeal.UserId = user.UserId;

                        if (TradingDeal.CreateTradingDeal(tradingDeal))
                        {
                            response.StatusCode = StatusCodes.Created;
                            response.Body = "Trading deal successfully created.";
                        }
                        else
                        {
                            response.StatusCode = StatusCodes.Conflict;
                            response.Body = "A deal with this deal ID already exists.";
                        }

                        return response;
                    }
                },
                new Route
                {
                    Path = "/tradings/{tradingdealid}",
                    HttpMethod = "DELETE",
                    Action = async (request, parameters) =>
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

                        if (!parameters.TryGetValue("tradingdealid", out var tradingDealIdStr) || !Guid.TryParse(tradingDealIdStr, out var tradingDealId))
                        {
                            response.StatusCode = StatusCodes.BadRequest;
                            response.Body = "Invalid trading deal ID.";
                            return response;
                        }

                        var tradingDeal = TradingDeal.GetTradingDealById(tradingDealId);
                        if (tradingDeal == null)
                        {
                            response.StatusCode = StatusCodes.NotFound;
                            response.Body = "The provided deal ID was not found.";
                            return response;
                        }

                        if (!user.OwnsCard(tradingDeal.CardToTrade))
                        {
                            response.StatusCode = StatusCodes.Forbidden;
                            response.Body = "The deal contains a card that is not owned by the user.";
                            return response;
                        }

                        if (TradingDeal.DeleteTradingDeal(tradingDealId))
                        {
                            response.StatusCode = StatusCodes.Ok;
                            response.Body = "Trading deal successfully deleted.";
                        }
                        else
                        {
                            response.StatusCode = StatusCodes.InternalServerError;
                            response.Body = "An error occurred while deleting the trading deal.";
                        }

                        return response;
                    }
                },
                new Route
                {
                    Path = "/tradings/{tradingdealid}",
                    HttpMethod = "POST",
                    Action = async (request, parameters) =>
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

                        if (!parameters.TryGetValue("tradingdealid", out var tradingDealIdStr) || !Guid.TryParse(tradingDealIdStr, out var tradingDealId))
                        {
                            response.StatusCode = StatusCodes.BadRequest;
                            response.Body = "Invalid trading deal ID.";
                            return response;
                        }

                        var offeredCardId = JsonConvert.DeserializeObject<Guid>(request.Body);
                        if (offeredCardId == Guid.Empty)
                        {
                            response.StatusCode = StatusCodes.BadRequest;
                            response.Body = "Invalid offered card ID.";
                            return response;
                        }

                        var tradingDeal = TradingDeal.GetTradingDealById(tradingDealId);
                        if (tradingDeal == null)
                        {
                            response.StatusCode = StatusCodes.NotFound;
                            response.Body = "The provided deal ID was not found.";
                            return response;
                        }

                        if (user.UserId == tradingDeal.UserId)
                        {
                            response.StatusCode = StatusCodes.Forbidden;
                            response.Body = "Trading with self is not allowed.";
                            return response;
                        }

                        if (!user.OwnsCard(offeredCardId))
                        {
                            response.StatusCode = StatusCodes.Forbidden;
                            response.Body = "The offered card is not owned by the user.";
                            return response;
                        }

                        if (!tradingDeal.MeetsRequirements(offeredCardId))
                        {
                            response.StatusCode = StatusCodes.Forbidden;
                            response.Body = "The offered card does not meet the requirements.";
                            return response;
                        }

                        if (TradingDeal.ExecuteTrade(tradingDealId, user.UserId, offeredCardId))
                        {
                            response.StatusCode = StatusCodes.Ok;
                            response.Body = "Trading deal successfully executed.";
                        }
                        else
                        {
                            response.StatusCode = StatusCodes.InternalServerError;
                            response.Body = "An error occurred while executing the trading deal.";
                        }

                        return response;
                    }
                }
            };

            return routes;
        }

        private static string GenerateToken(string username)
        {
            return $"{username}-mtcgToken";
        }

        private static bool TryGetQueryParameter(string path, string key, out string value)
        {
            value = null;
            if (string.IsNullOrEmpty(path))
                return false;

            int queryStart = path.IndexOf('?');
            if (queryStart == -1 || queryStart == path.Length - 1)
                return false;

            string queryString = path.Substring(queryStart + 1);
            var queryParams = queryString.Split('&', StringSplitOptions.RemoveEmptyEntries);

            foreach (var param in queryParams)
            {
                var kvp = param.Split('=', 2);
                if (kvp.Length == 2 && kvp[0].Equals(key, StringComparison.OrdinalIgnoreCase))
                {
                    value = Uri.UnescapeDataString(kvp[1]);
                    return true;
                }
            }
            return false;
        }

        private static string InferElementTypeFromName(string name)
        {
            if (name.ToLower().Contains("water"))
                return "Water";
            if (name.ToLower().Contains("fire"))
                return "Fire";
            if (name.ToLower().Contains("normal"))
                return "Normal";
            return "Normal";
        }
    }
}