using System;
using System.Collections.Generic;
using Npgsql;

namespace MTCG.Classes
{
    public class User
    {
        public string ID { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Token { get; set; }
        public int Elo { get; set; }
        public int Coins { get; set; }

        public User(string username, string password)
        {
            Username = username;
            Password = password;
            Token = null;
            Elo = 100;
            Coins = 20;
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
            }

            return false;
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
                    var cmd = new NpgsqlCommand("SELECT * FROM users WHERE username = @username AND password = @password", conn);
                    cmd.Parameters.AddWithValue("username", username);
                    cmd.Parameters.AddWithValue("password", password);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var user = new User(username, password)
                            {
                                ID = reader["user_id"].ToString(),
                                Token = reader["token"].ToString(),
                                Coins = (int)reader["coins"]
                            };

                            user.Token = username + "-mtcgToken";

                            reader.Close(); // Schließe den Reader, bevor ein neuer Befehl ausgeführt wird

                            var updateCmd = new NpgsqlCommand("UPDATE users SET token = @token WHERE user_id = @user_id", conn);
                            updateCmd.Parameters.AddWithValue("token", user.Token);
                            updateCmd.Parameters.AddWithValue("user_id", int.Parse(user.ID));
                            updateCmd.ExecuteNonQuery();

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

                    var cmd = new NpgsqlCommand("SELECT user_id, username, token, coins FROM users WHERE token = @token", conn);
                    cmd.Parameters.AddWithValue("token", token);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            Console.WriteLine($"[Server] User found: {reader["username"]}");

                            return new User(reader["username"].ToString(), null)
                            {
                                ID = reader["user_id"].ToString(),
                                Token = reader["token"].ToString(),
                                Coins = (int)reader["coins"]
                            };
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
    }
}


