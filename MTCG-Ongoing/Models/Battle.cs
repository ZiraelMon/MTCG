using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using MTCG.Server;
using Npgsql;
using static MTCG.Models.Card;

namespace MTCG.Models {
    public class Battle {
        private Token PlayerOne { get; set; }
        private Token PlayerTwo { get; set; }
        public List<Card> PlayerOneDeck { get; set; } = new List<Card>();
        public List<Card> PlayerTwoDeck { get; set; } = new List<Card>();
        private string BattleStatistics { get; set; }
        public bool PlayerOneWon { get; set; } = false;

        // Constructors

        public Battle(Token playerOne, Token playerTwo) {
            PlayerOne = playerOne;
            PlayerTwo = playerTwo;
            BattleStatistics = "";
        }

        //Start Battle for 2 players with 100 rounds
        public string StartBattle() {
            try {
                Deck carddeck = new Deck();
                PlayerOneDeck = carddeck.PrepareDeck(PlayerOne);
                PlayerTwoDeck = carddeck.PrepareDeck(PlayerTwo);
                BattleStatistics += PlayerOne.LoggedInUser + " || vs || " + PlayerTwo.LoggedInUser + ":\n";

                //randomize an index for both decks
                Random rnd = new Random();
                // Random round for elemental surge (unique feature)
                int elementalSurgeRound = rnd.Next(1, 101);
                // Random element for surge
                ElementCard surgeElement = (ElementCard)rnd.Next(0, Enum.GetNames(typeof(ElementCard)).Length); 

                for (int rounds = 1; rounds <= 100; rounds++) {
                    if (PlayerOneDeck.Count() == 0) {
                        BattleStatistics += "\n" + PlayerTwo.LoggedInUser + " has won! Congratulations!\n";
                        UpdateBattleStatistics(true, PlayerTwo);
                        UpdateBattleStatistics(false, PlayerOne);
                        break;
                    }
                    if (PlayerTwoDeck.Count() == 0) {
                        BattleStatistics += "\n" + PlayerOne.LoggedInUser + " wins the battle! Congratulations!\n";
                        UpdateBattleStatistics(false, PlayerTwo);
                        UpdateBattleStatistics(true, PlayerOne);
                        break;
                    }

                    Random rnd2 = new Random();
                    int indexOne = rnd2.Next(PlayerOneDeck.Count() - 1);
                    int indexTwo = rnd2.Next(PlayerTwoDeck.Count() - 1);

                    double[] totalDamage = TotalDamage(PlayerOneDeck[indexOne], PlayerTwoDeck[indexTwo]);

                    //Unique Feature: Elemental Surge
                    //On certain rounds, the battlefield experiences an elemental surge that temporarily boosts the damage or effects (by 20%) of cards matching that element

                    if (rounds == elementalSurgeRound) {
                        BattleStatistics += $"Elemental Surge! {surgeElement} cards get a damage boost this round.\n";

                        // Apply surge effect
                        if (PlayerOneDeck[indexOne].Element == surgeElement) {
                            totalDamage[0] *= 1.2;
                        }
                        if (PlayerTwoDeck[indexTwo].Element == surgeElement) {
                            totalDamage[1] *= 1.2;
                        }
                    }

                    BattleStatistics += "\nRound [" + rounds + "]\n" + PlayerOne.LoggedInUser + ": " + PlayerOneDeck[indexOne].Name + " (" + PlayerOneDeck[indexOne].Damage + ") vs " +
                    PlayerTwo.LoggedInUser + ": " + PlayerTwoDeck[indexTwo].Name + " (" + PlayerTwoDeck[indexTwo].Damage + ") => ";

                    string cardInteraction = CardInteraction(PlayerOneDeck[indexOne], PlayerTwoDeck[indexTwo]);
                    if (cardInteraction != "") {
                        BattleStatistics += cardInteraction;
                    }
                    else {
                        BattleStatistics += PlayerOneDeck[indexOne].Damage + " vs " + PlayerTwoDeck[indexTwo].Damage + " -> " +
                            totalDamage[0] + " vs " + totalDamage[1] + " => \n";

                        if (totalDamage[0] == totalDamage[1]) {
                            BattleStatistics += "Both cards tie.\n";

                        }
                        else if (totalDamage[0] > totalDamage[1]) {
                            PlayerOneWon = true;
                            BattleStatistics += PlayerOneDeck[indexOne].Name + " wins\n";
                        }
                        else {
                            PlayerOneWon = false;
                            BattleStatistics += PlayerTwoDeck[indexTwo].Name + " wins\n";
                        }
                    }
                    ChangeCard(indexOne, indexTwo);

                    RemoveCard(indexOne, indexTwo);

                    if (rounds >= 100) {
                        BattleStatistics += "\nBattle ended in a tie!\n";
                    }
                }

                return BattleStatistics;
            }
            catch {
                throw;
            }
        }

        public string CardInteraction(Card PlayerOneCard, Card PlayerTwoCard) {
            try {
                string interactionText = "";
                if (PlayerOneCard.Species == SpeciesCard.Goblin && PlayerTwoCard.Species == SpeciesCard.Dragon) {
                    PlayerOneWon = false;
                    interactionText = "Dragon defeats Goblin\n";
                    return interactionText;
                }
                else if (PlayerOneCard.Species == SpeciesCard.Dragon && PlayerTwoCard.Species == SpeciesCard.Goblin) {
                    PlayerOneWon = true;
                    interactionText = "Dragon defeats Goblin\n";
                    return interactionText;
                }
                else if (PlayerOneCard.Species == SpeciesCard.Wizard && PlayerTwoCard.Species == SpeciesCard.Ork) {
                    PlayerOneWon = true;
                    interactionText = "Wizzard defeats Ork\n";
                    return interactionText;
                }
                else if (PlayerOneCard.Species == SpeciesCard.Ork && PlayerTwoCard.Species == SpeciesCard.Wizard) {
                    PlayerOneWon = false;
                    interactionText = "Wizzard defeats Ork\n";
                    return interactionText;
                }
                else if (PlayerOneCard.Species == SpeciesCard.Knight && (PlayerTwoCard.Species == SpeciesCard.Spell && PlayerTwoCard.Element == ElementCard.Water)) {
                    PlayerOneWon = false;
                    interactionText = "WaterSpell drowns Knight\n";
                    return interactionText;
                }
                else if ((PlayerOneCard.Species == SpeciesCard.Spell && PlayerOneCard.Element == ElementCard.Water) && PlayerTwoCard.Species == SpeciesCard.Knight) {
                    PlayerOneWon = true;
                    interactionText = "WaterSpell drowns Knight\n";
                    return interactionText;
                }
                else if (PlayerOneCard.Species == SpeciesCard.Kraken && PlayerTwoCard.Species == SpeciesCard.Spell) {
                    PlayerOneWon = true;
                    interactionText = "Kraken defeats Spell\n";
                    return interactionText;
                }
                else if (PlayerOneCard.Species == SpeciesCard.Spell && PlayerTwoCard.Species == SpeciesCard.Kraken) {
                    PlayerOneWon = false;
                    interactionText = "Kraken defeats Spell\n";
                    return interactionText;
                }

                return interactionText;
            }
            catch {
                throw;
            }
        }

        public void ChangeCard(int indexOne, int indexTwo) {
            try {
                if (PlayerOneWon && PlayerTwoDeck.Count > 0) {
                    PlayerOneDeck.Add(PlayerTwoDeck[indexTwo]);
                    PlayerTwoDeck.RemoveAt(indexTwo);
                }
                else if (!PlayerOneWon && PlayerOneDeck.Count > 0) {
                    PlayerTwoDeck.Add(PlayerOneDeck[indexOne]);
                    PlayerOneDeck.RemoveAt(indexOne);
                }
            }
            catch {
                throw;
            }
        }

        public void RemoveCard(int indexOne, int indexTwo) {
            try {
                if (PlayerOneWon && PlayerTwoDeck.Count > 0) {
                    PlayerTwoDeck.RemoveAt(indexTwo);
                    PlayerOneDeck[indexOne].Damage *= 1.2;
                }
                else if (!PlayerOneWon && PlayerOneDeck.Count > 0) {
                    PlayerOneDeck.RemoveAt(indexOne);
                    PlayerTwoDeck[indexTwo].Damage *= 1.2;
                }
            }
            catch {
                throw;
            }
        }

        public double[] TotalDamage(Card PlayerOneCard, Card PlayerTwoCard) {
            try {
                double[] totalDamage = new double[2];
                PlayerOneCard = PlayerOneCard.GetCardStats(PlayerOneCard);
                PlayerTwoCard = PlayerTwoCard.GetCardStats(PlayerTwoCard);

                if ((PlayerOneCard.Species != SpeciesCard.Spell) && (PlayerTwoCard.Species != SpeciesCard.Spell)) {
                    totalDamage[0] = PlayerOneCard.Damage;
                    totalDamage[1] = PlayerTwoCard.Damage;
                }
                else {
                    if (PlayerOneCard.Element == ElementCard.Regular && PlayerTwoCard.Element == ElementCard.Water) {
                        totalDamage[0] = PlayerOneCard.Damage * 2;
                        totalDamage[1] = PlayerTwoCard.Damage / 2;
                    }
                    else if (PlayerOneCard.Element == ElementCard.Water && PlayerTwoCard.Element == ElementCard.Fire) {
                        totalDamage[0] = PlayerOneCard.Damage * 2;
                        totalDamage[1] = PlayerTwoCard.Damage / 2;
                    }
                    else if (PlayerOneCard.Element == ElementCard.Fire && PlayerTwoCard.Element == ElementCard.Regular) {
                        totalDamage[0] = PlayerOneCard.Damage * 2;
                        totalDamage[1] = PlayerTwoCard.Damage / 2;
                    }
                    else if (PlayerOneCard.Element == ElementCard.Water && PlayerTwoCard.Element == ElementCard.Regular) {
                        totalDamage[0] = PlayerOneCard.Damage / 2;
                        totalDamage[1] = PlayerTwoCard.Damage * 2;
                    }
                    else if (PlayerOneCard.Element == ElementCard.Fire && PlayerTwoCard.Element == ElementCard.Water) {
                        totalDamage[0] = PlayerOneCard.Damage / 2;
                        totalDamage[1] = PlayerTwoCard.Damage * 2;
                    }
                    else if (PlayerOneCard.Element == ElementCard.Regular && PlayerTwoCard.Element == ElementCard.Fire) {
                        totalDamage[0] = PlayerOneCard.Damage / 2;
                        totalDamage[1] = PlayerTwoCard.Damage * 2;
                    }
                    else {
                        totalDamage[0] = PlayerOneCard.Damage;
                        totalDamage[1] = PlayerTwoCard.Damage;
                    }
                }
                return totalDamage;
            }
            catch {
                throw;
            }
        }

        public void UpdateBattleStatistics(bool isWinner, Token userToken) {
            try {
                string connectionString = ConfigurationManager.GetConnectionString("DefaultConnection");
                using var conn = new NpgsqlConnection(connectionString);
                conn.Open();

                if (isWinner) {
                    using (var cmd = new NpgsqlCommand("UPDATE stats SET wins = wins + 1, elo = elo + 3 WHERE username = (@p1)", conn)) {
                        cmd.Parameters.AddWithValue("@p1", userToken.LoggedInUser ?? "");
                        cmd.ExecuteNonQuery();
                    }
                }
                else {
                    using (var cmd = new NpgsqlCommand("UPDATE stats SET losses = losses + 1, elo = elo - 3 WHERE username = (@p1)", conn)) {
                        cmd.Parameters.AddWithValue("@p1", userToken.LoggedInUser ?? "");
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch {
                throw;
            }
        }

    }
}
