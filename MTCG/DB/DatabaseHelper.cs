using Npgsql;

namespace MTCG.Classes
{
    public static class DatabaseHelper
    {
        private static string connectionString = "Host=localhost;Username=postgres;Password=postgres;Database=mtcgdb";

        public static Models.User User
        {
            get => default;
            set
            {
            }
        }

        public static Card Card
        {
            get => default;
            set
            {
            }
        }

        public static NpgsqlConnection GetOpenConnection()
        {
            var conn = new NpgsqlConnection(connectionString);
            conn.Open();
            return conn;
        }
    }
}

