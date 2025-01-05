using System;
using System.Collections.Generic;
using Npgsql;

namespace MTCG.Classes
{
    public class User
    {
        public int UserId { get; set; } // Geändert von string ID zu int UserId für Konsistenz mit der Datenbank
        public string Username { get; set; }
        public string Password { get; set; }
        public string Token { get; set; }
        public int Coins { get; set; }
        public List<Card> Cards { get; set; } // Zur Verwaltung der Benutzerkarten

        public User()
        {
            Cards = new List<Card>();
            Coins = 20;
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

                    var cmd = new NpgsqlCommand("INSERT INTO users (username, password, coins) VALUES (@username, @password, @coins)", conn);
                    cmd.Parameters.AddWithValue("username", Username);
                    cmd.Parameters.AddWithValue("password", Password);
                    cmd.Parameters.AddWithValue("coins", Coins);

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
                    var cmd = new NpgsqlCommand("SELECT user_id, token, coins FROM users WHERE username = @username AND password = @password", conn);
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
                                Coins = reader.GetInt32(reader.GetOrdinal("coins"))
                            };

                            // Generiere einen neuen Token, falls keiner existiert
                            if (string.IsNullOrEmpty(user.Token))
                            {
                                user.Token = GenerateToken(username);
                            }

                            reader.Close(); // Reader schließen, bevor neuer Befehl ausgeführt wird

                            var updateCmd = new NpgsqlCommand("UPDATE users SET token = @token WHERE user_id = @user_id", conn);
                            updateCmd.Parameters.AddWithValue("token", user.Token);
                            updateCmd.Parameters.AddWithValue("user_id", user.UserId);
                            updateCmd.ExecuteNonQuery();

                            // Laden der Karten des Benutzers
                            user.Cards = user.GetUserCards(conn);

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

                    var cmd = new NpgsqlCommand("SELECT user_id, username, coins FROM users WHERE token = @token", conn);
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
                                Token = token
                            };

                            Console.WriteLine($"[Server] User found: {user.Username}");

                            reader.Close();

                            // Laden der Karten des Benutzers
                            user.Cards = user.GetUserCards(conn);

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

        private List<Card> GetUserCards(NpgsqlConnection conn)
        {
            var cards = new List<Card>();

            var cmd = new NpgsqlCommand(
                @"SELECT c.card_id, c.name, c.damage, c.element_type, c.card_type
                  FROM usercards uc
                  JOIN cards c ON uc.card_id = c.card_id
                  WHERE uc.user_id = @user_id", conn);
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

            return cards;
        }

        private static string GenerateToken(string username)
        {
            return $"{username}-mtcgToken";
        }
    }
}
