using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public bool PlayerOneWon { get; set; }

        // Constructors

        public Battle(Token playerOne, Token playerTwo) {
            PlayerOne = playerOne;
            PlayerTwo = playerTwo;
        }

        //Start Battle for 2 players with 100 rounds
        public string StartBattle() {
            try {
                Deck carddeck = new Deck();
                DeckOne = carddeck.PrepareDeck(PlayerOne);
                DeckTwo = carddeck.PrepareDeck(PlayerTwo);
                BattleLog += PlayerOne.LoggedInUser + " is fighting " + PlayerTwo.LoggedInUser + ":\n";

                for (int rounds = 1; rounds <= 100; rounds++) {
                    if (DeckOne.Count() == 0) {
                        BattleLog += "\n" + PlayerTwo.LoggedInUser + " wins the battle! Congratulations!\n";
                        UpdateStats(true, PlayerTwo);
                        UpdateStats(false, PlayerOne);
                        break;
                    }
                    if (DeckTwo.Count() == 0) {
                        BattleLog += "\n" + PlayerOne.LoggedInUser + " wins the battle! Congratulations!\n";
                        UpdateStats(false, PlayerTwo);
                        UpdateStats(true, PlayerOne);
                        break;
                    }
                    if (IsSuddenDeathMatch) {
                        rounds += 9;
                    }
                    //randomize an index for both decks
                    Random rnd = new Random();
                    int indexOne = rnd.Next(DeckOne.Count());
                    int indexTwo = rnd.Next(DeckTwo.Count());

                    float[] calcDamage = CalculateDamage(DeckOne[indexOne], DeckTwo[indexTwo]);

                    BattleLog += "\nRound [" + (IsSuddenDeathMatch ? rounds / 10 : rounds) + "]\n" + PlayerOne.LoggedInUser + ": " + DeckOne[indexOne].Name + " (" + DeckOne[indexOne].Damage + ") vs " +
                        PlayerTwo.LoggedInUser + ": " + DeckTwo[indexTwo].Name + " (" + DeckTwo[indexTwo].Damage + ") => ";

                    //special case
                    string specialText = SpecialInteraction(DeckOne[indexOne], DeckTwo[indexTwo]);
                    if (specialText != "") {
                        BattleLog += specialText;
                    }
                    else {
                        BattleLog += DeckOne[indexOne].Damage + " vs " + DeckTwo[indexTwo].Damage + " -> " +
                            calcDamage[0] + " vs " + calcDamage[1] + " => \n";

                        if (calcDamage[0] == calcDamage[1]) {
                            BattleLog += "Both cards tie.\n";
                            if (rounds == 100) {
                                if (!IsSuddenDeathMatch) {
                                    IsSuddenDeathMatch = true;
                                    rounds = 1;
                                    BattleLog += "INITIATING SUDDEN DEATHMATCH! (Cards get now removed - not taken over and a winning card gets a boost do dmg)";
                                    continue;
                                }
                                else {
                                    break;
                                }
                            }
                            continue;
                        }
                        else if (calcDamage[0] > calcDamage[1]) {
                            PlayerOneWins = true;
                            BattleLog += DeckOne[indexOne].Name + " wins\n";
                        }
                        else {
                            PlayerOneWins = false;
                            BattleLog += DeckTwo[indexTwo].Name + " wins\n";
                        }
                    }
                    if (!IsSuddenDeathMatch) {
                        ChangeCardAfterRound(indexOne, indexTwo);
                    }
                    else {
                        RemoveCardAfterRoundAndBoostDmg(indexOne, indexTwo);
                    }

                    if (rounds >= 100) {
                        BattleLog += "\nBattle ended in a tie!\n";
                        if (!IsSuddenDeathMatch) {
                            IsSuddenDeathMatch = true;
                            rounds = 0;
                            BattleLog += "\nINITIATING SUDDEN DEATHMATCH! (Cards now get a damage boost if they win a round and losing cards get removed)\n";
                        }
                    }
                }

                return BattleLog;
            }
            catch {
                throw;
            }
        }

    }
}
