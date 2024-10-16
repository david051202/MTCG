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

        public User(string username, string password)
        {
            Username = username;
            Password = password;
            Token = Guid.NewGuid().ToString();
            Cards = new List<Cards>();
            Elo = 100;
            Coins = 20;
        }
    }
}
