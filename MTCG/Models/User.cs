using System;
using System.Collections.Generic;
using Npgsql;
using MTCG.Classes;

namespace MTCG.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Token { get; set; }
        public int Coins { get; set; }
        public int Elo { get; set; }
        public string Name { get; set; }
        public string Bio { get; set; }
        public string Image { get; set; }
        public List<Card> Cards { get; set; }

        public User()
        {
            Cards = new List<Card>();
            Coins = 20;
            Elo = 100;
        }

        public User(string username, string password) : this()
        {
            Username = username;
            Password = password;
        }

        public bool CreateUser()
        {
            try
            {
                using (var conn = DatabaseHelper.GetOpenConnection())
                {
                    if (IsUserInDatabase(conn))
                    {
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        Console.WriteLine("User already exists.");
                        Console.ResetColor();
                        return false;
                    }

                    var cmd = new NpgsqlCommand(
                        "INSERT INTO users (username, password, coins, elo, wins, losses, draws) VALUES (@username, @password, @coins, @elo, @wins, @losses, @draws)",
                        conn);
                    cmd.Parameters.AddWithValue("username", Username);
                    cmd.Parameters.AddWithValue("password", Password);
                    cmd.Parameters.AddWithValue("coins", Coins);
                    cmd.Parameters.AddWithValue("elo", Elo);
                    cmd.Parameters.AddWithValue("wins", 0);
                    cmd.Parameters.AddWithValue("losses", 0);
                    cmd.Parameters.AddWithValue("draws", 0);

                    cmd.ExecuteNonQuery();

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("User successfully created.");
                    Console.ResetColor();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during user creation: {ex.Message}");
                return false;
            }
        }

        private bool IsUserInDatabase(NpgsqlConnection conn)
        {
            var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM users WHERE username = @username", conn);
            cmd.Parameters.AddWithValue("username", Username);

            var count = (long)cmd.ExecuteScalar();
            return count > 0;
        }

        public static User Login(string username, string password)
        {
            try
            {
                using (var conn = DatabaseHelper.GetOpenConnection())
                {
                    var cmd = new NpgsqlCommand(
                        "SELECT user_id, token, coins, elo FROM users WHERE username = @username AND password = @password",
                        conn);
                    cmd.Parameters.AddWithValue("username", username);
                    cmd.Parameters.AddWithValue("password", password);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var user = new User(username, password)
                            {
                                UserId = reader.GetInt32(reader.GetOrdinal("user_id")),
                                Token = reader["token"] as string,
                                Coins = reader.GetInt32(reader.GetOrdinal("coins")),
                                Elo = reader.GetInt32(reader.GetOrdinal("elo"))
                            };

                            // Generate a new token if none exists
                            if (string.IsNullOrEmpty(user.Token))
                            {
                                user.Token = GenerateToken(username);
                            }

                            reader.Close(); // Close reader before executing a new command

                            using (var updateConn = DatabaseHelper.GetOpenConnection())
                            {
                                var updateCmd = new NpgsqlCommand(
                                    "UPDATE users SET token = @token WHERE user_id = @user_id",
                                    updateConn);
                                updateCmd.Parameters.AddWithValue("token", user.Token);
                                updateCmd.Parameters.AddWithValue("user_id", user.UserId);
                                updateCmd.ExecuteNonQuery();
                            }

                            // Load user's cards
                            user.Cards = user.GetUserCards();

                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("Login successful. Token generated: " + user.Token);
                            Console.ResetColor();

                            return user;
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.DarkRed;
                            Console.WriteLine("Invalid username or password.");
                            Console.ResetColor();
                            return null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during login: {ex.Message}");
                return null;
            }
        }

        public static User GetUserByToken(string token)
        {
            try
            {
                using (var conn = DatabaseHelper.GetOpenConnection())
                {
                    Console.WriteLine($"[Server] Searching for user with token: {token}");

                    var cmd = new NpgsqlCommand(
                        "SELECT user_id, username, coins, elo, name, bio, image FROM users WHERE token = @token",
                        conn);
                    cmd.Parameters.AddWithValue("token", token);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var user = new User
                            {
                                UserId = reader.GetInt32(reader.GetOrdinal("user_id")),
                                Username = reader.GetString(reader.GetOrdinal("username")),
                                Coins = reader.GetInt32(reader.GetOrdinal("coins")),
                                Elo = reader.GetInt32(reader.GetOrdinal("elo")),
                                Name = reader["name"] as string,
                                Bio = reader["bio"] as string,
                                Image = reader["image"] as string,
                                Token = token
                            };

                            Console.WriteLine($"[Server] User found: {user.Username}");

                            // Load user's cards
                            user.Cards = user.GetUserCards();

                            return user;
                        }
                        else
                        {
                            Console.WriteLine("[Server] No user found with the given token.");
                            return null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during token retrieval: {ex.Message}");
                return null;
            }
        }

        public static User GetUserByUsername(string username)
        {
            try
            {
                using (var conn = DatabaseHelper.GetOpenConnection())
                {
                    var cmd = new NpgsqlCommand(
                        "SELECT user_id, username, coins, elo, name, bio, image FROM users WHERE username = @username",
                        conn);
                    cmd.Parameters.AddWithValue("username", username);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var user = new User
                            {
                                UserId = reader.GetInt32(reader.GetOrdinal("user_id")),
                                Username = reader.GetString(reader.GetOrdinal("username")),
                                Coins = reader.GetInt32(reader.GetOrdinal("coins")),
                                Elo = reader.GetInt32(reader.GetOrdinal("elo")),
                                Name = reader["name"] as string,
                                Bio = reader["bio"] as string,
                                Image = reader["image"] as string
                            };

                            // Load user's cards
                            user.Cards = user.GetUserCards();

                            return user;
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving user by username: {ex.Message}");
                return null;
            }
        }

        public UserStats GetStats()
        {
            return UserStats.GetStatsByUserId(this.UserId);
        }

        public bool UpdateUserData(string name, string bio, string image)
        {
            try
            {
                using (var conn = DatabaseHelper.GetOpenConnection())
                {
                    var cmd = new NpgsqlCommand(
                        "UPDATE users SET name = @name, bio = @bio, image = @image WHERE user_id = @user_id",
                        conn);
                    cmd.Parameters.AddWithValue("name", name);
                    cmd.Parameters.AddWithValue("bio", bio);
                    cmd.Parameters.AddWithValue("image", image);
                    cmd.Parameters.AddWithValue("user_id", UserId);

                    cmd.ExecuteNonQuery();

                    Name = name;
                    Bio = bio;
                    Image = image;

                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating user data: {ex.Message}");
                return false;
            }
        }

        private List<Card> GetUserCards()
        {
            var cards = new List<Card>();

            using (var conn = DatabaseHelper.GetOpenConnection())
            {
                var cmd = new NpgsqlCommand(
                    @"SELECT c.card_id, c.name, c.damage, c.element_type, c.card_type
                      FROM usercards uc
                      JOIN cards c ON uc.card_id = c.card_id
                      WHERE uc.user_id = @user_id",
                    conn);
                cmd.Parameters.AddWithValue("user_id", UserId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var card = new Card
                        {
                            Id = reader.GetGuid(reader.GetOrdinal("card_id")),
                            Name = reader.GetString(reader.GetOrdinal("name")),
                            Damage = reader.GetDouble(reader.GetOrdinal("damage")),
                            ElementType = reader.GetString(reader.GetOrdinal("element_type")),
                            CardType = reader.GetString(reader.GetOrdinal("card_type"))
                        };
                        cards.Add(card);
                    }
                }
            }

            return cards;
        }

        private static string GenerateToken(string username)
        {
            return $"{username}-mtcgToken";
        }
    }
}
