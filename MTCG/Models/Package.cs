using System;
using System.Collections.Generic;
using Npgsql;

namespace MTCG.Classes
{
    public class Package
    {
        public static bool CreatePackage(List<Card> cards)
        {
            try
            {
                using (var conn = DatabaseHelper.GetOpenConnection())
                {
                    using (var transaction = conn.BeginTransaction())
                    {
                        // Insert the new package
                        var packageCmd = new NpgsqlCommand("INSERT INTO packages (price) VALUES (5) RETURNING package_id", conn);
                        packageCmd.Transaction = transaction;
                        var packageId = (int)packageCmd.ExecuteScalar();

                        foreach (var card in cards)
                        {
                            // Insert the card
                            var cardCmd = new NpgsqlCommand(
                                "INSERT INTO cards (card_id, name, damage, element_type, card_type) VALUES (@card_id, @name, @damage, @element_type, @card_type)", conn);
                            cardCmd.Transaction = transaction;
                            cardCmd.Parameters.AddWithValue("card_id", card.Id);
                            cardCmd.Parameters.AddWithValue("name", card.Name);
                            cardCmd.Parameters.AddWithValue("damage", card.Damage);
                            cardCmd.Parameters.AddWithValue("element_type", card.ElementType);
                            cardCmd.Parameters.AddWithValue("card_type", card.CardType);
                            cardCmd.ExecuteNonQuery();

                            // Link the card to the package
                            var packageCardCmd = new NpgsqlCommand(
                                "INSERT INTO packagecards (package_id, card_id) VALUES (@package_id, @card_id)", conn);
                            packageCardCmd.Transaction = transaction;
                            packageCardCmd.Parameters.AddWithValue("package_id", packageId);
                            packageCardCmd.Parameters.AddWithValue("card_id", card.Id);
                            packageCardCmd.ExecuteNonQuery();
                        }

                        // Commit the transaction
                        transaction.Commit();

                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during package creation: {ex.Message}");
                return false;
            }
        }


    }
}
