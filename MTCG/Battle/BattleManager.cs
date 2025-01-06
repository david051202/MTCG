using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using MTCG.Models;

namespace MTCG.Classes
{
    public class BattleManager
    {
        private static readonly Lazy<BattleManager> _instance = new Lazy<BattleManager>(() => new BattleManager());
        public static BattleManager Instance => _instance.Value;

        private readonly ConcurrentQueue<User> _lobbyQueue = new ConcurrentQueue<User>();

        private BattleManager() { }

        public async Task<string> InitiateBattleAsync(User user)
        {
            Console.WriteLine($"[Lobby] User {user.Username} is entering the battle lobby.");

            _lobbyQueue.Enqueue(user);

            while (true)
            {
                if (_lobbyQueue.TryDequeue(out var opponent))
                {
                    if (opponent.UserId != user.UserId)
                    {
                        Console.WriteLine($"[Lobby] Opponent found: {opponent.Username}. Starting battle.");
                        return await StartBattleAsync(user, opponent);
                    }
                    else
                    {
                        _lobbyQueue.Enqueue(opponent);
                    }
                }

                await Task.Delay(1000); // Wait for 1 second before checking again
            }
        }

        private async Task<string> StartBattleAsync(User player1, User player2)
        {
            try
            {
                var battleLogic = new BattleLogic(player1, player2);
                string battleLog = await battleLogic.StartBattleAsync();
                Console.WriteLine($"[Battle] Battle between {player1.Username} and {player2.Username} completed.");
                return battleLog;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Battle] Error during battle: {ex.Message}");
                return $"Error during battle: {ex.Message}";
            }
        }
    }
}
