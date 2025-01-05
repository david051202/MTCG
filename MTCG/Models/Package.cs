using System;
using System.Collections.Generic;
using Npgsql;
using MTCG.Models;

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

        public enum BuyResult
        {
            Success,
            NotEnoughCoins,
            NoPackageAvailable,
            Error
        }

        public static BuyResult BuyPackage(User user)
        {
            try
            {
                using (var conn = DatabaseHelper.GetOpenConnection())
                {
                    using (var transaction = conn.BeginTransaction())
                    {
                        // Check if the user has enough coins
                        if (user.Coins < 5)
                        {
                            Console.WriteLine($"[Server] User {user.Username} does not have enough coins. Current coins: {user.Coins}");
                            return BuyResult.NotEnoughCoins;
                        }

                        // Get the first available package
                        var getPackageCmd = new NpgsqlCommand("SELECT package_id FROM packages LIMIT 1 FOR UPDATE SKIP LOCKED", conn);
                        getPackageCmd.Transaction = transaction;
                        var packageIdObj = getPackageCmd.ExecuteScalar();

                        if (packageIdObj == null)
                        {
                            // No package available
                            Console.WriteLine("[Server] No package available for purchase.");
                            return BuyResult.NoPackageAvailable;
                        }

                        int packageId = Convert.ToInt32(packageIdObj);

                        // Retrieve the cards in the package
                        var getCardsCmd = new NpgsqlCommand(
                            @"SELECT c.card_id, c.name, c.damage, c.element_type, c.card_type
                      FROM packagecards pc
                      JOIN cards c ON pc.card_id = c.card_id
                      WHERE pc.package_id = @package_id", conn);
                        getCardsCmd.Transaction = transaction;
                        getCardsCmd.Parameters.AddWithValue("package_id", packageId);

                        var reader = getCardsCmd.ExecuteReader();
                        var cards = new List<Card>();

                        while (reader.Read())
                        {
                            var card = new Card
                            {
                                Id = reader.GetGuid(0),
                                Name = reader.GetString(1),
                                Damage = reader.GetDouble(2),
                                ElementType = reader.GetString(3),
                                CardType = reader.GetString(4)
                            };
                            cards.Add(card);
                        }
                        reader.Close();

                        // Deduct coins from the user
                        var updateUserCmd = new NpgsqlCommand("UPDATE users SET coins = coins - 5 WHERE user_id = @user_id", conn);
                        updateUserCmd.Transaction = transaction;
                        updateUserCmd.Parameters.AddWithValue("user_id", user.UserId);
                        updateUserCmd.ExecuteNonQuery();

                        // Add cards to the user's collection
                        foreach (var card in cards)
                        {
                            var insertUserCardCmd = new NpgsqlCommand("INSERT INTO usercards (user_id, card_id) VALUES (@user_id, @card_id)", conn);
                            insertUserCardCmd.Transaction = transaction;
                            insertUserCardCmd.Parameters.AddWithValue("user_id", user.UserId);
                            insertUserCardCmd.Parameters.AddWithValue("card_id", card.Id);
                            insertUserCardCmd.ExecuteNonQuery();

                            // Optional: Hinzufügen der Karte zur Benutzerobjektliste
                            user.Cards.Add(card);
                        }

                        // Remove the package from the database
                        var deletePackageCmd = new NpgsqlCommand("DELETE FROM packages WHERE package_id = @package_id", conn);
                        deletePackageCmd.Transaction = transaction;
                        deletePackageCmd.Parameters.AddWithValue("package_id", packageId);
                        deletePackageCmd.ExecuteNonQuery();

                        // Commit the transaction
                        transaction.Commit();

                        Console.WriteLine($"[Server] User {user.Username} successfully purchased a package.");
                        return BuyResult.Success;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during package purchase: {ex.Message}");
                return BuyResult.Error;
            }
        }
    }
}