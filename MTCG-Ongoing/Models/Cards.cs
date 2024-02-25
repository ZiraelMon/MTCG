using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MTCG.Server;
using Npgsql;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.VisualBasic;

namespace MTCG.Models
{
public class Card {
        public enum ElementCard {
            Regular,
            Water,
            Fire,
        }

        public enum SpeciesCard {
            Goblin,
            Troll,
            Elf,
            Knight,
            Dragon,
            Ork,
            Kraken,
            Wizard,
            Spell,
        }

        public Guid Id { get; set; }
        public string Name { get; set; } = "DefaultName"; // Default value to avoid "null" or "empty" name 
        public double Damage { get; set; }
        public ElementCard Element { get; set; }
        public SpeciesCard Species { get; set; }

        public void CreateCards(HttpSvrEventArgs e) {
            try {
                List<Card> carddeck = JsonSerializer.Deserialize<List<Card>>(e.Payload) ?? new List<Card>();

                string connectionString = ConfigurationManager.GetConnectionString("DefaultConnection");
                using var conn = new NpgsqlConnection(connectionString);
                conn.Open();

                if (carddeck == null || carddeck.Count != 5) {
                    e.Reply(400, "Error occurred while creating package: not enough cards for a package.");
                    return;
                }

                using var transaction = conn.BeginTransaction();
                Guid[] carddeckIds = new Guid[5];
                int i = 0;

                foreach (Card card in carddeck) {
                    var cmdText = "INSERT INTO cards (id, name, damage, deck, trade, username) VALUES ((@p1), (@p2), (@p3), (@p4), (@p5), (@p6))";
                    using (var cmd = new NpgsqlCommand(cmdText, conn, transaction)) {
                        cmd.Parameters.AddWithValue("@p1", card.Id);
                        cmd.Parameters.AddWithValue("@p2", card.Name);
                        cmd.Parameters.AddWithValue("@p3", card.Damage);
                        cmd.Parameters.AddWithValue("@p4", false);
                        cmd.Parameters.AddWithValue("@p5", false);
                        cmd.Parameters.AddWithValue("@p6", DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }
                    carddeckIds[i] = card.Id;
                    i++;
                    
                }

                var packageCmdText = "INSERT INTO carddeck (card1, card2, card3, card4, card5) VALUES (@p1, @p2, @p3, @p4, @p5)";
                using (var cmd = new NpgsqlCommand(packageCmdText, conn, transaction)) {
                    for (int j = 0; j < carddeckIds.Length; j++) {
                        cmd.Parameters.AddWithValue($"@p{j + 1}", carddeckIds[j]);
                    }
                    cmd.ExecuteNonQuery();
                }

                transaction.Commit();
                e.Reply(200, "Package created successfully.");
            }
            catch (NpgsqlException ex) {
                if (ex.SqlState == "23505") {
                    e.Reply(400, "Error: Card UUID already exists.");
                }
                else {
                    e.Reply(400, $"Error occurred while creating package: {ex.Message}");
                }
            }
            catch (Exception ex) {
                e.Reply(400, $"Error occurred while creating package: {ex.Message}");
            }
        }

        public Card GetCardStats(Card card) {
            try {
                string[] parts = Regex.Split(card.Name, "(?=[A-Z])");

                if (parts.Length > 2) {
                    card.Element = Enum.Parse<ElementCard>(parts[1], ignoreCase: true);
                    card.Species = Enum.Parse<SpeciesCard>(parts[2], ignoreCase: true);
                }
                else {
                    card.Species = Enum.Parse<SpeciesCard>(parts[1], ignoreCase: true);
                    card.Element = card.Species != SpeciesCard.Spell ? ElementCard.Regular : 0;
                }
                return card;
            }
            catch (Exception ex) {
                throw new Exception("Error parsing card stats: " + ex.Message);
            }
        }

    }
}
