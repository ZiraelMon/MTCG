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
        public Guid TradeID { get; set; }
        public Guid TradingCard { get; set; }
        public string? CardType { get; set; }
        public double CardStats { get; set; }

        //Methods
        public void CreateTrade(HttpSvrEventArgs e, Token userToken) {
            try {
                var connectionString = "Host=localhost; Username=user1; Password=userpwd1; Database=swen1db";
                using var conn = new NpgsqlConnection(connectionString);
                conn.Open();

                string replyString = "Trade created successfully.\n";
                using (var cmd = new NpgsqlCommand("SELECT trade.tradeid, cards.name, cards.damage, trade.type, trade.mindamage FROM trade JOIN cards ON trade.tradingcard = cards.id", conn)) {
                    using (var reader = cmd.ExecuteReader()) {
                        while (reader.Read()) {
                            replyString += "TradeID: " + reader.GetGuid(0) + " - Cardname: " + reader.GetString(1) + " - Damage: " + reader.GetDouble(2).ToString("0.0") + " - WantedType: " + reader.GetString(3) + " - MinDamage: " + reader.GetDouble(4).ToString("0.0") + "\n";
                        }
                    }

                }
                e.Reply(200, replyString);
            } catch {
                e.Reply(400, "Error occurred while creating trade.");
            }
        }

        public void PostTrade(HttpSvrEventArgs e, Token userToken) {
            try {
                var trade = JsonSerializer.Deserialize<Trade>(e.Payload);
                if (trade == null) {
                    e.Reply(400, "Malformed Request to post trade.");
                    return;
                }

                Trade? newTrade = JsonSerializer.Deserialize<Trade>(e.Payload);

                var connectionString = "Host=localhost; Username=user1; Password=userpwd1; Database=swen1db";
                using var conn = new NpgsqlConnection(connectionString);
                conn.Open();

                int postedTrade = 0;

                using (var cmd = new NpgsqlCommand("INSERT INTO trade (tradeid, tradingcard, type, mindamage) SELECT (@p1, @p2, @p3, @p4) WHERE EXISTS (SELECT 1 FROM cards WHERE id = (@p2) and unsername = (@p5))", conn)) {
                    cmd.Parameters.AddWithValue("@p1", newTrade.TradeID);
                    cmd.Parameters.AddWithValue("@p2", newTrade.TradingCard);
                    cmd.Parameters.AddWithValue("@p3", newTrade.CardType);
                    cmd.Parameters.AddWithValue("@p4", newTrade.CardStats);
                    cmd.Parameters.AddWithValue("@p5", userToken.LoggedInUser);
                    cmd.ExecuteNonQuery();
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
            try {
                var trade = JsonSerializer.Deserialize<Trade>(e.Payload);
                if (trade == null) {
                    e.Reply(400, "Malformed Request to trade cards.");
                    return;
                }

                string[] pathId = e.Path.Split("/");

                var connectionString = "Host=localhost; Username=user1; Password=userpwd1; Database=swen1db";
                using var conn = new NpgsqlConnection(connectionString);
                conn.Open();

                Trade? tradeCards = new Trade();
                tradeCards.TradeID = Guid.Parse(pathId[2]);
                string usernameTrade = "";

                using (var cmd = new NpgsqlCommand("SELECT cards.username, trade.type, trade.mindamage, trade.tradingcard FROM trade JOIN cards ON trade.tradingcard = cards.id WHERE trade.tradeid = (@p1)", conn)) {
                    cmd.Parameters.AddWithValue("@p1", trade.TradeID);
                    using (var reader = cmd.ExecuteReader()) {
                        if (reader.Read()) {
                            usernameTrade = reader.GetString(0);
                            tradeCards.TradingCard = reader.GetGuid(1);
                            tradeCards.CardType = reader.GetString(2);
                            tradeCards.CardStats = reader.GetDouble(3);
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
                    cmd.Parameters.AddWithValue("@p1", offeredCard.CardID);
                    using (var reader = cmd.ExecuteReader()) {
                        if (reader.Read()) {
                            offeredCard.CardID = reader.GetGuid(0);
                            offeredCard.Name = reader.GetString(1);
                            offeredCard.Damage = reader.GetDouble(2);
                        } else {
                            e.Reply(400, "Card not found.");
                        }
                    }
                }

                offeredCard = offeredCard.GetCardStats(offeredCard);
                bool match = false;

                // Dynamic trade query
                if (offeredCard.Species == Card.SpeciesCard.Spell && tradeCards.CardType.Equals("spell")) {
                    match = true;
                }
                else if (offeredCard.Species != Card.SpeciesCard.Spell && tradeCards.CardType.Equals("monster")) {
                    match = true;
                }
                else if ((offeredCard.Damage <= tradeCards.CardStats) && match) {
                    match = true;
                }
                if (!match) {
                    e.Reply(400, "Card does not match the trade.");
                    return;
                }

                // Proceed with the trade
                using(var cmd = new NpgsqlCommand("UPDATE cards SET username = (@p1) WHERE id = (@p2)", conn)) {
                    cmd.Parameters.AddWithValue("@p1", userToken.LoggedInUser);
                    cmd.Parameters.AddWithValue("@p2", trade.TradingCard);
                    cmd.ExecuteNonQuery();
                }
                using(var cmd = new NpgsqlCommand("UPDATE cards SET username = (@p1) WHERE id = (@p2)", conn)) {
                    cmd.Parameters.AddWithValue("@p1", usernameTrade);
                    cmd.Parameters.AddWithValue("@p2", offeredCard.CardID);
                    cmd.ExecuteNonQuery();
                }
                using(var cmd = new NpgsqlCommand("DELETE FROM trade WHERE tradeid = (@p1)", conn)) {
                    cmd.Parameters.AddWithValue("@p1", trade.TradeID);
                    cmd.ExecuteNonQuery();
                }
                e.Reply(200, "Trade successful.");
            } catch {
                e.Reply(400, "Error occurred while trading cards.");
            }
        }

        public void DeleteTrade(HttpSvrEventArgs e, Token userToken) {
            try {
                string[] pathId = e.Path.Split("/");
                var trade = new Trade();
                trade.TradeID = Guid.Parse(pathId[2]);

                var connectionString = "Host=localhost; Username=user1; Password=userpwd1; Database=swen1db";
                using var conn = new NpgsqlConnection(connectionString);
                conn.Open();

                int deletedTrade = 0;

                using (var cmd = new NpgsqlCommand("DELETE FROM trade WHERE (@p1) IN (SELECT tradeid FROM trade JOIN cards ON trade.tradingcard, = cards.id WHERE cards.username = (@p2))", conn)) {
                    cmd.Parameters.AddWithValue("@p2", userToken.LoggedInUser);
                    cmd.Parameters.AddWithValue("@p1", trade.TradeID);
                    deletedTrade = cmd.ExecuteNonQuery();
                }
                if(deletedTrade == 0) {
                    e.Reply(400, "Trade not found.");
                    return;
                }
                e.Reply(200, "Trade deleted successfully.");
            } catch {
                e.Reply(400, "Error occurred while deleting trade.");
            }
        }

    }
}
