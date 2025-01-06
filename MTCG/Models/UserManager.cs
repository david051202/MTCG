using System;
using System.Collections.Concurrent;
using MTCG.Models;
using Npgsql;

namespace MTCG.Classes
{
    public static class UserManager
    {
        private static readonly ConcurrentDictionary<int, User> _usersById = new ConcurrentDictionary<int, User>();
        private static readonly ConcurrentDictionary<string, User> _usersByToken = new ConcurrentDictionary<string, User>();

        public static User User
        {
            get => default;
            set
            {
            }
        }

        /// <summary>
        /// Retrieves a user by their token. Utilizes caching for consistency.
        /// </summary>
        /// <param name="conn">An open NpgsqlConnection.</param>
        /// <param name="token">The user's token.</param>
        /// <returns>The User object if found; otherwise, null.</returns>
        public static User GetUserByToken(NpgsqlConnection conn, string token)
        {
            if (_usersByToken.TryGetValue(token, out var cachedUser))
            {
                return cachedUser;
            }

            var user = RetrieveUserByToken(conn, token);
            if (user != null)
            {
                _usersById[user.UserId] = user;
                _usersByToken[user.Token] = user;
            }

            return user;
        }

        /// <summary>
        /// Retrieves a user by their username. Utilizes caching for consistency.
        /// </summary>
        /// <param name="conn">An open NpgsqlConnection.</param>
        /// <param name="username">The user's username.</param>
        /// <returns>The User object if found; otherwise, null.</returns>
        public static User GetUserByUsername(NpgsqlConnection conn, string username)
        {
            foreach (var user in _usersById.Values)
            {
                if (user.Username.Equals(username, StringComparison.OrdinalIgnoreCase))
                {
                    return user;
                }
            }

            var retrievedUser = RetrieveUserByUsername(conn, username);
            if (retrievedUser != null)
            {
                _usersById[retrievedUser.UserId] = retrievedUser;
                _usersByToken[retrievedUser.Token] = retrievedUser;
            }

            return retrievedUser;
        }

        /// <summary>
        /// Checks if a user exists in the database by username.
        /// </summary>
        /// <param name="conn">An open NpgsqlConnection.</param>
        /// <param name="username">The username to check.</param>
        /// <returns>True if the user exists; otherwise, false.</returns>
        public static bool IsUserInDatabase(NpgsqlConnection conn, string username)
        {
            var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM users WHERE username = @username", conn);
            cmd.Parameters.AddWithValue("username", username);

            var count = (long)cmd.ExecuteScalar();
            return count > 0;
        }

        /// <summary>
        /// Retrieves a user by their credentials (username and password).
        /// </summary>
        /// <param name="conn">An open NpgsqlConnection.</param>
        /// <param name="username">The user's username.</param>
        /// <param name="password">The user's password.</param>
        /// <returns>The User object if credentials are valid; otherwise, null.</returns>
        public static User GetUserByCredentials(NpgsqlConnection conn, string username, string password)
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

                    return user;
                }
            }

            return null;
        }

        /// <summary>
        /// Updates the user's token in the database and cache.
        /// </summary>
        /// <param name="conn">An open NpgsqlConnection.</param>
        /// <param name="userId">The user's ID.</param>
        /// <param name="newToken">The new token to assign.</param>
        public static void UpdateUserToken(NpgsqlConnection conn, int userId, string newToken)
        {
            var cmd = new NpgsqlCommand(
                "UPDATE users SET token = @token WHERE user_id = @user_id",
                conn);
            cmd.Parameters.AddWithValue("token", newToken);
            cmd.Parameters.AddWithValue("user_id", userId);
            cmd.ExecuteNonQuery();

            if (_usersById.TryGetValue(userId, out var user))
            {
                // Remove old token entry if exists
                foreach (var kvp in _usersByToken)
                {
                    if (kvp.Value.UserId == userId)
                    {
                        _usersByToken.TryRemove(kvp.Key, out _);
                        break;
                    }
                }

                user.Token = newToken;
                _usersByToken[newToken] = user;
            }
        }

        /// <summary>
        /// Retrieves a user from the database by token.
        /// </summary>
        /// <param name="conn">An open NpgsqlConnection.</param>
        /// <param name="token">The user's token.</param>
        /// <returns>The User object if found; otherwise, null.</returns>
        private static User RetrieveUserByToken(NpgsqlConnection conn, string token)
        {
            var cmd = new NpgsqlCommand(
                "SELECT username, password, coins, elo FROM users WHERE token = @token",
                conn);
            cmd.Parameters.AddWithValue("token", token);

            using (var reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    var username = reader.GetString(reader.GetOrdinal("username"));
                    var password = reader.GetString(reader.GetOrdinal("password"));
                    var user = new User(username, password)
                    {
                        Token = token,
                        Coins = reader.GetInt32(reader.GetOrdinal("coins")),
                        Elo = reader.GetInt32(reader.GetOrdinal("elo"))
                    };
                    return user;
                }
            }

            return null;
        }

        /// <summary>
        /// Retrieves a user from the database by username.
        /// </summary>
        /// <param name="conn">An open NpgsqlConnection.</param>
        /// <param name="username">The user's username.</param>
        /// <returns>The User object if found; otherwise, null.</returns>
        private static User RetrieveUserByUsername(NpgsqlConnection conn, string username)
        {
            var cmd = new NpgsqlCommand(
                "SELECT user_id, token, coins, elo FROM users WHERE username = @username",
                conn);
            cmd.Parameters.AddWithValue("username", username);

            using (var reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    var user = new User(username, reader.GetString(reader.GetOrdinal("password")))
                    {
                        UserId = reader.GetInt32(reader.GetOrdinal("user_id")),
                        Token = reader["token"] as string,
                        Coins = reader.GetInt32(reader.GetOrdinal("coins")),
                        Elo = reader.GetInt32(reader.GetOrdinal("elo"))
                    };
                    return user;
                }
            }

            return null;
        }
    }
}
