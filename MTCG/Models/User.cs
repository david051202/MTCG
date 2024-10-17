using System;
using System.Collections.Generic;

namespace MTCG.Classes
{
    public class User
    {
        public string ID { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Token { get; set; }
        public List<Cards> Cards { get; set; }
        public int Elo { get; set; }
        public int Coins { get; set; }

        public Http.HttpServer HttpServer
        {
            get => default;
            set
            {
            }
        }

        // Temporäre Speicherung der Benutzer in einer In-Memory-Liste
        private static List<User> users = new List<User>();

        public User(string username, string password)
        {
            Username = username;
            Password = password;
            Token = null;  // Token wird erst beim Login erstellt
            Cards = new List<Cards>();
            Elo = 100;
            Coins = 20;
        }

        public bool CreateUser()
        {
            try
            {
                if (IsUserInMemory())
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine("User already exists.");
                    Console.ResetColor();
                    return false;
                }

                users.Add(this);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("User successfully created.");
                Console.ResetColor();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during user creation: {ex.Message}");
            }

            return false;
        }

        // Methode zum Überprüfen, ob der Benutzer bereits in der In-Memory-Liste existiert
        private bool IsUserInMemory()
        {
            return users.Exists(u => u.Username.Equals(this.Username, StringComparison.OrdinalIgnoreCase));
        }

        // Login-Methode, die einen Token generiert und zurückgibt, falls der Login erfolgreich ist
        public static User Login(string username, string password)
        {
            var user = users.Find(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase) && u.Password == password);
            if (user != null)
            {
                // Generiere Token nur bei erfolgreichem Login
                user.Token = Guid.NewGuid().ToString();

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

        // Methode zum Abrufen eines Benutzers anhand seines Tokens
        public static User GetUserByToken(string token)
        {
            return users.Find(u => u.Token == token);
        }
    }
}
