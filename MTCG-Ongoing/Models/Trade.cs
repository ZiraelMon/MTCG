using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Threading.Tasks;
using MTCG.Server;
using Npgsql;
using System.Text.Json;

namespace MTCG.Models {
    internal class Trade {

        //Properties
        public Guid Id { get; set; }
        public Guid CardToTrade { get; set; }
        public string? Type { get; set; }
        public double MinimumDamage { get; set; }

        //Methods
        public void CreateTrade(HttpSvrEventArgs e, Token userToken) {
            try {
                string connectionString = ConfigurationManager.GetConnectionString("DefaultConnection");
                using var conn = new NpgsqlConnection(connectionString);
                conn.Open();

                string replyString = "Trade created successfully.\n";
                using (var cmd = new NpgsqlCommand("SELECT trade.id, cards.name, cards.damage, trade.type, trade.mindamage FROM trade JOIN cards ON trade.tradingcard = cards.id", conn)) {
                    using (var reader = cmd.ExecuteReader()) {
                        while (reader.Read()) {
                            replyString += "Id: " + reader.GetGuid(0) + " - Cardname: " + reader.GetString(1) + " - Damage: " + reader.GetDouble(2).ToString("0.0") + " - WantedType: " + reader.GetString(3) + " - MinDamage: " + reader.GetDouble(4).ToString("0.0") + "\n";
                        }
                    }

                }
                e.Reply(200, replyString);
            } catch {
                e.Reply(400, "Error occurred while creating trade.");
            }
        }

        public void PostTrade(HttpSvrEventArgs e, Token userToken) {
            if (!userToken.IsValid()) {
                e.Reply(400, "Invalid token. User not logged in.");
                return;
            }

            try {
                var newTrade = JsonSerializer.Deserialize<Trade>(e.Payload);
                if (newTrade == null) {
                    e.Reply(400, "Malformed Request to post trade.");
                    return;
                }

                string connectionString = ConfigurationManager.GetConnectionString("DefaultConnection");
                using var conn = new NpgsqlConnection(connectionString);
                conn.Open();

                int postedTrade = 0;

                using (var cmd = new NpgsqlCommand("INSERT INTO trade (id, tradingcard, type, mindamage) SELECT @p1, @p2, @p3, @p4 WHERE EXISTS (SELECT 1 FROM cards WHERE id = @p2 AND username = @p5)", conn)) {
                    cmd.Parameters.AddWithValue("@p1", newTrade.Id);
                    cmd.Parameters.AddWithValue("@p2", newTrade.CardToTrade);
                    cmd.Parameters.AddWithValue("@p3", newTrade.Type ?? "");
                    cmd.Parameters.AddWithValue("@p4", newTrade.MinimumDamage);
                    cmd.Parameters.AddWithValue("@p5", userToken.LoggedInUser!);
                    postedTrade = cmd.ExecuteNonQuery();
                }
                if(postedTrade == 0) {

                    e.Reply(400, "Trade not posted.");
                    return;
                }
                e.Reply(200, "Trade posted successfully.");
            } catch {
                e.Reply(400, "Error occurred while posting trade.");
            }
        }   

        public void TradeCards(HttpSvrEventArgs e, Token userToken) {
            if (!userToken.IsValid()) {
                e.Reply(400, "Invalid token. User not logged in.");
                return;
            }

            try {
                var trade = JsonSerializer.Deserialize<Trade>(e.Payload);
                if (trade == null) {
                    e.Reply(400, "Malformed Request to trade cards.");
                    return;
                }

                string[] pathId = e.Path.Split("/");

                string connectionString = ConfigurationManager.GetConnectionString("DefaultConnection");
                using var conn = new NpgsqlConnection(connectionString);
                conn.Open();

                Trade? tradeCards = new Trade();
                tradeCards.Id = Guid.Parse(pathId[2]);
                string usernameTrade = "";

                using (var cmd = new NpgsqlCommand("SELECT cards.username, trade.type, trade.mindamage, trade.tradingcard FROM trade JOIN cards ON trade.tradingcard = cards.id WHERE trade.Id = (@p1)", conn)) {
                    cmd.Parameters.AddWithValue("@p1", trade.Id);
                    using (var reader = cmd.ExecuteReader()) {
                        if (reader.Read()) {
                            usernameTrade = reader.GetString(0);
                            tradeCards.Type = reader.GetString(1);
                            tradeCards.MinimumDamage = reader.GetDouble(2);
                            tradeCards.CardToTrade = reader.GetGuid(3);
                        } else {
                            e.Reply(400, "Trade not found.");
                        }
                    }
                }

                if (usernameTrade == userToken.LoggedInUser) {
                    e.Reply(400, "You cannot trade with yourself.");
                    return;
                }

                Card offeredCard = new Card();
                using (var cmd = new NpgsqlCommand("SELECT cards.id, cards.name, cards.damage FROM cards WHERE cards.id = (@p1)", conn)) {
                    cmd.Parameters.AddWithValue("@p1", offeredCard.Id);
                    using (var reader = cmd.ExecuteReader()) {
                        if (reader.Read()) {
                            offeredCard.Id = reader.GetGuid(0);
                            offeredCard.Name = reader.GetString(1);
                            offeredCard.Damage = reader.GetDouble(2);
                        } else {
                            e.Reply(400, "Card not found.");
                        }
                    }
                }

                offeredCard = offeredCard.GetCardStats(offeredCard);

                // Dynamic trade query
                bool isTypeMatch = (offeredCard.Species == Card.SpeciesCard.Spell && tradeCards.Type == "spell") ||
                                   (offeredCard.Species != Card.SpeciesCard.Spell && tradeCards.Type == "monster");

                bool isDamageMatch = offeredCard.Damage >= tradeCards.MinimumDamage;

                // Proceed with the trade
                using (var cmd = new NpgsqlCommand("UPDATE cards SET username = (@p1) WHERE id = (@p2)", conn)) {
                    cmd.Parameters.AddWithValue("@p1", userToken.LoggedInUser!);
                    cmd.Parameters.AddWithValue("@p2", trade.CardToTrade);
                    cmd.ExecuteNonQuery();
                }
                using(var cmd = new NpgsqlCommand("UPDATE cards SET username = (@p1) WHERE id = (@p2)", conn)) {
                    cmd.Parameters.AddWithValue("@p1", usernameTrade);
                    cmd.Parameters.AddWithValue("@p2", offeredCard.Id);
                    cmd.ExecuteNonQuery();
                }
                using(var cmd = new NpgsqlCommand("DELETE FROM trade WHERE Id = (@p1)", conn)) {
                    cmd.Parameters.AddWithValue("@p1", trade.Id);
                    cmd.ExecuteNonQuery();
                }
                e.Reply(200, "Trade successful.");
            } catch {
                e.Reply(400, "Card does not match the trade requirements.");
            }
        }

        public void DeleteTrade(HttpSvrEventArgs e, Token userToken) {
            if (!userToken.IsValid()) {
                e.Reply(400, "Invalid token. User not logged in.");
                return;
            }

            try {
                string[] pathId = e.Path.Split("/");
                var trade = new Trade();
                trade.Id = Guid.Parse(pathId[2]);

                string connectionString = ConfigurationManager.GetConnectionString("DefaultConnection");
                using var conn = new NpgsqlConnection(connectionString);
                conn.Open();

                int deletedTrade = 0;

                using (var cmd = new NpgsqlCommand("DELETE FROM trade WHERE (@p1) IN (SELECT trade.id FROM trade JOIN cards ON trade.tradingcard = cards.id WHERE cards.username = (@p2))", conn)) {
                    cmd.Parameters.AddWithValue("@p2", userToken.LoggedInUser!);
                    cmd.Parameters.AddWithValue("@p1", trade.Id);
                    deletedTrade = cmd.ExecuteNonQuery();
                }
                if(deletedTrade == 0) {
                    e.Reply(400, "Trade not found.");
                    return;
                }
                e.Reply(200, "Trade deleted successfully.");
            } catch {
                e.Reply(400, "Error occured while deleting Trade");
            }
        }

    }
}
