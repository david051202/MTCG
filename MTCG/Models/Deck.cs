using System;
using System.Collections.Generic;
using Npgsql;

namespace MTCG.Classes
{
    public class Deck
    {
        public int UserId { get; set; }
        public List<Card> Cards { get; set; }

        public Deck()
        {
            Cards = new List<Card>();
        }

        public static Deck GetDeckByUserId(int userId)
        {
            var deck = new Deck { UserId = userId };

            try
            {
                using (var conn = DatabaseHelper.GetOpenConnection())
                {
                    var cmd = new NpgsqlCommand(
                        @"SELECT c.card_id, c.name, c.damage, c.element_type, c.card_type
                          FROM deck d
                          JOIN cards c ON d.card_id = c.card_id
                          WHERE d.user_id = @user_id", conn);
                    cmd.Parameters.AddWithValue("user_id", userId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var card = new Card
                            {
                                Id = reader.GetGuid(reader.GetOrdinal("card_id")),
                                Name = reader.GetString(reader.GetOrdinal("name")),
                                Damage = reader.GetDouble(reader.GetOrdinal("damage")),
                                ElementType = reader.GetString(reader.GetOrdinal("element_type")),
                                CardType = reader.GetString(reader.GetOrdinal("card_type"))
                            };
                            deck.Cards.Add(card);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving deck: {ex.Message}");
            }

            return deck;
        }

        public bool ConfigureDeck(List<Guid> cardIds)
        {
            if (cardIds.Count != 4)
            {
                throw new ArgumentException("A deck must contain exactly 4 unique card IDs.");
            }

            try
            {
                using (var conn = DatabaseHelper.GetOpenConnection())
                {
                    using (var transaction = conn.BeginTransaction())
                    {
                        // Lösche das aktuelle Deck
                        var deleteCmd = new NpgsqlCommand("DELETE FROM deck WHERE user_id = @user_id", conn);
                        deleteCmd.Parameters.AddWithValue("user_id", UserId);
                        deleteCmd.Transaction = transaction;
                        deleteCmd.ExecuteNonQuery();

                        // Füge die neuen Karten zum Deck hinzu
                        foreach (var cardId in cardIds)
                        {
                            var insertCmd = new NpgsqlCommand("INSERT INTO deck (user_id, card_id) VALUES (@user_id, @card_id)", conn);
                            insertCmd.Parameters.AddWithValue("user_id", UserId);
                            insertCmd.Parameters.AddWithValue("card_id", cardId);
                            insertCmd.Transaction = transaction;
                            insertCmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                }

                // Aktualisiere die Kartenliste im Deck
                Cards = GetDeckByUserId(UserId).Cards;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error configuring deck: {ex.Message}");
                return false;
            }
        }
    }
}
