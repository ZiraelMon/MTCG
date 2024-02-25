using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using System.Xml.Linq;
using System.Text.Json;
using MTCG.Server;
using Npgsql;
using System.Drawing;
using System.Security.Cryptography;
using Microsoft.VisualBasic;

namespace MTCG.Models
{
public class Deck {
        public void PrintDeck(HttpSvrEventArgs e, Token userToken) {
            if (!userToken.IsValid()) {
                e.Reply(400, "Invalid token. User not logged in.");
                return;
            }
            try {
                string connectionString = ConfigurationManager.GetConnectionString("DefaultConnection");
                using var conn = new NpgsqlConnection(connectionString);
                conn.Open();

                string replyString = "Your Deck: \n";
                using (var cmd = new NpgsqlCommand("SELECT name, damage FROM cards WHERE username = @p1 AND deck = TRUE", conn)) {
                    cmd.Parameters.AddWithValue("@p1", userToken.LoggedInUser!);
                    using (var reader = cmd.ExecuteReader()) {
                        while (reader.Read()) {
                            replyString += "Cardname: " + reader.GetString(0) + " - Damage: " + reader.GetDouble(1).ToString("0.0") + "\n";
                        }
                    }
                }
                e.Reply(replyString != "Your Deck: \n" ? 200 : 200, replyString != "Your Deck: \n" ? replyString : "No cards in your deck.");
            }
            catch {
                e.Reply(400, "Error occurred retrieving cards.");
            }
        }

        public void UpdateDeck(HttpSvrEventArgs e, Token userToken) {
            if (!userToken.IsValid()) {
                e.Reply(400, "Invalid token. User not logged in.");
                return;
            }
            try {
                Guid[] deck = JsonSerializer.Deserialize<Guid[]>(e.Payload) ?? Array.Empty<Guid>();
                if (deck == null || deck.Length != 4) {
                    e.Reply(400, "Malformed Request to update Decks.");
                    return;
                }
                string connectionString = ConfigurationManager.GetConnectionString("DefaultConnection");
                using var conn = new NpgsqlConnection(connectionString);
                conn.Open();

                // Start a transaction
                using var transaction = conn.BeginTransaction();

                // Checks if requested cards are valid for deckbuilding
                var checkCmdText = $"SELECT count(*) FROM cards WHERE username = @p1 AND trade = false AND id = ANY(@p2)";
                using (var checkCmd = new NpgsqlCommand(checkCmdText, conn)) {
                    checkCmd.Parameters.AddWithValue("@p1", userToken.LoggedInUser!);
                    checkCmd.Parameters.AddWithValue("@p2", deck);
                    var result = checkCmd.ExecuteScalar();
                    var requestedCardNumber = result != null ? Convert.ToInt64(result) : 0L;
                    if (requestedCardNumber < 4) {
                        e.Reply(400, "Not all requested Cards are available or in your collection.");
                        return;
                    }
                }

                // Update current deck flags to false
                var updateOldDeckCmdText = "UPDATE cards SET deck = false WHERE username = @p1 AND deck = true";
                using (var updateOldDeckCmd = new NpgsqlCommand(updateOldDeckCmdText, conn)) {
                    updateOldDeckCmd.Parameters.AddWithValue("@p1", userToken.LoggedInUser!);
                    updateOldDeckCmd.ExecuteNonQuery();
                }

                // Update new deck flags
                var updateNewDeckCmdText = "UPDATE cards SET deck = true WHERE username = @p1 AND id = ANY(@p2)";
                using (var updateNewDeckCmd = new NpgsqlCommand(updateNewDeckCmdText, conn)) {
                    updateNewDeckCmd.Parameters.AddWithValue("@p1", userToken.LoggedInUser!);
                    updateNewDeckCmd.Parameters.AddWithValue("@p2", deck);
                    updateNewDeckCmd.ExecuteNonQuery();
                }

                transaction.Commit();
                e.Reply(200, "Deck updated successfully.");
            }
            catch {
                e.Reply(400, "Error occurred while updating the deck.");
            }
        }

        public List<Card> PrepareDeck(Token userToken) {
            List<Card> fullDeck = new List<Card>();

            if(!userToken.IsValid()) {
                return fullDeck;
            }

            try {
                string connectionString = ConfigurationManager.GetConnectionString("DefaultConnection");
                using var conn = new NpgsqlConnection(connectionString);
                conn.Open();

                // Retrieve all cards with the deck tag belonging to the user
                string cmdText = "SELECT id, name, damage FROM cards WHERE username = @p1 AND deck = TRUE";
                using (var cmd = new NpgsqlCommand(cmdText, conn)) {
                    cmd.Parameters.AddWithValue("@p1", userToken.LoggedInUser!);
                    using (var reader = cmd.ExecuteReader()) {
                        while (reader.Read()) {
                            var newCard = new Card {
                                Id = reader.GetGuid(0),
                                Name = reader.GetString(1),
                                Damage = reader.GetDouble(2),
                            };
                            fullDeck.Add(newCard);
                        }
                    }
                }

                return fullDeck;
            }
            catch {
                throw;
            }
        }

        public void GetCards(HttpSvrEventArgs e, Token userToken) {
            if (!userToken.IsValid()) {
                e.Reply(400, "Invalid token. User not logged in.");
                return;
            }
            try {
                string connectionString = ConfigurationManager.GetConnectionString("DefaultConnection");
                using var conn = new NpgsqlConnection(connectionString);
                conn.Open();

                string replyString = "Your Cards: \n";
                using (var cmd = new NpgsqlCommand("SELECT name, damage FROM cards WHERE username = @p1", conn)) {
                    cmd.Parameters.AddWithValue("@p1", userToken.LoggedInUser!);
                    using (var reader = cmd.ExecuteReader()) {
                        while (reader.Read()) {
                            replyString += $"Cardname: {reader.GetString(0)} - Damage: {reader.GetDouble(1):0.0}\n";
                        }
                    }
                }
                e.Reply(replyString != "Your Cards: \n" ? 200 : 200, replyString != "Your Cards: \n" ? replyString : "No cards in your Collection.");
            }
            catch {
                e.Reply(400, "Error occurred while fetching cards.");
            }
        }
    }
}
