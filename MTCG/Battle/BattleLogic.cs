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

        private const int MaxRounds = 4; // Limit to 4 rounds

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
                Console.WriteLine($"[Battle] Round {round} begins between {_player1.Username} and {_player2.Username}.");

                ApplyBooster();

                var card1 = GetRandomCard(_player1);
                var card2 = GetRandomCard(_player2);

                if (card1 == null || card2 == null)
                {
                    _battleLog.AppendLine("A player has no cards left. Ending battle.");
                    break;
                }

                // Display cards and their damage
                _battleLog.AppendLine($"{_player1.Username} plays {card1.Name} ({card1.ElementType} {card1.CardType}) with {card1.Damage} damage.");
                _battleLog.AppendLine($"{_player2.Username} plays {card2.Name} ({card2.ElementType} {card2.CardType}) with {card2.Damage} damage.");

                int damage1 = CalculateDamage(card1, card2);
                int damage2 = CalculateDamage(card2, card1);

                // Display calculated damages
                _battleLog.AppendLine($"{_player1.Username}'s {card1.Name} attacks with effective damage {damage1}.");
                _battleLog.AppendLine($"{_player2.Username}'s {card2.Name} attacks with effective damage {damage2}.");

                Console.WriteLine($"[Battle] {card1.Name} ({damage1} dmg) vs {card2.Name} ({damage2} dmg)");

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
                Console.WriteLine($"[Battle] End of Round {round}");

                // Check if any player has run out of cards
                if (_player1.Cards.Count == 0 || _player2.Cards.Count == 0)
                {
                    break;
                }

                await Task.Delay(100); // Simulate async operation
            }

            // Determine the winner
            if (_player1.Cards.Count == 0 && _player2.Cards.Count == 0)
            {
                _battleLog.AppendLine("Battle ended in a draw. Both players have no remaining cards.");
            }
            else if (_player1.Cards.Count == 0)
            {
                _battleLog.AppendLine($"\nBattle concluded. Winner: {_player2.Username}");
                UpdateStats(_player2.Username);
            }
            else if (_player2.Cards.Count == 0)
            {
                _battleLog.AppendLine($"\nBattle concluded. Winner: {_player1.Username}");
                UpdateStats(_player1.Username);
            }
            else
            {
                // Battle ended due to round limit
                string winner = _player1.Cards.Count > _player2.Cards.Count ? _player1.Username :
                                _player2.Cards.Count > _player1.Cards.Count ? _player2.Username : null;

                if (winner != null)
                {
                    _battleLog.AppendLine($"\nBattle concluded. Winner based on remaining cards: {winner}");
                    UpdateStats(winner);
                }
                else
                {
                    _battleLog.AppendLine("\nBattle ended in a draw due to round limit.");
                    UpdateDraws();
                }
            }
            Console.WriteLine($"[Battle] Battle between {_player1.Username} and {_player2.Username} completed.");
            return _battleLog.ToString();
        }

        private Card GetRandomCard(User user)
        {
            try
            {
                if (user.Cards == null || user.Cards.Count == 0)
                    return null;

                int index = _random.Next(user.Cards.Count);
                return user.Cards[index];
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] Exception in GetRandomCard: {ex.Message}");
                return null;
            }
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

        private const int MaxDamage = 1000; // Define a reasonable maximum damage

        private int CalculateDamage(Card attacker, Card defender)
        {
            int baseDamage = (int)Math.Clamp(attacker.Damage, 0, MaxDamage); // Clamp damage to [0, MaxDamage]

            bool isSpell = attacker.CardType.Equals("spell", StringComparison.OrdinalIgnoreCase);

            if (isSpell)
            {
                // Check element effectiveness
                if (IsEffective(attacker.ElementType, defender.ElementType))
                {
                    _battleLog.AppendLine($"{attacker.ElementType} {attacker.CardType} is effective against {defender.ElementType} {defender.CardType}. Damage doubled.");
                    baseDamage *= 2;
                }
                else if (IsNotEffective(attacker.ElementType, defender.ElementType))
                {
                    _battleLog.AppendLine($"{attacker.ElementType} {attacker.CardType} is not effective against {defender.ElementType} {defender.CardType}. Damage halved.");
                    baseDamage /= 2;
                }
            }

            // Apply specialties
            baseDamage = ApplySpecialties(attacker, defender, baseDamage);

            // Ensure damage does not exceed MaxDamage after specialties
            baseDamage = Math.Clamp(baseDamage, 0, MaxDamage);

            return baseDamage;
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
                return MaxDamage; // Wizard wins
            }
            if (defender.Name.Equals("Knight", StringComparison.OrdinalIgnoreCase) && attacker.Name.Equals("WaterSpell", StringComparison.OrdinalIgnoreCase))
            {
                _battleLog.AppendLine("Knight's heavy armor is drowned by WaterSpell instantly.");
                return MaxDamage; // Represents instant defeat
            }
            if (defender.Name.Equals("Kraken", StringComparison.OrdinalIgnoreCase) && attacker.CardType.Equals("Spell", StringComparison.OrdinalIgnoreCase))
            {
                _battleLog.AppendLine("Kraken is immune against spells.");
                return 0;
            }
            if (attacker.Name.Equals("FireElf", StringComparison.OrdinalIgnoreCase) && defender.Name.Equals("Dragon", StringComparison.OrdinalIgnoreCase))
            {
                _battleLog.AppendLine("FireElf evades Dragon's attack.");
                return MaxDamage; // FireElf wins
            }

            return damage;
        }

        private void UpdateStats(string winnerUsername)
        {
            User winner = null;
            User loser = null;

            if (winnerUsername == _player1.Username)
            {
                winner = _player1;
                loser = _player2;
            }
            else if (winnerUsername == _player2.Username)
            {
                winner = _player2;
                loser = _player1;
            }

            if (winner != null && loser != null)
            {
                // Winner statistics
                winner.Stats.Wins += 1;
                winner.Stats.Elo += 10;

                // Loser statistics
                loser.Stats.Losses += 1;
                loser.Stats.Elo = Math.Max(loser.Stats.Elo - 5, 0); // Ensure Elo doesn't go negative

                // Update statistics in the database
                winner.Stats.UpdateStats(winner.Stats.Elo, winner.Stats.Wins, winner.Stats.Losses, winner.Stats.Draws);
                loser.Stats.UpdateStats(loser.Stats.Elo, loser.Stats.Wins, loser.Stats.Losses, loser.Stats.Draws);

                Console.WriteLine($"[Stats] Updated stats for {winner.Username} (Wins: {winner.Stats.Wins}, Elo: {winner.Stats.Elo}) and {loser.Username} (Losses: {loser.Stats.Losses}, Elo: {loser.Stats.Elo})");
            }
        }

        private void UpdateDraws()
        {
            _player1.Stats.Draws += 1;
            _player2.Stats.Draws += 1;

            // Update statistics in the database
            _player1.Stats.UpdateStats(_player1.Stats.Elo, _player1.Stats.Wins, _player1.Stats.Losses, _player1.Stats.Draws);
            _player2.Stats.UpdateStats(_player2.Stats.Elo, _player2.Stats.Wins, _player2.Stats.Losses, _player2.Stats.Draws);

            Console.WriteLine($"[Stats] Updated stats for {_player1.Username} and {_player2.Username} (Draws: {_player1.Stats.Draws}, {_player2.Stats.Draws})");
        }

        private static readonly Random _random = new Random(); // Use a single Random instance

        // Unique Feature: Double Strike Booster that allows a card to attack twice in one round
        private void ApplyBooster()
        {
            if (_player1.Cards.Count > 0 && _player2.Cards.Count > 0)
            {
                int boosterChance = _random.Next(100); // 20% chance to receive a booster
                if (boosterChance < 20)
                {
                    // Randomly select which player receives the booster
                    User selectedPlayer = _random.Next(2) == 0 ? _player1 : _player2;
                    Card selectedCard = GetRandomCard(selectedPlayer);

                    if (selectedCard != null)
                    {
                        selectedCard.Damage = Math.Min(selectedCard.Damage * 2, MaxDamage); // Double the damage with cap
                        _battleLog.AppendLine($"{selectedPlayer.Username} received a Double Strike booster! {selectedCard.Name}'s damage is doubled for this round.");
                    }
                }
            }
        }
    }
}
