using System;
using System.Collections.Generic;
using System.Linq;
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

        private const int MaxRounds = 100; // Limit to 100 rounds

        public BattleLogic(User player1, User player2)
        {
            _battleLog = new StringBuilder();

            // Limit deck to 4 cards
            _player1 = PreparePlayer(player1);
            _player2 = PreparePlayer(player2);
        }

        private User PreparePlayer(User player)
        {
            // Ensure the player has exactly 4 cards
            if (player.Cards.Count > 4)
            {
                player.Cards = GetRandomCards(player.Cards, 4);
            }
            else if (player.Cards.Count < 4)
            {
                throw new InvalidOperationException($"Player {player.Username} does not have enough cards to start a battle.");
            }

            return player;
        }

        private List<Card> GetRandomCards(List<Card> cards, int count)
        {
            return cards.OrderBy(x => _random.Next()).Take(count).ToList();
        }

        public async Task<BattleOutcome> StartBattleAsync()
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

                // Round breakdown
                _battleLog.AppendLine("**Card Selection:**");
                _battleLog.AppendLine($"{_player1.Username} plays {card1.Name} ({card1.ElementType} {card1.CardType}) with {card1.Damage} damage.");
                _battleLog.AppendLine($"{_player2.Username} plays {card2.Name} ({card2.ElementType} {card2.CardType}) with {card2.Damage} damage.");
                _battleLog.AppendLine("**Damage Calculation:**");

                int damage1 = CalculateDamage(card1, card2);
                int damage2 = CalculateDamage(card2, card1);

                // Display calculated damages
                _battleLog.AppendLine($"{_player1.Username}'s {card1.Name} attacks with effective damage of {damage1}.");
                _battleLog.AppendLine($"{_player2.Username}'s {card2.Name} attacks with effective damage of {damage2}.");

                Console.WriteLine($"[Battle] {card1.Name} ({damage1} dmg) vs {card2.Name} ({damage2} dmg)");

                string roundWinner = string.Empty;

                // Determine round winner
                if (damage1 > damage2)
                {
                    roundWinner = _player1.Username;
                    _player2.RemoveCard(card2);
                    _player1.AddCard(card2);
                }
                else if (damage2 > damage1)
                {
                    roundWinner = _player2.Username;
                    _player1.RemoveCard(card1);
                    _player2.AddCard(card1);
                }

                // End of round and add round winner
                _battleLog.AppendLine($"[Battle] End of Round {round}");
                Console.WriteLine($"[Battle] End of Round {round}");
                if (!string.IsNullOrEmpty(roundWinner))
                {
                    _battleLog.AppendLine($"[Battle] **Round Winner:** {roundWinner}\n");
                    Console.WriteLine($"[Battle] Round Winner: {roundWinner}\n");
                }
                else
                {
                    _battleLog.AppendLine($"[Battle] **Round {round} was a draw.** No cards were moved.\n");
                    Console.WriteLine($"[Battle] Round {round} was a draw.\n");
                }

                await Task.Delay(100); // Simulate async operation
            }

            // Determine overall winner
            User winner = null;
            User loser = null;

            if (_player1.Cards.Count == 0 && _player2.Cards.Count == 0)
            {
                _battleLog.AppendLine("\nBattle ended in a draw. Both players have no remaining cards.");
                UpdateDraws();
            }
            else if (_player1.Cards.Count == 0)
            {
                winner = _player2;
                loser = _player1;
                _battleLog.AppendLine($"\nBattle concluded. Winner: {winner.Username}");
            }
            else if (_player2.Cards.Count == 0)
            {
                winner = _player1;
                loser = _player2;
                _battleLog.AppendLine($"\nBattle concluded. Winner: {winner.Username}");
            }
            else if (round >= MaxRounds)
            {
                _battleLog.AppendLine("\nBattle ended in a draw due to maximum rounds reached.");
                UpdateDraws();
            }

            Console.WriteLine($"[Battle] Battle between {_player1.Username} and {_player2.Username} completed.");

            return new BattleOutcome
            {
                Winner = winner,
                Loser = loser,
                Log = _battleLog.ToString()
            };
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

        public class BattleOutcome
        {
            public User Winner { get; set; }
            public User Loser { get; set; }
            public string Log { get; set; }
        }

        public Battle.BattleManager BattleManager
        {
            get => default;
            set
            {
            }
        }
    }
}
