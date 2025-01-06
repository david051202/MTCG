using System;
using System.Collections.Generic;
using MTCG.Classes;
using Npgsql;

namespace MTCG.Models
{
    public class TradingDeal
    {
        public Guid Id { get; set; }
        public Guid CardToTrade { get; set; }
        public string Type { get; set; }
        public double MinimumDamage { get; set; }
        public int UserId { get; set; } // Add UserId to track the owner of the deal

        public static List<TradingDeal> GetAllTradingDeals()
        {
            var tradingDeals = new List<TradingDeal>();

            try
            {
                using (var conn = DatabaseHelper.GetOpenConnection())
                {
                    var cmd = new NpgsqlCommand("SELECT id, card_to_trade, type, minimum_damage, user_id FROM trading_deals", conn);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var tradingDeal = new TradingDeal
                            {
                                Id = reader.GetGuid(reader.GetOrdinal("id")),
                                CardToTrade = reader.GetGuid(reader.GetOrdinal("card_to_trade")),
                                Type = reader.GetString(reader.GetOrdinal("type")),
                                MinimumDamage = reader.GetDouble(reader.GetOrdinal("minimum_damage")),
                                UserId = reader.GetInt32(reader.GetOrdinal("user_id"))
                            };
                            tradingDeals.Add(tradingDeal);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving trading deals: {ex.Message}");
            }

            return tradingDeals;
        }

        public static TradingDeal GetTradingDealById(Guid id)
        {
            try
            {
                using (var conn = DatabaseHelper.GetOpenConnection())
                {
                    var cmd = new NpgsqlCommand("SELECT id, card_to_trade, type, minimum_damage, user_id FROM trading_deals WHERE id = @id", conn);
                    cmd.Parameters.AddWithValue("id", id);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new TradingDeal
                            {
                                Id = reader.GetGuid(reader.GetOrdinal("id")),
                                CardToTrade = reader.GetGuid(reader.GetOrdinal("card_to_trade")),
                                Type = reader.GetString(reader.GetOrdinal("type")),
                                MinimumDamage = reader.GetDouble(reader.GetOrdinal("minimum_damage")),
                                UserId = reader.GetInt32(reader.GetOrdinal("user_id"))
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving trading deal: {ex.Message}");
            }

            return null;
        }

        public static bool CreateTradingDeal(TradingDeal tradingDeal)
        {
            try
            {
                using (var conn = DatabaseHelper.GetOpenConnection())
                {
                    var cmd = new NpgsqlCommand(
                        "INSERT INTO trading_deals (id, card_to_trade, type, minimum_damage, user_id) VALUES (@id, @card_to_trade, @type, @minimum_damage, @user_id)",
                        conn);
                    cmd.Parameters.AddWithValue("id", tradingDeal.Id);
                    cmd.Parameters.AddWithValue("card_to_trade", tradingDeal.CardToTrade);
                    cmd.Parameters.AddWithValue("type", tradingDeal.Type);
                    cmd.Parameters.AddWithValue("minimum_damage", tradingDeal.MinimumDamage);
                    cmd.Parameters.AddWithValue("user_id", tradingDeal.UserId);

                    cmd.ExecuteNonQuery();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating trading deal: {ex.Message}");
                return false;
            }
        }

        public static bool DeleteTradingDeal(Guid id)
        {
            try
            {
                using (var conn = DatabaseHelper.GetOpenConnection())
                {
                    var cmd = new NpgsqlCommand("DELETE FROM trading_deals WHERE id = @id", conn);
                    cmd.Parameters.AddWithValue("id", id);

                    cmd.ExecuteNonQuery();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting trading deal: {ex.Message}");
                return false;
            }
        }

        public static bool ExecuteTrade(Guid tradingDealId, int userId, Guid offeredCardId)
        {
            try
            {
                using (var conn = DatabaseHelper.GetOpenConnection())
                {
                    using (var transaction = conn.BeginTransaction())
                    {
                        // Get the trading deal
                        var tradingDeal = GetTradingDealById(tradingDealId);
                        if (tradingDeal == null)
                        {
                            return false;
                        }

                        // Remove the card from the original owner
                        var removeCardCmd = new NpgsqlCommand("DELETE FROM usercards WHERE user_id = @user_id AND card_id = @card_id", conn);
                        removeCardCmd.Transaction = transaction;
                        removeCardCmd.Parameters.AddWithValue("user_id", tradingDeal.UserId);
                        removeCardCmd.Parameters.AddWithValue("card_id", tradingDeal.CardToTrade);
                        removeCardCmd.ExecuteNonQuery();

                        // Add the card to the new owner
                        var addCardCmd = new NpgsqlCommand("INSERT INTO usercards (user_id, card_id) VALUES (@user_id, @card_id)", conn);
                        addCardCmd.Transaction = transaction;
                        addCardCmd.Parameters.AddWithValue("user_id", userId);
                        addCardCmd.Parameters.AddWithValue("card_id", tradingDeal.CardToTrade);
                        addCardCmd.ExecuteNonQuery();

                        // Remove the offered card from the new owner
                        var removeOfferedCardCmd = new NpgsqlCommand("DELETE FROM usercards WHERE user_id = @user_id AND card_id = @card_id", conn);
                        removeOfferedCardCmd.Transaction = transaction;
                        removeOfferedCardCmd.Parameters.AddWithValue("user_id", userId);
                        removeOfferedCardCmd.Parameters.AddWithValue("card_id", offeredCardId);
                        removeOfferedCardCmd.ExecuteNonQuery();

                        // Add the offered card to the original owner
                        var addOfferedCardCmd = new NpgsqlCommand("INSERT INTO usercards (user_id, card_id) VALUES (@user_id, @card_id)", conn);
                        addOfferedCardCmd.Transaction = transaction;
                        addOfferedCardCmd.Parameters.AddWithValue("user_id", tradingDeal.UserId);
                        addOfferedCardCmd.Parameters.AddWithValue("card_id", offeredCardId);
                        addOfferedCardCmd.ExecuteNonQuery();

                        // Delete the trading deal
                        var deleteDealCmd = new NpgsqlCommand("DELETE FROM trading_deals WHERE id = @id", conn);
                        deleteDealCmd.Transaction = transaction;
                        deleteDealCmd.Parameters.AddWithValue("id", tradingDealId);
                        deleteDealCmd.ExecuteNonQuery();

                        // Commit the transaction
                        transaction.Commit();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing trade: {ex.Message}");
                return false;
            }
        }

        public bool MeetsRequirements(Guid offeredCardId)
        {
            try
            {
                using (var conn = DatabaseHelper.GetOpenConnection())
                {
                    var cmd = new NpgsqlCommand("SELECT card_type, damage FROM cards WHERE card_id = @card_id", conn);
                    cmd.Parameters.AddWithValue("card_id", offeredCardId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var cardType = reader.GetString(reader.GetOrdinal("card_type"));
                            var damage = reader.GetDouble(reader.GetOrdinal("damage"));

                            return cardType.Equals(Type, StringComparison.OrdinalIgnoreCase) && damage >= MinimumDamage;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking card requirements: {ex.Message}");
            }

            return false;
        }
    }
}
