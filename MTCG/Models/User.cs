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

        // Temporäre Speicherung der Benutzer in einer In-Memory-Liste
        private static List<User> users = new List<User>();

        public User(string username, string password)
        {
            Username = username;
            Password = password;
            Token = Guid.NewGuid().ToString();
            Cards = new List<Cards>();
            Elo = 100;
            Coins = 20;
        }

        // Methode zum Erstellen eines neuen Benutzers
        public bool CreateUser()
        {
            try
            {
                if (IsUserInMemory())
                {
                    Console.WriteLine("User already exists.");
                    return false;
                }



                users.Add(this);

                Console.WriteLine("User successfully created.");
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

        public static User Login(string username, string password)
        {
            var user = users.Find(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase) && u.Password == password);
            if (user != null)
            {
                Console.WriteLine("Login successful.");
                return user;
            }
            else
            {
                Console.WriteLine("Invalid username or password.");
                return null;
            }
        }
    }
}