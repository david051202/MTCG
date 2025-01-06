using System;
using MTCG.Classes;
using Npgsql;

namespace MTCG.Models
{
    public class UserStats
    {
        public int UserId { get; set; }
        public int Elo { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Draws { get; set; }
        public int GamesPlayed { get; set; }

        public static UserStats GetStatsByUserId(int userId)
        {
            try
            {
                using (var conn = DatabaseHelper.GetOpenConnection())
                {
                    var cmd = new NpgsqlCommand(
                        "SELECT elo, wins, losses, draws FROM users WHERE user_id = @user_id",
                        conn);
                    cmd.Parameters.AddWithValue("user_id", userId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new UserStats
                            {
                                UserId = userId,
                                Elo = reader.GetInt32(reader.GetOrdinal("elo")),
                                Wins = reader.GetInt32(reader.GetOrdinal("wins")),
                                Losses = reader.GetInt32(reader.GetOrdinal("losses")),
                                Draws = reader.GetInt32(reader.GetOrdinal("draws"))
                            };
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
                Console.WriteLine($"Error retrieving user stats: {ex.Message}");
                return null;
            }
        }

        public bool UpdateStats(int elo, int wins, int losses, int draws)
        {
            try
            {
                using (var conn = DatabaseHelper.GetOpenConnection())
                {
                    var cmd = new NpgsqlCommand(
                        "UPDATE users SET elo = @elo, wins = @wins, losses = @losses, draws = @draws WHERE user_id = @user_id",
                        conn);
                    cmd.Parameters.AddWithValue("elo", elo);
                    cmd.Parameters.AddWithValue("wins", wins);
                    cmd.Parameters.AddWithValue("losses", losses);
                    cmd.Parameters.AddWithValue("draws", draws);
                    cmd.Parameters.AddWithValue("user_id", UserId);

                    int rowsAffected = cmd.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating user stats: {ex.Message}");
                return false;
            }
        }

        public static List<ScoreboardEntry> GetAllOrderedByElo()
        {
            var scoreboard = new List<ScoreboardEntry>();
            try
            {
                using (var conn = DatabaseHelper.GetOpenConnection())
                {
                    var cmd = new NpgsqlCommand(
                        "SELECT u.username, u.elo, u.wins, u.losses, u.draws FROM users u ORDER BY u.elo DESC",
                        conn);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            scoreboard.Add(new ScoreboardEntry
                            {
                                Username = reader.GetString(reader.GetOrdinal("username")),
                                Elo = reader.GetInt32(reader.GetOrdinal("elo")),
                                Wins = reader.GetInt32(reader.GetOrdinal("wins")),
                                Losses = reader.GetInt32(reader.GetOrdinal("losses")),
                                Draws = reader.GetInt32(reader.GetOrdinal("draws"))
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving scoreboard: {ex.Message}");
                return null;
            }

            return scoreboard;
        }

    }
}
