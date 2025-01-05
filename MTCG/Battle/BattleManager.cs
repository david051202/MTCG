using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using MTCG.Models;

namespace MTCG.Classes
{
    public class BattleManager
    {
        private static readonly Lazy<BattleManager> _instance = new(() => new BattleManager());
        public static BattleManager Instance => _instance.Value;

        private readonly ConcurrentQueue<User> _lobby;

        private BattleManager()
        {
            _lobby = new ConcurrentQueue<User>();
        }

        public async Task<string> InitiateBattleAsync(User user)
        {
            Console.WriteLine($"[Lobby] User {user.Username} is entering the battle lobby.");

            if (_lobby.TryDequeue(out User opponent))
            {
                Console.WriteLine($"[Lobby] Opponent found: {opponent.Username}. Starting battle.");
                var battle = new BattleLogic(user, opponent);
                string log = await battle.StartBattleAsync();
                return log;
            }
            else
            {
                _lobby.Enqueue(user);
                Console.WriteLine($"[Lobby] No opponent found. User {user.Username} is waiting in the lobby.");
                return "Waiting for an opponent to join the battle.";
            }
        }
    }
}
