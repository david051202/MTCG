using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MTCG.Models;

namespace MTCG.Classes
{
    public class BattleLogic
    {
        private User _player1;
        private User _player2;
        private StringBuilder _battleLog;

        private const int MaxRounds = 100;

        public BattleLogic(User player1, User player2)
        {
            _player1 = player1;
            _player2 = player2;
            _battleLog = new StringBuilder();
        }

        public async Task<string> StartBattleAsync()
        {
            _battleLog.AppendLine($"Battle started between {_player1.Username} and {_player2.Username}.\n");

            int round = 0;
            while (round < MaxRounds && _player1.Cards.Count > 0 && _player2.Cards.Count > 0)
            {
                round++;
                _battleLog.AppendLine($"--- Round {round} ---");

                ApplyBooster();

                var card1 = GetRandomCard(_player1);
                var card2 = GetRandomCard(_player2);

                if (card1 == null || card2 == null)
                {
                    break;
                }

                int damage1 = CalculateDamage(card1, card2);
                int damage2 = CalculateDamage(card2, card1);

                _battleLog.AppendLine($"{_player1.Username} plays {card1.Name} with damage {damage1}.");
                _battleLog.AppendLine($"{_player2.Username} plays {card2.Name} with damage {damage2}.");

                if (damage1 > damage2)
                {
                    _battleLog.AppendLine($"{_player1.Username}'s {card1.Name} wins the round.");
                    _player2.RemoveCard(card2);
                    _player1.AddCard(card2);
                }
                else if (damage2 > damage1)
                {
                    _battleLog.AppendLine($"{_player2.Username}'s {card2.Name} wins the round.");
                    _player1.RemoveCard(card1);
                    _player2.AddCard(card1);
                }
                else
                {
                    _battleLog.AppendLine("Round is a draw. No cards are moved.");
                }

                await Task.Delay(100); // Simulate async operation
            }

            if (round >= MaxRounds)
            {
                _battleLog.AppendLine("Battle ended in a draw due to round limit.");
                // ELO remains unchanged
            }
            else
            {
                string winner = _player1.Cards.Count > _player2.Cards.Count ? _player1.Username : _player2.Username;
                _battleLog.AppendLine($"Battle concluded. Winner: {winner}");
                UpdateStats(winner);
            }

            return _battleLog.ToString();
        }

        private Card GetRandomCard(User user)
        {
            var random = new Random();
            int index = random.Next(user.Cards.Count);
            return user.Cards[index];
        }

        private int CalculateDamage(Card attacker, Card defender)
        {
            int baseDamage = attacker.Damage;

            if (attacker.CardType == "spell")
            {
                // Check element effectiveness
                if (IsEffective(attacker.ElementType, defender.ElementType))
                {
                    baseDamage *= 2;
                }
                else if (IsNotEffective(attacker.ElementType, defender.ElementType))
                {
                    baseDamage /= 2;
                }
            }

            // Apply specialties
            baseDamage = ApplySpecialties(attacker, defender, baseDamage);

            return baseDamage;
        }

        private bool IsEffective(string attackerElement, string defenderElement)
        {
            return (attackerElement == "water" && defenderElement == "fire") ||
                   (attackerElement == "fire" && defenderElement == "normal") ||
                   (attackerElement == "normal" && defenderElement == "water");
        }

        private bool IsNotEffective(string attackerElement, string defenderElement)
        {
            return (attackerElement == "fire" && defenderElement == "water") ||
                   (attackerElement == "normal" && defenderElement == "fire") ||
                   (attackerElement == "water" && defenderElement == "normal");
        }

        private int ApplySpecialties(Card attacker, Card defender, int damage)
        {
            // Implement specialties
            if (attacker.Name.Equals("Goblin", StringComparison.OrdinalIgnoreCase) && defender.Name.Equals("Dragon", StringComparison.OrdinalIgnoreCase))
            {
                _battleLog.AppendLine("Goblin is too afraid of Dragon and cannot attack.");
                return 0;
            }
            if (attacker.Name.Equals("Wizard", StringComparison.OrdinalIgnoreCase) && defender.Name.Equals("Ork", StringComparison.OrdinalIgnoreCase))
            {
                _battleLog.AppendLine("Wizard controls Ork. Ork cannot damage Wizard.");
                return damage;
            }
            if (defender.Name.Equals("Knight", StringComparison.OrdinalIgnoreCase) && attacker.Name.Equals("WaterSpell", StringComparison.OrdinalIgnoreCase))
            {
                _battleLog.AppendLine("Knight's heavy armor is drowned by WaterSpell instantly.");
                return int.MaxValue;
            }
            if (defender.Name.Equals("Kraken", StringComparison.OrdinalIgnoreCase) && attacker.CardType == "spell")
            {
                _battleLog.AppendLine("Kraken is immune against spells.");
                return 0;
            }
            if (attacker.Name.Equals("FireElf", StringComparison.OrdinalIgnoreCase) && defender.Name.Equals("Dragon", StringComparison.OrdinalIgnoreCase))
            {
                _battleLog.AppendLine("FireElf evades Dragon's attack.");
                return 0;
            }

            return damage;
        }

        private void UpdateStats(string winnerUsername)
        {
            User winner = User.GetUserByUsername(winnerUsername);
            User loser = winnerUsername == _player1.Username ? _player2 : _player1;

            winner.Stats.GamesPlayed += 1;
            loser.Stats.GamesPlayed += 1;

            // Simple ELO calculation example
            int kFactor = 32;
            double expectedWinner = 1;
            double expectedLoser = 0;

            winner.Stats.Elo += (int)(kFactor * (1 - expectedWinner));
            loser.Stats.Elo += (int)(kFactor * (0 - expectedLoser));

            Console.WriteLine($"[Stats] {winner.Username} Elo: {winner.Stats.Elo}, {loser.Username} Elo: {loser.Stats.Elo}");
        }

        // Unique Feature: Booster that doubles the damage of a random card for one round
        private void ApplyBooster()
        {
            var random = new Random();
            if (_player1.Cards.Count > 0 && _player2.Cards.Count > 0)
            {
                int playerIndex = random.Next(2);
                User selectedPlayer = playerIndex == 0 ? _player1 : _player2;
                int cardIndex = random.Next(selectedPlayer.Cards.Count);
                selectedPlayer.Cards[cardIndex].Damage *= 2;
                _battleLog.AppendLine($"{selectedPlayer.Username} received a booster! {selectedPlayer.Cards[cardIndex].Name}'s damage is doubled for this round.");
            }
        }
    }
}
