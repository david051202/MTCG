using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using MTCG.Models;
using MTCG.Classes; // Namespace für BattleLogic

namespace MTCG.Battle
{
    public class BattleManager
    {
        private static readonly Lazy<BattleManager> _instance = new Lazy<BattleManager>(() => new BattleManager());
        public static BattleManager Instance => _instance.Value;

        private readonly ConcurrentQueue<User> _lobbyQueue = new ConcurrentQueue<User>();
        private readonly ConcurrentDictionary<int, string> _battleResults = new ConcurrentDictionary<int, string>();

        private BattleManager()
        {
            // Start the matchmaking loop
            Task.Run(() => MatchmakingLoop());
        }

        public void AddUserToLobby(User user)
        {
            Console.WriteLine($"[Lobby] User {user.Username} added to the battle lobby.");
            _lobbyQueue.Enqueue(user);
        }

        private async Task MatchmakingLoop()
        {
            while (true)
            {
                try
                {
                    if (_lobbyQueue.Count >= 2)
                    {
                        if (_lobbyQueue.TryDequeue(out var player1) && _lobbyQueue.TryDequeue(out var player2))
                        {
                            Console.WriteLine($"[Lobby] Starting battle between {player1.Username} and {player2.Username}.");
                            await StartBattleAsync(player1, player2);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Error] Exception in MatchmakingLoop: {ex.Message}");
                }

                await Task.Delay(1000);
            }
        }

        private async Task StartBattleAsync(User player1, User player2)
        {
            try
            {
                var battleLogic = new BattleLogic(player1, player2);
                var outcome = await battleLogic.StartBattleAsync();

                // Update Battle Results
                _battleResults[player1.UserId] = outcome.Log;
                _battleResults[player2.UserId] = outcome.Log;

                Console.WriteLine($"[Battle] Battle between {player1.Username} and {player2.Username} completed.");

                // Update User Stats based on Battle Outcome
                if (outcome.Winner != null && outcome.Loser != null)
                {
                    UpdateUserStats(outcome);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Battle] Error during battle: {ex.Message}");
                Console.WriteLine(ex.ToString());
            }
        }

        private void UpdateUserStats(BattleLogic.BattleOutcome outcome)
        {
            try
            {
                UserStats statsWinner = UserStats.GetStatsByUserId(outcome.Winner.UserId);
                UserStats statsLoser = UserStats.GetStatsByUserId(outcome.Loser.UserId);

                if (statsWinner == null || statsLoser == null)
                {
                    Console.WriteLine("[Error] Failed to retrieve user stats for ELO update.");
                    return;
                }

                // ELO Calculation Parameters
                const int K = 32;

                // Calculate Expected Scores
                double expectedWinner = 1.0 / (1.0 + Math.Pow(10, (statsLoser.Elo - statsWinner.Elo) / 400.0));
                double expectedLoser = 1.0 / (1.0 + Math.Pow(10, (statsWinner.Elo - statsLoser.Elo) / 400.0));

                // Actual Scores
                double scoreWinner = 1.0;
                double scoreLoser = 0.0;

                // Update ELO Ratings
                statsWinner.Elo += (int)Math.Round(K * (scoreWinner - expectedWinner));
                statsLoser.Elo += (int)Math.Round(K * (scoreLoser - expectedLoser));

                // Update Wins and Losses
                statsWinner.Wins += 1;
                statsLoser.Losses += 1;

                // Persist Updated Stats to Database
                bool winnerUpdateSuccess = statsWinner.UpdateStats(statsWinner.Elo, statsWinner.Wins, statsWinner.Losses, statsWinner.Draws);
                bool loserUpdateSuccess = statsLoser.UpdateStats(statsLoser.Elo, statsLoser.Wins, statsLoser.Losses, statsLoser.Draws);

                if (winnerUpdateSuccess && loserUpdateSuccess)
                {
                    Console.WriteLine($"[Stats] Updated stats for {outcome.Winner.Username} (Wins: {statsWinner.Wins}, Elo: {statsWinner.Elo}) and {outcome.Loser.Username} (Losses: {statsLoser.Losses}, Elo: {statsLoser.Elo})");
                }
                else
                {
                    Console.WriteLine("[Error] Failed to update user stats in the database.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] Exception during ELO update: {ex.Message}");
            }
        }

        public string GetBattleResult(int userId)
        {
            if (_battleResults.TryRemove(userId, out var result))
            {
                return result;
            }
            return null;
        }

        public Http.Route Route
        {
            get => default;
            set
            {
            }
        }
    }
}
