using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using MTCG.Models;

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
                BattleOutcome outcome = await battleLogic.StartBattleAsync();

                // Update Battle Results
                _battleResults[player1.UserId] = outcome.Log;
                _battleResults[player2.UserId] = outcome.Log;

                Console.WriteLine($"[Battle] Battle between {player1.Username} and {player2.Username} completed.");

                // Update User Stats based on Battle Outcome
                UpdateUserStats(outcome);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Battle] Error during battle: {ex.Message}");
                Console.WriteLine(ex.ToString());
            }
        }

        private void UpdateUserStats(BattleOutcome outcome)
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
    }

    // Assuming a BattleOutcome class exists
    public class BattleOutcome
    {
        public User Winner { get; set; }
        public User Loser { get; set; }
        public string Log { get; set; }
    }

    // Placeholder for BattleLogic class
    public class BattleLogic
    {
        private readonly User _player1;
        private readonly User _player2;
        private readonly Random _random;

        public BattleLogic(User player1, User player2)
        {
            _player1 = player1;
            _player2 = player2;
            _random = new Random();
        }

        public async Task<BattleOutcome> StartBattleAsync()
        {
            int maxRounds = _random.Next(1, 101); // 1 to 100 inclusive
            int currentRound = 0;

            // Simulate battle rounds
            while (currentRound < maxRounds)
            {
                currentRound++;
                Console.WriteLine($"[Battle] Round {currentRound} begins between {_player1.Username} and {_player2.Username}.");

                // Simulate battle logic here (e.g., card interactions)

                Console.WriteLine($"[Battle] End of Round {currentRound}");
            }

            // Determine winner randomly for demonstration
            bool player1Wins = _random.Next(0, 2) == 0;
            User winner = player1Wins ? _player1 : _player2;
            User loser = player1Wins ? _player2 : _player1;
            string battleLog = $"Battle between {winner.Username} and {loser.Username} completed. Winner: {winner.Username}.";

            Console.WriteLine($"[Battle] {battleLog}");

            return new BattleOutcome
            {
                Winner = winner,
                Loser = loser,
                Log = battleLog
            };
        }
    }
}
