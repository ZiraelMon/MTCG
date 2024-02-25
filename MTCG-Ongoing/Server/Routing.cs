using MTCG.Models;
using MTCG.Server;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;

namespace MTCG.Controller {

    public class Routing {
        public static void _Svr_Incoming(object evt) {
            HttpSvrEventArgs e = (HttpSvrEventArgs)evt;

            Console.WriteLine("Incoming request: " + e.Method + " " + e.Path);

            try {
                //user token to pass username and check logged in status
                Token userToken = new Token();
                userToken.AuthenticateUser(e);
                User user = new User();

                if (e.Path == "/users" && e.Method == "POST") {
                    user.CreateUser(e);
                }
                else if (e.Path == "/sessions" && e.Method == "POST") {
                    user.LoginUser(e);
                }
                else if (userToken.IsLoggedIn) {
                    switch (e.Path) {
                        case string s when s.StartsWith("/users/"):
                            if (e.Method == "GET") {
                                user.GetUserData(e, userToken);
                            }
                            else if (e.Method == "PUT") {
                                user.UpdateUserData(e, userToken);
                            }
                            break;
                        case "/sessions":
                            if (e.Method == "POST") {
                                user.LoginUser(e);
                            }
                            break;
                        case "/packages":
                            if (e.Method == "POST") {
                                if (userToken.IsAdmin) {
                                    Card card = new Card();
                                    card.CreateCards(e);
                                }
                                else {
                                    e.Reply(400, "Only the admin can create packages.");
                                }
                            }
                            break;
                        case "/transactions/packages":
                            if (e.Method == "POST") {
                                user.GetCarddeck(e, userToken);
                            }
                            break;
                        case "/cards":
                            if (e.Method == "GET") {
                                Deck cardCollections = new Deck();
                                cardCollections.GetCards(e, userToken);
                            }
                            break;
                        case string s when s.StartsWith("/deck"):
                            Deck cardCollection = new Deck();
                            if (e.Method == "GET") {
                                cardCollection.PrintDeck(e, userToken);
                            }
                            else if (e.Method == "PUT") {
                                cardCollection.UpdateDeck(e, userToken);
                            }
                            break;
                        case "/stats":
                            if (e.Method == "GET") {
                                user.GetStats(e, userToken);
                            }
                            break;
                        case "/scoreboard":
                            if (e.Method == "GET") {
                                user.GetScoreboard(e);
                            }
                            break;
                        case "/battles":
                            if (e.Method == "POST") {
                                Game.Join(e, userToken);
                            }
                            break;
                        case "/tradings":
                            Trade newTrade = new Trade();
                            if (e.Method == "GET") {
                                newTrade.CreateTrade(e, userToken);
                            }
                            else if (e.Method == "POST") {
                                newTrade.PostTrade(e, userToken);
                            }
                            break;
                        case string s when s.StartsWith("/tradings/"):
                            Trade tryTrade = new Trade();
                            if (e.Method == "POST") {
                                tryTrade.TradeCards(e, userToken);
                            }
                            else if (e.Method == "DELETE") {
                                tryTrade.DeleteTrade(e, userToken);
                            }
                            break;
                        default:
                            Console.WriteLine("Rejected request.");
                            e.Reply(400);
                            break;
                    }
                }
                else {
                    e.Reply(400, "Missing or invalid token.");
                }
            }
            catch {
                e.Reply(400, "Request unsuccessful.");
            }
        }
    }
}
